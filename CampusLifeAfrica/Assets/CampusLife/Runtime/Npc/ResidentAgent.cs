using CampusLife.Core.Npcs;
using CampusLife.Runtime.Characters;
using UnityEngine;
using UnityEngine.AI;

namespace CampusLife.Runtime.Npc
{
    public enum DetailLevel { Unloaded, Low, Medium, High }

    /// <summary>
    /// The 3D body and moment-to-moment behaviour of an autonomous resident.
    /// The abstract <see cref="CampusSimulation"/> decides where they should be
    /// (schedule planning and goal selection); this component handles
    /// navigation, slot behaviour (seats, groups, queues, routes), social
    /// stops, reactions and conversations with the player. Animation is
    /// delegated to <see cref="CharacterAnimator"/>.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ResidentAgent : MonoBehaviour
    {
        enum Phase { Idle, ToAccess, Exiting, ToSlot, AtSlot, Seated, Queueing, OnRoute, Done }

        const float RunSpeed = 3.3f;
        const float StuckSeconds = 8f;

        public NpcProfile Profile { get; private set; }
        public NpcCharacter Talk { get; private set; }
        public CharacterAnimator Animator { get; private set; }
        public string AreaId { get; private set; }
        public DetailLevel Detail { get; private set; } = DetailLevel.Unloaded;
        /// <summary>Set when the body walked through a door into a hidden place or out of this area.</summary>
        public bool WantsDespawn { get; private set; }
        public bool IsTravelling => _phase == Phase.ToAccess || _phase == Phase.Exiting;
        public bool IsBusy => _pauseUntil > Time.time || Talk.IsTalking;
        public float NextChatTime { get; set; }
        public float NextReactTime { get; set; }

        ResidentPopulation _population;
        NavMeshAgent _agent;
        SpeechBubble _bubble;
        Phase _phase;
        double _stateStamp = double.NaN;
        Slot _slot;
        int _routeIndex;
        float _timer;
        float _pauseUntil;
        Vector3 _goal;
        Vector3 _lastProgressPos;
        float _lastProgressTime;
        bool _instantArrival;
        GameObject _phone, _book, _food, _broom;

        public void Initialise(NpcProfile profile, ResidentPopulation population, NpcCharacter talk, CharacterAnimator animator)
        {
            Profile = profile;
            _population = population;
            Talk = talk;
            Animator = animator;
            _agent = GetComponent<NavMeshAgent>();
            _agent.enabled = false;
            _agent.radius = 0.3f;
            _agent.height = 1.8f;
            _agent.baseOffset = 0f;
            _agent.angularSpeed = 540f;
            _agent.acceleration = 8f;
            _agent.stoppingDistance = 0.2f;
            _agent.autoBraking = true;
            _agent.avoidancePriority = 30 + (int)(DeterministicRandom.Hash(profile.Id) % 40);
            _bubble = SpeechBubble.Create(transform);
            talk.ConversationStarted += OnConversationStarted;
            talk.ConversationEnded += OnConversationEnded;
        }

        // ---------------------------------------------------------- lifecycle

        /// <summary>Activates the body at a point in an area and starts following the given state.</summary>
        public void Spawn(NpcSimState state, string areaId, Vector3 position, float yaw, bool instantArrival)
        {
            AreaId = areaId;
            WantsDespawn = false;
            _phase = Phase.Idle;
            _slot = null;
            _stateStamp = double.NaN;
            _instantArrival = instantArrival;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            gameObject.SetActive(true);
            EnableAgentAt(position);
            Animator.SetPose(CharacterPose.Stand);
            Animator.SetHands(HandPose.None);
            ShowProp(HandPose.None);
            Sync(state);
        }

        public void Despawn()
        {
            LeaveSlot();
            if (_agent.enabled) _agent.enabled = false;
            Detail = DetailLevel.Unloaded;
            gameObject.SetActive(false);
        }

        public void SetDetail(DetailLevel detail)
        {
            Detail = detail;
            _agent.obstacleAvoidanceType = detail == DetailLevel.High ? ObstacleAvoidanceType.HighQualityObstacleAvoidance : ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            Animator.UpdateInterval = detail == DetailLevel.High ? 1 : 2;
        }

        // ------------------------------------------------------------ planning

        /// <summary>Re-plans when the abstract state changed (new destination or activity).</summary>
        public void Sync(NpcSimState state)
        {
            if (state.ChangedMinute.Equals(_stateStamp) && _phase != Phase.Idle) return;
            _stateStamp = state.ChangedMinute;
            Plan(state);
        }

        void Plan(NpcSimState state)
        {
            var place = _population.Sim.Places.Get(state.PlaceId);
            bool belongsHere = state.Mode == NpcMode.AtPlace && !place.Hidden && place.AreaId == AreaId;
            if (belongsHere)
            {
                if (_slot != null && _slot.PlaceId == state.PlaceId && _phase != Phase.Idle && _phase != Phase.ToAccess)
                {
                    ApplyActivityPose(state.Activity);
                    return;
                }
                LeaveSlot();
                BeginActivity(state);
                return;
            }

            LeaveSlot();
            if (AreaId != PlaceIds.CampusArea)
            {
                // Inside an interior but needed elsewhere: walk out of the door.
                if (_population.Slots.TryGetAreaExit(AreaId, out var exit)) GoTo(exit, Phase.Exiting, state.Activity);
                else WantsDespawn = true;
                return;
            }
            if (_population.Slots.TryGetAccess(state.PlaceId, out var access)) GoTo(access, Phase.ToAccess, state.Activity);
            else WantsDespawn = true;
        }

        void BeginActivity(NpcSimState state)
        {
            var slots = _population.Slots;
            string place = state.PlaceId;
            int seed = (int)(DeterministicRandom.Hash(Profile.Id) % 997);

            if (state.Activity == ActivityKind.Eat && slots.HasQueue(place) && slots.JoinQueue(place, Profile.Id))
            {
                _phase = Phase.Queueing;
                _timer = 0f;
                ApplyActivityPose(ActivityKind.Relax);
                return;
            }

            _slot = slots.Reserve(place, Profile.Id, Profile.FriendGroup, seed, PreferredSlots(state.Activity));
            if (_slot == null)
            {
                // Full: mill about near the place.
                var p = slots.Center(place) + Random.insideUnitSphere * 3f;
                p.y = transform.position.y;
                GoTo(p, Phase.ToSlot, state.Activity);
                return;
            }
            if (_slot.Kind == SlotKind.Route)
            {
                _routeIndex = (seed + _slot.Route.Length / 2) % _slot.Route.Length;
                _agent.speed = state.Activity == ActivityKind.Exercise ? RunSpeed : Profile.WalkSpeed * (state.Activity == ActivityKind.Clean ? 0.8f : 1f);
                _phase = Phase.OnRoute;
                if (_instantArrival) WarpTo(_slot.Route[_routeIndex]);
                SetDestination(_slot.Route[_routeIndex]);
                ApplyActivityPose(state.Activity);
                _instantArrival = false;
                return;
            }
            var target = _slot.Kind == SlotKind.Seat ? _slot.Seat.StandPose.position : _slot.Position;
            if (_instantArrival)
            {
                WarpTo(target);
                _instantArrival = false;
                ArriveAtSlot(state.Activity);
                return;
            }
            GoTo(target, Phase.ToSlot, state.Activity);
        }

        static SlotKind[] PreferredSlots(ActivityKind activity)
        {
            switch (activity)
            {
                case ActivityKind.Study: return new[] { SlotKind.Seat, SlotKind.Stand };
                case ActivityKind.Relax: return new[] { SlotKind.GroundSeat, SlotKind.Seat, SlotKind.Stand };
                case ActivityKind.WaitForShuttle: return new[] { SlotKind.Seat, SlotKind.Stand };
                case ActivityKind.Exercise:
                case ActivityKind.Patrol:
                case ActivityKind.Clean: return new[] { SlotKind.Route, SlotKind.Stand };
                case ActivityKind.Socialize: return new[] { SlotKind.Stand, SlotKind.Seat, SlotKind.GroundSeat };
                default: return new[] { SlotKind.Stand, SlotKind.Seat };
            }
        }

        void GoTo(Vector3 target, Phase phase, ActivityKind activity)
        {
            _phase = phase;
            _agent.speed = Profile.WalkSpeed;
            Animator.SetPose(CharacterPose.Stand);
            // Students often check their phones while walking to class.
            Animator.SetHands(activity == ActivityKind.Socialize && DeterministicRandom.Hash(Profile.Id) % 3 == 0 ? HandPose.Phone : HandPose.None);
            ShowProp(Animator.Hands);
            SetDestination(target);
        }

        // -------------------------------------------------------------- update

        void Update()
        {
            Animator.SetSpeed(_agent.enabled ? new Vector3(_agent.velocity.x, 0f, _agent.velocity.z).magnitude : 0f);
            ClearLookIfExpired();
            if (!_agent.enabled) return; // seated or sitting on the grass
            if (Talk.IsTalking || Time.time < _pauseUntil)
            {
                if (_agent.enabled) _agent.isStopped = true;
                return;
            }
            if (_agent.enabled && _agent.isStopped) _agent.isStopped = false;

            switch (_phase)
            {
                case Phase.ToAccess:
                    if (Reached())
                    {
                        _population.OnReachedAccess(this);
                        var state = _population.Sim.Get(Profile.Id);
                        var place = _population.Sim.Places.Get(state.PlaceId);
                        if (state.Mode == NpcMode.AtPlace && !place.Hidden && place.AreaId == AreaId)
                        {
                            _stateStamp = state.ChangedMinute;
                            BeginActivity(state);
                        }
                        else if (state.Mode == NpcMode.AtPlace)
                        {
                            WantsDespawn = true;
                            _phase = Phase.Done;
                        }
                        else _phase = Phase.Idle;
                    }
                    else CheckStuck();
                    break;
                case Phase.Exiting:
                    if (Reached()) { WantsDespawn = true; _phase = Phase.Done; }
                    else CheckStuck();
                    break;
                case Phase.ToSlot:
                    if (Reached()) ArriveAtSlot(_population.Sim.Get(Profile.Id).Activity);
                    else CheckStuck();
                    break;
                case Phase.Queueing:
                    UpdateQueue();
                    break;
                case Phase.OnRoute:
                    UpdateRoute();
                    break;
                case Phase.AtSlot:
                    UpdateStanding();
                    break;
            }
        }

        void ArriveAtSlot(ActivityKind activity)
        {
            if (_slot == null)
            {
                _phase = Phase.AtSlot;
                ApplyActivityPose(activity);
                return;
            }
            if (_slot.Kind == SlotKind.Seat && !_slot.Seat.Occupied)
            {
                _agent.enabled = false;
                var sit = _slot.Seat.SitPose;
                transform.SetPositionAndRotation(sit.position, Quaternion.Euler(0f, sit.eulerAngles.y, 0f));
                _slot.Seat.Occupied = true;
                _phase = Phase.Seated;
                Animator.SetPose(CharacterPose.Sit, _slot.Seat.SeatHeight);
                ApplyHands(activity);
                return;
            }
            transform.rotation = Quaternion.Euler(0f, _slot.Yaw, 0f);
            _phase = Phase.AtSlot;
            _timer = Random.Range(4f, 10f);
            ApplyActivityPose(activity);
        }

        void ApplyActivityPose(ActivityKind activity)
        {
            if (_phase == Phase.Seated) { ApplyHands(activity); return; }
            bool ground = _slot != null && _slot.Kind == SlotKind.GroundSeat && _phase == Phase.AtSlot;
            Animator.SetPose(ground ? CharacterPose.SitGround : activity == ActivityKind.Socialize && _phase == Phase.AtSlot ? CharacterPose.Talk : CharacterPose.Stand);
            if (ground && _agent.enabled) _agent.enabled = false;
            ApplyHands(activity);
        }

        void ApplyHands(ActivityKind activity)
        {
            HandPose hands;
            switch (activity)
            {
                case ActivityKind.Study: hands = HandPose.Read; break;
                case ActivityKind.Eat: hands = _phase == Phase.Queueing ? HandPose.None : HandPose.Eat; break;
                case ActivityKind.Clean: hands = HandPose.Sweep; break;
                case ActivityKind.WaitForShuttle:
                case ActivityKind.Relax: hands = HandPose.Phone; break;
                default: hands = HandPose.None; break;
            }
            Animator.SetHands(hands);
            ShowProp(hands);
        }

        void UpdateStanding()
        {
            // Socialising alternates between chatting and checking phones.
            var state = _population.Sim.Get(Profile.Id);
            if (state.Activity != ActivityKind.Socialize) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = Random.Range(5f, 12f);
            bool phone = Random.value < 0.3f;
            Animator.SetPose(phone ? CharacterPose.Stand : CharacterPose.Talk);
            Animator.SetHands(phone ? HandPose.Phone : HandPose.None);
            ShowProp(Animator.Hands);
        }

        void UpdateQueue()
        {
            var slots = _population.Slots;
            var state = _population.Sim.Get(Profile.Id);
            int position = slots.QueuePosition(state.PlaceId, Profile.Id, out var point, out var yaw);
            if (position < 0) { LeaveSlot(); BeginActivity(state); return; }
            if ((_goal - point).sqrMagnitude > 0.01f) SetDestination(point);
            if (!Reached()) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, yaw, 0f), Time.deltaTime * 6f);
            if (position != 0) return;
            _timer += Time.deltaTime;
            if (_timer < 6f) return;
            // Served: leave the queue and eat nearby.
            slots.LeaveQueue(state.PlaceId, Profile.Id);
            _slot = slots.Reserve(state.PlaceId, Profile.Id, -1, (int)(DeterministicRandom.Hash(Profile.Id) % 97), SlotKind.Stand, SlotKind.Seat);
            if (_slot != null) GoTo(_slot.Kind == SlotKind.Seat ? _slot.Seat.StandPose.position : _slot.Position, Phase.ToSlot, ActivityKind.Eat);
            else { _phase = Phase.AtSlot; ApplyActivityPose(ActivityKind.Eat); }
            ShowProp(HandPose.Eat);
        }

        void UpdateRoute()
        {
            if (!Reached()) { CheckStuck(); return; }
            var activity = _population.Sim.Get(Profile.Id).Activity;
            if (activity == ActivityKind.Clean || (activity == ActivityKind.Patrol && _routeIndex % 3 == 0))
                Pause(activity == ActivityKind.Clean ? Random.Range(3f, 6f) : 2.5f);
            _routeIndex = (_routeIndex + 1) % _slot.Route.Length;
            SetDestination(_slot.Route[_routeIndex]);
        }

        // -------------------------------------------------------------- social

        /// <summary>Two friends meeting on a walkway stop for a quick chat.</summary>
        public void ChatWith(ResidentAgent other, float seconds)
        {
            Pause(seconds);
            FaceTowards(other.transform.position);
            Animator.SetPose(CharacterPose.Talk);
            NextChatTime = Time.time + 90f;
            _resumePoseAt = Time.time + seconds;
        }

        float _resumePoseAt = -1f;

        void LateUpdate()
        {
            if (_resumePoseAt > 0f && Time.time >= _resumePoseAt)
            {
                _resumePoseAt = -1f;
                if (_phase != Phase.Seated && _phase != Phase.AtSlot) Animator.SetPose(CharacterPose.Stand);
            }
        }

        /// <summary>Reacts to a disturbance (someone running into them): stop, look, comment.</summary>
        public void React(Vector3 source, string line)
        {
            NextReactTime = Time.time + 10f;
            if (_phase != Phase.Seated && _phase != Phase.Queueing) Pause(1.5f);
            Animator.LookAt(source + Vector3.up * 1.5f);
            _lookClearAt = Time.time + 2f;
            if (!string.IsNullOrEmpty(line)) Say(line);
        }

        float _lookClearAt = -1f;

        public void Say(string line) => _bubble.Say(line);

        void ClearLookIfExpired()
        {
            if (_lookClearAt > 0f && Time.time > _lookClearAt && !Talk.IsTalking)
            {
                _lookClearAt = -1f;
                Animator.LookAt(null);
            }
        }

        void Pause(float seconds) => _pauseUntil = Mathf.Max(_pauseUntil, Time.time + seconds);

        void OnConversationStarted(NpcCharacter npc, Transform player)
        {
            if (_phase != Phase.Seated && _phase != Phase.Queueing) FaceTowards(player.position);
            if (_phase != Phase.Seated && !(_slot != null && _slot.Kind == SlotKind.GroundSeat)) Animator.SetPose(CharacterPose.Talk);
            Animator.SetHands(HandPose.None);
            ShowProp(HandPose.None);
        }

        void OnConversationEnded(NpcCharacter npc)
        {
            Animator.LookAt(null);
            var state = _population.Sim.Get(Profile.Id);
            if (state != null) ApplyActivityPose(state.Activity);
            if (_phase == Phase.AtSlot && _slot != null) transform.rotation = Quaternion.Euler(0f, _slot.Yaw, 0f);
        }

        void FaceTowards(Vector3 p)
        {
            Vector3 d = p - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d);
        }

        // ---------------------------------------------------------- navigation

        void EnableAgentAt(Vector3 position)
        {
            if (NavMesh.SamplePosition(position, out var hit, 4f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                _agent.enabled = true;
                _agent.Warp(hit.position);
            }
        }

        void WarpTo(Vector3 position)
        {
            if (!_agent.enabled) EnableAgentAt(position);
            else if (NavMesh.SamplePosition(position, out var hit, 4f, NavMesh.AllAreas)) _agent.Warp(hit.position);
        }

        void SetDestination(Vector3 target)
        {
            _goal = target;
            _lastProgressPos = transform.position;
            _lastProgressTime = Time.time;
            if (!_agent.enabled) EnableAgentAt(transform.position);
            if (!_agent.enabled) return;
            if (NavMesh.SamplePosition(target, out var hit, 4f, NavMesh.AllAreas)) _agent.SetDestination(hit.position);
            else _agent.SetDestination(target);
        }

        bool Reached()
        {
            if (!_agent.enabled) return true;
            if (_agent.pathPending) return false;
            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid) return true;
            return _agent.remainingDistance <= _agent.stoppingDistance + 0.15f;
        }

        /// <summary>If the agent makes no progress for a while (blocked by a crowd), skip ahead.</summary>
        void CheckStuck()
        {
            if ((transform.position - _lastProgressPos).sqrMagnitude > 0.25f)
            {
                _lastProgressPos = transform.position;
                _lastProgressTime = Time.time;
                return;
            }
            float limit = Detail == DetailLevel.High ? StuckSeconds * 2.5f : StuckSeconds;
            if (Time.time - _lastProgressTime > limit)
            {
                WarpTo(_goal);
                _lastProgressTime = Time.time;
            }
        }

        void LeaveSlot()
        {
            if (_phase == Phase.Seated && _slot != null && _slot.Seat != null)
            {
                _slot.Seat.Occupied = false;
                var stand = _slot.Seat.StandPose;
                transform.SetPositionAndRotation(stand.position, Quaternion.Euler(0f, stand.eulerAngles.y, 0f));
                Animator.SetPose(CharacterPose.Stand);
            }
            if (_slot != null && _slot.Kind == SlotKind.GroundSeat) Animator.SetPose(CharacterPose.Stand);
            _population.Slots.Release(Profile.Id);
            _slot = null;
            if (!_agent.enabled && gameObject.activeInHierarchy) EnableAgentAt(transform.position);
            _phase = Phase.Idle;
        }

        // --------------------------------------------------------------- props

        void ShowProp(HandPose hands)
        {
            EnsureProps();
            _phone.SetActive(hands == HandPose.Phone);
            _book.SetActive(hands == HandPose.Read);
            _food.SetActive(hands == HandPose.Eat);
            _broom.SetActive(hands == HandPose.Sweep);
        }

        void EnsureProps()
        {
            if (_phone != null) return;
            var mats = _population.Materials;
            var hand = Animator.Visual.Props;
            _phone = CharacterRigBuilder.Part("Phone", PrimitiveType.Cube, hand, new Vector3(0f, 0.02f, 0.02f), new Vector3(0.07f, 0.13f, 0.015f), mats.Get(new Color(0.08f, 0.08f, 0.1f), 0.8f)).gameObject;
            _book = CharacterRigBuilder.Part("Book", PrimitiveType.Cube, hand, new Vector3(-0.08f, 0.02f, 0.05f), new Vector3(0.22f, 0.03f, 0.16f), mats.Get(new Color(0.15f, 0.35f, 0.6f))).gameObject;
            _food = CharacterRigBuilder.Part("MeatPie", PrimitiveType.Cube, hand, new Vector3(0f, 0.02f, 0.04f), new Vector3(0.1f, 0.04f, 0.08f), mats.Get(new Color(0.85f, 0.62f, 0.3f))).gameObject;
            _broom = CharacterRigBuilder.Part("Broom", PrimitiveType.Cylinder, hand, new Vector3(0f, -0.35f, 0.15f), new Vector3(0.03f, 0.6f, 0.03f), mats.Get(new Color(0.6f, 0.45f, 0.25f)), new Vector3(30f, 0f, 0f)).gameObject;
        }
    }
}

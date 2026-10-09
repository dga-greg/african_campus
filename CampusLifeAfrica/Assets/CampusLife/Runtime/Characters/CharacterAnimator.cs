using UnityEngine;

namespace CampusLife.Runtime.Characters
{
    public enum CharacterPose { Stand, Sit, Talk, Celebrate, SitGround }

    /// <summary>What the hands are doing, layered over any body pose (a seated student can read).</summary>
    public enum HandPose { None, Phone, Read, Eat, Sweep }

    /// <summary>
    /// Procedural animation for the modular rig: idle breathing, a walk/run
    /// cycle whose cadence is derived from actual ground speed (so feet do not
    /// visibly slide), sitting, talking gestures and head look-at. Poses blend
    /// smoothly. A Mecanim Animator Controller can replace this component once
    /// rigged character art exists; gameplay only talks to the public API.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimator : MonoBehaviour
    {
        // Distance covered by one full stride cycle (two steps).
        const float WalkStrideLength = 1.3f;
        const float RunStrideLength = 2.2f;

        public CharacterVisual Visual;
        public float RunSpeedThreshold = 3.2f;

        float _speed;
        float _phase;
        float _moveWeight;
        float _runWeight;
        float _sitWeight;
        float _talkWeight;
        float _celebrateWeight;
        float _phoneWeight, _readWeight, _eatWeight, _sweepWeight, _groundWeight;
        float _idleSeed;
        float _pendingDelta;
        int _frameCounter;
        CharacterPose _pose = CharacterPose.Stand;
        HandPose _hands = HandPose.None;
        Vector3? _lookTarget;
        float _seatHeight = 0.48f;

        public CharacterPose Pose => _pose;

        /// <summary>Animate every Nth frame (level of detail for distant characters). 1 = every frame.</summary>
        public int UpdateInterval { get; set; } = 1;

        void Awake()
        {
            if (Visual == null) Visual = GetComponentInChildren<CharacterVisual>();
            _idleSeed = Random.value * 10f;
        }

        /// <summary>Horizontal ground speed in metres per second.</summary>
        public void SetSpeed(float metresPerSecond) => _speed = Mathf.Max(0f, metresPerSecond);

        public void SetPose(CharacterPose pose, float seatHeight = 0.48f)
        {
            _pose = pose;
            _seatHeight = seatHeight;
        }

        public HandPose Hands => _hands;

        public void SetHands(HandPose hands) => _hands = hands;

        /// <summary>World point the head should turn toward, or null to look ahead.</summary>
        public void LookAt(Vector3? worldPoint) => _lookTarget = worldPoint;

        void LateUpdate()
        {
            if (Visual == null) return;
            _pendingDelta += Time.deltaTime;
            if (++_frameCounter < Mathf.Max(1, UpdateInterval)) return;
            _frameCounter = 0;
            float dt = Mathf.Min(_pendingDelta, 0.25f);
            _pendingDelta = 0f;

            float moving = Mathf.Clamp01(_speed / 1.2f);
            float running = Mathf.Clamp01((_speed - 2.2f) / (RunSpeedThreshold - 2.2f));
            _moveWeight = Mathf.MoveTowards(_moveWeight, _pose == CharacterPose.Sit ? 0f : moving, dt * 6f);
            _runWeight = Mathf.MoveTowards(_runWeight, running, dt * 4f);
            _sitWeight = Mathf.MoveTowards(_sitWeight, _pose == CharacterPose.Sit || _pose == CharacterPose.SitGround ? 1f : 0f, dt * 3f);
            _groundWeight = Mathf.MoveTowards(_groundWeight, _pose == CharacterPose.SitGround ? 1f : 0f, dt * 3f);
            // Hands are lowered while walking fast, except when carrying on with the activity is natural.
            float handsAllowed = 1f - _runWeight;
            _phoneWeight = Mathf.MoveTowards(_phoneWeight, _hands == HandPose.Phone ? handsAllowed : 0f, dt * 4f);
            _readWeight = Mathf.MoveTowards(_readWeight, _hands == HandPose.Read ? handsAllowed : 0f, dt * 4f);
            _eatWeight = Mathf.MoveTowards(_eatWeight, _hands == HandPose.Eat ? handsAllowed : 0f, dt * 4f);
            _sweepWeight = Mathf.MoveTowards(_sweepWeight, _hands == HandPose.Sweep ? handsAllowed : 0f, dt * 4f);
            _talkWeight = Mathf.MoveTowards(_talkWeight, _pose == CharacterPose.Talk ? 1f : 0f, dt * 4f);
            _celebrateWeight = Mathf.MoveTowards(_celebrateWeight, _pose == CharacterPose.Celebrate ? 1f : 0f, dt * 4f);

            float stride = Mathf.Lerp(WalkStrideLength, RunStrideLength, _runWeight);
            if (_speed > 0.05f) _phase += _speed / stride * Mathf.PI * 2f * dt;
            else _phase = Mathf.MoveTowards(_phase % (Mathf.PI * 2f), 0f, dt * 4f);
            float s = Mathf.Sin(_phase);
            float c = Mathf.Cos(_phase);
            float t = Time.time + _idleSeed;

            // --- Locomotion ---
            float legAmp = Mathf.Lerp(28f, 48f, _runWeight) * _moveWeight;
            float armAmp = Mathf.Lerp(22f, 55f, _runWeight) * _moveWeight;
            float kneeL = Mathf.Max(0f, -c) * Mathf.Lerp(35f, 80f, _runWeight) * _moveWeight;
            float kneeR = Mathf.Max(0f, c) * Mathf.Lerp(35f, 80f, _runWeight) * _moveWeight;
            float elbowBend = Mathf.Lerp(8f, 70f, _runWeight) * _moveWeight;
            float bob = Mathf.Abs(c) * Mathf.Lerp(0.025f, 0.06f, _runWeight) * _moveWeight;
            float lean = Mathf.Lerp(3f, 12f, _runWeight) * _moveWeight;

            // --- Idle ---
            float breathe = Mathf.Sin(t * 1.6f) * 0.008f * (1f - _moveWeight);
            float idleArm = Mathf.Sin(t * 0.9f) * 2f * (1f - _moveWeight);

            // --- Talk gestures ---
            float gesture = (Mathf.Sin(t * 2.3f) * 0.5f + 0.5f) * _talkWeight;

            // --- Celebrate: both arms up ---
            float cheer = _celebrateWeight;

            // --- Sitting overrides legs and lowers the body ---
            float hipPitch = Mathf.Lerp(s * legAmp, -90f, _sitWeight);
            float hipPitchR = Mathf.Lerp(-s * legAmp, -90f, _sitWeight);
            // Sitting on the ground keeps the legs straight out in front.
            float seatedKnee = Mathf.Lerp(90f, 0f, _groundWeight);
            float kneeLeft = Mathf.Lerp(kneeL, seatedKnee, _sitWeight);
            float kneeRight = Mathf.Lerp(kneeR, seatedKnee, _sitWeight);
            float seatHeight = Mathf.Lerp(_seatHeight, 0.13f, _groundWeight);
            float bodyY = Mathf.Lerp(bob + breathe, seatHeight - Visual.HipHeight, _sitWeight);

            // Hand-held activities (blended on top of the base pose).
            float bite = (Mathf.Sin(t * 1.3f) * 0.5f + 0.5f);
            float sweepSwing = Mathf.Sin(t * 2.4f);
            float rightArmX = -70f * _phoneWeight - 45f * _readWeight - Mathf.Lerp(35f, 95f, bite) * _eatWeight - 40f * _sweepWeight;
            float rightElbow = -95f * _phoneWeight - 60f * _readWeight - 70f * _eatWeight - 30f * _sweepWeight;
            float leftArmX = -45f * _readWeight - 30f * _eatWeight - 50f * _sweepWeight;
            float leftElbow = -60f * _readWeight - 60f * _eatWeight - 20f * _sweepWeight;
            float handWeight = Mathf.Clamp01(_phoneWeight + _readWeight + _eatWeight + _sweepWeight);
            float torsoTwist = sweepSwing * 18f * _sweepWeight;
            float torsoBend = 10f * _sweepWeight + 4f * _readWeight;

            Visual.Body.localPosition = new Vector3(0f, bodyY, 0f);
            Visual.Torso.localRotation = Quaternion.Euler(lean - 4f * _sitWeight + torsoBend, s * 4f * _moveWeight + torsoTwist, 0f);
            Visual.LegLeft.localRotation = Quaternion.Euler(hipPitch, 0f, 0f);
            Visual.LegRight.localRotation = Quaternion.Euler(hipPitchR, 0f, 0f);
            Visual.KneeLeft.localRotation = Quaternion.Euler(kneeLeft, 0f, 0f);
            Visual.KneeRight.localRotation = Quaternion.Euler(kneeRight, 0f, 0f);

            float armRest = 4f + idleArm;
            float sitArms = -35f * _sitWeight;
            float baseLeftX = Mathf.Lerp(-s * armAmp + sitArms, -170f, cheer);
            float baseRightX = Mathf.Lerp(s * armAmp + sitArms - gesture * 45f, -170f, cheer);
            float inward = 12f * handWeight;
            Visual.ArmLeft.localRotation = Quaternion.Euler(Mathf.Lerp(baseLeftX, leftArmX, Mathf.Clamp01(_readWeight + _eatWeight + _sweepWeight)), inward, Mathf.Lerp(-armRest, -15f, cheer));
            Visual.ArmRight.localRotation = Quaternion.Euler(Mathf.Lerp(baseRightX, rightArmX, handWeight), -inward, Mathf.Lerp(armRest, 15f, cheer));
            Visual.ForearmLeft.localRotation = Quaternion.Euler(Mathf.Lerp(-elbowBend - 25f * _sitWeight, leftElbow, Mathf.Clamp01(_readWeight + _eatWeight + _sweepWeight)), 0f, 0f);
            Visual.ForearmRight.localRotation = Quaternion.Euler(Mathf.Lerp(-elbowBend - 25f * _sitWeight - gesture * 50f, rightElbow, handWeight), 0f, 0f);

            UpdateHead(dt, t);
        }

        void UpdateHead(float dt, float t)
        {
            float lookDown = 28f * _phoneWeight + 25f * _readWeight + 15f * _sweepWeight;
            Quaternion target = Quaternion.Euler(Mathf.Sin(t * 0.5f) * 2f + _talkWeight * Mathf.Sin(t * 3.1f) * 4f + lookDown, Mathf.Sin(t * 0.31f) * 6f * (1f - _moveWeight), 0f);
            if (_lookTarget.HasValue && Visual.Head.parent != null)
            {
                Vector3 dir = _lookTarget.Value - Visual.Head.position;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Vector3 local = Visual.Head.parent.InverseTransformDirection(dir.normalized);
                    float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -70f, 70f);
                    float pitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg, -25f, 25f);
                    target = Quaternion.Euler(pitch, yaw, 0f);
                }
            }
            Visual.Head.localRotation = Quaternion.Slerp(Visual.Head.localRotation, target, 1f - Mathf.Exp(-8f * dt));
        }
    }
}

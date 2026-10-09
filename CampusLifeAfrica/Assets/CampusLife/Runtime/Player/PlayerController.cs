using CampusLife.Core.Simulation;
using CampusLife.Runtime.CameraSystem;
using CampusLife.Runtime.Characters;
using CampusLife.Runtime.World;
using UnityEngine;

namespace CampusLife.Runtime.Player
{
    /// <summary>
    /// Grounded third-person controller built on CharacterController: walk,
    /// run, turn, stop, stairs/slopes (step offset + ramp colliders), gravity,
    /// sitting and standing. Movement is camera-relative.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        const float Gravity = -18f;
        const float GroundedStick = -2f;

        public LocomotionSettings Locomotion = new LocomotionSettings();

        CharacterController _controller;
        float _speed;
        float _verticalVelocity;
        float _heading;
        Vector3 _moveDirection = Vector3.forward;
        SeatInteractable _seat;
        float _seatedAt;

        public PlayerInputReader Input { get; private set; }
        public ThirdPersonCameraRig CameraRig { get; set; }
        public CharacterAnimator Animator { get; set; }

        bool _controlEnabled = true;

        /// <summary>False during menus, conversations and area transitions.</summary>
        public bool ControlEnabled
        {
            get => _controlEnabled;
            set
            {
                if (value && !_controlEnabled) ControlEnabledFrame = Time.frameCount;
                _controlEnabled = value;
            }
        }

        /// <summary>Frame on which control was last re-enabled; the same key press must not also interact.</summary>
        public int ControlEnabledFrame { get; private set; } = -1;
        public bool IsSeated => _seat != null;

        /// <summary>A position that is safe to save and restore (standing spot when seated).</summary>
        public Vector3 SafePosition => _seat != null ? _seat.StandPose.position : transform.position;
        public float CurrentSpeed => _speed;
        public float Heading => _heading;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            Input = GetComponent<PlayerInputReader>();
            _heading = transform.eulerAngles.y;
        }

        public static void ConfigureController(CharacterController cc)
        {
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.92f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.04f;
            cc.minMoveDistance = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (IsSeated)
            {
                UpdateSeated();
                return;
            }

            Vector2 raw = ControlEnabled ? Input.Move : Vector2.zero;
            var input = MotionMath.ApplyDeadZone(new Planar(raw.x, raw.y), Locomotion.InputDeadZone);
            float magnitude = input.Magnitude;
            float cameraYaw = CameraRig != null ? CameraRig.Yaw : 0f;
            bool running = ControlEnabled && Input.RunHeld;

            if (magnitude > 0.001f)
            {
                var dir = MotionMath.CameraRelative(input, cameraYaw);
                _moveDirection = new Vector3(dir.X, 0f, dir.Y).normalized;
                _heading = MotionMath.MoveTowardsAngle(_heading, MotionMath.Heading(dir), Locomotion.TurnSpeedDegrees * dt);
            }

            float target = MotionMath.TargetSpeed(magnitude, running, Locomotion);
            _speed = MotionMath.StepSpeed(_speed, target, dt, Locomotion);

            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = GroundedStick;
            else _verticalVelocity += Gravity * dt;

            Vector3 before = transform.position;
            Vector3 velocity = _moveDirection * _speed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * dt);
            transform.rotation = Quaternion.Euler(0f, _heading, 0f);

            // Feed the animator the speed actually achieved (walls stop the cycle).
            Vector3 delta = transform.position - before;
            delta.y = 0f;
            float actual = dt > 0f ? delta.magnitude / dt : 0f;
            if (actual < _speed * 0.5f) _speed = Mathf.Min(_speed, actual + 0.5f);
            if (Animator != null) Animator.SetSpeed(actual);

            // Safety net: if the player ever falls through the world, put them back.
            if (transform.position.y < -20f && AreaManager.Instance != null) AreaManager.Instance.RespawnInCurrentArea();
        }

        void UpdateSeated()
        {
            if (Animator != null) Animator.SetSpeed(0f);
            if (!ControlEnabled || Time.frameCount <= ControlEnabledFrame || Time.time - _seatedAt < 0.4f) return;
            if (Input.InteractPressed || Input.Move.sqrMagnitude > 0.25f) StandUp();
        }

        public void SitOn(SeatInteractable seat)
        {
            if (IsSeated || seat == null) return;
            _seat = seat;
            _seat.Occupied = true;
            _seatedAt = Time.time;
            _speed = 0f;
            _controller.enabled = false;
            var sit = seat.SitPose;
            transform.SetPositionAndRotation(sit.position, Quaternion.Euler(0f, sit.eulerAngles.y, 0f));
            _heading = sit.eulerAngles.y;
            if (Animator != null) Animator.SetPose(CharacterPose.Sit, seat.SeatHeight);
        }

        public void StandUp()
        {
            if (!IsSeated) return;
            var stand = _seat.StandPose;
            _seat.Occupied = false;
            _seat = null;
            transform.SetPositionAndRotation(stand.position, Quaternion.Euler(0f, stand.eulerAngles.y, 0f));
            _heading = stand.eulerAngles.y;
            _controller.enabled = true;
            if (Animator != null) Animator.SetPose(CharacterPose.Stand);
        }

        /// <summary>Moves the player instantly (area transitions, loading).</summary>
        public void Teleport(Vector3 position, float yaw)
        {
            if (IsSeated) StandUp();
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            _controller.enabled = true;
            _heading = yaw;
            _speed = 0f;
            _verticalVelocity = GroundedStick;
            _moveDirection = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        }

        /// <summary>Turns the player to face a point (used when a conversation starts).</summary>
        public void FaceTowards(Vector3 point)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            _heading = Quaternion.LookRotation(d).eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, _heading, 0f);
            _speed = 0f;
            if (Animator != null) Animator.SetSpeed(0f);
        }
    }
}

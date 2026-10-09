using CampusLife.Core.Simulation;
using CampusLife.Runtime.Player;
using UnityEngine;

namespace CampusLife.Runtime.CameraSystem
{
    public enum CameraMode { Follow, Conversation, MenuOrbit, Portrait }

    /// <summary>
    /// Third-person orbit camera with smoothing, wall collision (sphere cast),
    /// zoom, sensitivity/invert settings, separate outdoor and interior
    /// profiles, conversation framing and a reduced-motion option. Orbit
    /// maths live in <see cref="CameraOrbit"/> (unit tested).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
        const int IgnoreRaycastLayer = 2;
        const float CastRadius = 0.22f;

        public Camera Camera { get; private set; }
        public Transform Target { get; set; }
        public PlayerInputReader Input { get; set; }
        public PlayerController Player { get; set; }
        public CameraMode Mode { get; private set; } = CameraMode.Follow;
        public bool LookEnabled { get; set; } = true;

        readonly CameraSettings _outdoor = new CameraSettings { MinDistance = 1.8f, MaxDistance = 9f };
        readonly CameraSettings _interior = new CameraSettings { MinDistance = 1.4f, MaxDistance = 4.2f, MaxPitch = 55f };
        CameraSettings _active;
        CameraOrbit _orbit;
        Vector3 _pivot;
        float _currentDistance;
        float _baseFov = 60f;

        // Conversation / menu / portrait framing
        Vector3 _shotPosition, _shotLookAt;
        Vector3 _orbitCenter;
        float _orbitRadius, _orbitHeight, _orbitAngle;

        public float Yaw => _orbit.Yaw;
        public bool InteriorProfile => _active == _interior;

        void Awake()
        {
            Camera = GetComponent<Camera>();
            if (Camera == null) Camera = gameObject.AddComponent<Camera>();
            Camera.nearClipPlane = 0.08f;
            Camera.fieldOfView = _baseFov;
            _active = _outdoor;
            _orbit = new CameraOrbit(0f, 14f, 4.5f, _active);
            _currentDistance = _orbit.Distance;
        }

        public void ApplyUserSettings(float sensitivity, bool invertY, bool reducedMotion)
        {
            foreach (var s in new[] { _outdoor, _interior })
            {
                s.Sensitivity = sensitivity;
                s.InvertY = invertY;
                s.ReducedMotion = reducedMotion;
                // Reduced motion: no lag or sway; the camera follows rigidly.
                s.FollowSharpness = reducedMotion ? 0f : 12f;
            }
        }

        public void UseInteriorProfile(bool interior)
        {
            _active = interior ? _interior : _outdoor;
            _orbit.ClampDistance(_active);
            _orbit.Look(0f, 0f, _active);
        }

        /// <summary>Returns to the follow camera, continuing from the current view direction.</summary>
        public void SetFollow()
        {
            if (Mode != CameraMode.Follow) _orbit.SetYaw(transform.eulerAngles.y);
            Mode = CameraMode.Follow;
            Camera.fieldOfView = _baseFov;
        }

        /// <summary>Places the camera behind the target immediately (after teleports).</summary>
        public void SnapBehindTarget()
        {
            if (Target == null) return;
            _orbit.SetYaw(Target.eulerAngles.y);
            _pivot = PivotFor(Target);
            _currentDistance = _orbit.Distance;
            Mode = CameraMode.Follow;
            ApplyFollow(0f, snap: true);
        }

        /// <summary>Over-the-shoulder framing of the player and a conversation partner.</summary>
        public void FrameConversation(Transform player, Transform npc)
        {
            Mode = CameraMode.Conversation;
            Vector3 p = player.position, n = npc.position;
            Vector3 toNpc = n - p;
            toNpc.y = 0f;
            if (toNpc.sqrMagnitude < 0.01f) toNpc = player.forward;
            toNpc.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, toNpc);
            _shotLookAt = Vector3.Lerp(p, n, 0.6f) + Vector3.up * 1.55f;
            _shotPosition = p - toNpc * 1.6f + right * 0.75f + Vector3.up * 1.85f;
            ResolveShotCollision(p + Vector3.up * 1.6f);
            if (Settings.ReducedMotion) Place(_shotPosition, _shotLookAt);
        }

        public void SetMenuOrbit(Vector3 center, float radius, float height)
        {
            Mode = CameraMode.MenuOrbit;
            _orbitCenter = center;
            _orbitRadius = radius;
            _orbitHeight = height;
        }

        /// <summary>Close framing of a character for the character creator.</summary>
        public void SetPortrait(Transform subject)
        {
            Mode = CameraMode.Portrait;
            // Offset the framing so the character sits right of centre, clear of the creator panel.
            Vector3 side = subject.right * 0.75f;
            _shotLookAt = subject.position + Vector3.up * 1.1f + side;
            _shotPosition = subject.position + subject.forward * 3.3f + Vector3.up * 1.4f + side;
            Place(_shotPosition, _shotLookAt);
        }

        CameraSettings Settings => _active;

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            switch (Mode)
            {
                case CameraMode.Follow:
                    if (Target == null) return;
                    if (LookEnabled && Input != null)
                    {
                        _orbit.Look(Input.Look.x, Input.Look.y, Settings);
                        if (Input.Zoom != 0f) _orbit.Zoom(Input.Zoom, Settings);
                    }
                    ApplyFollow(dt, snap: false);
                    break;
                case CameraMode.Conversation:
                case CameraMode.Portrait:
                {
                    float k = MotionMath.SmoothingFactor(Settings.ReducedMotion ? 0f : 5f, dt);
                    Vector3 pos = Vector3.Lerp(transform.position, _shotPosition, k);
                    Place(pos, _shotLookAt);
                    break;
                }
                case CameraMode.MenuOrbit:
                {
                    _orbitAngle += dt * (Settings.ReducedMotion ? 0f : 4f);
                    Vector3 offset = Quaternion.Euler(0f, _orbitAngle, 0f) * new Vector3(0f, _orbitHeight, -_orbitRadius);
                    Place(_orbitCenter + offset, _orbitCenter + Vector3.up * 4f);
                    break;
                }
            }
        }

        static Vector3 PivotFor(Transform target) => target.position + Vector3.up * 1.55f;

        void ApplyFollow(float dt, bool snap)
        {
            Vector3 desiredPivot = PivotFor(Target);
            float k = snap ? 1f : MotionMath.SmoothingFactor(Settings.FollowSharpness, dt);
            _pivot = Vector3.Lerp(_pivot, desiredPivot, k);

            Quaternion rotation = Quaternion.Euler(_orbit.Pitch, _orbit.Yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            float? hit = null;
            if (Physics.SphereCast(_pivot, CastRadius, back, out var info, _orbit.Distance, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                hit = info.distance;
            float allowed = CameraOrbit.ResolveCollision(_orbit.Distance, hit, Settings);

            // Pull in instantly to avoid clipping; ease back out to avoid popping.
            if (snap || allowed < _currentDistance) _currentDistance = allowed;
            else _currentDistance = Mathf.Lerp(_currentDistance, allowed, MotionMath.SmoothingFactor(Settings.ReducedMotion ? 0f : 6f, dt));

            transform.SetPositionAndRotation(_pivot + back * _currentDistance, rotation);

            // Subtle FOV widening while running; disabled in reduced-motion mode.
            float targetFov = _baseFov;
            if (!Settings.ReducedMotion && Player != null && Player.CurrentSpeed > Player.Locomotion.WalkSpeed + 0.5f) targetFov += 5f;
            Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView, targetFov, snap ? 1f : MotionMath.SmoothingFactor(4f, dt));
        }

        void ResolveShotCollision(Vector3 from)
        {
            Vector3 dir = _shotPosition - from;
            if (Physics.SphereCast(from, CastRadius, dir.normalized, out var info, dir.magnitude, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                _shotPosition = from + dir.normalized * Mathf.Max(0.3f, info.distance - 0.15f);
        }

        void Place(Vector3 position, Vector3 lookAt)
        {
            transform.position = position;
            Vector3 d = lookAt - position;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}

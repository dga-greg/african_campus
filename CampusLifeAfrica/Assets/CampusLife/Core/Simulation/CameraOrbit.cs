using System;

namespace CampusLife.Core.Simulation
{
    public sealed class CameraSettings
    {
        public float Sensitivity = 1f;
        public bool InvertY;
        public bool ReducedMotion;
        public float MinPitch = -25f;
        public float MaxPitch = 70f;
        public float MinDistance = 1.6f;
        public float MaxDistance = 8f;
        public float ZoomStep = 0.8f;
        public float CollisionPadding = 0.2f;
        public float FollowSharpness = 12f;
    }

    /// <summary>
    /// Orbit state for the third-person camera: yaw/pitch/zoom with clamping,
    /// plus the collision rule used to pull the camera in front of walls.
    /// </summary>
    public sealed class CameraOrbit
    {
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public float Distance { get; private set; }

        public CameraOrbit(float yaw, float pitch, float distance, CameraSettings s)
        {
            Yaw = Wrap(yaw);
            Pitch = Clamp(pitch, s.MinPitch, s.MaxPitch);
            Distance = Clamp(distance, s.MinDistance, s.MaxDistance);
        }

        /// <summary>Applies look input in degrees (already scaled for device).</summary>
        public void Look(float deltaYaw, float deltaPitch, CameraSettings s)
        {
            float sens = Math.Max(0.05f, s.Sensitivity);
            Yaw = Wrap(Yaw + deltaYaw * sens);
            float pitchDelta = deltaPitch * sens * (s.InvertY ? 1f : -1f);
            Pitch = Clamp(Pitch + pitchDelta, s.MinPitch, s.MaxPitch);
        }

        /// <summary>Positive scroll zooms in.</summary>
        public void Zoom(float scrollSteps, CameraSettings s)
        {
            Distance = Clamp(Distance - scrollSteps * s.ZoomStep, s.MinDistance, s.MaxDistance);
        }

        /// <summary>Clamps distance limits, e.g. when entering a tight interior.</summary>
        public void ClampDistance(CameraSettings s) => Distance = Clamp(Distance, s.MinDistance, s.MaxDistance);

        public void SetYaw(float yaw) => Yaw = Wrap(yaw);

        /// <summary>
        /// Given the distance to the first obstacle between pivot and camera
        /// (or null when the path is clear), returns how far the camera may sit.
        /// </summary>
        public static float ResolveCollision(float desiredDistance, float? hitDistance, CameraSettings s)
        {
            if (!hitDistance.HasValue) return desiredDistance;
            float allowed = hitDistance.Value - s.CollisionPadding;
            return Clamp(Math.Min(desiredDistance, allowed), 0.3f, desiredDistance);
        }

        static float Wrap(float angle)
        {
            angle %= 360f;
            return angle < 0f ? angle + 360f : angle;
        }

        static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    }
}

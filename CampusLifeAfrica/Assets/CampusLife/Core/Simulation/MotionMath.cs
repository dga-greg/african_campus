using System;

namespace CampusLife.Core.Simulation
{
    /// <summary>Engine-independent 2D vector on the ground plane (x = east, y = north).</summary>
    public struct Planar
    {
        public float X;
        public float Y;

        public Planar(float x, float y) { X = x; Y = y; }

        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y);

        public Planar ClampMagnitude(float max)
        {
            float m = Magnitude;
            return m > max && m > 0f ? new Planar(X / m * max, Y / m * max) : this;
        }

        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }

    /// <summary>Tunable locomotion values; defaults match a grounded, walking-pace campus sim.</summary>
    public sealed class LocomotionSettings
    {
        public float WalkSpeed = 2.0f;
        public float RunSpeed = 5.0f;
        public float Acceleration = 10f;
        public float Deceleration = 14f;
        public float TurnSpeedDegrees = 600f;
        public float InputDeadZone = 0.12f;
    }

    public static class MotionMath
    {
        /// <summary>
        /// Applies a radial dead zone and rescales so the remaining range maps to 0..1,
        /// which keeps analog sticks and the touch joystick precise near the centre.
        /// </summary>
        public static Planar ApplyDeadZone(Planar input, float deadZone)
        {
            float m = input.Magnitude;
            if (m <= deadZone || m <= 0f) return new Planar(0f, 0f);
            float scaled = Math.Min(1f, (m - deadZone) / (1f - deadZone));
            return new Planar(input.X / m * scaled, input.Y / m * scaled);
        }

        /// <summary>
        /// Converts stick input (x right, y forward) into a world-plane direction
        /// relative to the camera yaw (degrees, 0 = facing north/+z, clockwise).
        /// </summary>
        public static Planar CameraRelative(Planar input, float cameraYawDegrees)
        {
            double rad = cameraYawDegrees * Math.PI / 180.0;
            float sin = (float)Math.Sin(rad), cos = (float)Math.Cos(rad);
            // forward = (sin, cos), right = (cos, -sin)
            return new Planar(input.X * cos + input.Y * sin, -input.X * sin + input.Y * cos);
        }

        /// <summary>Speed the character should approach for a given input magnitude.</summary>
        public static float TargetSpeed(float inputMagnitude, bool running, LocomotionSettings s)
        {
            inputMagnitude = Math.Max(0f, Math.Min(1f, inputMagnitude));
            return inputMagnitude * (running ? s.RunSpeed : s.WalkSpeed);
        }

        /// <summary>Moves current speed toward target with separate acceleration and braking rates.</summary>
        public static float StepSpeed(float current, float target, float deltaTime, LocomotionSettings s)
        {
            float rate = target > current ? s.Acceleration : s.Deceleration;
            return MoveTowards(current, target, rate * deltaTime);
        }

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta) return target;
            return current + Math.Sign(target - current) * maxDelta;
        }

        /// <summary>Heading in degrees (0 = +z, clockwise) for a planar direction.</summary>
        public static float Heading(Planar direction) => (float)(Math.Atan2(direction.X, direction.Y) * 180.0 / Math.PI);

        public static float DeltaAngle(float current, float target)
        {
            float delta = (target - current) % 360f;
            if (delta > 180f) delta -= 360f;
            if (delta < -180f) delta += 360f;
            return delta;
        }

        public static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float delta = DeltaAngle(current, target);
            if (Math.Abs(delta) <= maxDelta) return target;
            return current + Math.Sign(delta) * maxDelta;
        }

        /// <summary>Frame-rate independent exponential smoothing factor (0..1).</summary>
        public static float SmoothingFactor(float sharpness, float deltaTime)
        {
            if (sharpness <= 0f) return 1f;
            return 1f - (float)Math.Exp(-sharpness * deltaTime);
        }
    }
}

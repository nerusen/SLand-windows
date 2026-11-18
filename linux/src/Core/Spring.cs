using System;

namespace Tepi.Linux.Core
{
    public class Spring
    {
        public float Value { get; set; }
        public float Target { get; set; }
        public float Velocity { get; set; }
        public float Stiffness { get; set; }
        public float Damping { get; set; }
        public float Precision { get; set; }

        public Spring(float value, float stiffness = 280f, float damping = 26f, float precision = 0.04f)
        {
            Value = Target = value;
            Stiffness = stiffness;
            Damping = damping;
            Precision = precision;
            Velocity = 0;
        }

        public bool Settled => Math.Abs(Target - Value) < Precision && Math.Abs(Velocity) < Precision * 2f;

        public bool Step(float dt)
        {
            if (Settled)
            {
                Value = Target;
                Velocity = 0;
                return false;
            }

            // Clamping dt to avoid numerical instability
            dt = Math.Min(dt, 0.05f);

            float force = -Stiffness * (Value - Target);
            float dampingForce = -Damping * Velocity;
            float acceleration = force + dampingForce;

            Velocity += acceleration * dt;
            Value += Velocity * dt;

            if (Settled)
            {
                Value = Target;
                Velocity = 0;
                return false;
            }

            return true;
        }

        public void Snap(float v)
        {
            Value = Target = v;
            Velocity = 0;
        }
    }

    public static class Ease
    {
        public static float Clamp01(float t) => Math.Min(1f, Math.Max(0f, t));

        public static float OutCubic(float t)
        {
            float rest = 1 - Clamp01(t);
            return 1 - rest * rest * rest;
        }

        public static float InOutCubic(float t)
        {
            t = Clamp01(t);
            return t < 0.5f ? 4 * t * t * t : 1 - (float)Math.Pow(-2 * t + 2, 3) / 2;
        }

        public static float OutBack(float t)
        {
            float back = Clamp01(t) - 1, sq = back * back;
            return 1 + 2.5f * sq * back + 1.5f * sq;
        }

        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}

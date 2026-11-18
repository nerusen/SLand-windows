using System;

namespace Tepi.Linux.Core
{
    public class Spring
    {
        const float MaxStep = 0.002f;

        public float Value { get; set; }
        public float Target { get; set; }
        public float Velocity { get; set; }
        public float Stiffness { get; set; }
        public float Damping { get; set; }
        public float Precision { get; set; }

        public Spring(float value, float stiffness = 300f, float damping = 26f, float precision = 0.04f)
        {
            Value = Target = value;
            Stiffness = stiffness;
            Damping = damping;
            Precision = precision;
            Velocity = 0;
        }

        public bool Settled => Math.Abs(Target - Value) < Precision && Math.Abs(Velocity) < 6 * Precision;

        // Sub-stepped analytical physics (identical to Base.cs in Windows src)
        public bool Step(float dt)
        {
            if (Settled)
            {
                Snap(Target);
                return false;
            }

            int n = (int)Math.Ceiling(dt / MaxStep);
            if (n < 1) n = 1;
            float h = dt / n;

            while (n-- > 0)
            {
                float accel = Stiffness * (Target - Value) - Damping * Velocity;
                Velocity += accel * h;
                Value += Velocity * h;
            }

            if (Settled)
            {
                Snap(Target);
                return false;
            }

            return true;
        }

        public void Snap(float v)
        {
            Value = Target = v;
            Velocity = 0;
        }

        // Factory presets for island states
        public static Spring ForWidth(float initial) => new Spring(initial, 300f, 26f, 0.04f);
        public static Spring ForHeight(float initial) => new Spring(initial, 240f, 23f, 0.04f);
        public static Spring ForRadius(float initial) => new Spring(initial, 300f, 26f, 0.04f);
        public static Spring ForGaze(float initial) => new Spring(initial, 120f, 20f, 0.04f);
        public static Spring ForEyelid(float initial) => new Spring(initial, 60f, 15f, 0.01f);
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

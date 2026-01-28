using System;
using System.Numerics;

namespace MCTwin.Shared.Geometry
{
    public static class SdfPrimitives
    {
        /// <summary>
        /// Signed Distance Function for a Sphere.
        /// </summary>
        public static float SdSphere(Vector3 p, float r)
        {
            return p.Length() - r;
        }

        /// <summary>
        /// Signed Distance Function for a Box.
        /// </summary>
        public static float SdBox(Vector3 p, Vector3 b)
        {
            Vector3 q = Vector3.Abs(p) - b;
            return Vector3.Max(q, Vector3.Zero).Length() + MathF.Min(MathF.Max(q.X, MathF.Max(q.Y, q.Z)), 0.0f);
        }

        /// <summary>
        /// Signed Distance Function for a Round Box.
        /// </summary>
        public static float SdRoundBox(Vector3 p, Vector3 b, float r)
        {
            Vector3 q = Vector3.Abs(p) - b;
            return Vector3.Max(q, Vector3.Zero).Length() + MathF.Min(MathF.Max(q.X, MathF.Max(q.Y, q.Z)), 0.0f) - r;
        }

        /// <summary>
        /// Signed Distance Function for a Vertical Capsule (Line Segment on Y axis).
        /// </summary>
        public static float SdVerticalCapsule(Vector3 p, float h, float r)
        {
            p.Y -= Math.Clamp(p.Y, 0.0f, h);
            return p.Length() - r;
        }

        /// <summary>
        /// Signed Distance Function for a Capsule between two points.
        /// </summary>
        public static float SdCapsule(Vector3 p, Vector3 a, Vector3 b, float r)
        {
            Vector3 pa = p - a;
            Vector3 ba = b - a;
            float h = Math.Clamp(Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba), 0.0f, 1.0f);
            return (pa - ba * h).Length() - r;
        }
    }
}

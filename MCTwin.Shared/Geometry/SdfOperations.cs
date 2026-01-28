using System;
using System.Numerics;

namespace MCTwin.Shared.Geometry
{
    public static class SdfOperations
    {
        // Standard Boolean Operations
        public static float Union(float d1, float d2) => MathF.Min(d1, d2);
        public static float Subtract(float d1, float d2) => MathF.Max(-d1, d2);
        public static float Intersect(float d1, float d2) => MathF.Max(d1, d2);

        // Smooth Boolean Operations (The "Organic" Sauce)
        
        /// <summary>
        /// Smooth Union with polynomial smooth min (k = blending factor).
        /// Higher k = more blending.
        /// </summary>
        public static float SmoothUnion(float d1, float d2, float k)
        {
            float h = Math.Clamp(0.5f + 0.5f * (d2 - d1) / k, 0.0f, 1.0f);
            return float.Lerp(d2, d1, h) - k * h * (1.0f - h);
        }

        public static float SmoothSubtract(float d1, float d2, float k)
        {
            float h = Math.Clamp(0.5f - 0.5f * (d2 + d1) / k, 0.0f, 1.0f);
            return float.Lerp(d2, -d1, h) + k * h * (1.0f - h);
        }

        public static float SmoothIntersect(float d1, float d2, float k)
        {
            float h = Math.Clamp(0.5f - 0.5f * (d2 - d1) / k, 0.0f, 1.0f);
            return float.Lerp(d2, d1, h) + k * h * (1.0f - h);
        }

        // Calculate Normal using Gradient (Finite Differences)
        public static Vector3 CalcNormal(Func<Vector3, float> sdf, Vector3 p)
        {
            float eps = 0.001f;
            Vector3 n = new Vector3(
                sdf(new Vector3(p.X + eps, p.Y, p.Z)) - sdf(new Vector3(p.X - eps, p.Y, p.Z)),
                sdf(new Vector3(p.X, p.Y + eps, p.Z)) - sdf(new Vector3(p.X, p.Y - eps, p.Z)),
                sdf(new Vector3(p.X, p.Y, p.Z + eps)) - sdf(new Vector3(p.X, p.Y, p.Z - eps))
            );
            return Vector3.Normalize(n);
        }
    }
}

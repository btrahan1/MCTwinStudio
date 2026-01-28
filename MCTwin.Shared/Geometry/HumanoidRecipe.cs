using System.Numerics;

namespace MCTwin.Shared.Geometry
{
    public class HumanoidRecipe
    {
        // Core Dimensions
        public float Height { get; set; } = 1.8f;
        public float TorsoWidth { get; set; } = 0.4f;
        public float WaistWidth { get; set; } = 0.3f; // Narrower
        public float HipWidth { get; set; } = 0.38f; // Wider than waist
        
        public float ShoulderWidth { get; set; } = 0.5f;
        
        // Muscle Definition (Used for SmoothUnion k-factor)
        public float MuscleTone { get; set; } = 0.1f; // low = sharp, high = smooth/fat

        // Specifics
        public float HeadSize { get; set; } = 0.25f;

        // Limbs
        public float ArmLength { get; set; } = 0.7f;
        public float ArmThickness { get; set; } = 0.12f;
        public float LegLength { get; set; } = 0.9f;
        public float LegThickness { get; set; } = 0.15f;
        
        public float NeckThickness { get; set; } = 0.06f;
    }
}

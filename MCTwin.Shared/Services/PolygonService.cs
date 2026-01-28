using System;
using System.Numerics;
using System.Threading.Tasks;
using MCTwin.Shared.Geometry;
using MCTwin.Shared.Meshing;

namespace MCTwin.Shared.Services
{
    public class PolygonService
    {
        public async Task<MeshResult> GenerateHumanoidAsync(HumanoidRecipe recipe)
        {
            // Run on background thread to avoid freezing UI
            return await Task.Run(() => 
            {
                // Define the SDF for the Humanoid
                Func<Vector3, float> humanoidSdf = (p) =>
                {
                    // 1. Torso (RoundBox)
                    Vector3 torsoPos = p - new Vector3(0, recipe.Height * 0.6f, 0);
                    float dTorso = SdfPrimitives.SdRoundBox(torsoPos, new Vector3(recipe.TorsoWidth * 0.5f, recipe.Height * 0.2f, recipe.TorsoWidth * 0.3f), recipe.MuscleTone);

                    // 2. Shoulders (Spheres)
                    Vector3 leftShoulderPos = new Vector3(-recipe.ShoulderWidth / 2, recipe.Height * 0.85f, 0);
                    Vector3 rightShoulderPos = new Vector3(recipe.ShoulderWidth / 2, recipe.Height * 0.85f, 0);
                    float shoulderRadius = recipe.ArmThickness * 1.5f; // Shoulders relate to arm thickness
                    
                    float dShoulders = SdfOperations.Union(
                        SdfPrimitives.SdSphere(p - leftShoulderPos, shoulderRadius),
                        SdfPrimitives.SdSphere(p - rightShoulderPos, shoulderRadius)
                    );

                    // 3. Arms (Capsules)
                    // Explicitly from Shoulder to Hand (Angled outwards ~25 degrees)
                    float armSpread = 0.45f; // Much wider spread
                    Vector3 lHandOffset = new Vector3(-recipe.ArmLength * armSpread, -recipe.ArmLength * 0.9f, 0);
                    Vector3 rHandOffset = new Vector3(recipe.ArmLength * armSpread, -recipe.ArmLength * 0.9f, 0);

                    float dArms = SdfOperations.Union(
                        SdfPrimitives.SdCapsule(p, leftShoulderPos, leftShoulderPos + lHandOffset, recipe.ArmThickness),
                        SdfPrimitives.SdCapsule(p, rightShoulderPos, rightShoulderPos + rHandOffset, recipe.ArmThickness)
                    );

                    // 4. Legs (Capsules)
                    // Hips are at approx Height * 0.45
                    float hipY = recipe.Height * 0.45f;
                    float legSpacing = recipe.TorsoWidth * 0.45f; // Significant gap
                    Vector3 legStartL = new Vector3(-legSpacing, hipY, 0);
                    Vector3 legStartR = new Vector3(legSpacing, hipY, 0);
                    
                    float dLegs = SdfOperations.Union(
                        SdfPrimitives.SdCapsule(p, legStartL, legStartL - new Vector3(0, recipe.LegLength, 0), recipe.LegThickness),
                        SdfPrimitives.SdCapsule(p, legStartR, legStartR - new Vector3(0, recipe.LegLength, 0), recipe.LegThickness)
                    );

                    // 5. Head
                    float dHead = SdfPrimitives.SdSphere(p - new Vector3(0, recipe.Height, 0), recipe.HeadSize);

                    // Combine everything
                    float dBody = SdfOperations.SmoothUnion(dTorso, dShoulders, recipe.MuscleTone);
                    dBody = SdfOperations.SmoothUnion(dBody, dArms, recipe.MuscleTone);
                    dBody = SdfOperations.SmoothUnion(dBody, dLegs, recipe.MuscleTone); 
                    dBody = SdfOperations.SmoothUnion(dBody, dHead, 0.05f); // Neck blend

                    return dBody;
                };

                // Bounding Box (Padding added)
                Vector3 min = new Vector3(-1.0f, 0.0f, -1.0f);
                Vector3 max = new Vector3(1.0f, 2.5f, 1.0f);

                // Resolution (Start low for performance)
                int res = 32; 

                return MarchingCubes.Generate(humanoidSdf, min, max, res, res, res);
            });
        }
    }
}

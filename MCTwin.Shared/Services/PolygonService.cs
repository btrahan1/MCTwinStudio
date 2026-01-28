using System;
using System.Numerics;
using System.Threading.Tasks;
using MCTwin.Shared.Geometry;
using MCTwin.Shared.Meshing;

namespace MCTwin.Shared.Services
{
    public class PolygonService
    {
        public async Task<MeshResult> GenerateHumanoidAsync(HumanoidRecipe recipe, bool smooth = false)
        {
            // Run on background thread to avoid freezing UI
            return await Task.Run(() => 
            {
                // Define the SDF for the Humanoid
                Func<Vector3, float> humanoidSdf = (p) =>
                {
                    // 1. Torso Complex (Chest, Waist, Hips)
                    
                    // Chest (Upper)
                    float chestY = recipe.Height * 0.7f;
                    Vector3 chestPos = p - new Vector3(0, chestY, 0);
                    // Chest is wider at top, slight V taper handled by waist blend
                    float dChest = SdfPrimitives.SdRoundBox(chestPos, new Vector3(recipe.TorsoWidth * 0.5f, recipe.Height * 0.12f, recipe.TorsoWidth * 0.25f), recipe.MuscleTone);

                    // Waist (Middle)
                    float waistY = recipe.Height * 0.55f;
                    Vector3 waistPos = p - new Vector3(0, waistY, 0);
                    float dWaist = SdfPrimitives.SdVerticalCapsule(waistPos, recipe.Height * 0.1f, recipe.WaistWidth * 0.4f); 
                    // Note: Capsule radius acts as width. 
                    
                    // Hips (Lower)
                    float hipsY = recipe.Height * 0.45f;
                    Vector3 hipsPos = p - new Vector3(0, hipsY, 0);
                    float dHips = SdfPrimitives.SdRoundBox(hipsPos, new Vector3(recipe.HipWidth * 0.5f, recipe.Height * 0.08f, recipe.HipWidth * 0.25f), recipe.MuscleTone);

                    // Blend Torso Components
                    float dTorso = SdfOperations.SmoothUnion(dChest, dWaist, 0.15f); // Smooth blend for organic waist
                    dTorso = SdfOperations.SmoothUnion(dTorso, dHips, 0.15f);


                    // 2. Shoulders (Spheres)
                    Vector3 leftShoulderPos = new Vector3(-recipe.ShoulderWidth / 2, recipe.Height * 0.82f, 0); 
                    Vector3 rightShoulderPos = new Vector3(recipe.ShoulderWidth / 2, recipe.Height * 0.82f, 0);
                    float shoulderRadius = recipe.ArmThickness * 1.5f; 
                    
                    float dShoulders = SdfOperations.Union(
                        SdfPrimitives.SdSphere(p - leftShoulderPos, shoulderRadius),
                        SdfPrimitives.SdSphere(p - rightShoulderPos, shoulderRadius)
                    );

                    // Trapezius (Slope from neck base to shoulders)
                    // We can simulate this with two angled capsules
                    Vector3 neckBase = new Vector3(0, recipe.Height * 0.78f, -recipe.NeckThickness * 0.5f); // Slightly back
                    float dTraps = SdfOperations.Union(
                        SdfPrimitives.SdCapsule(p, neckBase, leftShoulderPos + new Vector3(0.05f, 0.05f, 0), recipe.NeckThickness * 0.8f),
                        SdfPrimitives.SdCapsule(p, neckBase, rightShoulderPos + new Vector3(-0.05f, 0.05f, 0), recipe.NeckThickness * 0.8f)
                    );
                    
                    // Glutes (Buttocks)
                    float gluteY = hipsY; // Same height as hips center
                    float gluteZ = -recipe.HipWidth * 0.25f; // Backwards
                    float gluteRadius = recipe.HipWidth * 0.35f;
                    Vector3 lGlutePos = new Vector3(-recipe.HipWidth * 0.25f, gluteY, gluteZ);
                    Vector3 rGlutePos = new Vector3(recipe.HipWidth * 0.25f, gluteY, gluteZ);
                    
                    float dGlutes = SdfOperations.Union(
                        SdfPrimitives.SdSphere(p - lGlutePos, gluteRadius),
                        SdfPrimitives.SdSphere(p - rGlutePos, gluteRadius)
                    );
                    
                    // Blend Glutes into Hips
                    dTorso = SdfOperations.SmoothUnion(dTorso, dGlutes, 0.1f);

                    // 3. Arms (Upper + Forearm)
                    float armSpread = 0.45f;
                    float armLen = recipe.ArmLength;
                    // Elbow is halfway
                    Vector3 lElbowPos = leftShoulderPos + new Vector3(-armLen * 0.5f * armSpread, -armLen * 0.5f * 0.9f, 0);
                    Vector3 rElbowPos = rightShoulderPos + new Vector3(armLen * 0.5f * armSpread, -armLen * 0.5f * 0.9f, 0);
                    
                    Vector3 lHandPos = leftShoulderPos + new Vector3(-armLen * armSpread, -armLen * 0.9f, 0);
                    Vector3 rHandPos = rightShoulderPos + new Vector3(armLen * armSpread, -armLen * 0.9f, 0);

                    // Upper Arm (Thicker)
                    float dUpperArms = MathF.Min(
                        SdfPrimitives.SdCapsule(p, leftShoulderPos, lElbowPos, recipe.ArmThickness),
                        SdfPrimitives.SdCapsule(p, rightShoulderPos, rElbowPos, recipe.ArmThickness)
                    );

                    // Forearm (Thinner)
                    float dForearms = MathF.Min(
                        SdfPrimitives.SdCapsule(p, lElbowPos, lHandPos, recipe.ArmThickness * 0.8f),
                        SdfPrimitives.SdCapsule(p, rElbowPos, rHandPos, recipe.ArmThickness * 0.8f)
                    );
                    float dArms = SdfOperations.SmoothUnion(dUpperArms, dForearms, 0.05f); // Elbow blend

                    // 4. Legs (Thigh + Calf)
                    float hipY = recipe.Height * 0.45f;
                    float legSpacing = recipe.TorsoWidth * 0.45f;
                    Vector3 lHipPos = new Vector3(-legSpacing, hipY, 0);
                    Vector3 rHipPos = new Vector3(legSpacing, hipY, 0);
                    
                    Vector3 lKneePos = lHipPos - new Vector3(0, recipe.LegLength * 0.5f, 0);
                    Vector3 rKneePos = rHipPos - new Vector3(0, recipe.LegLength * 0.5f, 0);
                    
                    Vector3 lAnklePos = lHipPos - new Vector3(0, recipe.LegLength, 0);
                    Vector3 rAnklePos = rHipPos - new Vector3(0, recipe.LegLength, 0);

                    // Thighs (Thick)
                    float dThighs = MathF.Min(
                        SdfPrimitives.SdCapsule(p, lHipPos, lKneePos, recipe.LegThickness),
                        SdfPrimitives.SdCapsule(p, rHipPos, rKneePos, recipe.LegThickness)
                    );

                    // Calves (Tapered/Thinner)
                    float dCalves = MathF.Min(
                        SdfPrimitives.SdCapsule(p, lKneePos, lAnklePos, recipe.LegThickness * 0.8f),
                        SdfPrimitives.SdCapsule(p, rKneePos, rAnklePos, recipe.LegThickness * 0.8f)
                    );
                    float dLegs = SdfOperations.SmoothUnion(dThighs, dCalves, 0.05f); // Knee blend

                    // 5. Head
                    float dHead = SdfPrimitives.SdSphere(p - new Vector3(0, recipe.Height, 0), recipe.HeadSize);
                    
                    // 6. Neck
                    float neckStart = chestY + (recipe.Height * 0.12f); // Explicit top of chest box
                    float neckEnd = recipe.Height; 
                    float dNeck = SdfPrimitives.SdVerticalCapsule(p - new Vector3(0, neckStart, 0), neckEnd - neckStart, recipe.NeckThickness);

                    // Combine everything
                    float dBody = SdfOperations.SmoothUnion(dTorso, dShoulders, recipe.MuscleTone);
                    dBody = SdfOperations.SmoothUnion(dBody, dTraps, recipe.MuscleTone * 1.5f); // Smooth slope
                    dBody = SdfOperations.SmoothUnion(dBody, dArms, recipe.MuscleTone);
                    dBody = SdfOperations.SmoothUnion(dBody, dLegs, recipe.MuscleTone); 
                    
                    // Sharp blend for neck to preserve structure
                    dBody = SdfOperations.SmoothUnion(dBody, dNeck, 0.02f); 
                    dBody = SdfOperations.SmoothUnion(dBody, dHead, 0.02f);

                    return dBody;
                };

                // Bounding Box (Padding added)
                Vector3 min = new Vector3(-1.0f, 0.0f, -1.0f);
                Vector3 max = new Vector3(1.0f, 2.5f, 1.0f);

                // Resolution (Start low for performance)
                int res = 32; 

                // Define Color Function (RGBA)
                Func<Vector3, Vector4> colorFunc = (p) => 
                {
                    // Re-calculate primitive distances to determine "Part ID"
                    
                    // 1. Head
                    float dHead = SdfPrimitives.SdSphere(p - new Vector3(0, recipe.Height, 0), recipe.HeadSize);
                    
                    // 2. Neck
                    float torsoCenterY = recipe.Height * 0.55f; 
                    float neckStart = torsoCenterY + (recipe.Height * 0.2f);
                    float dNeck = SdfPrimitives.SdVerticalCapsule(p - new Vector3(0, neckStart, 0), recipe.Height - neckStart, recipe.NeckThickness);

                    // 3. Torso Complex
                    // Re-calc for coloring
                    float chestY = recipe.Height * 0.7f;
                    float dChest = SdfPrimitives.SdRoundBox(p - new Vector3(0, chestY, 0), new Vector3(recipe.TorsoWidth * 0.5f, recipe.Height * 0.12f, recipe.TorsoWidth * 0.25f), recipe.MuscleTone);

                    float waistY = recipe.Height * 0.55f;
                    float dWaist = SdfPrimitives.SdVerticalCapsule(p - new Vector3(0, waistY, 0), recipe.Height * 0.1f, recipe.WaistWidth * 0.4f); 

                    float hipsY = recipe.Height * 0.45f;
                    float dHips = SdfPrimitives.SdRoundBox(p - new Vector3(0, hipsY, 0), new Vector3(recipe.HipWidth * 0.5f, recipe.Height * 0.08f, recipe.HipWidth * 0.25f), recipe.MuscleTone);
                    
                    // Glutes
                    float gluteY = hipsY; 
                    float gluteZ = -recipe.HipWidth * 0.25f; 
                    float gluteRadius = recipe.HipWidth * 0.35f;
                    Vector3 lGlutePos = new Vector3(-recipe.HipWidth * 0.25f, gluteY, gluteZ);
                    Vector3 rGlutePos = new Vector3(recipe.HipWidth * 0.25f, gluteY, gluteZ);
                    float dGlutes = SdfOperations.Union(
                        SdfPrimitives.SdSphere(p - lGlutePos, gluteRadius),
                        SdfPrimitives.SdSphere(p - rGlutePos, gluteRadius)
                    );
                    
                    // Blend for distance check? No, check individual for coloring
                    // But effectively we want the union surface.

                    // 4. Arms (Calculate Shoulders here first)
                    Vector3 leftShoulderPos = new Vector3(-recipe.ShoulderWidth / 2, recipe.Height * 0.82f, 0); 
                    Vector3 rightShoulderPos = new Vector3(recipe.ShoulderWidth / 2, recipe.Height * 0.82f, 0);
                    float shoulderRadius = recipe.ArmThickness * 1.5f; 
                    float dShoulders = MathF.Min(
                        SdfPrimitives.SdSphere(p - leftShoulderPos, shoulderRadius),
                        SdfPrimitives.SdSphere(p - rightShoulderPos, shoulderRadius)
                    );

                    // Trapezius (Slope from neck base to shoulders)
                    Vector3 neckBase = new Vector3(0, recipe.Height * 0.78f, -recipe.NeckThickness * 0.5f); 
                    float dTraps = SdfOperations.Union(
                        SdfPrimitives.SdCapsule(p, neckBase, leftShoulderPos + new Vector3(0.05f, 0.05f, 0), recipe.NeckThickness * 0.8f),
                        SdfPrimitives.SdCapsule(p, neckBase, rightShoulderPos + new Vector3(-0.05f, 0.05f, 0), recipe.NeckThickness * 0.8f)
                    );
                    
                    float armSpread = 0.45f;
                    float armLen = recipe.ArmLength;
                    Vector3 lElbowPos = leftShoulderPos + new Vector3(-armLen * 0.5f * armSpread, -armLen * 0.5f * 0.9f, 0);
                    Vector3 rElbowPos = rightShoulderPos + new Vector3(armLen * 0.5f * armSpread, -armLen * 0.5f * 0.9f, 0);
                    Vector3 lHandPos = leftShoulderPos + new Vector3(-armLen * armSpread, -armLen * 0.9f, 0);
                    Vector3 rHandPos = rightShoulderPos + new Vector3(armLen * armSpread, -armLen * 0.9f, 0);

                    float dUpperArms = MathF.Min(
                        SdfPrimitives.SdCapsule(p, leftShoulderPos, lElbowPos, recipe.ArmThickness),
                        SdfPrimitives.SdCapsule(p, rightShoulderPos, rElbowPos, recipe.ArmThickness)
                    );
                    float dForearms = MathF.Min(
                        SdfPrimitives.SdCapsule(p, lElbowPos, lHandPos, recipe.ArmThickness * 0.8f),
                        SdfPrimitives.SdCapsule(p, rElbowPos, rHandPos, recipe.ArmThickness * 0.8f)
                    );

                    // 5. Legs
                    float hipY = recipe.Height * 0.45f;
                    float legSpacing = recipe.TorsoWidth * 0.45f;
                    Vector3 lHipPos = new Vector3(-legSpacing, hipY, 0);
                    Vector3 rHipPos = new Vector3(legSpacing, hipY, 0);
                    Vector3 lKneePos = lHipPos - new Vector3(0, recipe.LegLength * 0.5f, 0);
                    Vector3 rKneePos = rHipPos - new Vector3(0, recipe.LegLength * 0.5f, 0);
                    Vector3 lAnklePos = lHipPos - new Vector3(0, recipe.LegLength, 0);
                    Vector3 rAnklePos = rHipPos - new Vector3(0, recipe.LegLength, 0);

                    float dThighs = MathF.Min(
                        SdfPrimitives.SdCapsule(p, lHipPos, lKneePos, recipe.LegThickness),
                        SdfPrimitives.SdCapsule(p, rHipPos, rKneePos, recipe.LegThickness)
                    );
                    float dCalves = MathF.Min(
                        SdfPrimitives.SdCapsule(p, lKneePos, lAnklePos, recipe.LegThickness * 0.8f),
                        SdfPrimitives.SdCapsule(p, rKneePos, rAnklePos, recipe.LegThickness * 0.8f)
                    );

                    // Determine closest part
                    float minD = dHead;
                    Vector4 color = new Vector4(1.0f, 0.0f, 1.0f, 1.0f); // Head = Magenta

                    if (dNeck < minD) { minD = dNeck; color = new Vector4(0.0f, 1.0f, 1.0f, 1.0f); } // Neck = Cyan
                    if (dChest < minD) { minD = dChest; color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f); } // Chest = Red
                    if (dWaist < minD) { minD = dWaist; color = new Vector4(1.0f, 0.4f, 0.4f, 1.0f); } // Waist = Pinkish
                    if (dHips < minD) { minD = dHips; color = new Vector4(0.5f, 0.0f, 0.5f, 1.0f); } // Hips = Purple
                    
                    if (dTraps < minD) { minD = dTraps; color = new Vector4(0.0f, 0.5f, 0.5f, 1.0f); } // Traps = Teal
                    if (dGlutes < minD) { minD = dGlutes; color = new Vector4(0.3f, 0.0f, 0.3f, 1.0f); } // Glutes = Dark Purple

                    if (dShoulders < minD) { minD = dShoulders; color = new Vector4(1.0f, 0.5f, 0.0f, 1.0f); } // Shoulders = Orange
                    
                    if (dUpperArms < minD) { minD = dUpperArms; color = new Vector4(1.0f, 1.0f, 0.0f, 1.0f); } // Upper Arms = Yellow
                    if (dForearms < minD) { minD = dForearms; color = new Vector4(1.0f, 1.0f, 0.6f, 1.0f); } // Forearms = Pale Yellow
                    
                    if (dThighs < minD) { minD = dThighs; color = new Vector4(0.0f, 0.5f, 0.0f, 1.0f); } // Thighs = Dark Green
                    if (dCalves < minD) { minD = dCalves; color = new Vector4(0.4f, 1.0f, 0.4f, 1.0f); } // Calves = Light Green

                    // Eye Logic Override
                    float headH = recipe.Height;
                    // Simple Box-check for eyes
                    if (p.Y > headH - recipe.HeadSize * 0.3f && p.Y < headH + recipe.HeadSize * 0.3f)
                    {
                        if (p.Z < -recipe.HeadSize * 0.4f) // Front face (Flipped to -Z)
                        {
                            if (MathF.Abs(p.X) > recipe.HeadSize * 0.3f && MathF.Abs(p.X) < recipe.HeadSize * 0.8f)
                            {
                                return new Vector4(0.0f, 0.0f, 1.0f, 1.0f); // Blue Eyes
                            }
                        }
                    }

                    return color;
                };

                return MarchingCubes.Generate(humanoidSdf, min, max, res, res, res, smooth, colorFunc);
            });
        }
    }
}

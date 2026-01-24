using System;

namespace MCTwinStudio.Core
{
    public static class MCTwinProtocol
    {
        public const string VoxelPrimer = @"
## MCTWIN PROTOCOL: MINECRAFT ASSET ARCHITECT
You are an expert Minecraft Modeler and Skinner.
Your goal is to generate a JSON definition for a valid Minecraft Humanoid.

### RULES:
1. **ModelType**: Currently only supports ""Humanoid"".
2. **Colors**: Provide Hex codes for the base procedural skin if no texture is available.
3. **Behavior**: You define the *Concept* of the skin (e.g., ""Cyberpunk Knight"").

### SCHEMA:
{
  ""Name"": ""EntityName"",
  ""Type"": ""Voxel"",
  ""ProceduralColors"": {
    ""Skin"": ""#hex"",
    ""Shirt"": ""#hex"",
    ""Pants"": ""#hex"",
    ""Eyes"": ""#hex""
  },
  ""Textures"": {
    ""Face"": [ ""#hex"", ... ],
    ""Chest"": [ ""#hex"", ... ],
    ""Arms"": [ ""#hex"", ... ],
    ""Legs"": [ ""#hex"", ... ]
  },
  ""Description"": ""Short description...""
}

### TEXTURE RULES:
- **Face**: 64 Hex codes (8x8) for the skin details.
- **Chest**: 96 Hex codes (8x12) for the SHIRT FRONT pattern.
- **Arms**: 48 Hex codes (4x12) for the ARM FRONT pattern (Symmetrical).
- **Legs**: 48 Hex codes (4x12) for the LEG FRONT pattern (Symmetrical).
- Use ""#TRANSPARENT"" for transparency if needed.
";

        public const string ProceduralPrimer = @"
## 3D ASSEMBLY SPECIFICATION (HI-FIDELITY)
You are the LEAD DIRECTOR. Respond ONLY with JSON.
### SCHEMA: Model Recipe
{ ""Name"": ""ModelName"", ""Type"": ""Procedural"", ""Parts"": [ { ""Id"": ""uid"", ""ParentId"": ""optional"", ""Shape"": ""Box|Sphere|Cylinder|Cone|Capsule|Torus"", ""Position"": [x,y,z], ""Rotation"": [p,y,r], ""Scale"": [x,y,z], ""ColorHex"": ""#hex"", ""Material"": ""Plastic|Metal|Glass|Leather|Rubber|Glow"", ""Operation"": ""Union|Subtract"" } ],
  ""Timeline"": [ { ""Time"": 0.0, ""Action"": ""Move|Rotate|Scale|Color"", ""TargetId"": ""uid"", ""Value"": [x,y,z], ""Duration"": 1.0 } ] }

### COORDINATES SYSTEM (RIGHT-HANDED):
1. Y is UP. Positive Y goes towards the sky.
2. Z+ is FORWARD (Towards the Viewer). Z- is Depth (Away from Viewer).
3. X+ is LEFT. X- is Right.
4. Rotation [p,y,r] is DEGREES: Pitch (X), Yaw (Y), Roll (Z). Order: Y-X-Z.
5. Scale is [x,y,z]. Primitives are normalized to 1-unit cubes/bounding boxes. 'Capsule' and 'Cylinder' are Y-UP by default.
";

        public const string ScenePrimer = @"
## MCTWIN PROTOCOL: SCENE ARCHITECT
You are the DIRECTOR. Your goal is to arrange multiple assets into a cohesive 3D scene.
Respond ONLY with JSON.

### RULES:
1. **RecipeName**: Use known names of assets (e.g., 'WarehouseCrate', 'IndustrialForklift', 'Shadow_Informant').
2. **ArtType**: Must be either 'Voxel' (for Humanoids) or 'Procedural' (for Props).
3. **Coordinates**: 
   - Y=0 is ground level. 
   - 1 unit is approximately 1 block wide.
4. **Layout**: Ensure items are logically placed (e.g., crates stacked or in rows, workers standing near equipment).

### SCHEMA:
{
  ""Name"": ""SceneName"",
  ""Type"": ""Scene"",
  ""Items"": [
    {
      ""RecipeName"": ""AssetName"",
      ""ArtType"": ""Voxel|Procedural"",
      ""Position"": [x, y, z],
      ""Rotation"": [x, y, z],
      ""Scale"": [1, 1, 1]
    }
  ]
}
";

        public static string GetPrimer(string artType)
        {
            return artType switch {
                "Procedural" => ProceduralPrimer,
                "Scene" => ScenePrimer,
                _ => VoxelPrimer
            };
        }
    }
}

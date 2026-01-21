using System.Collections.Generic;

namespace MCTwinStudio.Core.Models
{
    public class HumanoidModel : BaseModel
    {
        public HumanoidModel()
        {
            ModelType = "Humanoid";
        }

        public string SkinToneHex { get; set; } = "#F0C0A0";
        public string ShirtHex { get; set; } = "#00AAFF";
        public string PantsHex { get; set; } = "#0000AA";
        public string EyeHex { get; set; } = "#FFFFFF";
        public string[]? FacePixels { get; set; } = null;
        public string[]? ChestPixels { get; set; } = null;
        public string[]? ArmPixels { get; set; } = null; // Single array applied to both arms
        public string[]? LegPixels { get; set; } = null; // Single array applied to both legs
        
        // Legacy/Optional Hat Support
        public string[]? HatPixels { get; set; } = null;
        public bool ShowHat { get; set; } = false;

        public void GenerateSkin()
        {
            var gen = new Services.SkinGenerator();
            // Pass all hybrid regions
            var bmp = gen.Generate(
                SkinToneHex, ShirtHex, PantsHex, EyeHex, 
                FacePixels, HatPixels, 
                ChestPixels, ArmPixels, LegPixels
            );
            
            using (var ms = new System.IO.MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                SkinBase64 = "data:image/png;base64," + System.Convert.ToBase64String(ms.ToArray());
            }
        }

        public override List<ModelPart> GetParts()
        {
            if (string.IsNullOrEmpty(SkinBase64)) GenerateSkin();
            
            var parts = new List<ModelPart>();
            
            // OFFSETS must match SkinGenerator layout.
            // Dimensions allow calculating inner faces.

            // 1. HEAD (Region: 0,0)
            parts.Add(new ModelPart {
                Name = "Head",
                Dimensions = new[] { 8, 8, 8 },
                Offset = new[] { 0f, 28f, 0f },
                TextureOffset = new[] { 0, 0 },
                HexColor = SkinToneHex
            });
            
            // 1b. HAT LAYER (Region: 32,0)
            if (ShowHat)
            {
                parts.Add(new ModelPart {
                    Name = "Hat",
                    Dimensions = new[] { 9, 9, 9 }, // Inflated geometry
                    TextureDimensions = new[] { 8, 8, 8 }, // Classic texture mapping
                    Offset = new[] { 0f, 28f, 0f },
                    TextureOffset = new[] { 32, 0 },
                    HexColor = "#FFFFFF" 
                });
            }

            // 2. BODY (Region: 16,16)
            parts.Add(new ModelPart {
                Name = "Body",
                Dimensions = new[] { 8, 12, 4 },
                Offset = new[] { 0f, 18f, 0f },
                TextureOffset = new[] { 16, 16 },
                HexColor = ShirtHex
            });

            // 3. RIGHT LEFT (Region: 0,16)
            parts.Add(new ModelPart {
                Name = "RightLeg",
                Dimensions = new[] { 4, 12, 4 },
                Offset = new[] { 2f, 6f, 0f }, 
                TextureOffset = new[] { 0, 16 },
                HexColor = PantsHex
            });

            // 4. LEFT LEG (Region: 16,48)
            parts.Add(new ModelPart {
                Name = "LeftLeg",
                Dimensions = new[] { 4, 12, 4 },
                Offset = new[] { -2f, 6f, 0f },
                TextureOffset = new[] { 16, 48 },
                HexColor = PantsHex
            });

            // 5. RIGHT ARM (Region: 40,16)
            parts.Add(new ModelPart {
                Name = "RightArm",
                Dimensions = new[] { 4, 12, 4 },
                Offset = new[] { 6f, 18f, 0f }, 
                TextureOffset = new[] { 40, 16 },
                HexColor = ShirtHex
            });

            // 6. LEFT ARM (Region: 32,48)
            parts.Add(new ModelPart {
                Name = "LeftArm",
                Dimensions = new[] { 4, 12, 4 },
                Offset = new[] { -6f, 18f, 0f },
                TextureOffset = new[] { 32, 48 },
                HexColor = ShirtHex
            });

            return parts;
        }
    }
}

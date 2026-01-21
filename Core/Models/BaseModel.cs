using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MCTwinStudio.Core.Models
{
    // The abstract base for all Minecraft-style entities
    public abstract class BaseModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Unknown Entity";
        public string ModelType { get; set; } = "Generic";

        // Base transform in world
        public float[] Position { get; set; } = new float[] { 0, 0, 0 };
        public float[] Rotation { get; set; } = new float[] { 0, 0, 0 };

        // The raw skin data (Base64 PNG or purely procedural colors)
        // If null, renderer uses procedural fallback colors
        public string? SkinBase64 { get; set; } 

        public abstract List<ModelPart> GetParts();
    }

    public class ModelPart
    {
        public string Name { get; set; } = "Part";
        public string ParentName { get; set; } = ""; // Empty if root
        
        // Dimensions in Minecraft Pixels (1 unit = 1 pixel usually, or scaled)
        // Standard Steve Head is 8x8x8
        public int[] Dimensions { get; set; } = new int[] { 8, 8, 8 };
        
        // Offset from Parent's Pivot
        public float[] Offset { get; set; } = new float[] { 0, 0, 0 };
        
        // Rotation Pivot Point (relative to Offset)
        public float[] Pivot { get; set; } = new float[] { 0, 0, 0 };
        
        // Initial Rotation
        public float[] Rotation { get; set; } = new float[] { 0, 0, 0 };

        // UV Coordinate origin (standard Minecraft layout)
        public int[] TextureOffset { get; set; } = new int[] { 0, 0 };
        
        // Procedural Fallback Color (if no skin)
        public string HexColor { get; set; } = "#FFFFFF";
        
        // Optional override for UV mapping size (defaults to Dimensions if null)
        public int[]? TextureDimensions { get; set; }
    }
}

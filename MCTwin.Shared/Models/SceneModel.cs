using System;
using System.Collections.Generic;

namespace MCTwinStudio.Core.Models
{
    public class SceneItem
    {
        public string RecipeName { get; set; } = string.Empty;
        public string ArtType { get; set; } = "Procedural"; // "Voxel" or "Procedural"
        public float[] Position { get; set; } = new float[3]; // [x, y, z]
        public float[] Rotation { get; set; } = new float[3]; // [x, y, z] in degrees
        public float[] Scale { get; set; } = new float[3] { 1, 1, 1 };
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    }

    public class SceneModel
    {
        public string Name { get; set; } = "New Scene";
        public List<SceneItem> Items { get; set; } = new List<SceneItem>();
    }
}

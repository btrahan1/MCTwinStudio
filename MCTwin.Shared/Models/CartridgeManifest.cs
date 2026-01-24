using System;
using System.Collections.Generic;

namespace MCTwinStudio.Models
{
    public class CartridgeManifest
    {
        public string GameId { get; set; } = "new_game";
        public string Title { get; set; } = "Untitled Game";
        public string Description { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = "MCTwin AI";
        
        public string StartupScene { get; set; } = "main.scene.json";
        
        public CartridgeAssetBundle Assets { get; set; } = new CartridgeAssetBundle();
        
        public Dictionary<string, string> Config { get; set; } = new Dictionary<string, string>();
    }

    public class CartridgeAssetBundle
    {
        public List<string> Actors { get; set; } = new List<string>();
        public List<string> Props { get; set; } = new List<string>();
        public List<string> Scenes { get; set; } = new List<string>();
    }
}

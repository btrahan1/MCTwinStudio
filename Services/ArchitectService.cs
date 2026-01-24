using System;
using System.Text.Json;
using System.Collections.Generic;
using MCTwinStudio.Core;
using MCTwinStudio.Core.Models;
using MCTwinStudio.Controls;

namespace MCTwinStudio.Services
{
    public class ArchitectService
    {
        private readonly BrainPane _brain;
        private readonly AssetService _assetService;
        private readonly SceneService _sceneService;

        public event Action<BaseModel>? OnAssetDelivered;
        public event Action<string>? OnSceneDelivered;
        public event Action<string>? OnStatusUpdate;
        
        private readonly Queue<GenerationRequest> _requestQueue = new Queue<GenerationRequest>();
        private bool _isProcessingQueue = false;

        public ArchitectService(BrainPane brain, AssetService assetService, SceneService sceneService)
        {
            _brain = brain;
            _assetService = assetService;
            _sceneService = sceneService;
            
            _brain.JsonResponseReceived += HandleBrainResponse;
        }

        public void Forge(string prompt, string mode, VoxelOptions? voxelOptions = null)
        {
            string primer = MCTwinProtocol.GetPrimer(mode);
            string fullPrompt = primer;

            if (mode == "Voxel" && voxelOptions != null)
            {
                var regions = new List<string>();
                if (voxelOptions.GenerateFace) regions.Add("Face");
                if (voxelOptions.GenerateChest) regions.Add("Chest");
                if (voxelOptions.GenerateArms) regions.Add("Arms");
                if (voxelOptions.GenerateLegs) regions.Add("Legs");

                if (regions.Count > 0) {
                    fullPrompt += $"\n\n[MANDATORY GENERATION RULES]\nGenerate pixel textures ONLY for these keys: {string.Join(", ", regions)}.\nOmit keys for unlisted regions.";
                } else {
                    fullPrompt += "\n\n[MANDATORY GENERATION RULES]\nDo NOT generate any pixel textures (Face, Chest, Arms, Legs). Only provide procedural hex colors.";
                }
            }

            if (mode == "Scene")
            {
                var recipes = _assetService.ListAvailableRecipes();
                fullPrompt += "\n\n[AVAILABLE ASSETS (USE THESE IF POSSIBLE)]\n" + string.Join(", ", recipes);
                fullPrompt += "\n\nIf the user asks for something NOT in this list, give it a unique name and I will generate it later.";
            }

            fullPrompt += "\n\n### USER REQUEST:\n" + prompt;
            OnStatusUpdate?.Invoke("Sending request to AI Brain...");
            _brain.SendPrompt(fullPrompt);
        }

        private void HandleBrainResponse(object? sender, string json)
        {
            _isProcessingQueue = false;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string type = root.TryGetProperty("Type", out var tProp) ? tProp.GetString() ?? "Voxel" : "Voxel";

                if (type == "Scene")
                {
                    _sceneService.SaveScene("ai_delivery", json);
                    OnStatusUpdate?.Invoke("AI Scene Delivery Saved.");
                    
                    // Recursive Logic: Check for missing assets
                    CheckForMissingAssets(root);
                    
                    OnSceneDelivered?.Invoke(json);
                }
                else if (type == "Procedural")
                {
                    var model = new ProceduralModel { RawRecipeJson = json };
                    model.Name = root.TryGetProperty("Name", out var pn) ? pn.GetString() ?? "Prop" : "Prop";
                    _assetService.SaveAsset(model.Name, json, AssetService.AssetCategory.Prop);
                    OnAssetDelivered?.Invoke(model);
                }
                else
                {
                    // Voxel
                    var human = MapToHumanoid(root);
                    _assetService.SaveAsset(human.Name, json, AssetService.AssetCategory.Actor);
                    OnAssetDelivered?.Invoke(human);
                }
            }
            catch (Exception ex)
            {
                OnStatusUpdate?.Invoke("Error parsing AI response: " + ex.Message);
            }
            finally
            {
                ProcessNextInQueue();
            }
        }

        private void CheckForMissingAssets(JsonElement root)
        {
            if (!root.TryGetProperty("Items", out var items) || items.ValueKind != JsonValueKind.Array) return;

            var missing = new List<string>();
            foreach (var item in items.EnumerateArray())
            {
                string recipeName = item.TryGetProperty("RecipeName", out var rn) ? rn.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(recipeName)) continue;

                if (string.IsNullOrEmpty(_assetService.GetBestMatch(recipeName)))
                {
                    if (!missing.Contains(recipeName)) missing.Add(recipeName);
                }
            }

            if (missing.Count > 0)
            {
                OnStatusUpdate?.Invoke($"Missing {missing.Count} assets. Queuing generation...");
                foreach (var name in missing)
                {
                    // For now, assume anything in a scene is a PROP (Procedural) unless it looks like a person
                    string mode = (name.ToLower().Contains("npc") || name.ToLower().Contains("steve") || name.ToLower().Contains("char")) ? "Voxel" : "Procedural";
                    _requestQueue.Enqueue(new GenerationRequest { Prompt = $"Generate the missing asset: {name}", Mode = mode });
                }
            }
        }

        private void ProcessNextInQueue()
        {
            if (_requestQueue.Count == 0 || _isProcessingQueue)
            {
                _isProcessingQueue = false;
                return;
            }

            _isProcessingQueue = true;
            var req = _requestQueue.Dequeue();
            OnStatusUpdate?.Invoke($"Recursive Step: {req.Prompt}");
            Forge(req.Prompt, req.Mode);
        }

        private class GenerationRequest
        {
            public string Prompt { get; set; } = "";
            public string Mode { get; set; } = "Procedural";
        }

        private HumanoidModel MapToHumanoid(JsonElement root)
        {
            var h = new HumanoidModel();
            h.Name = root.TryGetProperty("Name", out var n) ? n.GetString() ?? "Entity" : "Entity";
            
            if (root.TryGetProperty("ProceduralColors", out var colors))
            {
                h.SkinToneHex = GetColor(colors, "Skin", h.SkinToneHex);
                h.ShirtHex = GetColor(colors, "Shirt", h.ShirtHex);
                h.PantsHex = GetColor(colors, "Pants", h.PantsHex);
                h.EyeHex = GetColor(colors, "Eyes", h.EyeHex);
            }

            if (root.TryGetProperty("Textures", out var tex))
            {
                h.FacePixels = GetPixels(tex, "Face");
                h.HatPixels = GetPixels(tex, "Hat");
                if (h.HatPixels != null) h.ShowHat = true;
                h.ChestPixels = GetPixels(tex, "Chest");
                h.ArmPixels = GetPixels(tex, "Arms");
                h.LegPixels = GetPixels(tex, "Legs");
            }

            h.GenerateSkin();
            return h;
        }

        private string GetColor(JsonElement el, string prop, string def) => el.TryGetProperty(prop, out var p) ? p.GetString() ?? def : def;

        private string[]? GetPixels(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var px) && px.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var p in px.EnumerateArray()) list.Add(p.GetString() ?? "#000000");
                return list.ToArray();
            }
            return null;
        }
    }

    public class VoxelOptions
    {
        public bool GenerateFace { get; set; }
        public bool GenerateChest { get; set; }
        public bool GenerateArms { get; set; }
        public bool GenerateLegs { get; set; }
    }
}

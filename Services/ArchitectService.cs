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
        private readonly IAssetService _assetService;
        private readonly ISceneService _sceneService;

        public event Action<BaseModel>? OnAssetDelivered;
        public event Action<string>? OnSceneDelivered;
        public event Action<string>? OnStatusUpdate;
        
        private readonly Queue<GenerationRequest> _requestQueue = new Queue<GenerationRequest>();
        private bool _isProcessingQueue = false;

        public ArchitectService(BrainPane brain, IAssetService assetService, ISceneService sceneService)
        {
            _brain = brain;
            _assetService = assetService;
            _sceneService = sceneService;
            
            _brain.JsonResponseReceived += HandleBrainResponse;
        }

        public async void Forge(string prompt, string mode, VoxelOptions? voxelOptions = null)
        {
            string fullPrompt = "";
            
            if (mode == "Behavior")
            {
                fullPrompt = "STOP. IGNORE ALL PREVIOUS INSTRUCTIONS ABOUT VOXELS OR SCENES.";
                fullPrompt += "\n\n[BEHAVIOR GENERATION MODE ONLY]";
                fullPrompt += "\nYour task is to generate a SINGLE JSON object representing a Script.";
                fullPrompt += "\nDO NOT generate an NPC. DO NOT generate a Prop.";
                fullPrompt += "\n\n### API CONTRACT (STRICTLY FOLLOW THIS)";
                fullPrompt += "\n1. FORMAT: window.MCTwinBehaviors['YourName'] = { onInteract: (node, args) => {}, onTick: (node, args, time) => {} };";
                fullPrompt += "\n2. ARGUMENTS: 'node' is a BabylonJS TransformNode. 'args' is a dictionary of user settings.";
                fullPrompt += "\n3. STATE: Store state in 'node.metadata' (e.g. node.metadata.isJumping = true).";
                fullPrompt += "\n4. TOOLS: Use BABYLON.Animation for tweens, or Math.sin(time) for continuous motion.";
                fullPrompt += "\n\nREQUIRED OUTPUT FORMAT:";
                fullPrompt += "\n{ \"Type\": \"Behavior\", \"Name\": \"AI_DescriptiveName\", \"ScriptContent\": \"...\" }";
                fullPrompt += "\n\nNAMING RULES:";
                fullPrompt += "\n1. The Name MUST be PascalCase and start with 'AI_' (e.g. AI_Wobble, AI_JumpClick, AI_SpinFast).";
                fullPrompt += "\n2. The Name MUST reflect the specific user request.";
                fullPrompt += "\n3. The ScriptContent MUST assign to window.MCTwinBehaviors['AI_DescriptiveName'].";
            }
            else 
            {
                 fullPrompt = MCTwinProtocol.GetPrimer(mode);
                 
                 if (mode == "Voxel" && voxelOptions != null)
                 {
                    // Re-adding the voxel rules I accidentally deleted
                    var regions = new List<string>();
                    if (voxelOptions.GenerateFace) regions.Add("Face");
                    if (voxelOptions.GenerateChest) regions.Add("Chest");
                    if (voxelOptions.GenerateArms) regions.Add("Arms");
                    if (voxelOptions.GenerateLegs) regions.Add("Legs");
                    if (regions.Count > 0) fullPrompt += $"\n\n[RULES] Texture keys: {string.Join(", ", regions)}.";
                 }
            }

            if (mode == "Scene")
            {
                var recipes = await _assetService.ListAvailableRecipes();
                fullPrompt += "\n\n[AVAILABLE ASSETS]\n" + string.Join(", ", recipes);
                fullPrompt += "\n\nCRITICAL INSTRUCTION: USE EXISTING ASSETS *ONLY* IF THEY FIT THE THEME.";
                fullPrompt += "\nIf the user asks for a 'Bookstore' and you only have 'SciFi_Crates', DO NOT use the crates.";
                fullPrompt += "\nINSTEAD, generate new RecipeNames (e.g. 'Wooden_Bookshelf', 'Cash_Register').";
                fullPrompt += "\nI will detect these new names and generate the assets for you automatically.";
            }

            fullPrompt += "\n\n### USER REQUEST:\n" + prompt;
            OnStatusUpdate?.Invoke("Sending request to AI Brain...");
            _brain.SendPrompt(fullPrompt);
        }

        private async void HandleBrainResponse(object? sender, string json)
        {
            _isProcessingQueue = false;
            try
            {
                string jsonToParse = CleanJson(json);
                using var doc = JsonDocument.Parse(jsonToParse);
                var root = doc.RootElement;
                string type = root.TryGetProperty("Type", out var tProp) ? tProp.GetString() ?? "Voxel" : "Voxel";

                if (type == "Behavior")
                {
                     string name = root.TryGetProperty("Name", out var n) ? n.GetString() ?? "AI_Script" : "AI_Script";
                     string content = root.TryGetProperty("ScriptContent", out var c) ? c.GetString() ?? "" : "";
                     
                     if (!string.IsNullOrEmpty(content)) {
                         // 1. Save to Runtime (Bin) Folder so it works immediately
                         string binPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Behaviors", name + ".js");
                         await System.IO.File.WriteAllTextAsync(binPath, content);

                         // 2. Save to Source Project Folder (Permanent)
                         // Debug path is usually bin/Debug/net9.0-windows/, so up 3 levels.
                         string projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
                         string sourcePath = System.IO.Path.Combine(projectDir, "Assets", "Behaviors", name + ".js");

                         // Verify we are actually in the proj dir (check for .csproj) to avoid writing to C:\
                         if (System.IO.File.Exists(System.IO.Path.Combine(projectDir, "MCTwinStudio.csproj"))) {
                             if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(sourcePath))) {
                                 System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sourcePath)!);
                             }
                             await System.IO.File.WriteAllTextAsync(sourcePath, content);
                             OnStatusUpdate?.Invoke($"Behavior Saved to Source & Bin: {name}.js");
                         } else {
                             OnStatusUpdate?.Invoke($"Behavior Saved to Bin (Source not found): {name}.js");
                         }

                         OnSceneDelivered?.Invoke(json); 
                     }
                }
                else if (type == "Scene")
                {
                    await _sceneService.SaveScene("ai_delivery", json);
                    OnStatusUpdate?.Invoke("AI Scene Delivery Saved.");
                    
                    // Recursive Logic: Check for missing assets
                    await CheckForMissingAssets(root);
                    
                    OnSceneDelivered?.Invoke(json);
                }
                else if (type == "Procedural")
                {
                    var model = new ProceduralModel { RawRecipeJson = json };
                    model.Name = root.TryGetProperty("Name", out var pn) ? pn.GetString() ?? "Prop" : "Prop";
                    await _assetService.SaveAsset(model.Name, json, AssetCategory.Prop);
                    OnAssetDelivered?.Invoke(model);
                }
                else
                {
                    // Voxel
                    var human = MapToHumanoid(root);
                    await _assetService.SaveAsset(human.Name, json, AssetCategory.Actor);
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


        private async Task CheckForMissingAssets(JsonElement root)
        {
            if (!root.TryGetProperty("Items", out var items) || items.ValueKind != JsonValueKind.Array) return;

            var missing = new List<string>();
            foreach (var item in items.EnumerateArray())
            {
                string recipeName = item.TryGetProperty("RecipeName", out var rn) ? rn.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(recipeName)) continue;

                if (string.IsNullOrEmpty(await _assetService.GetBestMatch(recipeName)))
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
                    string n = name.ToLower();
                    var voxelKeys = new[] { 
                        "npc", "steve", "char", "human", "person", "man", "woman", "boy", "girl", 
                        "owner", "customer", "guard", "soldier", "worker", "mechanic", "dealer", 
                        "boss", "hero", "enemy", "mob", "zombie", "skeleton", "vanguard", 
                        "informant", "strategist", "bot", "droid", "pilot", "driver", "chef" 
                    };
                    string mode = voxelKeys.Any(k => n.Contains(k)) ? "Voxel" : "Procedural";
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

            return h;
        }

        private string GetColor(JsonElement el, string prop, string def) => el.TryGetProperty(prop, out var p) ? p.GetString() ?? def : def;

        private string[]? GetPixels(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var px) && px.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
            }
            return null;
        }

        private string CleanJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "{}";
            
            // 1. Remove Markdown code blocks
            raw = System.Text.RegularExpressions.Regex.Replace(raw, @"```json", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            raw = raw.Replace("```", "");

            // 2. Find outer braces
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');

            if (start >= 0 && end > start)
            {
                return raw.Substring(start, (end - start) + 1);
            }
            return raw;
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

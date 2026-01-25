using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCTwinStudio.Services
{
    public class CartridgeExporter
    {
        private readonly string _enginePath;
        private readonly string _behaviorPath;

        private const string Template = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <title>MCTwin Cartridge // {CARTRIDGE_NAME}</title>
    <style>
        html, body { width: 100%; height: 100%; margin: 0; padding: 0; overflow: hidden; background: #000; color: #00FF00; font-family: 'Courier New', monospace; }
        canvas { position: absolute; top: 0; left: 0; width: 100%; height: 100%; display: block; z-index: 1; }
        #loading { position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); text-align: center; pointer-events: none; z-index: 10; }
        .spinner { border: 4px solid rgba(0, 255, 0, 0.3); border-top: 4px solid #00FF00; border-radius: 50%; width: 40px; height: 40px; animation: spin 1s linear infinite; margin: 0 auto 20px auto; }
        @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
        
        #debugConsole { 
            display: none; /* Default hidden */
            position: absolute; 
            top: 0; 
            left: 0; 
            width: 50%; 
            max-height: 200px; 
            overflow-y: auto; 
            background: rgba(0,0,0,0.5); 
            z-index: 5; 
            pointer-events: none; 
            font-size: 10px;
            padding: 10px;
        }
    </style>
    <!-- BabylonJS (CDN for now, could be inlined later) -->
    <script src=""https://cdn.babylonjs.com/babylon.js""></script>
    <script src=""https://cdn.babylonjs.com/materialsLibrary/babylonjs.materials.min.js""></script>
</head>
<body>
    <div id=""loading"">
        <div class=""spinner""></div>
        <div>LOADING CARTRIDGE...</div>
        <div style=""font-size: 12px; margin-top: 10px; opacity: 0.7;"">{CARTRIDGE_NAME}</div>
    </div>
    <div id=""debugConsole""></div>
    <canvas id=""renderCanvas""></canvas>

    <!-- ENGINE LOGIC -->
    <script>
        {ENGINE_SCRIPT}
    </script>
    <!-- BEHAVIOR LIBRARY -->
    <script>
        {BEHAVIOR_SCRIPT}
    </script>

    <!-- CARTRIDGE DATA -->
    <script>
        (function() {
            const data = {CARTRIDGE_DATA};
            
            // Wait for engine to be ready
            const checkEngine = setInterval(() => {
                if (window.MCTwin && window.MCTwin.spawnRecipe) {
                    clearInterval(checkEngine);
                    document.getElementById('loading').style.display = 'none';
                    
                    console.log('Booting Cartridge: ' + (data.Name || ""Unnamed""));

                    setTimeout(() => {
                        // 1. Hydrate Asset Library
                        if (data.Assets) {
                            console.log(""Hydrating Asset Library: "" + Object.keys(data.Assets).length + "" items."");
                            window.CartridgeAssets = data.Assets;
                        }

                        // 2. Load Content
                        if (data.Scene && data.Scene.Items) {
                             data.Scene.Items.forEach(item => {
                                 const recipeData = window.CartridgeAssets ? window.CartridgeAssets[item.RecipeName] : null;
                                 if (recipeData) {
                                     window.MCTwin.spawnRecipe(recipeData, item.RecipeName, false, item);
                                 } else {
                                     console.log(""Missing asset data for: "" + item.RecipeName);
                                 }
                             });
                        } 
                        else if (data.Parts) {
                             if (data.Type === 'Voxel') window.MCTwin.renderModel(data);
                             else window.MCTwin.spawnRecipe(data, data.Name);
                        }
                        
                        // Flavor: Rotate logic
                        const root = scene.transformNodes.find(n => n.id === 'root' || n.id.startsWith('recipe_'));
                        if (root && !data.Scene) { 
                            scene.registerBeforeRender(() => root.rotation.y += 0.005);
                        }

                        // --- BEHAVIOR SYSTEM RUNNER ---
                        const runningBehaviors = [];

                        // A. Initialization & Collection
                        scene.transformNodes.forEach(node => {
                            if (node.metadata && node.metadata.tags && node.metadata.tags['Behavior']) {
                                const bName = node.metadata.tags['Behavior'];
                                const behavior = window.MCTwinBehaviors ? window.MCTwinBehaviors[bName] : null;
                                
                                if (behavior) {
                                    // Parse Args from Tags
                                    const args = { ...node.metadata.tags };
                                    
                                    // 1. Run onInit
                                    if (behavior.onInit) {
                                        behavior.onInit(node, args);
                                    }

                                    // 2. Register for Tick
                                    if (behavior.onTick) {
                                        runningBehaviors.push({ func: behavior.onTick, node: node, args: args });
                                    }

                                    // 3. Register for Interact (Attach to Node)
                                    if (behavior.onInteract) {
                                        node.metadata.onInteract = () => behavior.onInteract(node, args);
                                        node.isPickable = true; // Ensure raycast hits it
                                    }
                                } else {
                                    console.warn(""Behavior not found: "" + bName);
                                }
                            }
                        });
                        
                        // B. The Game Loop (onTick)
                        scene.registerBeforeRender(() => {
                            const time = performance.now() * 0.001;
                            runningBehaviors.forEach(b => {
                                b.func(b.node, b.args, time);
                            });
                        });

                        // C. Interaction Loop (Click Detection)
                        scene.onPointerObservable.add((pointerInfo) => {
                            if (pointerInfo.type === BABYLON.PointerEventTypes.POINTERDOWN) {
                                if (pointerInfo.pickInfo.hit && pointerInfo.pickInfo.pickedMesh) {
                                    let target = pointerInfo.pickInfo.pickedMesh;
                                    // Bubble up to find the behavior root (the recipe container)
                                    let p = target;
                                    while (p && !p.metadata?.onInteract && p.parent) {
                                        p = p.parent;
                                    }
                                    
                                    if (p && p.metadata && p.metadata.onInteract) {
                                        p.metadata.onInteract();
                                    }
                                }
                            }
                        });
                    }, 500);
                }
            }, 100);
        })();
    </script>
</body>
</html>";

        public CartridgeExporter(string webRootPath)
        {
            _enginePath = Path.Combine(webRootPath, "world.js");
            _behaviorPath = Path.Combine(webRootPath, "Assets", "behaviors.js");
        }

        public async Task<string> ExportAsync(string name, string jsonData, string outputPath)
        {
            // 1. Load Engine Script
            string engineScript = "";
            if (File.Exists(_enginePath))
            {
                engineScript = await File.ReadAllTextAsync(_enginePath);
            }
            else
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var candidates = new[] 
                {
                    Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\MCTwin.Web\wwwroot\world.js")),      
                    Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\MCTwin.Web\wwwroot\world.js")),   
                    Path.Combine(baseDir, "world.js")                                                     
                };

                foreach (var c in candidates) {
                    if (File.Exists(c)) { engineScript = await File.ReadAllTextAsync(c); break; }
                }

                if (string.IsNullOrEmpty(engineScript))
                {
                    var sbErr = new StringBuilder();
                    sbErr.AppendLine($"Engine script world.js not found at {_enginePath}");
                    sbErr.AppendLine("Checked candidates:");
                    foreach(var c in candidates) sbErr.AppendLine($"- {c}");
                    throw new FileNotFoundException(sbErr.ToString());
                }
            }
            
            // 2. Load Behavior Script
            string behaviors = "";
            if (File.Exists(_behaviorPath)) 
            {
                behaviors = await File.ReadAllTextAsync(_behaviorPath);
            }
            else 
            {
                 var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                 var candidates = new[] {
                     Path.Combine(baseDir, "Assets", "behaviors.js"),
                     Path.Combine(baseDir, "behaviors.js"),
                     Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\MCTwinStudio\Assets\behaviors.js"))
                 };
                 
                 foreach(var c in candidates) {
                     if (File.Exists(c)) { behaviors = await File.ReadAllTextAsync(c); break; }
                 }
                 
                 if (string.IsNullOrEmpty(behaviors)) {
                     behaviors = "console.error('MCTwin Studio: behaviors.js not found in Assets folder.');";
                 }
            }

            return Build(name, jsonData, engineScript, behaviors, outputPath);
        }

        private string Build(string name, string json, string engine, string behaviors, string path)
        {
            var sb = new StringBuilder(Template);
            sb.Replace("{CARTRIDGE_NAME}", name);
            sb.Replace("{ENGINE_SCRIPT}", engine);
            sb.Replace("{BEHAVIOR_SCRIPT}", behaviors);
            sb.Replace("{CARTRIDGE_DATA}", json);
            
            File.WriteAllText(path, sb.ToString());
            return path;
        }
    }
}

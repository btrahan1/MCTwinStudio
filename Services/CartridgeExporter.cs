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
        private const string Template = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <title>MCTwin Cartridge // {CARTRIDGE_NAME}</title>
    <style>
        html, body {{ width: 100%; height: 100%; margin: 0; padding: 0; overflow: hidden; background: #000; color: #00FF00; font-family: 'Courier New', monospace; }}
        canvas {{ position: absolute; top: 0; left: 0; width: 100%; height: 100%; display: block; z-index: 1; }}
        #loading {{ position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); text-align: center; pointer-events: none; z-index: 10; }}
        .spinner {{ border: 4px solid rgba(0, 255, 0, 0.3); border-top: 4px solid #00FF00; border-radius: 50%; width: 40px; height: 40px; animation: spin 1s linear infinite; margin: 0 auto 20px auto; }}
        @keyframes spin {{ 0% {{ transform: rotate(0deg); }} 100% {{ transform: rotate(360deg); }} }}
        
        #debugConsole {{ 
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
        }}
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

    <!-- CARTRIDGE DATA -->
    <script>
        (function() {{
            const data = {CARTRIDGE_DATA};
            
            // Wait for engine to be ready
            const checkEngine = setInterval(() => {{
                if (window.MCTwin && window.MCTwin.spawnRecipe) {{
                    clearInterval(checkEngine);
                    document.getElementById('loading').style.display = 'none';
                    
                    console.log('Booting Cartridge: ' + (data.Name || ""Unnamed""));

                    setTimeout(() => {{
                        // 1. Hydrate Asset Library
                        if (data.Assets) {{
                            console.log(""Hydrating Asset Library: "" + Object.keys(data.Assets).length + "" items."");
                            // We can store these in window.MCTwin.propRegistry or a new cache
                            // But better: when iterating items, if we need a recipe, we look here.
                            window.CartridgeAssets = data.Assets;
                        }}

                        // 2. Load Content
                        if (data.Scene && data.Scene.Items) {{
                             // It is a Full Scene
                             data.Scene.Items.forEach(item => {{
                                 // Try to find full recipe body in assets
                                 const recipeData = window.CartridgeAssets ? window.CartridgeAssets[item.RecipeName] : null;
                                 if (recipeData) {{
                                     // Spawn with geometry
                                     window.MCTwin.spawnRecipe(recipeData, item.RecipeName, false, item);
                                 }} else {{
                                     // Just a placeholder?
                                     console.log(""Missing asset data for: "" + item.RecipeName);
                                 }}
                             }});
                        }} 
                        else if (data.Parts) {{
                             // Single Voxel/Recipe
                             if (data.Type === 'Voxel') window.MCTwin.renderModel(data);
                             else window.MCTwin.spawnRecipe(data, data.Name);
                        }}
                        
                        // Flavor: Rotate logic
                        const root = scene.transformNodes.find(n => n.id === 'root' || n.id.startsWith('recipe_'));
                        if (root && !data.Scene) {{ // Only rotate if single item
                            scene.registerBeforeRender(() => root.rotation.y += 0.005);
                        }}

                        // Simulation: Heartbeat
                        scene.registerBeforeRender(() => {{
                            const time = performance.now() * 0.001;
                            scene.transformNodes.forEach(node => {{
                                if (node.metadata && node.metadata.tags && node.metadata.tags['Category'] === 'Heartbeat') {{
                                    const pulse = 1.0 + Math.sin(time * 5) * 0.2;
                                    node.scaling = new BABYLON.Vector3(pulse, pulse, pulse);
                                }}
                            }});
                        }});
                    }}, 500);
                }}
            }}, 100);
        }})();
    </script>
</body>
</html>";

        public CartridgeExporter(string webRootPath)
        {
            _enginePath = Path.Combine(webRootPath, "world.js");
        }

        public async Task<string> ExportAsync(string name, string jsonData, string outputPath)
        {
            if (!File.Exists(_enginePath))
            {
                // Fallback attempt: try to find it relative to the executable if not found
                // We typically run from bin/Debug/net9.0-windows
                // The source file is in MCTwin.Web/wwwroot/world.js
                // MCTwinStudio.csproj and MCTwin.Web folder are siblings in the source root.
                // So from bin/Debug/net9.0-windows -> Up 3 levels -> Root
                
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var candidates = new[] 
                {
                    Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\MCTwin.Web\wwwroot\world.js")),      // 3 levels up (Release/Debug)
                    Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\MCTwin.Web\wwwroot\world.js")),   // 4 levels up (Just in case)
                    Path.Combine(baseDir, "world.js")                                                     // In local dir
                };

                string? foundPath = null;
                foreach (var c in candidates) {
                    if (File.Exists(c)) { foundPath = c; break; }
                }

                if (foundPath != null)
                {
                    string engineScript = await File.ReadAllTextAsync(foundPath);
                    return Build(name, jsonData, engineScript, outputPath);
                }
                else 
                {
                    var sbErr = new StringBuilder();
                    sbErr.AppendLine($"Engine script world.js not found at {_enginePath}");
                    sbErr.AppendLine("Checked candidates:");
                    foreach(var c in candidates) sbErr.AppendLine($"- {c}");
                    throw new FileNotFoundException(sbErr.ToString());
                }
            }
            
            string script = await File.ReadAllTextAsync(_enginePath);
            return Build(name, jsonData, script, outputPath);
        }

        private string Build(string name, string json, string engine, string path)
        {
            var sb = new StringBuilder(Template);
            sb.Replace("{CARTRIDGE_NAME}", name);
            sb.Replace("{ENGINE_SCRIPT}", engine);
            sb.Replace("{CARTRIDGE_DATA}", json);
            
            File.WriteAllText(path, sb.ToString());
            return path;
        }
    }
}

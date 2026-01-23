using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Text.Json;
using MCTwinStudio.Core.Models;

namespace MCTwinStudio.Core
{
    /// <summary>
    /// Bridges C# to the JavaScript Scene logic in WebView2.
    /// Centralizes all ExecuteScriptAsync calls.
    /// </summary>
    public class SceneController
    {
        private readonly WebView2 _webView;

        public SceneController(WebView2 webView)
        {
            _webView = webView;
        }

        public async Task ClearAll()
        {
            if (_webView?.CoreWebView2 == null) return;
            await _webView.ExecuteScriptAsync("if(window.MCTwin && window.MCTwin.clearAll) window.MCTwin.clearAll();");
        }

        public async Task SetGizmoMode(string mode)
        {
            if (_webView?.CoreWebView2 == null) return;
            await _webView.ExecuteScriptAsync($"if(window.MCTwinGizmos) window.MCTwinGizmos.setMode('{mode}');");
        }

        public async Task ToggleGrid(bool visible)
        {
            if (_webView?.CoreWebView2 == null) return;
            await _webView.ExecuteScriptAsync($"if(window.MCTwin && window.MCTwin.toggleGrid) window.MCTwin.toggleGrid({visible.ToString().ToLower()});");
        }

        public async Task ToggleDebug(bool visible)
        {
            if (_webView?.CoreWebView2 == null) return;
            await _webView.ExecuteScriptAsync($"if(window.MCTwin && window.MCTwin.toggleDebug) window.MCTwin.toggleDebug({visible.ToString().ToLower()});");
        }

        public async Task UpdateWorldProperty(string property, string value)
        {
            if (_webView?.CoreWebView2 == null) return;
            
            // Format value correctly for JSON (bool, number, or string)
            string jsonValue = value;
            if (value != "true" && value != "false" && !double.TryParse(value, out _))
            {
                jsonValue = $"\"{value}\"";
            }
            
            await _webView.ExecuteScriptAsync($"if(window.MCTwin && window.MCTwin.updateWorld) window.MCTwin.updateWorld({{ {property}: {jsonValue} }});");
        }

        public async Task SpawnVoxel(object payload, string name, bool isSelectable = false, SceneItem? transform = null)
        {
            if (_webView?.CoreWebView2 == null) return;
            string payloadJson = JsonSerializer.Serialize(payload);
            string escapedName = JsonSerializer.Serialize(name);
            string transformJson = transform != null ? JsonSerializer.Serialize(transform) : "null";
            
            await _webView.ExecuteScriptAsync($"if(window.MCTwin && window.MCTwin.spawnVoxel) window.MCTwin.spawnVoxel({payloadJson}, {escapedName}, {isSelectable.ToString().ToLower()}, {transformJson});");
        }

        public async Task SpawnRecipe(string recipeJson, string name, bool isSelectable = false, SceneItem? transform = null)
        {
            if (_webView?.CoreWebView2 == null) return;
            string escapedName = JsonSerializer.Serialize(name);
            string transformJson = transform != null ? JsonSerializer.Serialize(transform) : "null";

            await _webView.ExecuteScriptAsync($"if(window.MCTwin && window.MCTwin.spawnRecipe) window.MCTwin.spawnRecipe({recipeJson}, {escapedName}, {isSelectable.ToString().ToLower()}, {transformJson});");
        }

        public async Task<string?> GetSceneData()
        {
            if (_webView?.CoreWebView2 == null) return null;
            string res = await _webView.ExecuteScriptAsync("window.MCTwin.getSceneData();");
            if (res == "null" || string.IsNullOrEmpty(res)) return null;
            
            // WebView2 returns a JSON-encoded string of the JS result
            return JsonSerializer.Deserialize<string>(res);
        }

        public async Task PlayAnimation(string name)
        {
            if (_webView?.CoreWebView2 == null) return;
            await _webView.ExecuteScriptAsync($"if(window.MCTwin && window.MCTwin.setAnimation) window.MCTwin.setAnimation('{name}');");
        }

        // Studio-specific (viewer.html)
        public async Task RenderModel(BaseModel model)
        {
            if (_webView?.CoreWebView2 == null) return;
            
            if (model is ProceduralModel p)
            {
                await SpawnRecipe(p.RawRecipeJson, p.Name, true);
            }
            else
            {
                var payload = new { Parts = model.GetParts(), Skin = model.SkinBase64 };
                string json = JsonSerializer.Serialize(payload);
                await _webView.ExecuteScriptAsync($"if(window.MCTwin && window.MCTwin.renderModel) window.MCTwin.renderModel({json});");
            }
        }
    }
}

using System.Threading.Tasks;
using Microsoft.JSInterop;
using MCTwinStudio.Core.Interfaces;
using MCTwinStudio.Core.Models;
using System.Text.Json;

namespace MCTwinStudio.Web.Renderers
{
    public class WebRenderer : IMCTwinRenderer
    {
        private readonly IJSRuntime _jsRuntime;

        public WebRenderer(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task RenderModel(BaseModel model)
        {
            await _jsRuntime.InvokeVoidAsync("MCTwinBridge.call", "MCTwin.renderModel", model);
        }

        public async Task SpawnRecipe(string json, string recipeName, bool isSelectable = false, object? transform = null)
        {
            await _jsRuntime.InvokeVoidAsync("MCTwinBridge.call", "MCTwin.spawnRecipe", json, recipeName, isSelectable, transform);
        }

        public async Task SpawnVoxel(object data, string name, bool isSelectable = false, object? transform = null)
        {
            await _jsRuntime.InvokeVoidAsync("MCTwinBridge.call", "MCTwin.spawnVoxel", data, name, isSelectable, transform);
        }

        public async Task SetGizmoMode(string mode)
        {
            await _jsRuntime.InvokeVoidAsync("MCTwinBridge.call", "MCTwinGizmos.setMode", mode);
        }

        public async Task UpdateWorldProperty(string key, string value)
        {
            var config = new Dictionary<string, object> { { key, value } };
            await _jsRuntime.InvokeVoidAsync("MCTwinBridge.call", "MCTwin.updateWorld", config);
        }

        public async Task ToggleGrid(bool enabled)
        {
            await _jsRuntime.InvokeVoidAsync("MCTwinBridge.call", "MCTwin.toggleGrid", enabled);
        }

        public async Task ToggleAnimation(bool enabled)
        {
            await _jsRuntime.InvokeVoidAsync("MCTwinBridge.call", "MCTwin.toggleAnimation", enabled);
        }

        public async Task PlayAnimation(string name)
        {
            await _jsRuntime.InvokeVoidAsync("MCTwinBridge.call", "MCTwin.playAnimation", name);
        }

        public async Task<string> GetSceneData()
        {
            return await _jsRuntime.InvokeAsync<string>("MCTwinBridge.call", "MCTwin.getSceneData");
        }

        public async Task ClearAll()
        {
            await _jsRuntime.InvokeVoidAsync("MCTwinBridge.call", "MCTwin.clearAll");
        }

        public async Task UpdateNodeTags(string id, Dictionary<string, string> tags)
        {
            await _jsRuntime.InvokeVoidAsync("MCTwinBridge.call", "MCTwin.updateNodeTags", id, tags);
        }
    }
}

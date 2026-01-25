using System.Threading.Tasks;
using MCTwinStudio.Core.Models;

namespace MCTwinStudio.Core.Interfaces
{
    public interface IMCTwinRenderer
    {
        Task RenderModel(BaseModel model);
        Task SpawnRecipe(string json, string recipeName, bool isSelectable = false, object? transform = null);
        Task SpawnVoxel(object data, string name, bool isSelectable = false, object? transform = null);
        Task SetGizmoMode(string mode);
        Task UpdateWorldProperty(string key, string value);
        Task ToggleGrid(bool enabled);
        Task ToggleAnimation(bool enabled);
        Task PlayAnimation(string name);
        Task<string> GetSceneData();
        Task UpdateNodeTags(string id, Dictionary<string, string> tags);
        Task ClearAll();
    }
}

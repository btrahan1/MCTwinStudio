using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCTwinStudio.Services
{
    public interface IAssetService
    {
        Task SaveAsset(string name, string json, AssetCategory category);
        Task<string> LoadAsset(AssetCategory? category = null);
        Task<string> GetBestMatch(string recipeName);
        Task<string[]> ListAvailableRecipes(AssetCategory? category = null);
    }
}

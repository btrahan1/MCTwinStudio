using System.Collections.Generic;

namespace MCTwinStudio.Services
{
    public interface IAssetService
    {
        void SaveAsset(string name, string json, AssetCategory category);
        string LoadAsset(AssetCategory? category = null);
        string GetBestMatch(string recipeName);
        string[] ListAvailableRecipes(AssetCategory? category = null);
    }
}

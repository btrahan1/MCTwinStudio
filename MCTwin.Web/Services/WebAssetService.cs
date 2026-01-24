using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MCTwinStudio.Services;

namespace MCTwinStudio.Web.Services
{
    public class WebAssetService : IAssetService
    {
        private readonly HttpClient _http;
        private string _cartridgeRoot = "cartridges/demo";

        public WebAssetService(HttpClient http)
        {
            _http = http;
        }

        public void SetCartridge(string cartridgePath)
        {
            _cartridgeRoot = cartridgePath;
        }

        public Task SaveAsset(string name, string json, AssetCategory category)
        {
            // Web Driver is usually read-only for cartridges, 
            // but could support LocalStorage in the future.
            return Task.CompletedTask;
        }

        public Task<string> LoadAsset(AssetCategory? category = null)
        {
            // For Web, this might trigger a file picker or just load a default
            return Task.FromResult(string.Empty);
        }

        public async Task<string> GetBestMatch(string recipeName)
        {
            try
            {
                // In a cartridge, assets are relative to the manifest
                string categoryPath = recipeName.ToLower().Contains("npc") ? "Actors" : "Props";
                string url = $"{_cartridgeRoot}/{categoryPath}/{recipeName}.json";
                return await _http.GetStringAsync(url);
            }
            catch
            {
                return string.Empty;
            }
        }

        public Task<string[]> ListAvailableRecipes(AssetCategory? category = null)
        {
            // In a real implementation, we'd fetch the manifest.json
            return Task.FromResult(Array.Empty<string>());
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using MCTwinStudio.Core;

namespace MCTwinStudio.Services
{
    public class DesktopAssetService : IAssetService
    {
        public DesktopAssetService()
        {
            EngineConfig.Initialize();
        }

        public string GetDirectory(AssetCategory category) => category == AssetCategory.Actor ? EngineConfig.ActorsDir : EngineConfig.PropsDir;

        public void SaveAsset(string name, string json, AssetCategory category = AssetCategory.Prop)
        {
            try
            {
                string dir = GetDirectory(category);
                string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                string filename = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string path = Path.Combine(dir, filename);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string LoadAsset(AssetCategory? category = null)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = category.HasValue ? GetDirectory(category.Value) : EngineConfig.RootDataPath;
                dialog.Filter = "MCTwin Recipes (*.json)|*.json";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try { return File.ReadAllText(dialog.FileName); }
                    catch (Exception ex) { MessageBox.Show($"Load Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
            return string.Empty;
        }

        public string GetBestMatch(string recipeName)
        {
            // Search BOTH directories
            string actorMatch = FindMatchInDir(EngineConfig.ActorsDir, recipeName);
            if (!string.IsNullOrEmpty(actorMatch)) return actorMatch;

            return FindMatchInDir(EngineConfig.PropsDir, recipeName);
        }

        private string FindMatchInDir(string dir, string recipeName)
        {
            if (!Directory.Exists(dir)) return string.Empty;
            var files = new List<string>(Directory.GetFiles(dir, $"{recipeName}*.json"));
            files.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
            return files.Count > 0 ? File.ReadAllText(files[0]) : string.Empty;
        }

        public string[] ListAvailableRecipes(AssetCategory? category = null)
        {
            var recipes = new System.Collections.Generic.HashSet<string>();
            
            if (category == null || category == AssetCategory.Actor) ScanDir(EngineConfig.ActorsDir, recipes);
            if (category == null || category == AssetCategory.Prop) ScanDir(EngineConfig.PropsDir, recipes);

            var list = new List<string>(recipes);
            list.Sort();
            return list.ToArray();
        }

        private void ScanDir(string dir, HashSet<string> recipes)
        {
            if (!Directory.Exists(dir)) return;
            var files = Directory.GetFiles(dir, "*.json");
            foreach (var f in files)
            {
                string name = Path.GetFileNameWithoutExtension(f);
                if (name.Contains("_202"))
                {
                    int idx = name.LastIndexOf('_');
                    if (idx > 0)
                    {
                        int secondIdx = name.LastIndexOf('_', idx - 1);
                        if (secondIdx > 0) name = name.Substring(0, secondIdx);
                        else name = name.Substring(0, idx);
                    }
                }
                recipes.Add(name);
            }
        }
    }
}

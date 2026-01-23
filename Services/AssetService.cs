using System;
using System.IO;
using System.Windows.Forms;

namespace MCTwinStudio.Services
{
    public class AssetService
    {
        private string _creationsDir;

        public AssetService()
        {
            _creationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Creations");
            if (!Directory.Exists(_creationsDir))
            {
                Directory.CreateDirectory(_creationsDir);
            }
        }

        public void SaveAsset(string name, string json)
        {
            try
            {
                // Clean name for filename
                string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                string filename = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string path = Path.Combine(_creationsDir, filename);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string LoadAsset()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = _creationsDir;
                dialog.Filter = "MCTwin Recipes (*.json)|*.json";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        return File.ReadAllText(dialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Load Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            return string.Empty;
        }

        public string GetBestMatch(string recipeName)
        {
            if (!Directory.Exists(_creationsDir)) return string.Empty;
            
            // Look for files starting with recipeName
            var files = new List<string>(Directory.GetFiles(_creationsDir, $"{recipeName}*.json"));
            
            // Sort by modification date descending to get the latest
            files.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
            
            return files.Count > 0 ? File.ReadAllText(files[0]) : string.Empty;
        }

        public string[] ListAvailableRecipes()
        {
            if (!Directory.Exists(_creationsDir)) return new string[0];
            var files = Directory.GetFiles(_creationsDir, "*.json");
            var recipes = new System.Collections.Generic.HashSet<string>();
            foreach (var f in files)
            {
                // Typical format: Name_YYYYMMDD_HHMMSS.json or just Name.json
                string name = Path.GetFileNameWithoutExtension(f);
                if (name.Contains("_202")) // Looks like a timestamped file
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
            var list = new List<string>(recipes);
            list.Sort();
            return list.ToArray();
        }
    }
}
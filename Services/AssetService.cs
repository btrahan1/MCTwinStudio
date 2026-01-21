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
    }
}
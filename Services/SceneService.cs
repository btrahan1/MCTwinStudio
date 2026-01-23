using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using MCTwinStudio.Core.Models;

namespace MCTwinStudio.Services
{
    public class SceneService
    {
        private string _scenesDir;

        public SceneService()
        {
            _scenesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scenes");
            if (!Directory.Exists(_scenesDir))
            {
                Directory.CreateDirectory(_scenesDir);
            }
        }

        public void SaveScene(string name, string json)
        {
            try
            {
                string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                string path = Path.Combine(_scenesDir, $"{safeName}.scene.json");
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save Scene Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string LoadScene()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = _scenesDir;
                dialog.Filter = "MCTwin Scenes (*.scene.json)|*.scene.json";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        return File.ReadAllText(dialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Load Scene Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            return string.Empty;
        }

        public string[] ListScenes()
        {
            if (!Directory.Exists(_scenesDir)) return new string[0];
            var files = Directory.GetFiles(_scenesDir, "*.scene.json");
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(files[i]));
            }
            return files;
        }
    }
}

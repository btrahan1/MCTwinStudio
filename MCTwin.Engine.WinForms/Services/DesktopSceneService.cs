using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using MCTwinStudio.Core;
using MCTwinStudio.Core.Models;

namespace MCTwinStudio.Services
{
    public class DesktopSceneService : ISceneService
    {
        public DesktopSceneService()
        {
            EngineConfig.Initialize();
        }

        public Task SaveScene(string name, string json)
        {
            try
            {
                string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                string path = Path.Combine(EngineConfig.ScenesDir, $"{safeName}.scene.json");
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save Scene Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return Task.CompletedTask;
        }

        public Task<string> LoadScene()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = EngineConfig.ScenesDir;
                dialog.Filter = "MCTwin Scenes (*.scene.json)|*.scene.json";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try { return Task.FromResult(File.ReadAllText(dialog.FileName)); }
                    catch (Exception ex) { MessageBox.Show($"Load Scene Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
            return Task.FromResult(string.Empty);
        }

        public Task<string[]> ListScenes()
        {
            if (!Directory.Exists(EngineConfig.ScenesDir)) return Task.FromResult(new string[0]);
            var files = Directory.GetFiles(EngineConfig.ScenesDir, "*.scene.json");
            var sceneNames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                sceneNames[i] = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(files[i]));
            }
            return Task.FromResult(sceneNames);
        }
    }
}

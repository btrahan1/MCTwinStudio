using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MCTwinStudio.Core;
using MCTwinStudio.Core.Models;
using System.Text.Json;
using System.Collections.Generic;

namespace MCTwinStudio
{
    public class WorldForm : Form
    {
        private WebView2 _webView;
        private SceneController _controller = null!;
        private HumanoidModel _model;
        private CoreWebView2Environment _env;
        private Services.AssetService _assetService;
        private Services.SceneService _sceneService;
        private Controls.PaletteControl _palette = null!;
        private Button _btnMove, _btnRot, _btnSize, _btnDrag, _btnNone, _btnPullAI;

        public WorldForm(HumanoidModel model, CoreWebView2Environment env, Services.AssetService assetService)
        {
            _model = model ?? CreateDefaultModel();
            _assetService = assetService;
            _sceneService = new Services.SceneService();
            _env = env;

            this.Text = $"World Explorer - {_model.Name}";
            this.Size = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = NexusStyles.BackColor;

            InitializeLayout();
            InitializeAsync();
        }

        private HumanoidModel CreateDefaultModel()
        {
            var m = new HumanoidModel { Name = "Quick Steve", SkinToneHex = "#C68E6F", ShirtHex = "#0099AA", PantsHex = "#333399", EyeHex = "#FFFFFF" };
            m.GenerateSkin();
            return m;
        }

        private void InitializeLayout()
        {
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, FixedPanel = FixedPanel.Panel2 };
            this.Controls.Add(split);
            split.SplitterDistance = this.ClientSize.Width - 320;

            _webView = new WebView2 { Dock = DockStyle.Fill };
            split.Panel1.Controls.Add(_webView);

            var pnlSettings = new Panel { Dock = DockStyle.Fill, BackColor = NexusStyles.CardColor, Padding = new Padding(15), AutoScroll = true };
            split.Panel2.Controls.Add(pnlSettings);

            // Controls (Bottom-up ADDITION to get Top-down DOCK order)
            // 1. Scene Management
            AddLabel(pnlSettings, "Scene Management");
            var btnLoadScene = new Button { Text = "LOAD SCENE", Dock = DockStyle.Top, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White };
            btnLoadScene.Click += async (s, e) => {
                string json = _sceneService.LoadScene();
                if (!string.IsNullOrEmpty(json)) {
                    var scene = JsonSerializer.Deserialize<SceneModel>(json);
                    if (scene != null) {
                        await _controller.ClearAll();
                        foreach (var item in scene.Items) {
                            string recipe = _assetService.GetBestMatch(item.RecipeName);
                            if (!string.IsNullOrEmpty(recipe)) ImportProp(recipe, item.RecipeName, item);
                        }
                    }
                }
            };
            var btnSaveScene = new Button { Text = "SAVE SCENE", Dock = DockStyle.Top, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White };
            btnSaveScene.Click += async (s, e) => {
                string? sceneJson = await _controller.GetSceneData();
                if (sceneJson != null) {
                    string name = Microsoft.VisualBasic.Interaction.InputBox("Enter Scene Name:", "Save Scene", "New Scene");
                    if (!string.IsNullOrEmpty(name)) _sceneService.SaveScene(name, sceneJson);
                }
            };
            pnlSettings.Controls.Add(btnLoadScene);
            pnlSettings.Controls.Add(btnSaveScene);

            // 2. AI Architect
            AddLabel(pnlSettings, "AI Architect");
            _btnPullAI = new Button { Text = "PULL AI DESIGN", Dock = DockStyle.Top, Height = 45, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 80, 150), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            _btnPullAI.Click += async (s, e) => {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scenes", "ai_delivery.scene.json");
                if (File.Exists(path)) {
                    await _controller.ClearAll();
                    string json = File.ReadAllText(path);
                    var scene = JsonSerializer.Deserialize<SceneModel>(json);
                    if (scene != null) foreach (var item in scene.Items) {
                        string recipe = _assetService.GetBestMatch(item.RecipeName);
                        if (!string.IsNullOrEmpty(recipe)) ImportProp(recipe, item.RecipeName, item);
                    }
                }
            };
            pnlSettings.Controls.Add(_btnPullAI);

            // 3. Local Palette
            AddLabel(pnlSettings, "Local Palette");
            _palette = new Controls.PaletteControl(_assetService) { Height = 180, Dock = DockStyle.Top };
            _palette.OnAssetSelected += (name, recipe) => ImportProp(recipe, name);
            pnlSettings.Controls.Add(_palette);

            // 4. Manipulation
            AddLabel(pnlSettings, "Manipulation");
            var pnlGizmo = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 130 };
            _btnMove = AddGizmoBtn(pnlGizmo, "MOVE", "move");
            _btnRot = AddGizmoBtn(pnlGizmo, "ROT", "rotate");
            _btnSize = AddGizmoBtn(pnlGizmo, "SIZE", "scale");
            _btnDrag = AddGizmoBtn(pnlGizmo, "DRAG", "drag");
            _btnNone = AddGizmoBtn(pnlGizmo, "OFF", "none");
            UpdateGizmoButtonStates(_btnMove);
            var btnGrid = new Button { Text = "GRID: OFF", Width = 105, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White };
            bool gridOn = false;
            btnGrid.Click += (s, e) => { gridOn = !gridOn; btnGrid.Text = gridOn ? "GRID: ON" : "GRID: OFF"; _controller.ToggleGrid(gridOn); };
            pnlGizmo.Controls.Add(btnGrid);
            pnlSettings.Controls.Add(pnlGizmo);

            // 5. Lighting
            AddLabel(pnlSettings, "Light Intensity");
            var trkLight = new TrackBar { Dock = DockStyle.Top, Minimum = 0, Maximum = 200, Value = 100, Height = 45 };
            trkLight.Scroll += (s, e) => _controller.UpdateWorldProperty("lightIntensity", (trkLight.Value / 100.0).ToString());
            pnlSettings.Controls.Add(trkLight);

            // 6. Environment
            AddLabel(pnlSettings, "Environment");
            var pnlEnv = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 120 };
            
            var btnSky = new Button { Text = "SKY COLOR", Width = 110, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White };
            btnSky.Click += (s, e) => {
                using var cd = new ColorDialog();
                if (cd.ShowDialog() == DialogResult.OK) {
                    string hex = "#" + cd.Color.R.ToString("X2") + cd.Color.G.ToString("X2") + cd.Color.B.ToString("X2");
                    _controller.UpdateWorldProperty("skyColor", hex);
                }
            };

            var btnFloor = new Button { Text = "FLOOR COLOR", Width = 110, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White };
            btnFloor.Click += (s, e) => {
                using var cd = new ColorDialog();
                if (cd.ShowDialog() == DialogResult.OK) {
                    string hex = "#" + cd.Color.R.ToString("X2") + cd.Color.G.ToString("X2") + cd.Color.B.ToString("X2");
                    _controller.UpdateWorldProperty("groundColor", hex);
                }
            };

            var cbTheme = new ComboBox { Width = 225, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White, DropDownStyle = ComboBoxStyle.DropDownList };
            cbTheme.Items.AddRange(new string[] { "Wood", "Grass", "Desert", "Concrete", "Checker", "Space" });
            cbTheme.SelectedIndex = 0;
            cbTheme.SelectedIndexChanged += (s, e) => _controller.UpdateWorldProperty("floorTheme", cbTheme.SelectedItem?.ToString() ?? "Wood");

            pnlEnv.Controls.AddRange(new Control[] { btnSky, btnFloor, cbTheme });
            pnlSettings.Controls.Add(pnlEnv);

            // 7. Floor Size
            AddLabel(pnlSettings, "Floor Size");
            var trkSize = new TrackBar { Dock = DockStyle.Top, Minimum = 100, Maximum = 5000, Value = 500, Height = 45 };
            trkSize.Scroll += (s, e) => _controller.UpdateWorldProperty("groundSize", trkSize.Value.ToString());
            pnlSettings.Controls.Add(trkSize);

            // Standard Top-Down Ordering by reversing Dock order
            foreach (Control c in pnlSettings.Controls) c.BringToFront();
        }

        private async void InitializeAsync()
        {
            await _webView.EnsureCoreWebView2Async(_env);
            _controller = new SceneController(_webView);
            
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "world.html");
            if (File.Exists(htmlPath)) _webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
            
            _webView.NavigationCompleted += (s, e) => { if (e.IsSuccess) _controller.RenderModel(_model); };
        }

        public async void ImportProp(string json, string name = "Prop", SceneItem? transform = null)
        {
            try {
                using var doc = JsonDocument.Parse(json);
                string type = doc.RootElement.TryGetProperty("Type", out var t) ? t.GetString() ?? "Procedural" : "Procedural";
                
                if (type == "Voxel" || type == "Humanoid") {
                    var h = new HumanoidModel { Name = name };
                    // Reconstruct humanoid from JSON colors/textures
                    if (doc.RootElement.TryGetProperty("ProceduralColors", out var colors)) {
                        h.SkinToneHex = GetProp(colors, "Skin", h.SkinToneHex);
                        h.ShirtHex = GetProp(colors, "Shirt", h.ShirtHex);
                        h.PantsHex = GetProp(colors, "Pants", h.PantsHex);
                        h.EyeHex = GetProp(colors, "Eyes", h.EyeHex);
                    }
                    if (doc.RootElement.TryGetProperty("Textures", out var tex)) {
                        h.FacePixels = GetPixels(tex, "Face");
                        h.HatPixels = GetPixels(tex, "Hat");
                        h.ChestPixels = GetPixels(tex, "Chest");
                        h.ArmPixels = GetPixels(tex, "Arms");
                        h.LegPixels = GetPixels(tex, "Legs");
                    }
                    h.GenerateSkin();
                    await _controller.SpawnVoxel(new { Parts = h.GetParts(), Skin = h.SkinBase64 }, name, false, transform);
                } else {
                    await _controller.SpawnRecipe(json, name, false, transform);
                }
            } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private string GetProp(JsonElement el, string key, string def) => el.TryGetProperty(key, out var p) ? p.GetString() ?? def : def;
        private string[]? GetPixels(JsonElement el, string key) {
            if (el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.Array) {
                var list = new List<string>();
                foreach (var item in p.EnumerateArray()) list.Add(item.GetString() ?? "#000000");
                return list.ToArray();
            }
            return null;
        }

        private Button AddGizmoBtn(FlowLayoutPanel p, string text, string mode)
        {
            var btn = new Button { Text = text, Width = 50, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, Font = new Font("Segoe UI", 7, FontStyle.Bold) };
            btn.Click += (s, e) => { UpdateGizmoButtonStates(btn); _controller.SetGizmoMode(mode); };
            p.Controls.Add(btn);
            return btn;
        }

        private void UpdateGizmoButtonStates(Button active) 
        { 
            foreach (var b in new[] { _btnMove, _btnRot, _btnSize, _btnDrag, _btnNone }) 
                if (b != null) b.BackColor = (b == active) ? Color.FromArgb(30, 100, 30) : Color.FromArgb(60, 60, 60); 
        }

        private void AddLabel(Panel p, string text) { p.Controls.Add(new Label { Text = text, Dock = DockStyle.Top, Height = 25, ForeColor = Color.LightGray, TextAlign = ContentAlignment.BottomLeft }); }
    }
}

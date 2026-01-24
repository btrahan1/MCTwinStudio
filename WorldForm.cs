using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MCTwinStudio.Core;
using MCTwinStudio.Core.Models;
using MCTwinStudio.Core.Interfaces;
using MCTwinStudio.Services;
using System.Text.Json;
using System.Collections.Generic;

namespace MCTwinStudio
{
    public class WorldForm : Form
    {
        private WebView2 _webView;
        private IMCTwinRenderer _controller = null!;
        private HumanoidModel? _model;
        private CoreWebView2Environment _env;
        private IAssetService _assetService;
        private ISceneService _sceneService;
        private Controls.PaletteControl _palette = null!;
        private Button _btnMove, _btnRot, _btnSize, _btnDrag, _btnNone, _btnPullAI;
        private Panel _pnlSaveOverlay = null!;
        private TextBox _txtSaveName = null!;
        private string? _pendingSceneJson = null;

        public WorldForm(HumanoidModel? model, CoreWebView2Environment env, IAssetService assetService, ISceneService sceneService)
        {
            _model = model;
            _assetService = assetService;
            _sceneService = sceneService;
            _env = env;

            this.Text = $"World Explorer - {(_model?.Name ?? "Sandbox")}";
            this.Size = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = NexusStyles.BackColor;

            InitializeLayout();
            InitializeAsync();
        }

        // CreateDefaultModel removed as skinning is now host-specific

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
                _pendingSceneJson = await _controller.GetSceneData();
                if (_pendingSceneJson != null) {
                    _pnlSaveOverlay.Visible = true;
                    _pnlSaveOverlay.BringToFront();
                    _txtSaveName.Text = "New Scene";
                    _txtSaveName.Focus();
                    _txtSaveName.SelectAll();
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
            
            var btnAnim = new Button { Text = "ANIMATE: OFF", Width = 105, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White };
            bool animOn = false;
            btnAnim.Click += (s, e) => { animOn = !animOn; btnAnim.Text = animOn ? "ANIMATE: ON" : "ANIMATE: OFF"; _controller.ToggleAnimation(animOn); };

            pnlGizmo.Controls.Add(btnGrid);
            pnlGizmo.Controls.Add(btnAnim);
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

            InitializeSaveOverlay();
        }

        private void InitializeSaveOverlay()
        {
            _pnlSaveOverlay = new Panel { 
                Size = new Size(350, 150), 
                BackColor = NexusStyles.CardColor, 
                BorderStyle = BorderStyle.FixedSingle, 
                Visible = false 
            };
            // Center in form
            _pnlSaveOverlay.Location = new Point((this.ClientSize.Width - 350) / 2, (this.ClientSize.Height - 150) / 2);
            
            var lbl = new Label { Text = "ENTER SCENE NAME:", Left = 20, Top = 20, Width = 310, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            _txtSaveName = new TextBox { Left = 20, Top = 50, Width = 310, BackColor = Color.FromArgb(30, 30, 30), ForeColor = NexusStyles.AccentAmber, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            
            var btnDoSave = new Button { Text = "CONFIRM SAVE", Left = 20, Top = 90, Width = 150, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.AccentIndigo, ForeColor = Color.White };
            btnDoSave.Click += (s, e) => {
                if (!string.IsNullOrEmpty(_txtSaveName.Text) && _pendingSceneJson != null) {
                    _sceneService.SaveScene(_txtSaveName.Text, _pendingSceneJson);
                }
                _pnlSaveOverlay.Visible = false;
            };

            var btnCancel = new Button { Text = "CANCEL", Left = 180, Top = 90, Width = 150, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.DimGray, ForeColor = Color.White };
            btnCancel.Click += (s, e) => { _pnlSaveOverlay.Visible = false; };

            _pnlSaveOverlay.Controls.AddRange(new Control[] { lbl, _txtSaveName, btnDoSave, btnCancel });
            this.Controls.Add(_pnlSaveOverlay);
            _pnlSaveOverlay.BringToFront();
        }

        private async void InitializeAsync()
        {
            await _webView.EnsureCoreWebView2Async(_env);
            _controller = new DesktopSceneController(_webView);
            
            string htmlPath = Path.Combine(EngineConfig.RendererDir, "world.html");
            if (File.Exists(htmlPath)) _webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
            
            _webView.NavigationCompleted += (s, e) => { if (e.IsSuccess && _model != null) _controller.RenderModel(_model); };
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
                    // Skin generation should happen here via service
                    var skinGen = new Services.SkinGenerator();
                    var bmp = skinGen.Generate(h.SkinToneHex, h.ShirtHex, h.PantsHex, h.EyeHex, h.FacePixels, h.HatPixels, h.ChestPixels, h.ArmPixels, h.LegPixels);
                    using (var ms = new System.IO.MemoryStream()) {
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        h.SkinBase64 = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                    }
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

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
        private WebView2? _webView;
        private IMCTwinRenderer _controller = null!;
        private HumanoidModel? _model;
        private CoreWebView2Environment? _env;
        private IAssetService _assetService;
        private ISceneService _sceneService;
        private Controls.PaletteControl? _palette;
        private Button? _btnMove, _btnRot, _btnSize, _btnDrag, _btnNone, _btnPullAI;
        private Panel? _pnlSaveOverlay;
        private TextBox? _txtSaveName;
        private string? _pendingSceneJson = null;
        private bool _isExportMode = false;

        // Prop Editor
        private Panel? _pnlPpt;
        private TextBox? _txtPptTags;
        private ComboBox? _cbBehavior;
        private Label? _lblPptId;
        private string? _selectedId = null;

        // ... [Constructor remains same] ...

        private string[] LoadAvailableBehaviors()
        {
            try {
                // Determine path relative to exe
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "behaviors.js");
                if (!File.Exists(path)) return Array.Empty<string>();

                var lines = File.ReadAllLines(path);
                var list = new List<string>();
                
                // Simple regex-like scan for keys like "Spin": {
                foreach (var line in lines) {
                    var trimmed = line.Trim();
                    // Pattern 1: "Wave": {
                    if (trimmed.StartsWith("\"") && trimmed.EndsWith("\": {")) {
                        var name = trimmed.Substring(1, trimmed.Length - 5); 
                        list.Add(name);
                    }
                    // Pattern 2: window.MCTwinBehaviors['AI_Wave'] = {  (Allow ' or " quotes)
                    else {
                        var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"MCTwinBehaviors\[['""](.*?)['""]\]\s*=\s*{");
                        if (match.Success) {
                            list.Add(match.Groups[1].Value);
                        }
                    }
                }
                return list.ToArray();
            } catch { return Array.Empty<string>(); }
        }

        private void InitializePptPanel(Panel parent)
        {
             AddLabel(parent, "Selection Properties");
             _pnlPpt = new Panel { Dock = DockStyle.Top, Height = 220, BackColor = Color.FromArgb(50,50,55), Padding = new Padding(5), Visible = true };
             
             _lblPptId = new Label { Text = "Select an Object...", Dock = DockStyle.Top, Height = 20, ForeColor = Color.Gray, Font = new Font("Consolas", 8) };
             
             // Behavior Dropdown
             AddLabel(_pnlPpt, "Behavior");
             _cbBehavior = new ComboBox { Dock = DockStyle.Top, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(30,30,30), ForeColor = Color.White, DropDownStyle = ComboBoxStyle.DropDown };
             _cbBehavior.Items.AddRange(LoadAvailableBehaviors());
             _cbBehavior.SelectedIndexChanged += (s, e) => {
                 // When behavior changes, ensure it's in the tags text box
                 UpdateTagsTextFromCombo();
                 UpdateSelectedTags(); // Auto-apply
             };
             // Allow typing new ones
             _cbBehavior.TextChanged += (s, e) => { /* Optional: Validate */ };
             
             var lblTags = new Label { Text = "Parameters (Key=Value)", Dock = DockStyle.Top, Height = 20, ForeColor = Color.White };
             _txtPptTags = new TextBox { Dock = DockStyle.Top, Height = 80, Multiline = true, BackColor = Color.FromArgb(30,30,30), ForeColor = NexusStyles.AccentAmber, Font = new Font("Consolas", 9), ScrollBars = ScrollBars.Vertical };
             
             var btnUpdate = new Button { Text = "UPDATE TAGS", Dock = DockStyle.Top, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.AccentIndigo, ForeColor = Color.White };
             btnUpdate.Click += (s, e) => UpdateSelectedTags();
             
             _pnlPpt.Controls.AddRange(new Control[] { btnUpdate, _txtPptTags, lblTags, _cbBehavior, _lblPptId });
             // Reverse order for dock
             _lblPptId.BringToFront(); // Top
             // Then the label "Behavior" (added via AddLabel, which puts it in controls) need to settle order.
             // Simplest is to clear and re-add in reverse dock order
             _pnlPpt.Controls.Clear();
             _pnlPpt.Controls.Add(btnUpdate);          // Bottom
             _pnlPpt.Controls.Add(_txtPptTags);        // Middle
             _pnlPpt.Controls.Add(lblTags);            // Middle
             _pnlPpt.Controls.Add(_cbBehavior);        // Top-ish
             _pnlPpt.Controls.Add(new Label { Text = "Behavior", Dock = DockStyle.Top, Height = 20, ForeColor = Color.LightGray });
             _pnlPpt.Controls.Add(_lblPptId);          // Top
             
             parent.Controls.Add(_pnlPpt);
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
             try {
                 string json = e.WebMessageAsJson;
                 using var doc = JsonDocument.Parse(json);
                 if (doc.RootElement.TryGetProperty("type", out var t) && t.GetString() == "selection") {
                      var data = doc.RootElement.GetProperty("data");
                      _selectedId = data.GetProperty("id").GetString();
                      string recipe = data.GetProperty("recipeName").GetString() ?? "?";
                      
                      if (_pnlPpt != null && _lblPptId != null && _txtPptTags != null && _cbBehavior != null) {
                          _pnlPpt.Visible = true;
                          _lblPptId.Text = $"ID: {recipe}";
                          
                          var sb = new System.Text.StringBuilder();
                          string foundBehavior = "";
                          
                          if (data.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Object) {
                               foreach(var prop in tags.EnumerateObject()) {
                                   if (prop.Name == "Behavior") foundBehavior = prop.Value.GetString() ?? "";
                                   else sb.AppendLine($"{prop.Name}={prop.Value.GetString()}");
                               }
                          }
                          _txtPptTags.Text = sb.ToString();
                          _cbBehavior.Text = foundBehavior; // Set params
                      }
                 }
             } catch {}
        }
        
        private void UpdateTagsTextFromCombo()
        {
            // If user selects "Spin", ensure "Behavior=Spin" is accounted for
            // Actually, we separate them in UI, but merge them in Model.
        }

        private async void UpdateSelectedTags()
        {
             if (_selectedId == null || _txtPptTags == null || _cbBehavior == null) return;
             
             var tags = new Dictionary<string,string>();
             
             // 1. Add Behavior if set
             if (!string.IsNullOrWhiteSpace(_cbBehavior.Text)) {
                 tags["Behavior"] = _cbBehavior.Text.Trim();
             }

             // 2. Add other tags
             foreach(var line in _txtPptTags.Lines) {
                 var parts = line.Split('=', 2);
                 if (parts.Length == 2) {
                     string k = parts[0].Trim();
                     if (k != "Behavior") tags[k] = parts[1].Trim();
                 }
             }
             
             await _controller.UpdateNodeTags(_selectedId, tags);
        }

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

            BundleBehaviors();
            InitializeLayout();
            InitializeAsync();
        }

        private void BundleBehaviors()
        {
            try {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Behaviors");
                var outFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "behaviors.js");
                
                if (!Directory.Exists(dir)) return;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("// AUTO-GENERATED BUNDLE. DO NOT EDIT DIRECTLY.");
                sb.AppendLine("// Edit files in Assets/Behaviors/ instead.");
                sb.AppendLine();

                // 1. Load Standard First (It inits the object)
                var stdPath = Path.Combine(dir, "standard.js");
                if (File.Exists(stdPath)) {
                    sb.AppendLine(File.ReadAllText(stdPath));
                    sb.AppendLine();
                } else {
                    // Fallback if missing
                    sb.AppendLine("window.MCTwinBehaviors = {};");
                }

                // 2. Load Others
                var files = Directory.GetFiles(dir, "*.js");
                foreach(var f in files) {
                    if (Path.GetFileName(f).ToLower() == "standard.js") continue;
                    sb.AppendLine($"// --- {Path.GetFileName(f)} ---");
                    sb.AppendLine(File.ReadAllText(f));
                    sb.AppendLine();
                }

                File.WriteAllText(outFile, sb.ToString());
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine("Bundle Error: " + ex.Message);
            }
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
                string json = await _sceneService.LoadScene();
                if (!string.IsNullOrEmpty(json)) {
                    var scene = JsonSerializer.Deserialize<SceneModel>(json);
                    if (scene != null) {
                        await _controller.ClearAll();
                        foreach (var item in scene.Items) {
                            string recipe = await _assetService.GetBestMatch(item.RecipeName);
                            if (!string.IsNullOrEmpty(recipe)) ImportProp(recipe, item.RecipeName, item);
                        }
                    }
                }
            };
            var btnSaveScene = new Button { Text = "SAVE SCENE", Dock = DockStyle.Top, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White };
            btnSaveScene.Click += async (s, e) => {
                _pendingSceneJson = await _controller.GetSceneData();
                _isExportMode = false;
                if (_pendingSceneJson != null && _pnlSaveOverlay != null && _txtSaveName != null) {
                    _pnlSaveOverlay.Visible = true;
                    _pnlSaveOverlay.BringToFront();
                    _txtSaveName.Text = "New Scene";
                    _txtSaveName.Focus();
                    _txtSaveName.SelectAll();
                }
            };
            var btnExport = new Button { Text = "EXPORT CARTRIDGE", Dock = DockStyle.Top, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 55), ForeColor = NexusStyles.AccentAmber };
            btnExport.Click += async (s, e) => {
                 _pendingSceneJson = await _controller.GetSceneData();
                 _isExportMode = true;
                if (_pendingSceneJson != null && _pnlSaveOverlay != null && _txtSaveName != null) {
                    _pnlSaveOverlay.Visible = true;
                    _pnlSaveOverlay.BringToFront();
                    _txtSaveName.Text = (_model?.Name ?? "SceneCartridge");
                    _txtSaveName.Focus();
                    _txtSaveName.SelectAll();
                }
            };

            pnlSettings.Controls.Add(btnLoadScene);
            pnlSettings.Controls.Add(btnSaveScene);
            pnlSettings.Controls.Add(btnExport);

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
                        string recipe = await _assetService.GetBestMatch(item.RecipeName);
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

            // 8. Properties (Top Most)
            InitializePptPanel(pnlSettings);

            // Standard Top-Down Ordering by reversing Dock order
            // Note: We want Selection (Added Last) to be completely FIRST (Index 0).
            // The previous loop reversed everything so First Added (Scene) was Top.
            // We'll keep that for 1-7, but ensure 8 is Front.
            
            // 1. Reverse 1-7 (Scene..FloorSize) to make Scene Top of that stack
            foreach (Control c in pnlSettings.Controls) {
                if (c != _pnlPpt) c.BringToFront();
            }
            // 2. Now Bring Selection to Front (Absolute Top)
            if (_pnlPpt != null) _pnlPpt.BringToFront();

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
            btnDoSave.Click += async (s, e) => {
                if (!string.IsNullOrEmpty(_txtSaveName.Text) && _pendingSceneJson != null) {
                    string name = _txtSaveName.Text;
                    if (_isExportMode) {
                        try {
                            string exportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports");
                            Directory.CreateDirectory(exportsDir);
                            string filename = Path.Combine(exportsDir, name.Replace(" ", "_") + ".html");
                            
                            // DEEP EXPORT: Gather all unique assets used in the scene
                            var sceneObj = JsonSerializer.Deserialize<SceneModel>(_pendingSceneJson);
                            var uniqueAssets = new Dictionary<string, object>();
                            
                            if (sceneObj != null) {
                                foreach (var item in sceneObj.Items) {
                                    if (!uniqueAssets.ContainsKey(item.RecipeName)) {
                                        string assetJson = await _assetService.GetBestMatch(item.RecipeName);
                                        if (!string.IsNullOrEmpty(assetJson)) {
                                            if (IsVoxelType(assetJson)) {
                                                // BAKING FOR EXPORT
                                                var baked = BakeAsset(assetJson, item.RecipeName);
                                                if (baked != null) uniqueAssets[item.RecipeName] = baked;
                                            } else {
                                                // Standard Static Asset
                                                using var doc = JsonDocument.Parse(assetJson);
                                                uniqueAssets[item.RecipeName] = doc.RootElement.Clone();
                                            }
                                        }
                                    }
                                }
                            }

                            // Bundle Structure
                            var bundle = new {
                                Name = name,
                                Type = "SceneBundle",
                                Scene = sceneObj,
                                Assets = uniqueAssets
                            };
                            string bundleJson = JsonSerializer.Serialize(bundle);

                            var exporter = new CartridgeExporter(System.IO.Path.GetDirectoryName(Application.ExecutablePath));
                            await exporter.ExportAsync(name, bundleJson, filename);
                            
                             MessageBox.Show($"Cartridge Exported Successfully!\n{filename}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = exportsDir, UseShellExecute = true });
                        } catch (Exception ex) { MessageBox.Show($"Export Failed: {ex.Message}"); }
                    } else {
                        _sceneService.SaveScene(name, _pendingSceneJson);
                    }
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
            if (_webView == null) return;
            await _webView.EnsureCoreWebView2Async(_env);
            _controller = new DesktopSceneController(_webView);
            
            string htmlPath = Path.Combine(EngineConfig.RendererDir, "world.html");
            if (File.Exists(htmlPath)) _webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
            
            
            _webView.NavigationCompleted += (s, e) => { 
                if (e.IsSuccess) {
                    _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                    if (_model != null) _controller.RenderModel(_model); 
                }
            };
        }

        public async void ImportProp(string json, string name = "Prop", SceneItem? transform = null)
        {
            var baked = BakeAsset(json, name);
            if (baked != null && IsVoxelType(json)) {
                 await _controller.SpawnVoxel(baked, name, false, transform);
            } else {
                 await _controller.SpawnRecipe(json, name, false, transform);
            }
        }

        private bool IsVoxelType(string json) {
            try { 
                using var doc = JsonDocument.Parse(json);
                string type = doc.RootElement.TryGetProperty("Type", out var t) ? t.GetString() ?? "" : "";
                return type == "Voxel" || type == "Humanoid";
            } catch { return false; }
        }

        private object? BakeAsset(string json, string name) {
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
                    
                    var skinGen = new Services.SkinGenerator();
                    var bmp = skinGen.Generate(h.SkinToneHex, h.ShirtHex, h.PantsHex, h.EyeHex, h.FacePixels, h.HatPixels, h.ChestPixels, h.ArmPixels, h.LegPixels);
                    string b64 = "";
                    using (var ms = new System.IO.MemoryStream()) {
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        b64 = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                    }
                    return new { Type = "Voxel", Parts = h.GetParts(), Skin = b64 };
                }
                return null; // Not a bake-able type
            } catch { return null; }
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

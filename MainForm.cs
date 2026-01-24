using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using MCTwinStudio.Core;
using MCTwinStudio.Core.Models;
using MCTwinStudio.Core.Interfaces;
using MCTwinStudio.Controls;
using System.Collections.Generic;
using MCTwinStudio.Services;

namespace MCTwinStudio
{
    public class MainForm : Form
    {
        private SplitContainer _sidebarSplit = null!; 
        private SplitContainer _mainSplit = null!;    
        private ViewportPane _viewport = null!;
        private BrainPane _brain = null!;
        private OptionsPane _options = null!;
        private CoreWebView2Environment _sharedEnv = null!;
        private IAssetService _assetService = null!;
        private ISceneService _sceneService = null!;
        private ArchitectService _architectService = null!;
        
        private TextBox _txtPrompt = null!;
        private Button _btnForge = null!;
        private Button _btnSave = null!;
        private Button _btnToggleBrain = null!;
        private bool _isBrainExpanded = false;
        private WorldForm? _activeWorld = null;
        private ListBox _lstConsole = null!;
        private Button _btnToggleConsole = null!;
        private bool _isConsoleExpanded = false;
        private RadioButton _rbVoxel = null!;
        private RadioButton _rbProcedural = null!;
        private RadioButton _rbScene = null!;
        private RadioButton _rbSculpted = null!;
        private PaletteControl _palette = null!;

        private BaseModel? _currentModel = null;
        private string _currentDescription = "";

        public MainForm()
        {
            this.Text = "MCTwin Studio // Minecraft Meta-Architect";
            this.Size = new Size(1600, 1000);
            this.BackColor = NexusStyles.BackColor;
            this.ForeColor = NexusStyles.WhiteText;
            this.StartPosition = FormStartPosition.CenterScreen;
            _assetService = new DesktopAssetService();
            _sceneService = new DesktopSceneService();
            InitializeLayout();
            InitializeWebViews();
        }

        private SplitContainer _leftSplit = null!;
        private JsonPane _jsonViewer = null!;
        private AnimationPane _animPane = null!;
        private Button _btnToggleJson = null!;
        private bool _isJsonExpanded = false;

        private void InitializeLayout()
        {
            _sidebarSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterWidth = 4, BackColor = NexusStyles.BorderColor, FixedPanel = FixedPanel.Panel2 };
            this.Controls.Add(_sidebarSplit);
            
            // Left Split: JSON (P1) | Main Content (P2)
            _leftSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterWidth = 4, BackColor = NexusStyles.BorderColor, FixedPanel = FixedPanel.Panel1 };
            _sidebarSplit.Panel1.Controls.Add(_leftSplit);

            // JSON Viewer (Hidden by default)
            _jsonViewer = new JsonPane { Dock = DockStyle.Fill };
            _leftSplit.Panel1.Controls.Add(_jsonViewer);
            _leftSplit.Panel1Collapsed = true;

            // Main Content (Viewport/Brain)
            _mainSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 4, BackColor = NexusStyles.BorderColor, FixedPanel = FixedPanel.Panel2 };
            _leftSplit.Panel2.Controls.Add(_mainSplit);
            
            _options = new OptionsPane { Dock = DockStyle.Fill };
            _animPane = new AnimationPane { Dock = DockStyle.Fill, Visible = false };
            _animPane.AnimationRequested += (s, anim) => _viewport.PlayAnimation(anim);

            // Custom Tab System for Sidebar
            var tabContainer = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = NexusStyles.BorderColor };
            var contentContainer = new Panel { Dock = DockStyle.Fill };
            
            _sidebarSplit.Panel2.Controls.Add(contentContainer);
            _sidebarSplit.Panel2.Controls.Add(tabContainer);
            
            contentContainer.Controls.Add(_options);
            contentContainer.Controls.Add(_animPane);

            var btnGen = CreateTabBtn("FORGE", tabContainer, true);
            var btnAnim = CreateTabBtn("ACT", tabContainer, false);
            
            btnGen.Click += (s, e) => { 
                _options.Visible = true; _animPane.Visible = false; 
                HighlightTab(btnGen, true); HighlightTab(btnAnim, false); 
            };
            btnAnim.Click += (s, e) => { 
                _options.Visible = false; _animPane.Visible = true; 
                HighlightTab(btnGen, false); HighlightTab(btnAnim, true); 
            };

            _sidebarSplit.SplitterDistance = this.ClientSize.Width - 320; 

            // Sidebar Controls (Palette and Load)
            var pnlSidebar = new Panel { Dock = DockStyle.Bottom, Height = 250, BackColor = Color.FromArgb(35, 35, 40), Padding = new Padding(10) };
            _sidebarSplit.Panel2.Controls.Add(pnlSidebar);
            pnlSidebar.BringToFront();

            var btnLoad = new Button { Text = "LOAD FILE", Width = 100, Dock = DockStyle.Top, Height = 35, FlatStyle = FlatStyle.Flat, ForeColor = NexusStyles.AccentCyan, BackColor = Color.FromArgb(45, 45, 50) };
            btnLoad.Click += (s, e) => LoadAsset();
            pnlSidebar.Controls.Add(btnLoad);

            AddLabel(pnlSidebar, "LOCAL CREATIONS");
            _palette = new PaletteControl(_assetService);
            _palette.OnAssetSelected += (name, recipe) => {
                ProcessAssetJson(recipe);
                AddLog($"Previewing creation: {name}");
            };
            pnlSidebar.Controls.Add(_palette);

            // Studio UI Elements
            int btnY = 5;
            int btnW = 150;
            int btnH = 35;
            int rightMargin = 20;

            _btnToggleBrain = CreateToggleBtn("OPEN AI BRAIN", NexusStyles.AccentIndigo, btnY);
            _btnToggleBrain.Click += (s, e) => ToggleBrain();
            
            _btnToggleJson = CreateToggleBtn("SHOW JSON", NexusStyles.AccentAmber, btnY);
            _btnToggleJson.Location = new Point(_btnToggleBrain.Left - btnW - 10, btnY);
            _btnToggleJson.Click += (s, e) => ToggleJson();

            _btnToggleConsole = CreateToggleBtn("STUDIO CONSOLE", Color.Lime, btnY);
            _btnToggleConsole.Location = new Point(_btnToggleJson.Left - btnW - 10, btnY);
            _btnToggleConsole.Click += (s, e) => ToggleConsole();

            var pnlControl = new Panel { Height = 220, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(30, 30, 35), Padding = new Padding(10) };
            _mainSplit.Panel1.Controls.Add(pnlControl);

            _lstConsole = new ListBox { Dock = DockStyle.Fill, BackColor = Color.Black, ForeColor = Color.Lime, Font = new Font("Consolas", 9), BorderStyle = BorderStyle.None, Visible = false };
            _mainSplit.Panel1.Controls.Add(_lstConsole);
            _lstConsole.BringToFront();

            var pnlArtType = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(40, 40, 45), FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10, 5, 0, 0) };
            pnlControl.Controls.Add(pnlArtType);

            _rbVoxel = new RadioButton { Text = "NPC", Checked = true, ForeColor = NexusStyles.WhiteText, Font = new Font("Segoe UI", 9, FontStyle.Bold), Width = 70 };
            _rbProcedural = new RadioButton { Text = "PROP", ForeColor = NexusStyles.WhiteText, Font = new Font("Segoe UI", 9, FontStyle.Bold), Width = 80 };
            _rbScene = new RadioButton { Text = "SCENE", ForeColor = NexusStyles.WhiteText, Font = new Font("Segoe UI", 9, FontStyle.Bold), Width = 80 };

            _rbVoxel.CheckedChanged += (s, e) => UpdateAIModeFile();
            _rbProcedural.CheckedChanged += (s, e) => UpdateAIModeFile();
            _rbScene.CheckedChanged += (s, e) => UpdateAIModeFile();
            UpdateAIModeFile();
            
            pnlArtType.Controls.AddRange(new Control[] { _rbVoxel, _rbProcedural, _rbScene });

            _txtPrompt = new TextBox { Dock = DockStyle.Fill, Multiline = true, BackColor = NexusStyles.CardColor, ForeColor = NexusStyles.AccentAmber, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 12) };
            pnlControl.Controls.Add(_txtPrompt);
            _txtPrompt.BringToFront();

            var pnlButtons = new Panel { Dock = DockStyle.Right, Width = 130, Padding = new Padding(5, 0, 0, 0) };
            pnlControl.Controls.Add(pnlButtons);

            _btnForge = new Button { Text = "FORGE", Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.AccentIndigo, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            _btnForge.Click += (s, e) => ForgeAsset();
            pnlButtons.Controls.Add(_btnForge);
            
            var btnQuick = new Button { Text = "QUICK STEVE", Dock = DockStyle.Top, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.CardColor, ForeColor = NexusStyles.AccentCyan, Font = new Font("Segoe UI", 8, FontStyle.Bold) };
            btnQuick.Click += (s, e) => QuickSteve();
            pnlButtons.Controls.Add(btnQuick);

            var btnExplore = new Button { Text = "EXPLORE", Dock = DockStyle.Top, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.CardColor, ForeColor = NexusStyles.AccentPink, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnExplore.Click += (s, e) => {
                if (_activeWorld == null || _activeWorld.IsDisposed) {
                    var model = _currentModel as HumanoidModel;
                    _activeWorld = new WorldForm(model, _sharedEnv, _assetService, _sceneService);
                    _activeWorld.Show();
                } else {
                    _activeWorld.BringToFront();
                }
            };
            pnlButtons.Controls.Add(btnExplore);

            _btnSave = new Button { Text = "SAVE REFINED", Dock = DockStyle.Bottom, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.CardColor, ForeColor = NexusStyles.AccentEmerald, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            _btnSave.Click += (s, e) => SaveAsset();
            pnlButtons.Controls.Add(_btnSave);

            var btnLoadRefined = new Button { Text = "LOAD REFINED", Dock = DockStyle.Bottom, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.CardColor, ForeColor = NexusStyles.AccentCyan, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnLoadRefined.Click += (s, e) => LoadAsset();
            pnlButtons.Controls.Add(btnLoadRefined);
        }

        private void RefreshPalette() => _palette.RefreshItems();

        private Button CreateToggleBtn(string text, Color color, int y)
        {
            var btn = new Button {
                Text = text,
                Size = new Size(150, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = NexusStyles.CardColor,
                ForeColor = color,
                Font = NexusStyles.HeaderFont,
                Cursor = Cursors.Hand,
                Location = new Point(this.ClientSize.Width - 170 - 320, y)
            };
            this.Controls.Add(btn);
            btn.BringToFront();
            return btn;
        }

        private void UpdateAIModeFile() {
            string m = _rbVoxel.Checked ? "Voxel" : (_rbProcedural.Checked ? "Procedural" : "Scene");
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ai_mode.txt"), m); } catch {}
        }

        private async void InitializeWebViews()
        {
            try {
                var args = "--disable-background-timer-throttling --disable-renderer-backgrounding --disable-features=CalculateNativeWinOcclusion"; 
                _sharedEnv = await CoreWebView2Environment.CreateAsync(null, null, new CoreWebView2EnvironmentOptions(additionalBrowserArguments: args));
                
                _viewport = new ViewportPane(_sharedEnv) { Dock = DockStyle.Fill };
                _viewport.LogReceived += (s, msg) => AddLog(msg);
                _mainSplit.Panel1.Controls.Add(_viewport);

                _brain = new BrainPane(_sharedEnv) { Dock = DockStyle.Fill };
                _mainSplit.Panel2.Controls.Add(_brain);

                _architectService = new ArchitectService(_brain, _assetService, _sceneService);
                _architectService.OnAssetDelivered += (model) => {
                    this.Invoke(new Action(() => {
                        if (model is HumanoidModel h) {
                             var skinGen = new SkinGenerator();
                             var bmp = skinGen.Generate(h.SkinToneHex, h.ShirtHex, h.PantsHex, h.EyeHex, h.FacePixels, h.HatPixels, h.ChestPixels, h.ArmPixels, h.LegPixels);
                             using (var ms = new System.IO.MemoryStream()) {
                                 bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                 h.SkinBase64 = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                             }
                        }
                        _currentModel = model;
                        _viewport.RenderModel(model);
                        _jsonViewer.SetJson(model.ExportJson());
                        _palette.RefreshItems(); // Sync palette
                        _btnForge.Enabled = true;
                        _btnForge.Text = "FORGE";
                        AddLog($"AI Asset Delivered & Saved: {model.Name}");
                    }));
                };
                _architectService.OnSceneDelivered += (json) => {
                    this.Invoke(new Action(() => {
                        _jsonViewer.SetJson(json);
                        _btnForge.Enabled = true;
                        _btnForge.Text = "FORGE";
                        AddLog("AI Scene Delivered.");
                    }));
                };
                _architectService.OnStatusUpdate += (msg) => AddLog(msg);
                _mainSplit.SplitterDistance = _mainSplit.Height - 1;
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ForgeAsset()
        {
            string userPrompt = _txtPrompt.Text.Trim();
            if (string.IsNullOrEmpty(userPrompt)) return;
            string mode = _rbVoxel.Checked ? "Voxel" : (_rbProcedural.Checked ? "Procedural" : "Scene");
            var options = new VoxelOptions {
                GenerateFace = _options.GenerateFace,
                GenerateChest = _options.GenerateChest,
                GenerateArms = _options.GenerateArms,
                GenerateLegs = _options.GenerateLegs
            };
            _architectService.Forge(userPrompt, mode, options);
            _btnForge.Enabled = false;
            _btnForge.Text = "WAITING...";
        }

        private void QuickSteve()
        {
            var h = new HumanoidModel { Name = "Quick Steve", SkinToneHex = "#C68E6F", ShirtHex = "#0099AA", PantsHex = "#333399", EyeHex = "#FFFFFF" };
            var skinGen = new SkinGenerator();
            var bmp = skinGen.Generate(h.SkinToneHex, h.ShirtHex, h.PantsHex, h.EyeHex, h.FacePixels, h.HatPixels, h.ChestPixels, h.ArmPixels, h.LegPixels);
            using (var ms = new System.IO.MemoryStream()) {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                h.SkinBase64 = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }
            _currentModel = h;
            _viewport.RenderModel(h);
        }

        private void SaveAsset() 
        { 
            if (_currentModel == null) return;
            string json = (_currentModel is ProceduralModel p) ? p.RawRecipeJson : _currentModel.ExportJson();
            var category = (_currentModel is HumanoidModel) ? AssetCategory.Actor : AssetCategory.Prop;
            _assetService.SaveAsset(_currentModel.Name, json, category);
            AddLog($"Asset Saved: {_currentModel.Name}");
        }

        private void LoadAsset() { 
            string json = _assetService.LoadAsset(); 
            if (!string.IsNullOrEmpty(json)) ProcessAssetJson(json);
        }

        private void ProcessAssetJson(string json)
        {
            try {
                using var doc = JsonDocument.Parse(json);
                string type = doc.RootElement.TryGetProperty("Type", out var t) ? t.GetString() ?? "Voxel" : "Voxel";
                string name = doc.RootElement.TryGetProperty("Name", out var n) ? n.GetString() ?? "Unnamed" : "Unnamed";

                if (type == "Procedural") {
                    _currentModel = new ProceduralModel { RawRecipeJson = json, Name = name };
                    _viewport.RenderRecipe(json);
                } else {
                    var human = ReconstructHumanoid(doc.RootElement);
                    _currentModel = human;
                    _viewport.RenderModel(human);
                }
                _jsonViewer.SetJson(json);
                AddLog($"Processed: {name} ({type})");
            } catch (Exception ex) { AddLog($"Process Error: {ex.Message}"); }
        }

        private HumanoidModel ReconstructHumanoid(JsonElement root)
        {
            var h = new HumanoidModel { Name = root.TryGetProperty("Name", out var n) ? n.GetString() ?? "Entity" : "Entity" };
            if (root.TryGetProperty("ProceduralColors", out var colors)) {
                h.SkinToneHex = GetColor(colors, "Skin", h.SkinToneHex);
                h.ShirtHex = GetColor(colors, "Shirt", h.ShirtHex);
                h.PantsHex = GetColor(colors, "Pants", h.PantsHex);
                h.EyeHex = GetColor(colors, "Eyes", h.EyeHex);
            }
            if (root.TryGetProperty("Textures", out var tex)) {
                h.FacePixels = GetPixels(tex, "Face");
                h.HatPixels = GetPixels(tex, "Hat");
                if (h.HatPixels != null) h.ShowHat = true;
                h.ChestPixels = GetPixels(tex, "Chest");
                h.ArmPixels = GetPixels(tex, "Arms");
                h.LegPixels = GetPixels(tex, "Legs");
            }
            
            // Skin generation via Host-Specific service
            var skinGen = new SkinGenerator();
            var bmp = skinGen.Generate(h.SkinToneHex, h.ShirtHex, h.PantsHex, h.EyeHex, h.FacePixels, h.HatPixels, h.ChestPixels, h.ArmPixels, h.LegPixels);
            using (var ms = new System.IO.MemoryStream()) {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                h.SkinBase64 = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }
            return h;
        }

        private string GetColor(JsonElement el, string prop, string def) => el.TryGetProperty(prop, out var p) ? p.GetString() ?? def : def;

        private string[]? GetPixels(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var px) && px.ValueKind == JsonValueKind.Array) {
                var list = new List<string>();
                foreach (var p in px.EnumerateArray()) list.Add(p.GetString() ?? "#000000");
                return list.ToArray();
            }
            return null;
        }
        
        private void ToggleBrain() { _isBrainExpanded = !_isBrainExpanded; _mainSplit.SplitterDistance = _isBrainExpanded ? this.ClientSize.Height / 2 : _mainSplit.Height - 1; _btnToggleBrain.Text = _isBrainExpanded ? "CLOSE AI BRAIN" : "OPEN AI BRAIN"; }
        private void ToggleJson() { _isJsonExpanded = !_isJsonExpanded; _leftSplit.Panel1Collapsed = !_isJsonExpanded; _btnToggleJson.Text = _isJsonExpanded ? "HIDE JSON" : "SHOW JSON"; }
        private void ToggleConsole() { _isConsoleExpanded = !_isConsoleExpanded; _lstConsole.Visible = _isConsoleExpanded; _btnToggleConsole.Text = _isConsoleExpanded ? "CLOSE CONSOLE" : "STUDIO CONSOLE"; }

        private void AddLog(string msg) { if (this.InvokeRequired) { this.Invoke(new Action(() => AddLog(msg))); return; } _lstConsole.Items.Add($"[{DateTime.Now:HH:mm:ss}] {msg}"); _lstConsole.SelectedIndex = _lstConsole.Items.Count - 1; }

        private Button CreateTabBtn(string text, Panel parent, bool active) {
            var btn = new Button { Text = text, Dock = DockStyle.Left, Width = parent.Width / 2, FlatStyle = FlatStyle.Flat, Font = NexusStyles.HeaderFont };
            btn.FlatAppearance.BorderSize = 0;
            HighlightTab(btn, active);
            parent.Controls.Add(btn);
            parent.SizeChanged += (s,e) => btn.Width = parent.Width / 2;
            return btn;
        }

        private void HighlightTab(Button btn, bool active) { btn.BackColor = active ? NexusStyles.CardColor : Color.FromArgb(40, 40, 45); btn.ForeColor = active ? NexusStyles.AccentCyan : Color.Gray; }
        private void AddLabel(Panel p, string text) { p.Controls.Add(new Label { Text = text, Dock = DockStyle.Top, Height = 25, ForeColor = Color.LightGray, TextAlign = ContentAlignment.BottomLeft }); }
    }
}
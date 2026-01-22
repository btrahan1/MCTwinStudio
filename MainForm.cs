using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using MCTwinStudio.Core;
using MCTwinStudio.Core.Models;
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
        private AssetService _assetService = null!;
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
        private RadioButton _rbSculpted = null!;

        private BaseModel? _currentModel = null;
        private string _currentDescription = "";

        public MainForm()
        {
            this.Text = "MCTwin Studio // Minecraft Meta-Architect";
            this.Size = new Size(1600, 1000);
            this.BackColor = NexusStyles.BackColor;
            this.ForeColor = NexusStyles.WhiteText;
            this.StartPosition = FormStartPosition.CenterScreen;
            _assetService = new AssetService();
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

            // Toggle Buttons Area

            // Toggle Buttons Area
            int btnY = 5;
            int btnW = 150;
            int btnH = 35;
            int rightMargin = 20;

            // Brain Toggle
            _btnToggleBrain = new Button
            {
                Text = "OPEN AI BRAIN",
                Size = new Size(btnW, btnH),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = NexusStyles.CardColor,
                ForeColor = NexusStyles.AccentIndigo,
                Font = NexusStyles.HeaderFont,
                Cursor = Cursors.Hand
            };
            _btnToggleBrain.Location = new Point(this.ClientSize.Width - (rightMargin + btnW + 320), btnY); // Offset from Sidebar
            _btnToggleBrain.Click += (s, e) => ToggleBrain();
            this.Controls.Add(_btnToggleBrain);
            _btnToggleBrain.BringToFront();

            // JSON Toggle
            _btnToggleJson = new Button
            {
                Text = "SHOW JSON",
                Size = new Size(btnW, btnH),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = NexusStyles.CardColor,
                ForeColor = NexusStyles.AccentAmber,
                Font = NexusStyles.HeaderFont,
                Cursor = Cursors.Hand
            };
            _btnToggleJson.Location = new Point(_btnToggleBrain.Left - btnW - 10, btnY);
            _btnToggleJson.Click += (s, e) => ToggleJson();
            this.Controls.Add(_btnToggleJson);
            _btnToggleJson.BringToFront();

            _btnToggleConsole = new Button
            {
                Text = "STUDIO CONSOLE",
                Size = new Size(btnW, btnH),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = NexusStyles.CardColor,
                ForeColor = Color.Lime,
                Font = NexusStyles.HeaderFont,
                Cursor = Cursors.Hand
            };
            _btnToggleConsole.Location = new Point(_btnToggleJson.Left - btnW - 10, btnY);
            _btnToggleConsole.Click += (s, e) => ToggleConsole();
            this.Controls.Add(_btnToggleConsole);
            _btnToggleConsole.BringToFront();

            var pnlControl = new Panel { Height = 220, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(30, 30, 35), Padding = new Padding(10) };
            _mainSplit.Panel1.Controls.Add(pnlControl);

            // Studio Console (Hidden by default)
            _lstConsole = new ListBox { Dock = DockStyle.Fill, BackColor = Color.Black, ForeColor = Color.Lime, Font = new Font("Consolas", 9), BorderStyle = BorderStyle.None, Visible = false };
            _mainSplit.Panel1.Controls.Add(_lstConsole);
            _lstConsole.BringToFront();

            // Art Type Selection
            var pnlArtType = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(40, 40, 45), FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10, 5, 0, 0) };
            pnlControl.Controls.Add(pnlArtType);

            _rbVoxel = new RadioButton { Text = "VOXEL", Checked = true, ForeColor = NexusStyles.WhiteText, Font = new Font("Segoe UI", 9, FontStyle.Bold), Width = 100 };
            _rbProcedural = new RadioButton { Text = "PROCEDURAL", ForeColor = NexusStyles.WhiteText, Font = new Font("Segoe UI", 9, FontStyle.Bold), Width = 120 };
            _rbSculpted = new RadioButton { Text = "SCULPTED (.GLB)", ForeColor = Color.Gray, Font = new Font("Segoe UI", 9, FontStyle.Bold), Width = 150, Enabled = false };
            
            pnlArtType.Controls.AddRange(new Control[] { _rbVoxel, _rbProcedural, _rbSculpted });

            _txtPrompt = new TextBox { Dock = DockStyle.Fill, Multiline = true, BackColor = NexusStyles.CardColor, ForeColor = NexusStyles.AccentAmber, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 12) };
            pnlControl.Controls.Add(_txtPrompt);
            _txtPrompt.BringToFront(); // Ensure it sits below the FlowPanel if Docking gets weird

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
                    _activeWorld = new WorldForm(_currentModel as HumanoidModel, _sharedEnv);
                    _activeWorld.Show();
                } else {
                    _activeWorld.BringToFront();
                }
            };
            pnlButtons.Controls.Add(btnExplore);

            var btnImport = new Button { Text = "IMPORT PROP", Dock = DockStyle.Top, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.CardColor, ForeColor = NexusStyles.AccentGold, Font = new Font("Segoe UI", 8, FontStyle.Bold) };
            btnImport.Click += (s, e) => {
                if (_activeWorld == null || _activeWorld.IsDisposed) { MessageBox.Show("Please open World Explorer first."); return; }
                
                using (var ofd = new OpenFileDialog { Filter = "JSON Files|*.json|All Files|*.*", Title = "Import Prop from Nexus" }) {
                     if (ofd.ShowDialog() == DialogResult.OK) {
                         try {
                             string json = System.IO.File.ReadAllText(ofd.FileName);
                             _activeWorld.ImportProp(json);
                         } catch (Exception ex) { MessageBox.Show("Error loading prop: " + ex.Message); }
                     }
                }
            };
            pnlButtons.Controls.Add(btnImport);

            _btnSave = new Button { Text = "SAVE REFINED", Dock = DockStyle.Bottom, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.CardColor, ForeColor = NexusStyles.AccentEmerald, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            _btnSave.Click += (s, e) => SaveAsset();
            pnlButtons.Controls.Add(_btnSave);

            var btnLoad = new Button { Text = "LOAD", Width = 80, Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat, ForeColor = NexusStyles.AccentCyan };
            btnLoad.Click += (s, e) => LoadAsset();
            pnlControl.Controls.Add(btnLoad);
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
                _brain.JsonResponseReceived += OnJsonResponseReceived;
                _mainSplit.Panel2.Controls.Add(_brain);
                _mainSplit.SplitterDistance = _mainSplit.Height - 1;
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ForgeAsset()
        {
            string userPrompt = _txtPrompt.Text.Trim();
            if (string.IsNullOrEmpty(userPrompt)) return;
            
            string artType = _rbProcedural.Checked ? "Procedural" : "Voxel";
            string primer = MCTwinProtocol.GetPrimer(artType);
            
            string fullPrompt = primer;
            
            if (artType == "Voxel") {
                var regions = new List<string>();
                if (_options.GenerateFace) regions.Add("Face");
                if (_options.GenerateChest) regions.Add("Chest");
                if (_options.GenerateArms) regions.Add("Arms");
                if (_options.GenerateLegs) regions.Add("Legs");
                
                if (regions.Count > 0) {
                    fullPrompt += $"\n\n[MANDATORY GENERATION RULES]\nGenerate pixel textures ONLY for these keys: {string.Join(", ", regions)}.\nOmit keys for unlisted regions.";
                } else {
                    fullPrompt += "\n\n[MANDATORY GENERATION RULES]\nDo NOT generate any pixel textures (Face, Chest, Arms, Legs). Only provide procedural hex colors.";
                }
            }
            
            fullPrompt += "\n\n### USER REQUEST:\n" + userPrompt;
            _brain.SendPrompt(fullPrompt);
            _btnForge.Enabled = false;
            _btnForge.Text = "WAITING...";
        }

        private void QuickSteve()
        {
            var steve = new HumanoidModel {
                SkinToneHex = "#C68E6F", // Tan
                ShirtHex = "#0099AA",    // Cyan
                PantsHex = "#333399",    // Indigo
                EyeHex = "#FFFFFF"
            };
            steve.Name = "Quick Steve";
            steve.GenerateSkin(); // Force generation
            _currentModel = steve;
            _viewport.RenderModel(steve);
            _currentDescription = "Quick Steve Generated Prototype";
        }

        private void OnJsonResponseReceived(object? sender, string json)
        {
            this.Invoke(new Action(() => {
                ApplyJsonToState(json);
                _btnForge.Enabled = true;
                _btnForge.Text = "FORGE";
            }));
        }

        private void ApplyJsonToState(string json)
        {
            try {
                _jsonViewer?.SetJson(json);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                // 1. Detect Type
                string type = root.TryGetProperty("Type", out var tProp) ? tProp.GetString() ?? "Voxel" : "Voxel";

                if (type == "Procedural")
                {
                    var proc = new ProceduralModel { RawRecipeJson = json };
                    proc.Name = root.TryGetProperty("Name", out var pn) ? pn.GetString() ?? "Prop" : "Prop";
                    _currentModel = proc;
                    _currentDescription = root.TryGetProperty("Description", out var pd) ? pd.GetString() ?? "" : "";
                    _viewport.RenderRecipe(json);
                    return;
                }

                // 2. Instantiate Model (Legacy Voxel Flow)
                _currentModel = new HumanoidModel();

                _currentModel.Name = root.TryGetProperty("Name", out var n) ? n.GetString() ?? "Entity" : "Entity";
                _currentDescription = root.TryGetProperty("Description", out var d) ? d.GetString() ?? "" : "";

                // 3. Apply Colors
                if (_currentModel is HumanoidModel human) {
                    if (root.TryGetProperty("ProceduralColors", out var colors)) {
                        human.SkinToneHex = GetColor(colors, "Skin", human.SkinToneHex);
                        human.ShirtHex = GetColor(colors, "Shirt", human.ShirtHex);
                        human.PantsHex = GetColor(colors, "Pants", human.PantsHex);
                        human.EyeHex = GetColor(colors, "Eyes", human.EyeHex);
                    }
                    
                    // 4. Apply Textures
                    if (root.TryGetProperty("Textures", out var tex)) {
                        if (tex.TryGetProperty("Face", out var faceJson) && faceJson.ValueKind == JsonValueKind.Array) {
                            var list = new System.Collections.Generic.List<string>();
                            foreach(var pixel in faceJson.EnumerateArray()) list.Add(pixel.GetString() ?? "#000000");
                            human.FacePixels = list.ToArray();
                        }
                        if (tex.TryGetProperty("Hat", out var hatEl) && hatEl.ValueKind == JsonValueKind.Array) {
                            var px = new List<string>();
                            foreach (var el in hatEl.EnumerateArray()) px.Add(el.GetString() ?? "#TRANSPARENT");
                            human.HatPixels = px.ToArray();
                            human.ShowHat = true; // Auto-enable if generated
                        }

                        if (tex.TryGetProperty("Chest", out var chestEl) && chestEl.ValueKind == JsonValueKind.Array) {
                            var px = new List<string>();
                            foreach (var el in chestEl.EnumerateArray()) px.Add(el.GetString() ?? "#TRANSPARENT");
                            human.ChestPixels = px.ToArray();
                        }

                        // Arms
                        if (tex.TryGetProperty("Arms", out var armEl) && armEl.ValueKind == JsonValueKind.Array) {
                            var px = new List<string>();
                            foreach (var el in armEl.EnumerateArray()) px.Add(el.GetString() ?? "#TRANSPARENT");
                            human.ArmPixels = px.ToArray();
                        }

                        // Legs
                        if (tex.TryGetProperty("Legs", out var legEl) && legEl.ValueKind == JsonValueKind.Array) {
                            var px = new List<string>();
                            foreach (var el in legEl.EnumerateArray()) px.Add(el.GetString() ?? "#TRANSPARENT");
                            human.LegPixels = px.ToArray();
                        }
                    }
                }

                // 5. Render
                if (_currentModel is HumanoidModel humanModel) humanModel.GenerateSkin();
                _viewport.RenderModel(_currentModel);

            } catch (Exception ex) {
                // Ignore parsing errors for now, or log them
                System.Diagnostics.Debug.WriteLine($"Error parsing brain response: {ex.Message}");
            }
        }

        private string GetColor(JsonElement el, string prop, string def) => el.TryGetProperty(prop, out var p) ? p.GetString() ?? def : def;

        // private void SyncRefinerTo3D() { ... } // Removed
        private void SaveAsset() 
        { 
            if (_currentModel == null) return;
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json;

            if (_currentModel is ProceduralModel p)
            {
                json = p.RawRecipeJson;
                // If RawRecipeJson is empty for some reason, we fallback to a minimal save, 
                // but it should be populated in ApplyJsonToState.
                if (string.IsNullOrEmpty(json)) {
                     var data = new { Name = p.Name, Type = "Procedural", Description = _currentDescription, Parts = new List<object>() };
                     json = JsonSerializer.Serialize(data, options);
                }
            }
            else
            {
                // Reconstruct JSON to match Protocol (Voxel)
                var data = new {
                    Name = _currentModel.Name,
                    Type = _currentModel.ModelType,
                    Description = _currentDescription,
                    ProceduralColors = (_currentModel is HumanoidModel h) ? new {
                        Skin = h.SkinToneHex,
                        Shirt = h.ShirtHex,
                        Pants = h.PantsHex,
                        Eyes = h.EyeHex
                    } : null,
                    Textures = (_currentModel is HumanoidModel ht) ? new {
                        Face = ht.FacePixels,
                        Hat = ht.HatPixels,
                        Chest = ht.ChestPixels,
                        Arms = ht.ArmPixels,
                        Legs = ht.LegPixels
                    } : null
                };
                json = JsonSerializer.Serialize(data, options);
            }

            _assetService.SaveAsset(_currentModel.Name, json);
        }
        private void LoadAsset() { string json = _assetService.LoadAsset(); if (!string.IsNullOrEmpty(json)) ApplyJsonToState(json); }
        

        
        private void ToggleBrain() 
        { 
            _isBrainExpanded = !_isBrainExpanded; 
            _mainSplit.SplitterDistance = _isBrainExpanded ? this.ClientSize.Height / 2 : _mainSplit.Height - 1;
            _btnToggleBrain.Text = _isBrainExpanded ? "CLOSE AI BRAIN" : "OPEN AI BRAIN";
        }

        private void ToggleJson()
        {
            _isJsonExpanded = !_isJsonExpanded;
            _leftSplit.Panel1Collapsed = !_isJsonExpanded;
            _leftSplit.SplitterDistance = 400; // Default width
            _btnToggleJson.Text = _isJsonExpanded ? "HIDE JSON" : "SHOW JSON";
        }

        private void ToggleConsole()
        {
            _isConsoleExpanded = !_isConsoleExpanded;
            _lstConsole.Visible = _isConsoleExpanded;
            if (_isConsoleExpanded) _lstConsole.BringToFront();
            else _txtPrompt.BringToFront();
            _btnToggleConsole.Text = _isConsoleExpanded ? "CLOSE CONSOLE" : "STUDIO CONSOLE";
        }

        private void AddLog(string msg)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => AddLog(msg))); return; }
            _lstConsole.Items.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
            _lstConsole.SelectedIndex = _lstConsole.Items.Count - 1;
        }

        private Button CreateTabBtn(string text, Panel parent, bool active)
        {
            var btn = new Button {
                Text = text,
                Dock = DockStyle.Left,
                Width = parent.Width / 2, // Approximate
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = NexusStyles.HeaderFont,
                Margin = Padding.Empty
            };
            btn.FlatAppearance.BorderSize = 0;
            HighlightTab(btn, active);
            parent.Controls.Add(btn);
            // Fix width resize
            parent.SizeChanged += (s,e) => btn.Width = parent.Width / 2;
            return btn;
        }

        private void HighlightTab(Button btn, bool active)
        {
            if (active) {
                btn.BackColor = NexusStyles.CardColor;
                btn.ForeColor = NexusStyles.AccentCyan;
            } else {
                btn.BackColor = Color.FromArgb(40, 40, 45); // Darker
                btn.ForeColor = Color.Gray;
            }
        }
    }
}
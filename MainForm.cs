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
            _sidebarSplit.Panel2.Controls.Add(_options);
            _sidebarSplit.SplitterDistance = this.ClientSize.Width - 300; // slightly narrower sidebar

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

            var pnlControl = new Panel { Height = 120, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(30, 30, 35), Padding = new Padding(10) };
            _mainSplit.Panel1.Controls.Add(pnlControl);
            _txtPrompt = new TextBox { Dock = DockStyle.Fill, Multiline = true, BackColor = NexusStyles.CardColor, ForeColor = NexusStyles.AccentAmber, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 12) };
            pnlControl.Controls.Add(_txtPrompt);

            var pnlButtons = new Panel { Dock = DockStyle.Right, Width = 130, Padding = new Padding(5, 0, 0, 0) };
            pnlControl.Controls.Add(pnlButtons);
            _btnForge = new Button { Text = "FORGE", Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.AccentIndigo, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            _btnForge.Click += (s, e) => ForgeAsset();
            pnlButtons.Controls.Add(_btnForge);
            
            var btnQuick = new Button { Text = "QUICK STEVE", Dock = DockStyle.Top, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.CardColor, ForeColor = NexusStyles.AccentCyan, Font = new Font("Segoe UI", 8, FontStyle.Bold) };
            btnQuick.Click += (s, e) => QuickSteve();
            pnlButtons.Controls.Add(btnQuick);

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
            
            // Build Region Instructions
            var regions = new List<string>();
            if (_options.GenerateFace) regions.Add("Face");
            if (_options.GenerateChest) regions.Add("Chest");
            if (_options.GenerateArms) regions.Add("Arms");
            if (_options.GenerateLegs) regions.Add("Legs");
            
            string instructions = "";
            if (regions.Count > 0) {
                instructions = $"\n\n[MANDATORY GENERATION RULES]\nGenerate pixel textures ONLY for these keys: {string.Join(", ", regions)}.\nOmit keys for unlisted regions.";
            } else {
                instructions = "\n\n[MANDATORY GENERATION RULES]\nDo NOT generate any pixel textures (Face, Chest, Arms, Legs). Only provide procedural hex colors.";
            }

            string fullPrompt = MCTwinProtocol.SystemPrimer + instructions + "\n\n### USER REQUEST:\n" + userPrompt;
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
                string type = root.TryGetProperty("Type", out var tProp) ? tProp.GetString() ?? "Humanoid" : "Humanoid";

                // 2. Instantiate Model
                _currentModel = type switch {
                    _ => new HumanoidModel() // Default to Humanoid
                };

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
            // Reconstruct JSON to match Protocol
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

            string json = JsonSerializer.Serialize(data, options);
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
    }
}
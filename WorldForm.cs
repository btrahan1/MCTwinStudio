using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MCTwinStudio.Core;
using MCTwinStudio.Core.Models;

namespace MCTwinStudio
{
    public class WorldForm : Form
    {
        private WebView2 _webView;
        private HumanoidModel _model;
        private CoreWebView2Environment _env;
        private Services.AssetService _assetService;
        private Services.SceneService _sceneService;
        private ListBox _lstPalette;

        public WorldForm(HumanoidModel model, CoreWebView2Environment env, Services.AssetService assetService)
        {
            _model = model;
            _assetService = assetService;
            _sceneService = new Services.SceneService();
            
            if (_model == null) {
                _model = new HumanoidModel {
                    SkinToneHex = "#C68E6F", // Tan
                    ShirtHex = "#0099AA",    // Cyan
                    PantsHex = "#333399",    // Indigo
                    EyeHex = "#FFFFFF",
                    Name = "Quick Steve"
                };
                _model.GenerateSkin();
            }
            _env = env;

            this.Text = $"World Explorer - {_model.Name}";
            this.Size = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = NexusStyles.BackColor;

            // Main Layout Container
            var splitContainer = new SplitContainer { 
                Dock = DockStyle.Fill, 
                Orientation = Orientation.Vertical, 
                FixedPanel = FixedPanel.Panel2
            };
            this.Controls.Add(splitContainer);
            
            // Set distance first based on form size
            splitContainer.SplitterDistance = Math.Max(0, this.ClientSize.Width - 300);
            
            // Now apply min constraints
            splitContainer.Panel1MinSize = 400;
            splitContainer.Panel2MinSize = 250;

            _webView = new WebView2 { Dock = DockStyle.Fill };
            splitContainer.Panel1.Controls.Add(_webView);

            // Controls Panel
            var pnlSettings = new Panel { 
                Dock = DockStyle.Fill, 
                BackColor = NexusStyles.CardColor, 
                Padding = new Padding(15),
                AutoScroll = true // Ensure scroll if controls exceed height
            };
            splitContainer.Panel2.Controls.Add(pnlSettings);

            // Add controls from bottom to top of the visual stack (since Dock.Top stacks)
            // Or better: Add them and BringToFront() in reverse order.
            // Let's just follow the visual order the user saw in the screenshot but fix the clipping.

            AddLabel(pnlSettings, "Floor Size");
            var trkSize = new TrackBar { Dock = DockStyle.Top, Minimum = 100, Maximum = 5000, Value = 500, LargeChange = 100, SmallChange = 10 };
            trkSize.Scroll += (s, e) => UpdateWorld("groundSize", trkSize.Value.ToString());
            pnlSettings.Controls.Add(trkSize);

            AddLabel(pnlSettings, "Light Intensity");
            var trkLight = new TrackBar { Dock = DockStyle.Top, Minimum = 0, Maximum = 20, Value = 10 };
            trkLight.Scroll += (s, e) => UpdateWorld("lightIntensity", (trkLight.Value / 10.0).ToString());
            pnlSettings.Controls.Add(trkLight);

            var chkGround = new CheckBox { Text = "Show Floor", Dock = DockStyle.Top, Height = 30, ForeColor = Color.White, Checked = true };
            chkGround.CheckedChanged += (s, e) => UpdateWorld("groundVisible", chkGround.Checked ? "true" : "false");
            pnlSettings.Controls.Add(chkGround);

            AddLabel(pnlSettings, "Manipulation");
            var pnlGizmo = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 130 }; // Height for 3 rows
            AddGizmoBtn(pnlGizmo, "MOVE", "move");
            AddGizmoBtn(pnlGizmo, "ROT", "rotate");
            AddGizmoBtn(pnlGizmo, "SIZE", "scale");
            AddGizmoBtn(pnlGizmo, "DRAG", "drag");
            AddGizmoBtn(pnlGizmo, "OFF", "none");
            
            var btnGrid = new Button { Text = "GRID: OFF", Width = 105, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White };
            bool gridOn = false;
            btnGrid.Click += async (s, e) => {
                gridOn = !gridOn;
                btnGrid.Text = gridOn ? "GRID: ON" : "GRID: OFF";
                if (_webView?.CoreWebView2 != null) await _webView.ExecuteScriptAsync($"window.MCTwin.toggleGrid({(gridOn ? "true" : "false")});");
            };
            pnlGizmo.Controls.Add(btnGrid);
            
            var btnDebug = new Button { Text = "DEBUG: OFF", Width = 105, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 30, 30), ForeColor = Color.White };
            bool debugOn = false;
            btnDebug.Click += async (s, e) => {
                debugOn = !debugOn;
                btnDebug.Text = debugOn ? "DEBUG: ON" : "DEBUG: OFF";
                btnDebug.BackColor = debugOn ? Color.FromArgb(30, 100, 30) : Color.FromArgb(100, 30, 30);
                if (_webView?.CoreWebView2 != null) await _webView.ExecuteScriptAsync($"window.MCTwin.toggleDebug({(debugOn ? "true" : "false")});");
            };
            pnlGizmo.Controls.Add(btnDebug);
            pnlSettings.Controls.Add(pnlGizmo);

            AddLabel(pnlSettings, "Floor Color");
            AddSetting(pnlSettings, "Pick Color", (s, e) => {
                using (var cd = new ColorDialog()) { if (cd.ShowDialog() == DialogResult.OK) UpdateWorld("groundColor", $"#{cd.Color.R:X2}{cd.Color.G:X2}{cd.Color.B:X2}"); }
            });

            AddLabel(pnlSettings, "Floor Theme");
            var cmbTheme = new ComboBox { 
                Dock = DockStyle.Top, 
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbTheme.Items.AddRange(new object[] { "Checker", "Concrete", "Wood", "Desert", "Grass", "Space" });
            cmbTheme.SelectedIndex = 2; // Default to Wood
            cmbTheme.SelectedIndexChanged += (s, e) => UpdateWorld("floorTheme", cmbTheme.SelectedItem.ToString());
            pnlSettings.Controls.Add(cmbTheme);
            pnlSettings.Controls.Add(new Panel { Height = 10, Dock = DockStyle.Top }); // Spacer

            AddSetting(pnlSettings, "IMPORT PROP", (s, e) => {
                using (var ofd = new OpenFileDialog { Filter = "JSON Files|*.json" }) {
                    if (ofd.ShowDialog() == DialogResult.OK) {
                        try { ImportProp(File.ReadAllText(ofd.FileName)); } catch (Exception ex) { MessageBox.Show(ex.Message); }
                    }
                }
            });

            AddSetting(pnlSettings, "Sky Color", (s, e) => {
                using (var cd = new ColorDialog()) { if (cd.ShowDialog() == DialogResult.OK) UpdateWorld("skyColor", $"#{cd.Color.R:X2}{cd.Color.G:X2}{cd.Color.B:X2}"); }
            });

            AddSetting(pnlSettings, "LOAD SCENE", async (s, e) => {
                string json = _sceneService.LoadScene();
                if (!string.IsNullOrEmpty(json) && _webView?.CoreWebView2 != null) {
                    try {
                        var sceneData = System.Text.Json.JsonSerializer.Deserialize<Core.Models.SceneModel>(json);
                        if (sceneData != null) {
                            await _webView.ExecuteScriptAsync("window.MCTwin.clearAll();");
                            foreach (var item in sceneData.Items) {
                                string recipe = _assetService.GetBestMatch(item.RecipeName);
                                if (!string.IsNullOrEmpty(recipe)) ImportProp(recipe, item.RecipeName, item);
                            }
                        }
                    } catch (Exception ex) { MessageBox.Show("Error loading scene: " + ex.Message); }
                }
            });

            AddSetting(pnlSettings, "SAVE SCENE", async (s, e) => {
                if (_webView?.CoreWebView2 != null) {
                    string sceneName = Microsoft.VisualBasic.Interaction.InputBox("Enter Scene Name:", "Save Scene", "Warehouse1");
                    if (!string.IsNullOrEmpty(sceneName)) {
                        string res = await _webView.ExecuteScriptAsync("window.MCTwin.getSceneData();");
                        // WebView2 returns a quoted string if it's a JS string, but we want the outer JSON
                        if (res != "null" && !string.IsNullOrEmpty(res)) {
                             // The result is a JSON string of a JSON string.
                             string sceneJson = System.Text.Json.JsonSerializer.Deserialize<string>(res);
                             _sceneService.SaveScene(sceneName, sceneJson);
                        }
                    }
                }
            });

            AddLabel(pnlSettings, "Asset Palette");
            _lstPalette = new ListBox {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9)
            };
            _lstPalette.DoubleClick += (s, e) => {
                if (_lstPalette.SelectedItem != null) {
                    string name = _lstPalette.SelectedItem.ToString();
                    string recipe = _assetService.GetBestMatch(name);
                    if (!string.IsNullOrEmpty(recipe)) ImportProp(recipe, name);
                }
            };
            pnlSettings.Controls.Add(_lstPalette);
            RefreshPalette();

            var lblTitle = new Label { 
                Text = "WORLD SETTINGS", 
                Dock = DockStyle.Top, 
                Height = 40, 
                ForeColor = NexusStyles.AccentGold, 
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlSettings.Controls.Add(lblTitle);

            // Re-stack everything from top to bottom (BringToFront on everything in order)
            foreach (Control c in pnlSettings.Controls) c.BringToFront();

            InitializeAsync();
        }

        private void AddSetting(Panel p, string text, EventHandler click)
        {
            var btn = new Button { 
                Text = text, 
                Dock = DockStyle.Top, 
                Height = 35, 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(40, 40, 40), 
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 0, 5)
            };
            btn.Click += click;
            p.Controls.Add(btn);
            p.Controls.Add(new Panel { Height = 5, Dock = DockStyle.Top }); // Spacer
        }

        private void AddLabel(Panel p, string text)
        {
            p.Controls.Add(new Label { Text = text, Dock = DockStyle.Top, Height = 25, ForeColor = Color.LightGray, TextAlign = ContentAlignment.BottomLeft });
        }

        private void AddGizmoBtn(FlowLayoutPanel p, string text, string mode)
        {
            var btn = new Button { 
                Text = text, 
                Width = 50, 
                Height = 35, 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(60, 60, 60), 
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 7, FontStyle.Bold)
            };
            btn.Click += async (s, e) => {
                if (_webView?.CoreWebView2 != null)
                    await _webView.ExecuteScriptAsync($"window.MCTwinGizmos.setMode('{mode}');");
            };
            p.Controls.Add(btn);
        }

        private async void UpdateWorld(string property, string value)
        {
             if (_webView?.CoreWebView2 != null)
             {
                 string script = $"window.MCTwin.updateWorld({{ {property}: {(property.EndsWith("Visible") || property.EndsWith("Intensity") || property.EndsWith("Size") ? value : $"'{value}'")} }});";
                 await _webView.ExecuteScriptAsync(script);
             }
        }

        private async void InitializeAsync()
        {
            await _webView.EnsureCoreWebView2Async(_env);
            
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "world.html");
            if (File.Exists(htmlPath)) 
            {
                _webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
            }

            _webView.CoreWebView2.WebMessageReceived += (s, e) => {
                System.Diagnostics.Debug.WriteLine("JS LOG: " + e.TryGetWebMessageAsString());
            };

            _webView.NavigationCompleted += (s, e) => {
                if (e.IsSuccess) RenderModel();
            };
            
            // Focus webview for keyboard input
            _webView.Focus();
        }

        private async void RenderModel()
        {
            var parts = _model.GetParts();
            var payload = new { Parts = parts, Skin = _model.SkinBase64 };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            string script = $"window.MCTwin.renderModel({json});";
            await _webView.ExecuteScriptAsync(script);
        }

        public async void ImportProp(string json, string recipeName = "Unknown", Core.Models.SceneItem itemTransform = null)
        {
            if (_webView?.CoreWebView2 == null) return;
            System.Diagnostics.Debug.WriteLine($"IMPORTING: {recipeName}");

            try {
                using (var doc = System.Text.Json.JsonDocument.Parse(json)) {
                    var root = doc.RootElement;
                    string type = root.TryGetProperty("Type", out var t) ? t.GetString() : "Procedural";
                    string transformJson = itemTransform != null ? System.Text.Json.JsonSerializer.Serialize(itemTransform) : "null";
                    
                    if (type == "Voxel" || type == "Humanoid") {
                        // It's a humanoid! We need to generate its skin and parts.
                        var human = new HumanoidModel { Name = recipeName };
                        
                        if (root.TryGetProperty("ProceduralColors", out var colors)) {
                            human.SkinToneHex = GetProp(colors, "Skin", "#C68E6F");
                            human.ShirtHex = GetProp(colors, "Shirt", "#0099AA");
                            human.PantsHex = GetProp(colors, "Pants", "#333399");
                            human.EyeHex = GetProp(colors, "Eyes", "#FFFFFF");
                        }

                        if (root.TryGetProperty("Textures", out var tex)) {
                            human.FacePixels = GetPixels(tex, "Face");
                            human.HatPixels = GetPixels(tex, "Hat");
                            human.ChestPixels = GetPixels(tex, "Chest");
                            human.ArmPixels = GetPixels(tex, "Arms");
                            human.LegPixels = GetPixels(tex, "Legs");
                        }

                        human.GenerateSkin();
                        var payload = new { Parts = human.GetParts(), Skin = human.SkinBase64 };
                        string payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
                        string escapedName = System.Text.Json.JsonSerializer.Serialize(recipeName);
                        await _webView.ExecuteScriptAsync($"if(window.MCTwin && window.MCTwin.spawnVoxel) window.MCTwin.spawnVoxel({payloadJson}, {escapedName}, false, {transformJson});");
                    } else {
                        // Default to Procedural
                        string escapedName = System.Text.Json.JsonSerializer.Serialize(recipeName);
                        await _webView.ExecuteScriptAsync($"if(window.MCTwin && window.MCTwin.spawnRecipe) window.MCTwin.spawnRecipe({json}, {escapedName}, {transformJson});");
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine("Error importing prop: " + ex.Message);
            }
        }

        private string GetProp(System.Text.Json.JsonElement el, string key, string def) => el.TryGetProperty(key, out var p) ? p.GetString() : def;
        
        private string[] GetPixels(System.Text.Json.JsonElement el, string key) {
            if (el.TryGetProperty(key, out var p) && p.ValueKind == System.Text.Json.JsonValueKind.Array) {
                var list = new List<string>();
                foreach (var item in p.EnumerateArray()) list.Add(item.GetString() ?? "#TRANSPARENT");
                return list.ToArray();
            }
            return null;
        }

        private void RefreshPalette()
        {
            _lstPalette.Items.Clear();
            var recipes = _assetService.ListAvailableRecipes();
            foreach (var r in recipes) _lstPalette.Items.Add(r);
        }
    }
}

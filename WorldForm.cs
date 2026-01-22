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

        public WorldForm(HumanoidModel model, CoreWebView2Environment env)
        {
            _model = model;
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
                Padding = new Padding(15) 
            };
            splitContainer.Panel2.Controls.Add(pnlSettings);

            var lblTitle = new Label { 
                Text = "WORLD SETTINGS", 
                Dock = DockStyle.Top, 
                Height = 40, 
                ForeColor = NexusStyles.AccentGold, 
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlSettings.Controls.Add(lblTitle);

            AddSetting(pnlSettings, "Sky Color", (s, e) => {
                using (var cd = new ColorDialog()) { if (cd.ShowDialog() == DialogResult.OK) UpdateWorld("skyColor", $"#{cd.Color.R:X2}{cd.Color.G:X2}{cd.Color.B:X2}"); }
            });

            AddSetting(pnlSettings, "IMPORT PROP", (s, e) => {
                using (var ofd = new OpenFileDialog { Filter = "JSON Files|*.json" }) {
                    if (ofd.ShowDialog() == DialogResult.OK) {
                        try { ImportProp(File.ReadAllText(ofd.FileName)); } catch { }
                    }
                }
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
            cmbTheme.SelectedIndex = 0;
            cmbTheme.SelectedIndexChanged += (s, e) => UpdateWorld("floorTheme", cmbTheme.SelectedItem.ToString());
            pnlSettings.Controls.Add(cmbTheme);
            pnlSettings.Controls.Add(new Panel { Height = 10, Dock = DockStyle.Top }); // Spacer

            AddSetting(pnlSettings, "Floor Color", (s, e) => {
                using (var cd = new ColorDialog()) { if (cd.ShowDialog() == DialogResult.OK) UpdateWorld("groundColor", $"#{cd.Color.R:X2}{cd.Color.G:X2}{cd.Color.B:X2}"); }
            });

            AddLabel(pnlSettings, "Manipulation");
            var pnlGizmo = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 };
            AddGizmoBtn(pnlGizmo, "MOVE", "move");
            AddGizmoBtn(pnlGizmo, "ROT", "rotate");
            AddGizmoBtn(pnlGizmo, "SIZE", "scale");
            AddGizmoBtn(pnlGizmo, "OFF", "none");
            pnlSettings.Controls.Add(pnlGizmo);

            var chkGround = new CheckBox { Text = "Show Floor", Dock = DockStyle.Top, Height = 30, ForeColor = Color.White, Checked = true };
            chkGround.CheckedChanged += (s, e) => UpdateWorld("groundVisible", chkGround.Checked ? "true" : "false");
            pnlSettings.Controls.Add(chkGround);

            AddLabel(pnlSettings, "Light Intensity");
            var trkLight = new TrackBar { Dock = DockStyle.Top, Minimum = 0, Maximum = 20, Value = 10 };
            trkLight.Scroll += (s, e) => UpdateWorld("lightIntensity", (trkLight.Value / 10.0).ToString());
            pnlSettings.Controls.Add(trkLight);

            AddLabel(pnlSettings, "Floor Size");
            var trkSize = new TrackBar { Dock = DockStyle.Top, Minimum = 100, Maximum = 5000, Value = 500, LargeChange = 100, SmallChange = 10 };
            trkSize.Scroll += (s, e) => UpdateWorld("groundSize", trkSize.Value.ToString());
            pnlSettings.Controls.Add(trkSize);

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

        public async void ImportProp(string json)
        {
             if (_webView?.CoreWebView2 != null)
             {
                 await _webView.ExecuteScriptAsync($"if(window.MCTwin && window.MCTwin.spawnRecipe) window.MCTwin.spawnRecipe({json});");
             }
        }
    }
}

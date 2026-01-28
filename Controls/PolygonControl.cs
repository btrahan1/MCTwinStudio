using System;
using System.Drawing;
using System.Windows.Forms;
using MCTwin.Shared.Services;
using MCTwin.Shared.Geometry;
using MCTwin.Shared.Meshing;
using MCTwinStudio.Core;

namespace MCTwinStudio.Controls
{
    public class PolygonControl : UserControl
    {
        private PolygonService _service;
        private HumanoidRecipe _recipe = new HumanoidRecipe();
        
        public event EventHandler<MeshResult>? OnMeshGenerated;

        private Label _lblStatus;
        private Button _btnGenerate;
        
        // Inputs
        private TrackBar _trkMuscle;
        private TrackBar _trkTorso;
        private TrackBar _trkShoulder;
        private TrackBar _trkHeight;

        private System.Windows.Forms.Timer _debounceTimer;

        public PolygonControl()
        {
            _service = new PolygonService();
            _debounceTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _debounceTimer.Tick += (s, e) => {
                _debounceTimer.Stop();
                Generate_Click(null, null);
            };

            this.BackColor = Color.FromArgb(40, 40, 45);
            this.ForeColor = Color.White;
            this.Padding = new Padding(10);
            
            InitializeUI();
        }

        private void InitializeUI()
        {
            var title = new Label { Text = "POLYGON DNA", Font = NexusStyles.HeaderFont, ForeColor = NexusStyles.AccentCyan, Dock = DockStyle.Top, Height = 30 };
            this.Controls.Add(title);

            _btnGenerate = new Button { Text = "GENERATE", Dock = DockStyle.Bottom, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = NexusStyles.AccentIndigo, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            _btnGenerate.Click += Generate_Click;
            this.Controls.Add(_btnGenerate);
            
            this.VisibleChanged += (s, e) => {
                 if (this.Visible) Generate_Click(null, null);
            };

            var pnlInputs = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoScroll = true, WrapContents = false };
            this.Controls.Add(pnlInputs);
            pnlInputs.BringToFront();

            AddSlider(pnlInputs, "Muscle Tone", 1, 50, (int)(_recipe.MuscleTone * 100), (v) => _recipe.MuscleTone = v / 100.0f);
            AddSlider(pnlInputs, "Torso Width", 20, 80, (int)(_recipe.TorsoWidth * 100), (v) => _recipe.TorsoWidth = v / 100.0f);
            AddSlider(pnlInputs, "Shoulder Width", 30, 100, (int)(_recipe.ShoulderWidth * 100), (v) => _recipe.ShoulderWidth = v / 100.0f);
            AddSlider(pnlInputs, "Height", 100, 250, (int)(_recipe.Height * 100), (v) => _recipe.Height = v / 100.0f);
            AddSlider(pnlInputs, "Arm Length", 20, 150, (int)(_recipe.ArmLength * 100), (v) => _recipe.ArmLength = v / 100.0f);
            AddSlider(pnlInputs, "Leg Length", 20, 150, (int)(_recipe.LegLength * 100), (v) => _recipe.LegLength = v / 100.0f);
            AddSlider(pnlInputs, "Arm Thickness", 5, 40, (int)(_recipe.ArmThickness * 100), (v) => _recipe.ArmThickness = v / 100.0f);
            AddSlider(pnlInputs, "Leg Thickness", 5, 50, (int)(_recipe.LegThickness * 100), (v) => _recipe.LegThickness = v / 100.0f);

            _lblStatus = new Label { Text = "Ready", Dock = DockStyle.Bottom, Height = 20, ForeColor = Color.Gray };
            this.Controls.Add(_lblStatus);
        }

        private void AddSlider(Panel parent, string label, int min, int max, int val, Action<int> onChange)
        {
            var p = new Panel { Width = 280, Height = 60 };
            var lbl = new Label { Text = $"{label}: {val}", Dock = DockStyle.Top, Height = 20 };
            var trk = new TrackBar { Minimum = min, Maximum = max, Value = val, Dock = DockStyle.Top, Height = 30, TickStyle = TickStyle.None };
            
            trk.Scroll += (s, e) => {
                lbl.Text = $"{label}: {trk.Value}";
                onChange(trk.Value);
                _debounceTimer.Stop();
                _debounceTimer.Start();
            };
            
            p.Controls.Add(trk);
            p.Controls.Add(lbl);
            parent.Controls.Add(p);
        }

        private async void Generate_Click(object? sender, EventArgs e)
        {
            _btnGenerate.Enabled = false;
            _lblStatus.Text = "Generating...";
            
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var mesh = await _service.GenerateHumanoidAsync(_recipe);
                sw.Stop();
                
                _lblStatus.Text = $"Generated {mesh.Vertices.Count / 3} verts in {sw.ElapsedMilliseconds}ms";
                OnMeshGenerated?.Invoke(this, mesh);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                _btnGenerate.Enabled = true;
            }
        }
    }
}

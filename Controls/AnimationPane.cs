using System;
using System.Drawing;
using System.Windows.Forms;
using MCTwinStudio.Core;

namespace MCTwinStudio.Controls
{
    public class AnimationPane : UserControl
    {
        public event EventHandler<string>? AnimationRequested;

        public AnimationPane()
        {
            this.BackColor = NexusStyles.CardColor;
            this.Padding = new Padding(10);
            
            var lbl = new Label {
                Text = "VIRTUAL ACTOR",
                Dock = DockStyle.Top,
                Height = 30,
                Font = NexusStyles.HeaderFont,
                ForeColor = NexusStyles.AccentEmerald
            };
            this.Controls.Add(lbl);

            var flow = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(5)
            };
            this.Controls.Add(flow);
            flow.BringToFront();

            flow.Controls.Add(CreateBtn("Idle", "idle"));
            flow.Controls.Add(CreateBtn("Walk", "walk"));
            flow.Controls.Add(CreateBtn("Run", "run"));
            flow.Controls.Add(CreateBtn("Wave", "wave"));
            flow.Controls.Add(CreateBtn("Reset Pose", "reset"));
        }

        private Button CreateBtn(string text, string command)
        {
            var btn = new Button {
                Text = text,
                Width = 260,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = NexusStyles.BackColor,
                ForeColor = NexusStyles.WhiteText,
                Font = NexusStyles.MainFont,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 10)
            };
            
            btn.FlatAppearance.BorderColor = NexusStyles.BorderColor;
            btn.FlatAppearance.MouseOverBackColor = NexusStyles.AccentIndigo;
            
            btn.Click += (s, e) => AnimationRequested?.Invoke(this, command);
            return btn;
        }
    }
}

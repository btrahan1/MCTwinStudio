using System;
using System.Drawing;
using System.Windows.Forms;
using MCTwinStudio.Core;

namespace MCTwinStudio.Controls
{
    public class OptionsPane : UserControl
    {
        public bool GenerateFace => _chkFace.Checked;
        public bool GenerateChest => _chkChest.Checked;
        public bool GenerateArms => _chkArms.Checked;
        public bool GenerateLegs => _chkLegs.Checked;

        private CheckBox _chkFace = null!;
        private CheckBox _chkChest = null!;
        private CheckBox _chkArms = null!;
        private CheckBox _chkLegs = null!;
        private Label _lblHeader;

        public OptionsPane()
        {
            this.BackColor = NexusStyles.CardColor;
            this.Padding = new Padding(10);
            
            _lblHeader = new Label {
                Text = "GENERATION TARGETS",
                Dock = DockStyle.Top,
                Height = 30,
                Font = NexusStyles.HeaderFont,
                ForeColor = NexusStyles.AccentCyan,
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(_lblHeader);

            var pnlContent = new FlowLayoutPanel { 
                Dock = DockStyle.Fill, 
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(5),
                AutoSize = true
            };
            this.Controls.Add(pnlContent);
            pnlContent.BringToFront();

            _chkFace = CreateCheck("Face Region", true);
            _chkChest = CreateCheck("Chest Region", true);
            _chkArms = CreateCheck("Arm Regions (Symmetrical)", true);
            _chkLegs = CreateCheck("Leg Regions (Symmetrical)", true);

            pnlContent.Controls.Add(_chkFace);
            pnlContent.Controls.Add(_chkArms);
            pnlContent.Controls.Add(_chkLegs);
        }

        private CheckBox CreateCheck(string text, bool check)
        {
            return new CheckBox {
                Text = text,
                ForeColor = NexusStyles.WhiteText,
                Font = NexusStyles.MainFont,
                Checked = check,
                Width = 250,
                Height = 30,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
        }
    }
}

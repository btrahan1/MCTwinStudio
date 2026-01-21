using System;
using System.Windows.Forms;
using System.Drawing;
using MCTwinStudio.Core;

namespace MCTwinStudio.Controls
{
    public class JsonPane : UserControl
    {
        private TextBox _txtJson;

        public JsonPane()
        {
            this.BackColor = NexusStyles.CardColor;
            this.Padding = new Padding(10);
            
            var lbl = new Label {
                Text = "RAW JSON DATA",
                Dock = DockStyle.Top,
                Height = 30,
                Font = NexusStyles.HeaderFont,
                ForeColor = NexusStyles.AccentAmber
            };
            this.Controls.Add(lbl);

            _txtJson = new TextBox {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BackColor = NexusStyles.BackColor,
                ForeColor = NexusStyles.WhiteText,
                Font = new Font("Consolas", 9),
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };
            this.Controls.Add(_txtJson);
            _txtJson.BringToFront();
        }

        public void SetJson(string json)
        {
            // format json? 
            try {
                if (!string.IsNullOrEmpty(json)) {
                    var obj = System.Text.Json.JsonDocument.Parse(json);
                    _txtJson.Text = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                } else {
                    _txtJson.Text = "";
                }
            } catch {
                 _txtJson.Text = json;
            }
        }
    }
}

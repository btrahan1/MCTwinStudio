using System;
using System.Drawing;
using System.Windows.Forms;
using MCTwinStudio.Services;
using MCTwinStudio.Core;

namespace MCTwinStudio.Controls
{
    public class PaletteControl : UserControl
    {
        private ListBox _lstPalette;
        private AssetService _assetService;
        public event Action<string, string>? OnAssetSelected;

        public PaletteControl(AssetService assetService)
        {
            _assetService = assetService;
            this.Dock = DockStyle.Top;
            this.Height = 150;
            
            _lstPalette = new ListBox {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9)
            };

            _lstPalette.DoubleClick += (s, e) => {
                if (_lstPalette.SelectedItem != null) {
                    string name = _lstPalette.SelectedItem.ToString()!;
                    string recipe = _assetService.GetBestMatch(name);
                    if (!string.IsNullOrEmpty(recipe)) OnAssetSelected?.Invoke(name, recipe);
                }
            };

            this.Controls.Add(_lstPalette);
            RefreshItems();
        }

        public void RefreshItems()
        {
            _lstPalette.Items.Clear();
            var recipes = _assetService.ListAvailableRecipes();
            foreach (var r in recipes) _lstPalette.Items.Add(r);
        }
    }
}

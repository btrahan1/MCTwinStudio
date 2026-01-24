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
        private AssetService.AssetCategory _currentCategory = AssetService.AssetCategory.Actor;
        private Button _btnActors, _btnProps;
        public event Action<string, string>? OnAssetSelected;

        public PaletteControl(AssetService assetService)
        {
            _assetService = assetService;
            this.Dock = DockStyle.Top;
            this.Height = 180;
            this.BackColor = NexusStyles.CardColor;
            
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };
            _btnActors = CreateToggleBtn("ACTORS", AssetService.AssetCategory.Actor);
            _btnProps = CreateToggleBtn("PROPS", AssetService.AssetCategory.Prop);
            pnlHeader.Controls.Add(_btnProps);
            pnlHeader.Controls.Add(_btnActors);
            this.Controls.Add(pnlHeader);

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
            _lstPalette.BringToFront();
            RefreshItems();
            UpdateToggleStates();
        }

        private Button CreateToggleBtn(string text, AssetService.AssetCategory cat)
        {
            var btn = new Button { 
                Text = text, 
                Dock = DockStyle.Left, 
                Width = 120, 
                FlatStyle = FlatStyle.Flat, 
                Font = new Font("Segoe UI", 8, FontStyle.Bold) 
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => {
                _currentCategory = cat;
                RefreshItems();
                UpdateToggleStates();
            };
            return btn;
        }

        private void UpdateToggleStates()
        {
            _btnActors.BackColor = _currentCategory == AssetService.AssetCategory.Actor ? NexusStyles.AccentIndigo : Color.Transparent;
            _btnProps.BackColor = _currentCategory == AssetService.AssetCategory.Prop ? NexusStyles.AccentIndigo : Color.Transparent;
        }

        public void RefreshItems()
        {
            _lstPalette.Items.Clear();
            var recipes = _assetService.ListAvailableRecipes(_currentCategory);
            foreach (var r in recipes) _lstPalette.Items.Add(r);
        }
    }
}

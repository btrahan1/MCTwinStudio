using System;
using System.Drawing;
using System.Windows.Forms;
using MCTwinStudio.Services;
using MCTwinStudio.Core;
using MCTwinStudio.Core.Interfaces;

namespace MCTwinStudio.Controls
{
    public class PaletteControl : UserControl
    {
        private ListBox _lstPalette;
        private IAssetService _assetService;
        private AssetCategory _currentCategory = AssetCategory.Actor;
        private Button _btnActors, _btnProps;
        public event Action<string, string>? OnAssetSelected;

        public PaletteControl(IAssetService assetService)
        {
            _assetService = assetService;
            this.Dock = DockStyle.Top;
            this.Height = 180;
            this.BackColor = NexusStyles.CardColor;
            
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };
            pnlHeader.BackColor = Color.FromArgb(45, 45, 50);
            _btnActors = CreateToggleBtn("ACTORS", AssetCategory.Actor);
            _btnProps = CreateToggleBtn("PROPS", AssetCategory.Prop);
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

            _lstPalette.DoubleClick += async (s, e) => {
                if (_lstPalette.SelectedItem != null) {
                    string name = _lstPalette.SelectedItem.ToString()!;
                    string recipe = await _assetService.GetBestMatch(name);
                    if (!string.IsNullOrEmpty(recipe)) OnAssetSelected?.Invoke(name, recipe);
                }
            };

            this.Controls.Add(_lstPalette);
            _lstPalette.BringToFront();
            _ = RefreshItems();
            UpdateToggleStates();
        }

        private Button CreateToggleBtn(string text, AssetCategory cat)
        {
            var btn = new Button { 
                Text = text, 
                Dock = DockStyle.Left, 
                Width = 120, 
                FlatStyle = FlatStyle.Flat, 
                Font = new Font("Segoe UI", 8, FontStyle.Bold) 
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += async (s, e) => {
                _currentCategory = cat;
                await RefreshItems();
                UpdateToggleStates();
            };
            return btn;
        }

        private void UpdateToggleStates()
        {
            _btnActors.BackColor = _currentCategory == AssetCategory.Actor ? NexusStyles.AccentIndigo : Color.Transparent;
            _btnProps.BackColor = _currentCategory == AssetCategory.Prop ? NexusStyles.AccentIndigo : Color.Transparent;
        }

        public async Task RefreshItems()
        {
            _lstPalette.Items.Clear();
            var recipes = await _assetService.ListAvailableRecipes(_currentCategory);
            foreach (var r in recipes) _lstPalette.Items.Add(r);
        }
    }
}

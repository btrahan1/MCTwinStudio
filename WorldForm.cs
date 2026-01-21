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
            _env = env;

            this.Text = $"World Explorer - {model.Name}";
            this.Size = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = NexusStyles.BackColor;

            _webView = new WebView2 { Dock = DockStyle.Fill };
            this.Controls.Add(_webView);

            InitializeAsync();
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
    }
}

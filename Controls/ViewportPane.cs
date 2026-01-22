using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MCTwinStudio.Core;
using MCTwinStudio.Core.Models;

namespace MCTwinStudio.Controls
{
    public class ViewportPane : UserControl
    {
        private WebView2 _webView = null!;
        public event EventHandler<string>? MeshSelected;
        public event EventHandler<string>? LogReceived;

        public ViewportPane(CoreWebView2Environment env)
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = NexusStyles.BackColor;
            InitializeWebView(env);
        }

        private async void InitializeWebView(CoreWebView2Environment env)
        {
            _webView = new WebView2 { Dock = DockStyle.Fill };
            this.Controls.Add(_webView);

            await _webView.EnsureCoreWebView2Async(env);
            _webView.CoreWebView2.WebMessageReceived += (s, e) => {
                string msg = e.TryGetWebMessageAsString();
                if (msg.StartsWith("SELECT:")) MeshSelected?.Invoke(this, msg.Replace("SELECT:", ""));
                if (msg.StartsWith("LOG:")) LogReceived?.Invoke(this, msg.Replace("LOG:", ""));
            };
            
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "viewer.html");
            if (File.Exists(htmlPath)) _webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
        }

        public async void RenderModel(BaseModel model)
        {
            if (_webView?.CoreWebView2 == null) return;
            var parts = model.GetParts();
            // Send { Parts, Skin }
            var payload = new { Parts = parts, Skin = model.SkinBase64 };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            string script = $"window.MCTwin.renderModel({json});";
            await _webView.ExecuteScriptAsync(script);
        }

        // Legacy support if needed, or remove
        public async void RenderRecipe(string json)
        {
            if (_webView?.CoreWebView2 == null) return;
            string script = $"window.MCTwin.renderRecipe({json});";
            await _webView.ExecuteScriptAsync(script);
        }

        public async void ExecuteCommand(string json) 
        {
             RenderRecipe(json);
        }

        public async void ClearScene()
        {
             if (_webView?.CoreWebView2 != null) await _webView.ExecuteScriptAsync("window.MCTwin.clear();");
        }

        public async void PlayAnimation(string name)
        {
            if (_webView?.CoreWebView2 != null) 
            {
                await _webView.ExecuteScriptAsync($"if(window.MCTwin && window.MCTwin.setAnimation) window.MCTwin.setAnimation('{name}');");
            }
        }
    }
}
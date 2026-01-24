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
        public SceneController Controller { get; private set; } = null!;
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
            Controller = new SceneController(_webView);

            _webView.CoreWebView2.WebMessageReceived += (s, e) => {
                string msg = e.TryGetWebMessageAsString();
                if (msg.StartsWith("SELECT:")) MeshSelected?.Invoke(this, msg.Replace("SELECT:", ""));
                if (msg.StartsWith("LOG:")) LogReceived?.Invoke(this, msg.Replace("LOG:", ""));
            };
            
            string htmlPath = Path.Combine(EngineConfig.RendererDir, "viewer.html");
            if (File.Exists(htmlPath)) _webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
        }

        public async void RenderModel(BaseModel model) => await Controller.RenderModel(model);
        public async void RenderRecipe(string json) => await Controller.SpawnRecipe(json, "Preview");
        public async void ClearScene() => await Controller.ClearAll();
        public async void PlayAnimation(string name) => await Controller.PlayAnimation(name);
    }
}
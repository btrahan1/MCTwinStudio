using System;
using System.Drawing;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MCTwinStudio.Core;

namespace MCTwinStudio.Controls
{
    public class BrainPane : UserControl
    {
        private WebView2 _webView = null!;
        public event EventHandler<string>? JsonResponseReceived;

        public BrainPane(CoreWebView2Environment env)
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
            
            _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            
            _webView.Source = new Uri("https://aistudio.google.com/");
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.TryGetWebMessageAsString();
                if (message.StartsWith("SCRAPE:"))
                {
                    string rawContent = message.Replace("SCRAPE:", "");
                    ExtractJson(rawContent);
                }
            }
            catch { /* Ignore non-string messages */ }
        }

        private void ExtractJson(string content)
        {
            // Robust regex for objects and arrays
            var match = System.Text.RegularExpressions.Regex.Match(content, @"\{[\s\S]*\}|\[[\s\S]*\]");
            if (match.Success)
            {
                JsonResponseReceived?.Invoke(this, match.Value);
            }
        }

        public async void SendPrompt(string text)
        {
            var escapedText = System.Text.Json.JsonSerializer.Serialize(text);
            string script = $@"
                (async function(text) {{
                    const promptSelectors = [
                        'textarea[placeholder*=""typing a prompt""]',
                        'textarea.ms-autosize-textarea',
                        'div[contenteditable=""true""]',
                        '.prompt-textarea',
                        '.ql-editor'
                    ];

                    let targetEl = null;
                    for (const selector of promptSelectors) {{
                        targetEl = document.querySelector(selector);
                        if (targetEl) break;
                    }}

                    if (!targetEl) return;

                    // Inject Text
                    if (targetEl.tagName === ""TEXTAREA"" || targetEl.tagName === ""INPUT"") {{
                        targetEl.value = text;
                    }} else {{
                        targetEl.innerText = text;
                        if (targetEl.classList.contains('ql-editor')) {{
                            targetEl.innerHTML = '<p>' + text + '</p>';
                        }}
                    }}
                    targetEl.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    targetEl.dispatchEvent(new Event('change', {{ bubbles: true }}));

                    await new Promise(r => setTimeout(r, 600));

                    // Click Run
                    const buttons = Array.from(document.querySelectorAll('button'));
                    const runButton = buttons.find(b => 
                        (b.innerText.includes('Run') || b.getAttribute('aria-label')?.includes('Run') || b.querySelector('mat-icon')?.innerText === 'play_arrow') && 
                        !b.disabled
                    );

                    if (runButton) {{
                        runButton.click();
                    }} else {{
                        targetEl.focus();
                        targetEl.dispatchEvent(new KeyboardEvent('keydown', {{ 
                            key: 'Enter', code: 'Enter', ctrlKey: true, bubbles: true 
                        }}));
                    }}
                }})({escapedText});";
            
            await _webView.ExecuteScriptAsync(script);
        }

        private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                await InjectKeepAliveAndScraper();
            }
        }

        private async System.Threading.Tasks.Task InjectKeepAliveAndScraper()
        {
            string script = @"
                let stableCycles = 0;
                let lastScrapedContent = '';

                // Combined Keep-Alive, Scraper, and Visibility Mock
                setInterval(() => {
                    // 1. Keep-Alive (Scroll)
                    window.scrollTo(0, document.body.scrollHeight);
                    const scrollables = document.querySelectorAll('div, main');
                    scrollables.forEach(el => {
                        if (el.scrollHeight > el.clientHeight) el.scrollTop = el.scrollHeight;
                    });

                    // 2. Mock Visibility
                    if (document.visibilityState === 'hidden') {
                        Object.defineProperty(document, 'visibilityState', {value: 'visible', writable: true});
                        Object.defineProperty(document, 'hidden', {value: false, writable: true});
                        document.dispatchEvent(new Event('visibilitychange'));
                    }

                    // 3. Robust Response Completion Detection (The 'Nexus' Way)
                    const buttons = Array.from(document.querySelectorAll('button'));
                    const isStopPresent = buttons.some(b => 
                        b.innerText.includes('Stop') || 
                        b.querySelector('mat-icon')?.innerText === 'stop' || 
                        b.getAttribute('aria-label')?.includes('Stop')
                    );
                    const runBtn = buttons.find(b => b.innerText.includes('Run') || b.getAttribute('aria-label')?.includes('Run'));
                    const isGenerating = isStopPresent || (runBtn && runBtn.disabled);

                    if (!isGenerating) {
                        stableCycles++;
                        if (stableCycles === 3) {
                            // Find AI Studio specific chat turns
                            const chatTurns = document.querySelectorAll('ms-chat-turn');
                            const modelResponses = document.querySelectorAll('.model-response-text, .response-content');
                            
                            let content = '';
                            if (chatTurns.length > 0) {
                                content = chatTurns[chatTurns.length - 1].innerText;
                            } else if (modelResponses.length > 0) {
                                content = modelResponses[modelResponses.length - 1].innerText;
                            }

                            if (content && content !== lastScrapedContent) {
                                window.chrome.webview.postMessage('SCRAPE:' + content);
                                lastScrapedContent = content;
                            }
                        }
                    } else {
                        stableCycles = 0;
                    }
                }, 1000);
            ";
            await _webView.ExecuteScriptAsync(script);
        }
    }
}

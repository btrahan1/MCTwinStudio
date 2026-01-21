using System;
using System.Drawing;
using System.Windows.Forms;

namespace MCTwinStudio.Core
{
    public static class NexusStyles
    {
        // Colors: Sleek Obsidian & Electric Accents
        public static Color BackColor = Color.FromArgb(10, 10, 12);
        public static Color CardColor = Color.FromArgb(20, 20, 25);
        public static Color BorderColor = Color.FromArgb(35, 35, 45);
        
        public static Color WhiteText = Color.FromArgb(230, 230, 240);
        public static Color GrayText = Color.FromArgb(140, 140, 155);

        // Functional Accents
        public static Color AccentEmerald = Color.FromArgb(16, 185, 129); // Rendering / Success
        public static Color AccentIndigo = Color.FromArgb(99, 102, 241);  // Discovery / Chat
        public static Color AccentCyan = Color.FromArgb(6, 182, 212);    // AI Engine
        public static Color AccentGold = Color.FromArgb(245, 158, 11);    // System / Lights
        public static Color AccentAmber = Color.FromArgb(245, 158, 11);   // Source Code

        // Typography
        public static Font HeaderFont = new Font("Segoe UI Semibold", 10f);
        public static Font MainFont = new Font("Segoe UI", 9.5f);
        public static Font PromptFont = new Font("Consolas", 10f);

        // Spacing
        public static int PanePadding = 12;
        public static int HeaderHeight = 35;

        public static Button CreateCommandButton(string text, Color accent)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, accent),
                ForeColor = WhiteText,
                Font = HeaderFont,
                Height = 35,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(100, accent);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, accent);
            return btn;
        }
    }
}

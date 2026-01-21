using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MCTwinStudio.Services
{
    public class SkinGenerator
    {
        public Bitmap Generate(
            string skinHex, string shirtHex, string pantsHex, string eyeHex,
            string[]? facePixels = null,
            string[]? hatPixels = null,
            string[]? chestPixels = null,
            string[]? armPixels = null, 
            string[]? legPixels = null 
        )
        {
            var bmp = new Bitmap(64, 64);
            using var g = Graphics.FromImage(bmp);
            
            Color skin = ColorTranslator.FromHtml(skinHex);
            Color shirt = ColorTranslator.FromHtml(shirtHex);
            Color pants = ColorTranslator.FromHtml(pantsHex);
            Color eye = ColorTranslator.FromHtml(eyeHex);

            g.Clear(Color.Transparent);

            // HELPER: FillRect
            void Fill(int x, int y, int w, int h, Color c) => g.FillRectangle(new SolidBrush(c), x, y, w, h);

            // HELPER: DrawRegion (Safe Pixel Set)
            Action<int, int, int, int, string[]> drawRegion = (x, y, w, h, pixels) => {
                 for (int i = 0; i < pixels.Length; i++)
                {
                    int row = i / w;
                    int col = i % w;
                    if (row >= h) break; 
                    string hex = pixels[i];
                    if (!string.IsNullOrEmpty(hex) && hex.ToUpper() != "#TRANSPARENT")
                    {
                         try { bmp.SetPixel(x + col, y + row, ColorTranslator.FromHtml(hex)); } catch {}
                    }
                }
            };

            // ==========================================
            // 1. BASE LAYERS (Procedural Fallback)
            // ==========================================
            
            // HEAD (0,0 to 32,16)
            Fill(0, 8, 32, 8, skin); // Sides
            Fill(8, 0, 16, 8, skin); // Top/Bottom
            
            // RIGHT LEG (0,16 to 16,32)
            Fill(4, 16, 8, 4, pants); // Top/Bot
            Fill(0, 20, 16, 12, pants); // Sides

            // BODY (16,16 to 40,32)
            Fill(20, 16, 8, 4, shirt); // Top (Neck)
            Fill(28, 16, 8, 4, shirt); // Bottom (Waist)
            Fill(16, 20, 24, 12, shirt); // Sides (Body)
            Fill(23, 20, 2, 2, skin); // V-Neck

            // RIGHT ARM (40,16 to 56,32)
            Fill(44, 16, 8, 4, shirt); // Top/Bot
            Fill(40, 20, 16, 8, shirt); // Sleeve
            Fill(40, 28, 16, 4, skin); // Hand

            // LEFT LEG (16,48 to 32,64)
            Fill(20, 48, 8, 4, pants); // Top/Bot
            Fill(16, 52, 16, 12, pants); // Sides

            // LEFT ARM (32,48 to 48,64)
            Fill(36, 48, 8, 4, shirt); // Top/Bot
            Fill(32, 52, 16, 8, shirt); // Sleeve
            Fill(32, 60, 16, 4, skin); // Hand

            // ==========================================
            // 2. HYBRID REGIONS (AI Textures)
            // ==========================================

            // FACE (8x8) -> Head Front (8,8)
            if (facePixels != null && facePixels.Length == 64) {
                 drawRegion(8, 8, 8, 8, facePixels);
            } else {
                 // Procedural Eyes/Mouth Fallback
                 Fill(9, 12, 1, 1, Color.White); Fill(10, 12, 1, 1, eye);
                 Fill(13, 12, 1, 1, Color.White); Fill(14, 12, 1, 1, eye);
                 Fill(11, 14, 2, 1, ControlPaint.Dark(skin, 0.1f));
            }

            // CHEST (8x12) -> Torso Front [20, 20]
            if (chestPixels != null && chestPixels.Length == 96) {
                drawRegion(20, 20, 8, 12, chestPixels); // Front
                drawRegion(32, 20, 8, 12, chestPixels); // Back (Copy)
            }

            // ARMS (4x12) -> Right [44,20], Left [36,52]
            if (armPixels != null && armPixels.Length == 48) {
                drawRegion(44, 20, 4, 12, armPixels); // Right Front
                drawRegion(52, 20, 4, 12, armPixels); // Right Back (Copy)
                drawRegion(36, 52, 4, 12, armPixels); // Left Front
                drawRegion(44, 52, 4, 12, armPixels); // Left Back (Copy)
                // Sides? Let's assume standard wrap (sides remain base color or we could stretch properties)
                // For "Paper Doll", leaving sides plain is safer than glitchy stretching.
            }

            // LEGS (4x12) -> Right [4,20], Left [20,52]
            if (legPixels != null && legPixels.Length == 48) {
                drawRegion(4, 20, 4, 12, legPixels); // Right Front
                drawRegion(12, 20, 4, 12, legPixels); // Right Back
                drawRegion(20, 52, 4, 12, legPixels); // Left Front
                drawRegion(28, 52, 4, 12, legPixels); // Left Back
            }

            // ==========================================
            // 3. HAT LAYER (32,0)
            // ==========================================
            if (hatPixels != null && hatPixels.Length == 64) {
                 int sumR=0, sumG=0, sumB=0, count=0;
                 foreach(var hatHex in hatPixels) {
                     if (hatHex!=null && hatHex.Length >=7 && hatHex.ToUpper()!="#TRANSPARENT"){
                         try { var col = ColorTranslator.FromHtml(hatHex); sumR+=col.R; sumG+=col.G; sumB+=col.B; count++; } catch {}
                     }
                 }
                 if (count > 0) {
                     var baseCol = Color.FromArgb(sumR/count, sumG/count, sumB/count);
                     Fill(40,0,8,8, baseCol); Fill(48,0,8,8, baseCol); // Top/Bot
                     Fill(32,8,8,8, baseCol); Fill(48,8,8,8,baseCol); Fill(56,8,8,8, baseCol); // Sides/Back
                 }
                 // Detail Front
                 drawRegion(40, 8, 8, 8, hatPixels);
            }

            AddNoise(bmp, 0, 0, 64, 64, 15);
            return bmp;
        }

        private static void AddNoise(Bitmap bmp, int x, int y, int w, int h, int intensity)
        {
            var rnd = new Random();
            for (int i = x; i < x + w; i++) {
                for (int j = y; j < y + h; j++) {
                    if (i >= bmp.Width || j >= bmp.Height) continue;
                    Color original = bmp.GetPixel(i, j);
                    if (original.A == 0) continue; 
                    int factor = rnd.Next(-intensity, intensity);
                    int r = Math.Clamp(original.R + factor, 0, 255);
                    int g = Math.Clamp(original.G + factor, 0, 255);
                    int b = Math.Clamp(original.B + factor, 0, 255);
                    bmp.SetPixel(i, j, Color.FromArgb(original.A, r, g, b));
                }
            }
        }
    }
}

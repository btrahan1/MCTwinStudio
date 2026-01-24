using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

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

            void Fill(int x, int y, int w, int h, Color c) => g.FillRectangle(new SolidBrush(c), x, y, w, h);

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

            Fill(0, 8, 32, 8, skin);
            Fill(8, 0, 16, 8, skin);
            Fill(4, 16, 8, 4, pants);
            Fill(0, 20, 16, 12, pants);
            Fill(20, 16, 8, 4, shirt);
            Fill(28, 16, 8, 4, shirt);
            Fill(16, 20, 24, 12, shirt);
            Fill(23, 20, 2, 2, skin);
            Fill(44, 16, 8, 4, shirt);
            Fill(40, 20, 16, 8, shirt);
            Fill(40, 28, 16, 4, skin);
            Fill(20, 48, 8, 4, pants);
            Fill(16, 52, 16, 12, pants);
            Fill(36, 48, 8, 4, shirt);
            Fill(32, 52, 16, 8, shirt);
            Fill(32, 60, 16, 4, skin);

            if (facePixels != null && facePixels.Length == 64) {
                 drawRegion(8, 8, 8, 8, facePixels);
            } else {
                 Fill(9, 12, 1, 1, Color.White); Fill(10, 12, 1, 1, eye);
                 Fill(13, 12, 1, 1, Color.White); Fill(14, 12, 1, 1, eye);
                 Fill(11, 14, 2, 1, ControlPaint.Dark(skin, 0.1f));
            }

            if (chestPixels != null && chestPixels.Length == 96) {
                drawRegion(20, 20, 8, 12, chestPixels);
                drawRegion(32, 20, 8, 12, chestPixels);
            }

            if (armPixels != null && armPixels.Length == 48) {
                drawRegion(44, 20, 4, 12, armPixels);
                drawRegion(52, 20, 4, 12, armPixels);
                drawRegion(36, 52, 4, 12, armPixels);
                drawRegion(44, 52, 4, 12, armPixels);
            }

            if (legPixels != null && legPixels.Length == 48) {
                drawRegion(4, 20, 4, 12, legPixels);
                drawRegion(12, 20, 4, 12, legPixels);
                drawRegion(20, 52, 4, 12, legPixels);
                drawRegion(28, 52, 4, 12, legPixels);
            }

            if (hatPixels != null && hatPixels.Length == 64) {
                 int sumR=0, sumG=0, sumB=0, count=0;
                 foreach(var hatHex in hatPixels) {
                     if (hatHex!=null && hatHex.Length >=7 && hatHex.ToUpper()!="#TRANSPARENT"){
                         try { var col = ColorTranslator.FromHtml(hatHex); sumR+=col.R; sumG+=col.G; sumB+=col.B; count++; } catch {}
                     }
                 }
                 if (count > 0) {
                     var baseCol = Color.FromArgb(sumR/count, sumG/count, sumB/count);
                     Fill(40,0,8,8, baseCol); Fill(48,0,8,8, baseCol);
                     Fill(32,8,8,8, baseCol); Fill(48,8,8,8,baseCol); Fill(56,8,8,8, baseCol);
                 }
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

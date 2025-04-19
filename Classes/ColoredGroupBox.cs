using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SUBR
{
    public class ColoredGroupBox : GroupBox
    {
        [Category("Appearance")]
        public Color BorderColor { get; set; } = Color.Orange;

        [Category("Appearance")]
        public int CornerRadius { get; set; } = 10;

        [Category("Glow")]
        public bool EnableGlow { get; set; } = true;

        [Category("Glow")]
        public int GlowSize { get; set; } = 5;

        [Category("Glow")]
        public int GlowOpacity { get; set; } = 20; // 0–255

        [Category("Header")]
        public Color HeaderBackColor { get; set; } = Color.Black;

        [Category("Header")]
        public Color HeaderForeColor { get; set; } = Color.Orange;

        [Category("Header")]
        public int BorderThickness { get; set; } = 2;

    // ⬇️ then your OnPaint and RoundedRect methods stay like normal


        


        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Size textSize = TextRenderer.MeasureText(this.Text, this.Font);
            int textHeight = textSize.Height;

            Rectangle bounds = new Rectangle(0, textHeight / 2, this.Width - 1, this.Height - 1 - textHeight / 2);

            using (SolidBrush background = new SolidBrush(this.BackColor))
                e.Graphics.FillRectangle(background, bounds);

            using (GraphicsPath path = RoundedRect(bounds, CornerRadius))
            {
                // 🔥 Glow behind border
                if (EnableGlow && GlowSize > 0)
                {
                    for (int i = GlowSize; i >= 1; i--)
                    {
                        int alpha = Math.Min(255, GlowOpacity * i);
                        using (Pen glowPen = new Pen(Color.FromArgb(alpha, BorderColor), 2f + i))
                        {
                            glowPen.LineJoin = LineJoin.Round;
                            e.Graphics.DrawPath(glowPen, path);
                        }
                    }
                }

                // 🔲 Solid border
                using (Pen borderPen = new Pen(BorderColor, 2f))
                    e.Graphics.DrawPath(borderPen, path);
            }

            using (SolidBrush textBrush = new SolidBrush(this.ForeColor))
                e.Graphics.DrawString(this.Text, this.Font, textBrush, new PointF(10, 0));
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}

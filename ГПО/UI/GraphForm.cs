using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace MathApp.UI
{
    public class GraphForm : Form
    {
        private List<double> values = new List<double>();
        private Timer renderTimer;
        private Bitmap buffer;
        private bool needRedraw = true;
        private string sourceName;

        public GraphForm(string source)
        {
            sourceName = source;
            Text = $"График - {sourceName}";
            Size = new Size(800, 500);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(30, 30, 32);
            DoubleBuffered = true;

            var topPanel = new Panel { Height = 40, Dock = DockStyle.Top, BackColor = Color.FromArgb(45, 45, 48) };

            var btnClear = new Button
            {
                Text = "Очистить",
                Location = new Point(10, 8),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClear.Click += (s, e) => { values.Clear(); needRedraw = true; };

            topPanel.Controls.Add(btnClear);
            Controls.Add(topPanel);

            renderTimer = new Timer { Interval = 50 };
            renderTimer.Tick += (s, e) => { if (needRedraw) { DrawToBuffer(); Invalidate(); needRedraw = false; } };
            renderTimer.Start();
        }

        public void AddValue(double value)
        {
            values.Add(value);
            needRedraw = true;
        }

        private void DrawToBuffer()
        {
            if (Width <= 0 || Height <= 40) return;

            // Создаем новый буфер только если размер изменился
            if (buffer == null || buffer.Width != Width || buffer.Height != Height)
            {
                if (buffer != null) buffer.Dispose();
                buffer = new Bitmap(Width, Height);
            }

            using (var g = Graphics.FromImage(buffer))
            {
                g.Clear(BackColor);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int topOffset = 50;

                using (var gridPen = new Pen(Color.FromArgb(60, 60, 65), 1))
                {
                    for (int x = 50; x < Width - 50; x += 50)
                        g.DrawLine(gridPen, x, topOffset, x, Height - 50);
                    for (int y = topOffset; y < Height - 50; y += 50)
                        g.DrawLine(gridPen, 50, y, Width - 50, y);
                }

                using (var axisPen = new Pen(Color.White, 2))
                {
                    g.DrawLine(axisPen, 50, Height - 50, Width - 50, Height - 50);
                    g.DrawLine(axisPen, 50, topOffset, 50, Height - 50);
                }

                if (values.Count > 1)
                {
                    int graphLeft = 60, graphRight = Width - 60, graphTop = topOffset, graphBottom = Height - 70;
                    double min = values.Min(), max = values.Max(), range = max - min;
                    if (range < 0.001) range = 1;

                    using (var linePen = new Pen(Color.Cyan, 2))
                    {
                        Point? prev = null;
                        for (int i = 0; i < values.Count; i++)
                        {
                            float x = graphLeft + (float)(i * (graphRight - graphLeft) / (values.Count - 1));
                            float y = graphTop + (graphBottom - graphTop) - (float)((values[i] - min) / range * (graphBottom - graphTop));
                            y = Math.Max(graphTop, Math.Min(graphBottom, y));
                            if (prev.HasValue) g.DrawLine(linePen, prev.Value.X, prev.Value.Y, x, y);
                            prev = new Point((int)x, (int)y);
                        }
                    }
                }
                else
                {
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString("Нет данных", new Font("Segoe UI", 14, FontStyle.Bold), Brushes.Gray, new Rectangle(0, topOffset, Width, Height - topOffset), sf);
                }

                if (values.Count > 0)
                {
                    string stats = $"Значений: {values.Count} | Мин: {values.Min():F2} | Макс: {values.Max():F2} | Тек: {values.Last():F2}";
                    g.DrawString(stats, new Font("Segoe UI", 9), Brushes.LightGreen, 60, Height - 30);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (buffer != null)
                e.Graphics.DrawImage(buffer, 0, 0);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            needRedraw = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                renderTimer?.Dispose();
                if (buffer != null) buffer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
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
        private List<double> _values = new List<double>();
        private Timer _timer;
        private int _maxPoints = 200;
        private Bitmap _buffer;
        private bool _needRedraw = true;
        private string _sourceName;

        public GraphForm(string source)
        {
            _sourceName = source;
            Text = $"График - {_sourceName}";
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
            btnClear.Click += (s, e) => { _values.Clear(); _needRedraw = true; };

            topPanel.Controls.Add(btnClear);
            Controls.Add(topPanel);

            _timer = new Timer { Interval = 50 };
            _timer.Tick += (s, e) => _needRedraw = true;
            _timer.Start();

            var renderTimer = new Timer { Interval = 16 };
            renderTimer.Tick += (s, e) => { if (_needRedraw) { DrawToBuffer(); Invalidate(); _needRedraw = false; } };
            renderTimer.Start();
        }

        public void AddValue(double value)
        {
            _values.Add(value);
            if (_values.Count > _maxPoints * 2)
                _values.RemoveRange(0, _values.Count - _maxPoints);
            _needRedraw = true;
        }

        private void CreateBuffer()
        {
            if (Width <= 0 || Height <= 40) return;
            _buffer?.Dispose();
            _buffer = new Bitmap(Width, Height);
        }

        private void DrawToBuffer()
        {
            if (_buffer == null) CreateBuffer();
            if (_buffer == null) return;

            using (var g = Graphics.FromImage(_buffer))
            {
                g.Clear(BackColor);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int topOffset = 50;

                // Сетка
                using (var gridPen = new Pen(Color.FromArgb(60, 60, 65), 1))
                {
                    for (int x = 50; x < Width - 50; x += 50)
                        g.DrawLine(gridPen, x, topOffset, x, Height - 50);
                    for (int y = topOffset; y < Height - 50; y += 50)
                        g.DrawLine(gridPen, 50, y, Width - 50, y);
                }

                // Оси
                using (var axisPen = new Pen(Color.White, 2))
                {
                    g.DrawLine(axisPen, 50, Height - 50, Width - 50, Height - 50);
                    g.DrawLine(axisPen, 50, topOffset, 50, Height - 50);
                }

                // График
                if (_values.Count > 1)
                {
                    int graphLeft = 60, graphRight = Width - 60, graphTop = topOffset, graphBottom = Height - 70;
                    double min = _values.Min(), max = _values.Max(), range = max - min;
                    if (range < 0.001) range = 1;

                    int startIdx = 0;
                    if (_values.Count > _maxPoints) startIdx = _values.Count - _maxPoints;
                    int pointCount = _values.Count - startIdx;

                    using (var linePen = new Pen(Color.Cyan, 2))
                    {
                        Point? prev = null;
                        for (int i = startIdx; i < _values.Count; i++)
                        {
                            float x = graphLeft + (float)((i - startIdx) * (graphRight - graphLeft) / (pointCount - 1));
                            float y = graphTop + (graphBottom - graphTop) - (float)((_values[i] - min) / range * (graphBottom - graphTop));
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

                // Статистика
                if (_values.Count > 0)
                {
                    string stats = $"Значений: {_values.Count} | Мин: {_values.Min():F2} | Макс: {_values.Max():F2} | Тек: {_values.Last():F2}";
                    g.DrawString(stats, new Font("Segoe UI", 9), Brushes.LightGreen, 60, Height - 30);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) => e.Graphics.DrawImage(_buffer, 0, 0);
        protected override void OnResize(EventArgs e) { base.OnResize(e); CreateBuffer(); _needRedraw = true; }
        protected override void Dispose(bool disposing) { if (disposing) _timer?.Dispose(); base.Dispose(disposing); }
    }
}
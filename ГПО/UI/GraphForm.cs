using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MathApp.Helpers;

namespace MathApp.UI
{
    public class GraphForm : Form
    {
        private List<double> _values = new List<double>();
        private Timer _refreshTimer;
        private string _sourceName;
        private int _maxPoints = 200;

        private CheckBox _chkAutoScroll;
        private CheckBox _chkShowGrid;
        private ComboBox _cmbLineColor;

        private Bitmap _backBuffer;
        private Graphics _backGraphics;
        private bool needsRedraw = true;
        private Color _lineColor = Color.Cyan;
        private bool _isDisposing = false;

        public GraphForm(string source)
        {
            _sourceName = source;
            InitializeComponent();

            _refreshTimer = new Timer();
            _refreshTimer.Interval = 50;
            _refreshTimer.Tick += (s, e) => needsRedraw = true;
            _refreshTimer.Start();

            var renderTimer = new Timer();
            renderTimer.Interval = 16;
            renderTimer.Tick += (s, e) =>
            {
                if (needsRedraw && !_isDisposing && !this.IsDisposed)
                {
                    DrawToBuffer();
                    this.Invalidate();
                    needsRedraw = false;
                }
            };
            renderTimer.Start();

            this.FormClosing += GraphForm_FormClosing;

            // Создаём буфер после того, как форма отобразилась
            this.Shown += (s, e) => CreateBackBuffer();
        }

        private void GraphForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isDisposing = true;
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }
        }

        private void InitializeComponent()
        {
            this.Text = $"График - {_sourceName}";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 32);
            this.DoubleBuffered = true;

            var topPanel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            _chkAutoScroll = new CheckBox
            {
                Text = "Авто-скролл",
                Location = new Point(10, 10),
                Size = new Size(100, 25),
                ForeColor = Color.White,
                Checked = true,
                BackColor = Color.Transparent
            };
            _chkAutoScroll.CheckedChanged += (s, e) => needsRedraw = true;

            _chkShowGrid = new CheckBox
            {
                Text = "Сетка",
                Location = new Point(120, 10),
                Size = new Size(80, 25),
                ForeColor = Color.White,
                Checked = true,
                BackColor = Color.Transparent
            };
            _chkShowGrid.CheckedChanged += (s, e) => needsRedraw = true;

            _cmbLineColor = new ComboBox
            {
                Location = new Point(210, 10),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };
            _cmbLineColor.Items.AddRange(new object[] {
                "Голубой", "Зеленый", "Красный", "Желтый", "Белый"
            });
            _cmbLineColor.SelectedIndex = 0;
            _cmbLineColor.SelectedIndexChanged += (s, e) =>
            {
                int index = _cmbLineColor.SelectedIndex;
                if (index == 0) _lineColor = Color.Cyan;
                else if (index == 1) _lineColor = Color.LightGreen;
                else if (index == 2) _lineColor = Color.Orange;
                else if (index == 3) _lineColor = Color.Yellow;
                else _lineColor = Color.White;

                needsRedraw = true;
            };

            var btnClear = new Button
            {
                Text = "Очистить",
                Location = new Point(320, 10),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClear.Click += (s, e) => { _values.Clear(); needsRedraw = true; };

            var btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(410, 10),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += SaveButton_Click;

            topPanel.Controls.AddRange(new Control[] {
                _chkAutoScroll, _chkShowGrid, _cmbLineColor, btnClear, btnSave
            });

            this.Controls.Add(topPanel);
        }

        public void AddValue(double value)
        {
            if (_isDisposing || this.IsDisposed) return;

            _values.Add(value);
            if (_values.Count > _maxPoints * 2)
                _values.RemoveRange(0, _values.Count - _maxPoints);
            needsRedraw = true;
        }

        private void CreateBackBuffer()
        {
            if (_isDisposing || this.IsDisposed) return;

            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;

            // Проверяем, что размеры допустимы
            if (width <= 0 || height <= 40)
                return;

            try
            {
                if (_backBuffer != null)
                {
                    _backGraphics?.Dispose();
                    _backBuffer.Dispose();
                }

                _backBuffer = new Bitmap(width, height);
                _backGraphics = Graphics.FromImage(_backBuffer);
                _backGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                _backGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки при создании буфера
                System.Diagnostics.Debug.WriteLine($"CreateBackBuffer error: {ex.Message}");
            }
        }

        private void DrawToBuffer()
        {
            if (_isDisposing || this.IsDisposed) return;
            if (_backGraphics == null) return;

            try
            {
                _backGraphics.Clear(this.BackColor);

                int topOffset = 50;

                // Проверяем, что размеры клиентской области допустимы
                if (this.ClientSize.Width <= 100 || this.ClientSize.Height <= 100)
                    return;

                if (_chkShowGrid.Checked)
                {
                    DrawGrid(_backGraphics, topOffset);
                }

                DrawAxes(_backGraphics, topOffset);

                if (_values.Count > 1)
                {
                    DrawGraph(_backGraphics, topOffset);
                }
                else
                {
                    DrawNoDataMessage(_backGraphics, topOffset);
                }

                if (_values.Count > 0)
                {
                    DrawStats(_backGraphics);
                }

                DrawSourceInfo(_backGraphics, topOffset);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DrawToBuffer error: {ex.Message}");
            }
        }

        private void DrawGrid(Graphics g, int topOffset)
        {
            using (var pen = new Pen(Color.FromArgb(60, 60, 65), 1))
            {
                for (int x = 50; x < this.ClientSize.Width - 50; x += 50)
                    g.DrawLine(pen, x, topOffset, x, this.ClientSize.Height - 50);

                for (int y = topOffset; y < this.ClientSize.Height - 50; y += 50)
                    g.DrawLine(pen, 50, y, this.ClientSize.Width - 50, y);
            }
        }

        private void DrawAxes(Graphics g, int topOffset)
        {
            using (var pen = new Pen(Color.White, 2))
            {
                g.DrawLine(pen, 50, this.ClientSize.Height - 50, this.ClientSize.Width - 50, this.ClientSize.Height - 50);
                g.DrawLine(pen, 50, topOffset, 50, this.ClientSize.Height - 50);
            }
        }

        private void DrawGraph(Graphics g, int topOffset)
        {
            int graphLeft = 60;
            int graphRight = this.ClientSize.Width - 60;
            int graphTop = topOffset;
            int graphBottom = this.ClientSize.Height - 70;
            int graphWidth = graphRight - graphLeft;
            int graphHeight = graphBottom - graphTop;

            if (graphWidth <= 0 || graphHeight <= 0) return;

            double minValue = _values.Min();
            double maxValue = _values.Max();
            double range = maxValue - minValue;
            if (range < 0.001) range = 1;

            int startIndex = 0;
            int endIndex = _values.Count - 1;

            if (_chkAutoScroll.Checked && _values.Count > _maxPoints)
                startIndex = _values.Count - _maxPoints;

            int pointCount = endIndex - startIndex + 1;
            if (pointCount < 2) return;

            var points = new List<PointF>();

            for (int i = startIndex; i <= endIndex; i++)
            {
                float x = graphLeft + (float)((i - startIndex) * graphWidth / (pointCount - 1));
                float y = graphTop + graphHeight -
                          (float)((_values[i] - minValue) / range * graphHeight);
                y = Math.Max(graphTop, Math.Min(graphBottom, y));
                points.Add(new PointF(x, y));
            }

            using (var pen = new Pen(_lineColor, 2))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                for (int i = 0; i < points.Count - 1; i++)
                {
                    g.DrawLine(pen, points[i], points[i + 1]);
                }
            }

            foreach (var point in points)
            {
                g.FillEllipse(Brushes.Red, point.X - 3, point.Y - 3, 6, 6);
            }
        }

        private void DrawNoDataMessage(Graphics g, int topOffset)
        {
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString("Ожидание данных...",
                    new Font("Segoe UI", 14, FontStyle.Bold),
                    Brushes.Gray,
                    new Rectangle(0, topOffset, this.ClientSize.Width, this.ClientSize.Height - topOffset),
                    sf);
            }
        }

        private void DrawStats(Graphics g)
        {
            string stats = $"Значений: {_values.Count} | " +
                          $"Мин: {_values.Min():F2} | " +
                          $"Макс: {_values.Max():F2} | " +
                          $"Среднее: {_values.Average():F2} | " +
                          $"Текущее: {_values.Last():F2}";

            g.DrawString(stats, new Font("Segoe UI", 9),
                Brushes.LightGreen, 60, this.ClientSize.Height - 30);
        }

        private void DrawSourceInfo(Graphics g, int topOffset)
        {
            g.DrawString($"Источник: {_sourceName}",
                new Font("Segoe UI", 9, FontStyle.Bold),
                Brushes.Yellow, 60, topOffset - 20);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (_values.Count == 0) return;

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = $"graph_{_sourceName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                using (var writer = new System.IO.StreamWriter(saveDialog.FileName))
                {
                    writer.WriteLine("Index,Value");
                    for (int i = 0; i < _values.Count; i++)
                    {
                        writer.WriteLine($"{i},{_values[i]:F6}");
                    }
                }

                MessageBox.Show("Данные сохранены!", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_backBuffer != null && !_isDisposing && !this.IsDisposed)
                e.Graphics.DrawImage(_backBuffer, 0, 0);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!_isDisposing && !this.IsDisposed && this.ClientSize.Width > 0 && this.ClientSize.Height > 40)
            {
                CreateBackBuffer();
                needsRedraw = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _isDisposing = true;

            if (disposing)
            {
                if (_refreshTimer != null)
                {
                    _refreshTimer.Stop();
                    _refreshTimer.Dispose();
                    _refreshTimer = null;
                }

                if (_backGraphics != null)
                {
                    _backGraphics.Dispose();
                    _backGraphics = null;
                }

                if (_backBuffer != null)
                {
                    _backBuffer.Dispose();
                    _backBuffer = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
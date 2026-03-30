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
        private bool _needsRedraw = true;
        private Color _lineColor = Color.Cyan;
        private bool _isInitialized = false;

        // Axis ranges
        private double _currentMin = 0;
        private double _currentMax = 1;
        private bool _autoScaleY = true;
        private double _fixedMin = 0;
        private double _fixedMax = 100;

        public GraphForm(string source)
        {
            _sourceName = source;
            InitializeComponent();

            _refreshTimer = new Timer();
            _refreshTimer.Interval = 50;
            _refreshTimer.Tick += (s, e) => _needsRedraw = true;
            _refreshTimer.Start();

            var renderTimer = new Timer();
            renderTimer.Interval = 16;
            renderTimer.Tick += (s, e) =>
            {
                if (_needsRedraw)
                {
                    DrawToBuffer();
                    this.Invalidate();
                    _needsRedraw = false;
                }
            };
            renderTimer.Start();

            this.Load += GraphForm_Load;
        }

        private void GraphForm_Load(object sender, EventArgs e)
        {
            CreateBackBuffer();
            DrawToBuffer();
            this.Invalidate();
            _isInitialized = true;
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
            _chkAutoScroll.CheckedChanged += (s, e) => _needsRedraw = true;

            _chkShowGrid = new CheckBox
            {
                Text = "Сетка",
                Location = new Point(120, 10),
                Size = new Size(80, 25),
                ForeColor = Color.White,
                Checked = true,
                BackColor = Color.Transparent
            };
            _chkShowGrid.CheckedChanged += (s, e) => _needsRedraw = true;

            var chkAutoScale = new CheckBox
            {
                Text = "Авто-масштаб Y",
                Location = new Point(210, 10),
                Size = new Size(110, 25),
                ForeColor = Color.White,
                Checked = true,
                BackColor = Color.Transparent
            };
            chkAutoScale.CheckedChanged += (s, e) =>
            {
                _autoScaleY = chkAutoScale.Checked;
                _needsRedraw = true;
            };

            _cmbLineColor = new ComboBox
            {
                Location = new Point(330, 10),
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

                _needsRedraw = true;
            };

            var btnClear = new Button
            {
                Text = "Очистить",
                Location = new Point(440, 10),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClear.Click += (s, e) => { _values.Clear(); _needsRedraw = true; };

            var btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(530, 10),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += SaveButton_Click;

            topPanel.Controls.AddRange(new Control[] {
                _chkAutoScroll, _chkShowGrid, chkAutoScale, _cmbLineColor, btnClear, btnSave
            });

            this.Controls.Add(topPanel);
        }

        public void AddValue(double value)
        {
            _values.Add(value);
            if (_values.Count > _maxPoints * 2)
                _values.RemoveRange(0, _values.Count - _maxPoints);
            _needsRedraw = true;
        }

        private void CreateBackBuffer()
        {
            if (this.Width <= 0 || this.Height <= 40) return;

            if (_backBuffer != null)
            {
                _backGraphics?.Dispose();
                _backBuffer.Dispose();
            }

            _backBuffer = new Bitmap(this.Width, this.Height);
            _backGraphics = Graphics.FromImage(_backBuffer);
            _backGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _backGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        }

        private void DrawToBuffer()
        {
            if (_backGraphics == null) return;
            if (this.Width <= 0 || this.Height <= 0) return;

            _backGraphics.Clear(this.BackColor);

            int topOffset = 50;
            int bottomOffset = 70;
            int leftOffset = 70;
            int rightOffset = 40;

            // Update Y-axis range if auto-scaling
            if (_autoScaleY && _values.Count > 0)
            {
                _currentMin = _values.Min();
                _currentMax = _values.Max();
                double padding = (_currentMax - _currentMin) * 0.1;
                if (padding < 0.001) padding = 1;
                _currentMin -= padding;
                _currentMax += padding;
                if (_currentMin == _currentMax)
                {
                    _currentMin -= 0.5;
                    _currentMax += 0.5;
                }
            }
            else if (_values.Count > 0 && !_autoScaleY)
            {
                _currentMin = _fixedMin;
                _currentMax = _fixedMax;
            }

            if (_chkShowGrid.Checked)
            {
                DrawGrid(_backGraphics, topOffset, bottomOffset, leftOffset, rightOffset);
            }

            DrawAxes(_backGraphics, topOffset, bottomOffset, leftOffset, rightOffset);
            DrawAxisLabels(_backGraphics, topOffset, bottomOffset, leftOffset, rightOffset);
            DrawTickLabels(_backGraphics, topOffset, bottomOffset, leftOffset, rightOffset);

            if (_values.Count > 1)
            {
                DrawGraph(_backGraphics, topOffset, bottomOffset, leftOffset, rightOffset);
            }
            else if (_values.Count == 1)
            {
                DrawSinglePoint(_backGraphics, topOffset, bottomOffset, leftOffset, rightOffset);
            }
            else
            {
                DrawNoDataMessage(_backGraphics, topOffset);
            }

            if (_values.Count > 0)
            {
                DrawStats(_backGraphics, bottomOffset);
            }

            DrawSourceInfo(_backGraphics, topOffset);
        }

        private void DrawGrid(Graphics g, int topOffset, int bottomOffset, int leftOffset, int rightOffset)
        {
            using (var pen = new Pen(Color.FromArgb(60, 60, 65), 1))
            {
                // Vertical grid lines
                int gridCount = 8;
                int graphWidth = this.Width - leftOffset - rightOffset;
                for (int i = 0; i <= gridCount; i++)
                {
                    int x = leftOffset + (i * graphWidth / gridCount);
                    g.DrawLine(pen, x, topOffset, x, this.Height - bottomOffset);
                }

                // Horizontal grid lines
                int horizontalGridCount = 5;
                int graphHeight = this.Height - topOffset - bottomOffset;
                for (int i = 0; i <= horizontalGridCount; i++)
                {
                    int y = topOffset + (i * graphHeight / horizontalGridCount);
                    g.DrawLine(pen, leftOffset, y, this.Width - rightOffset, y);
                }
            }
        }

        private void DrawAxes(Graphics g, int topOffset, int bottomOffset, int leftOffset, int rightOffset)
        {
            using (var pen = new Pen(Color.White, 2))
            {
                // X-axis
                g.DrawLine(pen, leftOffset, this.Height - bottomOffset,
                    this.Width - rightOffset, this.Height - bottomOffset);
                // Y-axis
                g.DrawLine(pen, leftOffset, topOffset, leftOffset, this.Height - bottomOffset);
            }

            // Arrow heads
            using (var arrowPen = new Pen(Color.White, 2))
            {
                // X-axis arrow
                g.DrawLine(arrowPen, this.Width - rightOffset - 5, this.Height - bottomOffset - 3,
                    this.Width - rightOffset, this.Height - bottomOffset);
                g.DrawLine(arrowPen, this.Width - rightOffset - 5, this.Height - bottomOffset + 3,
                    this.Width - rightOffset, this.Height - bottomOffset);

                // Y-axis arrow
                g.DrawLine(arrowPen, leftOffset - 3, topOffset + 5,
                    leftOffset, topOffset);
                g.DrawLine(arrowPen, leftOffset + 3, topOffset + 5,
                    leftOffset, topOffset);
            }
        }

        private void DrawAxisLabels(Graphics g, int topOffset, int bottomOffset, int leftOffset, int rightOffset)
        {
            using (var labelFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                // Подпись оси X (горизонтальная) - сдвинута выше, чтобы была видна
                string xLabel = "Время (точки)";
                SizeF xLabelSize = g.MeasureString(xLabel, labelFont);
                float xLabelX = (this.Width - xLabelSize.Width) / 2;
                float xLabelY = this.Height - bottomOffset + 15; // Уменьшил отступ
                g.DrawString(xLabel, labelFont, brush, xLabelX, xLabelY);

                // Подпись оси Y (вертикальная, повернутая)
                string yLabel = "Значение";
                using (var transform = new Matrix())
                {
                    var state = g.Save();

                    float yLabelX = leftOffset - 35;
                    float yLabelY = (this.Height + topOffset) / 2;

                    transform.RotateAt(-90, new PointF(yLabelX, yLabelY));
                    g.Transform = transform;
                    g.DrawString(yLabel, labelFont, brush, yLabelX, yLabelY - 10);

                    g.Restore(state);
                }
            }
        }

        private void DrawTickLabels(Graphics g, int topOffset, int bottomOffset, int leftOffset, int rightOffset)
        {
            using (var labelFont = new Font("Segoe UI", 8))
            using (var brush = new SolidBrush(Color.LightGray))
            {
                // Y-axis tick labels
                if (_values.Count > 0 && _currentMax > _currentMin)
                {
                    int horizontalGridCount = 5;
                    int graphHeight = this.Height - topOffset - bottomOffset;

                    for (int i = 0; i <= horizontalGridCount; i++)
                    {
                        int y = topOffset + (i * graphHeight / horizontalGridCount);
                        double value = _currentMax - (i * (_currentMax - _currentMin) / horizontalGridCount);
                        string label = value.ToString("F2");
                        SizeF textSize = g.MeasureString(label, labelFont);
                        g.DrawString(label, labelFont, brush,
                            leftOffset - textSize.Width - 5, y - textSize.Height / 2);
                    }
                }

                // X-axis tick labels
                if (_values.Count > 0)
                {
                    int verticalGridCount = 8;
                    int graphWidth = this.Width - leftOffset - rightOffset;
                    int startIndex = 0;
                    int endIndex = _values.Count - 1;

                    if (_chkAutoScroll.Checked && _values.Count > _maxPoints)
                        startIndex = _values.Count - _maxPoints;

                    int pointCount = endIndex - startIndex + 1;

                    for (int i = 0; i <= verticalGridCount; i++)
                    {
                        int x = leftOffset + (i * graphWidth / verticalGridCount);
                        int pointIndex = startIndex + (i * pointCount / verticalGridCount);
                        if (pointIndex <= endIndex && pointIndex >= 0)
                        {
                            string label = pointIndex.ToString();
                            SizeF textSize = g.MeasureString(label, labelFont);
                            g.DrawString(label, labelFont, brush,
                                x - textSize.Width / 2, this.Height - bottomOffset + 5);
                        }
                    }
                }
            }
        }

        private void DrawGraph(Graphics g, int topOffset, int bottomOffset, int leftOffset, int rightOffset)
        {
            int graphLeft = leftOffset;
            int graphRight = this.Width - rightOffset;
            int graphTop = topOffset;
            int graphBottom = this.Height - bottomOffset;
            int graphWidth = graphRight - graphLeft;
            int graphHeight = graphBottom - graphTop;

            double minValue = _autoScaleY ? _currentMin : _values.Min();
            double maxValue = _autoScaleY ? _currentMax : _values.Max();
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

            // Draw points
            using (var pointBrush = new SolidBrush(Color.FromArgb(180, 255, 100, 100)))
            {
                foreach (var point in points)
                {
                    g.FillEllipse(pointBrush, point.X - 3, point.Y - 3, 6, 6);
                }
            }
        }

        private void DrawSinglePoint(Graphics g, int topOffset, int bottomOffset, int leftOffset, int rightOffset)
        {
            int graphLeft = leftOffset;
            int graphRight = this.Width - rightOffset;
            int graphTop = topOffset;
            int graphBottom = this.Height - bottomOffset;
            int graphWidth = graphRight - graphLeft;
            int graphHeight = graphBottom - graphTop;

            double minValue = _autoScaleY ? _currentMin : _values.Min();
            double maxValue = _autoScaleY ? _currentMax : _values.Max();
            double range = maxValue - minValue;
            if (range < 0.001) range = 1;

            int x = graphLeft + graphWidth / 2;
            float y = graphTop + graphHeight -
                      (float)((_values[0] - minValue) / range * graphHeight);
            y = Math.Max(graphTop, Math.Min(graphBottom, y));

            using (var pointBrush = new SolidBrush(_lineColor))
            {
                g.FillEllipse(pointBrush, x - 5, y - 5, 10, 10);
            }

            using (var font = new Font("Segoe UI", 10))
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString($"Значение: {_values[0]:F2}", font, brush,
                    x + 10, y - 10);
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
                    new Rectangle(0, topOffset, this.Width, this.Height - topOffset),
                    sf);
            }
        }

        private void DrawStats(Graphics g, int bottomOffset)
        {
            if (_values.Count == 0) return;

            string stats = $"Значений: {_values.Count} | " +
                          $"Мин: {_values.Min():F2} | " +
                          $"Макс: {_values.Max():F2} | " +
                          $"Среднее: {_values.Average():F2} | " +
                          $"Текущее: {_values.Last():F2}";

            if (!_autoScaleY && _values.Count > 0)
            {
                stats += $" | Диапазон Y: [{_fixedMin:F2}; {_fixedMax:F2}]";
            }

            g.DrawString(stats, new Font("Segoe UI", 9),
                Brushes.LightGreen, 10, this.Height - bottomOffset + 35);
        }

        private void DrawSourceInfo(Graphics g, int topOffset)
        {
            g.DrawString($"Источник: {_sourceName}",
                new Font("Segoe UI", 9, FontStyle.Bold),
                Brushes.Yellow, 10, topOffset - 25);
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
            if (_backBuffer != null)
                e.Graphics.DrawImage(_backBuffer, 0, 0);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.Width > 0 && this.Height > 0)
            {
                CreateBackBuffer();
                _needsRedraw = true;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!_isInitialized)
            {
                CreateBackBuffer();
                DrawToBuffer();
                this.Invalidate();
                _isInitialized = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_refreshTimer != null)
                {
                    _refreshTimer.Stop();
                    _refreshTimer.Dispose();
                }
                if (_backGraphics != null)
                {
                    _backGraphics.Dispose();
                }
                if (_backBuffer != null)
                {
                    _backBuffer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
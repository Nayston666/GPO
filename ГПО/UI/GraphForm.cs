using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MathApp.Helpers;

namespace MathApp.UI
{
    /// <summary>
    /// Устанавливает данные напрямую (для файлового блока)
    /// </summary>
    
    public class GraphForm : Form
    {
        private List<double> _values = new List<double>();
        private List<double> _timeValues = new List<double>();
        private string _sourceName;

        private double _xMin = 0, _xMax = 10, _yMin = -2, _yMax = 2;
        private double _viewXMin, _viewXMax, _viewYMin, _viewYMax;
        private bool _autoScale = true;

        private Point _dragStart;
        private bool _isPanning = false;

        private CheckBox _chkAutoScale, _chkShowGrid;
        private ComboBox _cmbLineColor;
        private Button _btnResetView, _btnExport, _btnCalculate;
        private Label _lblCoordinates;
        private NumericUpDown _numPoints;

        private Bitmap _backBuffer;
        private Graphics _backGraphics;
        private bool _needsRedraw = true;
        private Color _lineColor = Color.Cyan;

        private Func<double, double> _calcFunction;
        private double _calcStart = 0, _calcEnd = 10;
        private int _calcPoints = 200;
        public void SetDataDirectly(List<double> timeValues, List<double> values)
        {
            _timeValues = new List<double>(timeValues);
            _values = new List<double>(values);
            if (_autoScale) AutoScale();
            _needsRedraw = true;
        }

        public GraphForm(string source)
        {
            _sourceName = source;
            _viewXMin = _xMin; _viewXMax = _xMax; _viewYMin = _yMin; _viewYMax = _yMax;
            InitializeComponent();

            var timer = new Timer { Interval = 16 };
            timer.Tick += (s, e) => { if (_needsRedraw) { DrawToBuffer(); Invalidate(); _needsRedraw = false; } };
            timer.Start();
            CreateBackBuffer();
        }

        private void InitializeComponent()
        {
            Text = $"График - {_sourceName}";
            Size = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(30, 30, 32);
            DoubleBuffered = true;

            var topPanel = new Panel { Height = 45, Dock = DockStyle.Top, BackColor = Color.FromArgb(45, 45, 48) };

            _chkAutoScale = new CheckBox { Text = "Автомасштаб", Location = new Point(10, 12), Size = new Size(100, 25), ForeColor = Color.White, Checked = true, FlatStyle = FlatStyle.Flat };
            _chkAutoScale.CheckedChanged += (s, e) => { _autoScale = _chkAutoScale.Checked; if (_autoScale) AutoScale(); _needsRedraw = true; };

            _chkShowGrid = new CheckBox { Text = "Сетка", Location = new Point(120, 12), Size = new Size(80, 25), ForeColor = Color.White, Checked = true, FlatStyle = FlatStyle.Flat };
            _chkShowGrid.CheckedChanged += (s, e) => _needsRedraw = true;

            var lblColor = new Label { Text = "Цвет:", Location = new Point(210, 15), Size = new Size(40, 20), ForeColor = Color.White };
            _cmbLineColor = new ComboBox { Location = new Point(250, 12), Size = new Size(100, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White };
            _cmbLineColor.Items.AddRange(new[] { "Голубой", "Зеленый", "Красный", "Желтый", "Белый" });
            _cmbLineColor.SelectedIndex = 0;
            _cmbLineColor.SelectedIndexChanged += (s, e) =>
            {
                int idx = _cmbLineColor.SelectedIndex;
                if (idx == 0) _lineColor = Color.Cyan;
                else if (idx == 1) _lineColor = Color.LightGreen;
                else if (idx == 2) _lineColor = Color.Orange;
                else if (idx == 3) _lineColor = Color.Yellow;
                else _lineColor = Color.White;
                _needsRedraw = true;
            };

            var lblPoints = new Label { Text = "Точек:", Location = new Point(360, 15), Size = new Size(45, 20), ForeColor = Color.White };
            _numPoints = new NumericUpDown
            {
                Location = new Point(405, 12),
                Size = new Size(100, 25),
                Minimum = 100,        // Минимум 100 точек
                Maximum = 10000,      // Максимум 10000 точек
                Value = 1000,         // По умолчанию 1000
                Increment = 500,      // Шаг 500
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };

            _btnCalculate = new Button { Text = "⟳ Пересчитать", Location = new Point(515, 12), Size = new Size(100, 25), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnCalculate.Click += (s, e) => Recalculate();

            _btnResetView = new Button { Text = "Сброс масштаба", Location = new Point(625, 12), Size = new Size(100, 25), BackColor = Color.FromArgb(100, 100, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnResetView.Click += (s, e) => ResetView();

            _btnExport = new Button { Text = "💾 Экспорт", Location = new Point(735, 12), Size = new Size(80, 25), BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnExport.Click += (s, e) => ExportData();

            _lblCoordinates = new Label { Text = "x: --  y: --", Location = new Point(825, 15), Size = new Size(150, 20), ForeColor = Color.LightGray, Font = new Font("Segoe UI", 8) };

            topPanel.Controls.AddRange(new Control[] { _chkAutoScale, _chkShowGrid, lblColor, _cmbLineColor, lblPoints, _numPoints, _btnCalculate, _btnResetView, _btnExport, _lblCoordinates });
            Controls.Add(topPanel);

            MouseWheel += (s, e) => { if (_values.Count > 0) Zoom(e.Delta > 0 ? 0.9 : 1.1, PointToClient(Cursor.Position)); };
            MouseDown += (s, e) => { if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Left) { _isPanning = true; _dragStart = e.Location; } };
            MouseMove += (s, e) => { double x = ScreenToGraphX(e.X), y = ScreenToGraphY(e.Y); _lblCoordinates.Text = $"x: {x:F3}  y: {y:F3}"; if (_isPanning) Pan(e.Location); };
            MouseUp += (s, e) => _isPanning = false;
        }

        public void SetCalculationFunction(Func<double, double> func, double start, double end, int points)
        {
            _calcFunction = func;
            _calcStart = start;
            _calcEnd = end;
            _calcPoints = points;
            Recalculate();
        }

       
        private void Recalculate()
        {
            if (_calcFunction == null) return;
            _calcPoints = (int)_numPoints.Value;
            _values.Clear();
            _timeValues.Clear();
            double step = (_calcEnd - _calcStart) / (_calcPoints - 1);
            for (int i = 0; i < _calcPoints; i++)
            {
                double x = _calcStart + i * step;
                _timeValues.Add(x);
                _values.Add(_calcFunction(x));
            }
            if (_autoScale) AutoScale();
            _needsRedraw = true;
        }

        private void AutoScale()
        {
            if (_values.Count == 0) return;
            _xMin = _timeValues.Min();
            _xMax = _timeValues.Max();
            _yMin = _values.Min();
            _yMax = _values.Max();
            double xMargin = Math.Max(0.1, (_xMax - _xMin) * 0.05);
            double yMargin = Math.Max(0.1, (_yMax - _yMin) * 0.05);
            _xMin -= xMargin;
            _xMax += xMargin;
            _yMin -= yMargin;
            _yMax += yMargin;
            _viewXMin = _xMin;
            _viewXMax = _xMax;
            _viewYMin = _yMin;
            _viewYMax = _yMax;
        }

        private void ResetView() { _viewXMin = _xMin; _viewXMax = _xMax; _viewYMin = _yMin; _viewYMax = _yMax; _needsRedraw = true; }

        private void Zoom(double factor, Point mouse)
        {
            double xMouse = ScreenToGraphX(mouse.X), yMouse = ScreenToGraphY(mouse.Y);
            double newWidth = (_viewXMax - _viewXMin) * factor;
            double newHeight = (_viewYMax - _viewYMin) * factor;
            double tX = (xMouse - _viewXMin) / (_viewXMax - _viewXMin);
            double tY = (yMouse - _viewYMin) / (_viewYMax - _viewYMin);
            _viewXMin = xMouse - newWidth * tX;
            _viewXMax = xMouse + newWidth * (1 - tX);
            _viewYMin = yMouse - newHeight * tY;
            _viewYMax = yMouse + newHeight * (1 - tY);
            _needsRedraw = true;
        }

        private void Pan(Point mouse)
        {
            double dx = ScreenToGraphX(_dragStart.X) - ScreenToGraphX(mouse.X);
            double dy = ScreenToGraphY(_dragStart.Y) - ScreenToGraphY(mouse.Y);
            _viewXMin += dx;
            _viewXMax += dx;
            _viewYMin += dy;
            _viewYMax += dy;
            _dragStart = mouse;
            _needsRedraw = true;
        }

        private double ScreenToGraphX(int screenX) => _viewXMin + (screenX - 70) / (double)(Width - 120) * (_viewXMax - _viewXMin);
        private double ScreenToGraphY(int screenY) => _viewYMax - (screenY - 60) / (double)(Height - 130) * (_viewYMax - _viewYMin);

        private void CreateBackBuffer()
        {
            if (Width <= 0 || Height <= 60) return;
            _backBuffer?.Dispose();
            _backGraphics?.Dispose();
            _backBuffer = new Bitmap(Width, Height);
            _backGraphics = Graphics.FromImage(_backBuffer);
            _backGraphics.SmoothingMode = SmoothingMode.AntiAlias;
        }

        private void DrawToBuffer()
        {
            if (_backGraphics == null) return;
            _backGraphics.Clear(BackColor);
            int left = 70, right = Width - 50, top = 60, bottom = Height - 70;
            using (var pen = new Pen(Color.FromArgb(80, 80, 85), 1))
                _backGraphics.DrawRectangle(pen, left, top, right - left, bottom - top);
            if (_chkShowGrid.Checked) DrawGrid(left, right, top, bottom);
            DrawAxes(left, right, top, bottom);
            if (_values.Count > 1) DrawGraph(left, right, top, bottom);
            else _backGraphics.DrawString("Нет данных", new Font("Segoe UI", 14), Brushes.Gray, left, top + (bottom - top) / 2);
            if (_values.Count > 0) DrawStats();
        }

        private void DrawGrid(int left, int right, int top, int bottom)
        {
            using (var pen = new Pen(Color.FromArgb(60, 60, 65), 1))
            {
                // Автоматический шаг сетки в зависимости от масштаба
                double xStep = Math.Pow(10, Math.Floor(Math.Log10(_viewXMax - _viewXMin))) / 2;
                if (xStep < 0.1) xStep = 0.1;

                for (double x = Math.Ceiling(_viewXMin / xStep) * xStep; x <= _viewXMax; x += xStep)
                {
                    if (x < _viewXMin) continue;
                    int sx = left + (int)((x - _viewXMin) / (_viewXMax - _viewXMin) * (right - left));
                    if (sx >= left && sx <= right)
                    {
                        _backGraphics.DrawLine(pen, sx, top, sx, bottom);
                        _backGraphics.DrawString(x.ToString("F2"), new Font("Segoe UI", 7), Brushes.Gray, sx - 15, bottom + 5);
                    }
                }

                double yStep = Math.Pow(10, Math.Floor(Math.Log10(_viewYMax - _viewYMin))) / 2;
                if (yStep < 0.1) yStep = 0.1;

                for (double y = Math.Ceiling(_viewYMin / yStep) * yStep; y <= _viewYMax; y += yStep)
                {
                    if (y < _viewYMin) continue;
                    int sy = bottom - (int)((y - _viewYMin) / (_viewYMax - _viewYMin) * (bottom - top));
                    if (sy >= top && sy <= bottom)
                    {
                        _backGraphics.DrawLine(pen, left, sy, right, sy);
                        _backGraphics.DrawString(y.ToString("F2"), new Font("Segoe UI", 7), Brushes.Gray, left - 35, sy - 7);
                    }
                }
            }
        }

        private void DrawAxes(int left, int right, int top, int bottom)
        {
            using (var pen = new Pen(Color.White, 2))
            {
                if (_viewYMin <= 0 && _viewYMax >= 0)
                {
                    int yZero = bottom - (int)((0 - _viewYMin) / (_viewYMax - _viewYMin) * (bottom - top));
                    _backGraphics.DrawLine(pen, left, yZero, right, yZero);
                }
                if (_viewXMin <= 0 && _viewXMax >= 0)
                {
                    int xZero = left + (int)((0 - _viewXMin) / (_viewXMax - _viewXMin) * (right - left));
                    _backGraphics.DrawLine(pen, xZero, top, xZero, bottom);
                }
            }
        }

        private void DrawGraph(int left, int right, int top, int bottom)
        {
            var points = new List<PointF>();
            int step = Math.Max(1, _values.Count / 2000); // Оптимизация: не рисуем каждую точку при >2000

            for (int i = 0; i < _values.Count; i += step)
            {
                double x = _timeValues[i], y = _values[i];
                if (x < _viewXMin || x > _viewXMax || y < _viewYMin || y > _viewYMax) continue;
                float sx = left + (float)((x - _viewXMin) / (_viewXMax - _viewXMin) * (right - left));
                float sy = bottom - (float)((y - _viewYMin) / (_viewYMax - _viewYMin) * (bottom - top));
                points.Add(new PointF(sx, sy));
            }
            if (points.Count < 2) return;

            using (var pen = new Pen(_lineColor, 2))
            {
                for (int i = 0; i < points.Count - 1; i++)
                    _backGraphics.DrawLine(pen, points[i], points[i + 1]);
            }

            // Точки рисуем только если их не слишком много
            if (points.Count < 500)
            {
                foreach (var p in points)
                    _backGraphics.FillEllipse(Brushes.Red, p.X - 2, p.Y - 2, 4, 4);
            }
        }

        private void DrawStats()
        {
            string stats = $"Значений: {_values.Count} | Мин: {_values.Min():F3} | Макс: {_values.Max():F3} | Среднее: {_values.Average():F3}";
            _backGraphics.DrawString(stats, new Font("Segoe UI", 9), Brushes.LightGreen, 70, Height - 25);
        }

        private void ExportData()
        {
            if (_values.Count == 0) return;
            var dlg = new SaveFileDialog { Filter = "CSV файлы (*.csv)|*.csv", FileName = $"graph_{_sourceName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv" };
            if (dlg.ShowDialog() == DialogResult.OK)
                using (var w = new System.IO.StreamWriter(dlg.FileName))
                {
                    w.WriteLine("Time,Value");
                    for (int i = 0; i < _values.Count; i++)
                        w.WriteLine($"{_timeValues[i]:F6},{_values[i]:F6}");
                    MessageBox.Show($"Сохранено {_values.Count} точек!", "Успех");
                }
        }

        protected override void OnPaint(PaintEventArgs e) { if (_backBuffer != null) e.Graphics.DrawImage(_backBuffer, 0, 0); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); CreateBackBuffer(); _needsRedraw = true; }
        protected override void Dispose(bool disposing) { if (disposing) { _backGraphics?.Dispose(); _backBuffer?.Dispose(); } base.Dispose(disposing); }
    }
}
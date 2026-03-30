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
        private List<double> _timeValues = new List<double>();
        private string _sourceName;

        // Настройки отображения
        private double _xMin = 0, _xMax = 10, _yMin = -2, _yMax = 2;
        private double _xRange, _yRange;
        private bool _autoScale = true;

        // Для масштабирования
        private Point _dragStart;
        private bool _isPanning = false;
        private double _viewXMin, _viewXMax, _viewYMin, _viewYMax;

        // UI элементы
        private CheckBox _chkAutoScale;
        private CheckBox _chkShowGrid;
        private ComboBox _cmbLineColor;
        private Button _btnResetView;
        private Button _btnExport;
        private Label _lblCoordinates;
        private NumericUpDown _numPoints;
        private Button _btnCalculate;

        // Для отрисовки
        private Bitmap _backBuffer;
        private Graphics _backGraphics;
        private bool _needsRedraw = true;
        private Color _lineColor = Color.Cyan;

        // Данные для расчета
        private Func<double, double> _calculationFunction;
        private double _calculationStart = 0;
        private double _calculationEnd = 10;
        private int _calculationPoints = 200;

        public GraphForm(string source)
        {
            _sourceName = source;
            _viewXMin = _xMin;
            _viewXMax = _xMax;
            _viewYMin = _yMin;
            _viewYMax = _yMax;

            InitializeComponent();

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

            CreateBackBuffer();
        }

        private void InitializeComponent()
        {
            this.Text = $"График - {_sourceName}";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 32);
            this.DoubleBuffered = true;

            // Верхняя панель инструментов
            var topPanel = new Panel
            {
                Height = 45,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Автомасштабирование
            _chkAutoScale = new CheckBox
            {
                Text = "Автомасштаб",
                Location = new Point(10, 12),
                Size = new Size(100, 25),
                ForeColor = Color.White,
                Checked = true,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat
            };
            _chkAutoScale.CheckedChanged += (s, e) =>
            {
                _autoScale = _chkAutoScale.Checked;
                if (_autoScale) AutoScale();
                _needsRedraw = true;
            };

            // Сетка
            _chkShowGrid = new CheckBox
            {
                Text = "Сетка",
                Location = new Point(120, 12),
                Size = new Size(80, 25),
                ForeColor = Color.White,
                Checked = true,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat
            };
            _chkShowGrid.CheckedChanged += (s, e) => _needsRedraw = true;

            // Цвет линии
            var lblColor = new Label
            {
                Text = "Цвет:",
                Location = new Point(210, 15),
                Size = new Size(40, 20),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            _cmbLineColor = new ComboBox
            {
                Location = new Point(250, 12),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _cmbLineColor.Items.AddRange(new object[] { "Голубой", "Зеленый", "Красный", "Желтый", "Белый" });
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

            // Количество точек
            var lblPoints = new Label
            {
                Text = "Точек:",
                Location = new Point(360, 15),
                Size = new Size(45, 20),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            _numPoints = new NumericUpDown
            {
                Location = new Point(405, 12),
                Size = new Size(70, 25),
                Minimum = 50,
                Maximum = 1000,
                Value = 200,
                Increment = 50,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };

            // Кнопка расчета
            _btnCalculate = new Button
            {
                Text = "⟳ Пересчитать",
                Location = new Point(485, 12),
                Size = new Size(100, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            _btnCalculate.Click += (s, e) => Recalculate();

            // Кнопка сброса масштаба
            _btnResetView = new Button
            {
                Text = "Сброс масштаба",
                Location = new Point(595, 12),
                Size = new Size(100, 25),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnResetView.Click += (s, e) => ResetView();

            // Кнопка экспорта
            _btnExport = new Button
            {
                Text = "💾 Экспорт",
                Location = new Point(705, 12),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnExport.Click += (s, e) => ExportData();

            // Координаты под курсором
            _lblCoordinates = new Label
            {
                Text = "x: --  y: --",
                Location = new Point(795, 15),
                Size = new Size(150, 20),
                ForeColor = Color.LightGray,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8)
            };

            topPanel.Controls.AddRange(new Control[] {
                _chkAutoScale, _chkShowGrid, lblColor, _cmbLineColor,
                lblPoints, _numPoints, _btnCalculate, _btnResetView, _btnExport, _lblCoordinates
            });

            this.Controls.Add(topPanel);

            // Подписка на события мыши для масштабирования
            this.MouseWheel += GraphForm_MouseWheel;
            this.MouseDown += GraphForm_MouseDown;
            this.MouseMove += GraphForm_MouseMove;
            this.MouseUp += GraphForm_MouseUp;
        }

        /// <summary>
        /// Устанавливает функцию для расчета
        /// </summary>
        public void SetCalculationFunction(Func<double, double> function, double start, double end, int points)
        {
            _calculationFunction = function;
            _calculationStart = start;
            _calculationEnd = end;
            _calculationPoints = points;

            Recalculate();
        }

        /// <summary>
        /// Выполняет расчет значений
        /// </summary>
        public void Recalculate()
        {
            if (_calculationFunction == null) return;

            _calculationPoints = (int)_numPoints.Value;

            _values.Clear();
            _timeValues.Clear();

            double step = (_calculationEnd - _calculationStart) / (_calculationPoints - 1);

            for (int i = 0; i < _calculationPoints; i++)
            {
                double x = _calculationStart + i * step;
                double y = _calculationFunction(x);

                _timeValues.Add(x);
                _values.Add(y);
            }

            if (_autoScale)
            {
                AutoScale();
            }

            _needsRedraw = true;
        }

        /// <summary>
        /// Автоматическое масштабирование
        /// </summary>
        private void AutoScale()
        {
            if (_values.Count == 0) return;

            _xMin = _timeValues.Min();
            _xMax = _timeValues.Max();
            _yMin = _values.Min();
            _yMax = _values.Max();

            // Добавляем отступ 10%
            double xMargin = (_xMax - _xMin) * 0.1;
            double yMargin = (_yMax - _yMin) * 0.1;
            if (xMargin < 0.001) xMargin = 0.1;
            if (yMargin < 0.001) yMargin = 0.1;

            _xMin -= xMargin;
            _xMax += xMargin;
            _yMin -= yMargin;
            _yMax += yMargin;

            _xRange = _xMax - _xMin;
            _yRange = _yMax - _yMin;

            _viewXMin = _xMin;
            _viewXMax = _xMax;
            _viewYMin = _yMin;
            _viewYMax = _yMax;
        }

        /// <summary>
        /// Сброс масштаба
        /// </summary>
        private void ResetView()
        {
            if (_autoScale)
            {
                AutoScale();
            }
            else
            {
                _viewXMin = _xMin;
                _viewXMax = _xMax;
                _viewYMin = _yMin;
                _viewYMax = _yMax;
            }
            _needsRedraw = true;
        }

        /// <summary>
        /// Масштабирование колесиком мыши
        /// </summary>
        private void GraphForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_values.Count == 0) return;

            double scaleFactor = e.Delta > 0 ? 0.9 : 1.1;

            // Получаем координаты мыши в системе координат графика
            Point mousePoint = this.PointToClient(Cursor.Position);
            double xMouse = ScreenToGraphX(mousePoint.X);
            double yMouse = ScreenToGraphY(mousePoint.Y);

            // Масштабируем относительно курсора
            double newWidth = (_viewXMax - _viewXMin) * scaleFactor;
            double newHeight = (_viewYMax - _viewYMin) * scaleFactor;

            double tX = (xMouse - _viewXMin) / (_viewXMax - _viewXMin);
            double tY = (yMouse - _viewYMin) / (_viewYMax - _viewYMin);

            _viewXMin = xMouse - newWidth * tX;
            _viewXMax = xMouse + newWidth * (1 - tX);
            _viewYMin = yMouse - newHeight * tY;
            _viewYMax = yMouse + newHeight * (1 - tY);

            _needsRedraw = true;
        }

        /// <summary>
        /// Панорамирование
        /// </summary>
        private void GraphForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Left)
            {
                _isPanning = true;
                _dragStart = e.Location;
            }
        }

        private void GraphForm_MouseMove(object sender, MouseEventArgs e)
        {
            // Обновляем координаты под курсором
            double x = ScreenToGraphX(e.X);
            double y = ScreenToGraphY(e.Y);
            _lblCoordinates.Text = $"x: {x:F3}  y: {y:F3}";

            // Панорамирование
            if (_isPanning)
            {
                double dx = ScreenToGraphX(_dragStart.X) - ScreenToGraphX(e.X);
                double dy = ScreenToGraphY(_dragStart.Y) - ScreenToGraphY(e.Y);

                _viewXMin += dx;
                _viewXMax += dx;
                _viewYMin += dy;
                _viewYMax += dy;

                _dragStart = e.Location;
                _needsRedraw = true;
            }
        }

        private void GraphForm_MouseUp(object sender, MouseEventArgs e)
        {
            _isPanning = false;
        }

        private double ScreenToGraphX(int screenX)
        {
            int graphLeft = 70;
            int graphRight = this.ClientSize.Width - 50;
            double t = (screenX - graphLeft) / (double)(graphRight - graphLeft);
            return _viewXMin + t * (_viewXMax - _viewXMin);
        }

        private double ScreenToGraphY(int screenY)
        {
            int graphTop = 60;
            int graphBottom = this.ClientSize.Height - 70;
            double t = (screenY - graphTop) / (double)(graphBottom - graphTop);
            return _viewYMax - t * (_viewYMax - _viewYMin);
        }

        private void CreateBackBuffer()
        {
            if (this.Width <= 0 || this.Height <= 60) return;

            _backBuffer?.Dispose();
            _backGraphics?.Dispose();

            _backBuffer = new Bitmap(this.Width, this.Height);
            _backGraphics = Graphics.FromImage(_backBuffer);
            _backGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _backGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        }

        private void DrawToBuffer()
        {
            if (_backGraphics == null) return;
            _backGraphics.Clear(this.BackColor);

            int graphLeft = 70;
            int graphRight = this.ClientSize.Width - 50;
            int graphTop = 60;
            int graphBottom = this.ClientSize.Height - 70;
            int graphWidth = graphRight - graphLeft;
            int graphHeight = graphBottom - graphTop;

            // Рисуем рамку
            using (var pen = new Pen(Color.FromArgb(80, 80, 85), 1))
            {
                _backGraphics.DrawRectangle(pen, graphLeft, graphTop, graphWidth, graphHeight);
            }

            // Рисуем сетку
            if (_chkShowGrid.Checked)
            {
                DrawGrid(_backGraphics, graphLeft, graphRight, graphTop, graphBottom);
            }

            // Рисуем оси
            DrawAxes(_backGraphics, graphLeft, graphRight, graphTop, graphBottom);

            // Рисуем график
            if (_values.Count > 1)
            {
                DrawGraph(_backGraphics, graphLeft, graphRight, graphTop, graphBottom);
            }
            else
            {
                DrawNoDataMessage(_backGraphics);
            }

            // Статистика
            if (_values.Count > 0)
            {
                DrawStats(_backGraphics);
            }
        }

        private void DrawGrid(Graphics g, int left, int right, int top, int bottom)
        {
            using (var pen = new Pen(Color.FromArgb(60, 60, 65), 1))
            {
                // Вертикальные линии
                for (double x = Math.Ceiling(_viewXMin / 0.5) * 0.5; x <= _viewXMax; x += 0.5)
                {
                    if (x < _viewXMin) continue;
                    int screenX = left + (int)((x - _viewXMin) / (_viewXMax - _viewXMin) * (right - left));
                    if (screenX >= left && screenX <= right)
                    {
                        g.DrawLine(pen, screenX, top, screenX, bottom);

                        // Подпись
                        using (var font = new Font("Segoe UI", 8))
                        {
                            g.DrawString(x.ToString("F1"), font, Brushes.Gray, screenX - 15, bottom + 5);
                        }
                    }
                }

                // Горизонтальные линии
                for (double y = Math.Ceiling(_viewYMin / 0.5) * 0.5; y <= _viewYMax; y += 0.5)
                {
                    if (y < _viewYMin) continue;
                    int screenY = bottom - (int)((y - _viewYMin) / (_viewYMax - _viewYMin) * (bottom - top));
                    if (screenY >= top && screenY <= bottom)
                    {
                        g.DrawLine(pen, left, screenY, right, screenY);

                        using (var font = new Font("Segoe UI", 8))
                        {
                            g.DrawString(y.ToString("F1"), font, Brushes.Gray, left - 35, screenY - 7);
                        }
                    }
                }
            }
        }

        private void DrawAxes(Graphics g, int left, int right, int top, int bottom)
        {
            using (var pen = new Pen(Color.White, 2))
            {
                // Ось X (y=0)
                if (_viewYMin <= 0 && _viewYMax >= 0)
                {
                    int yZero = bottom - (int)((0 - _viewYMin) / (_viewYMax - _viewYMin) * (bottom - top));
                    g.DrawLine(pen, left, yZero, right, yZero);
                }

                // Ось Y (x=0)
                if (_viewXMin <= 0 && _viewXMax >= 0)
                {
                    int xZero = left + (int)((0 - _viewXMin) / (_viewXMax - _viewXMin) * (right - left));
                    g.DrawLine(pen, xZero, top, xZero, bottom);
                }
            }
        }

        private void DrawGraph(Graphics g, int left, int right, int top, int bottom)
        {
            int graphWidth = right - left;
            int graphHeight = bottom - top;

            var points = new List<PointF>();

            for (int i = 0; i < _values.Count; i++)
            {
                double x = _timeValues[i];
                double y = _values[i];

                if (x < _viewXMin || x > _viewXMax) continue;
                if (y < _viewYMin || y > _viewYMax) continue;

                float screenX = left + (float)((x - _viewXMin) / (_viewXMax - _viewXMin) * graphWidth);
                float screenY = bottom - (float)((y - _viewYMin) / (_viewYMax - _viewYMin) * graphHeight);

                points.Add(new PointF(screenX, screenY));
            }

            if (points.Count < 2) return;

            using (var pen = new Pen(_lineColor, 2))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                for (int i = 0; i < points.Count - 1; i++)
                {
                    g.DrawLine(pen, points[i], points[i + 1]);
                }
            }

            // Рисуем точки
            using (var brush = new SolidBrush(Color.Red))
            {
                foreach (var point in points)
                {
                    g.FillEllipse(brush, point.X - 2, point.Y - 2, 4, 4);
                }
            }
        }

        private void DrawNoDataMessage(Graphics g)
        {
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("Нет данных. Нажмите 'Пересчитать'",
                    new Font("Segoe UI", 14, FontStyle.Bold),
                    Brushes.Gray,
                    new Rectangle(0, 0, this.Width, this.Height),
                    sf);
            }
        }

        private void DrawStats(Graphics g)
        {
            string stats = $"Значений: {_values.Count} | " +
                          $"Мин: {_values.Min():F3} | " +
                          $"Макс: {_values.Max():F3} | " +
                          $"Среднее: {_values.Average():F3} | " +
                          $"Диапазон: [{_xMin:F2} .. {_xMax:F2}]";

            g.DrawString(stats, new Font("Segoe UI", 9),
                Brushes.LightGreen, 70, this.ClientSize.Height - 25);
        }

        private void ExportData()
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
                    writer.WriteLine("Time,Value");
                    for (int i = 0; i < _values.Count; i++)
                    {
                        writer.WriteLine($"{_timeValues[i]:F6},{_values[i]:F6}");
                    }
                }

                MessageBox.Show($"Данные сохранены!\n{saveDialog.FileName}", "Успех",
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
            CreateBackBuffer();
            _needsRedraw = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _backGraphics?.Dispose();
                _backBuffer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
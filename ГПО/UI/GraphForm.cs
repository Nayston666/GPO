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
        private RadioButton _rbProgressive;
        private RadioButton _rbInstant;

        // Новые элементы управления для функции и временного промежутка
        private ComboBox _cmbFunction;
        private NumericUpDown _nudStartTime;
        private NumericUpDown _nudEndTime;
        private NumericUpDown _nudStep;
        private Button _btnGenerate;
        private Button _btnAddPoint;
        private TextBox _txtManualValue;
        private Label _lblStatus;

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

        private int _autoScrollThreshold = 150;

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
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 32);
            this.DoubleBuffered = true;

            var topPanel = new Panel
            {
                Height = 130,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Первая строка элементов (Y = 5)
            int currentY = 5;

            _chkAutoScroll = new CheckBox
            {
                Text = "Авто-скролл",
                Location = new Point(10, currentY),
                Size = new Size(100, 25),
                ForeColor = Color.White,
                Checked = true,
                BackColor = Color.Transparent
            };
            _chkAutoScroll.CheckedChanged += (s, e) => _needsRedraw = true;

            _chkShowGrid = new CheckBox
            {
                Text = "Сетка",
                Location = new Point(120, currentY),
                Size = new Size(80, 25),
                ForeColor = Color.White,
                Checked = true,
                BackColor = Color.Transparent
            };
            _chkShowGrid.CheckedChanged += (s, e) => _needsRedraw = true;

            var chkAutoScale = new CheckBox
            {
                Text = "Авто-масштаб Y",
                Location = new Point(210, currentY),
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
                Location = new Point(330, currentY),
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
                Location = new Point(440, currentY),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClear.Click += (s, e) => { _values.Clear(); _needsRedraw = true; };

            var btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(530, currentY),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += SaveButton_Click;

            // Вторая строка - выбор режима отображения (Y = 35)
            currentY = 35;

            var lblDisplayMode = new Label
            {
                Text = "Режим:",
                Location = new Point(10, currentY),
                Size = new Size(45, 20),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            _rbProgressive = new RadioButton
            {
                Text = "Постепенный",
                Location = new Point(55, currentY),
                Size = new Size(90, 20),
                ForeColor = Color.White,
                Checked = true,
                BackColor = Color.Transparent
            };
            _rbProgressive.CheckedChanged += (s, e) => { _needsRedraw = true; };

            _rbInstant = new RadioButton
            {
                Text = "Моментальный",
                Location = new Point(150, currentY),
                Size = new Size(90, 20),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            _rbInstant.CheckedChanged += (s, e) =>
            {
                if (_rbInstant.Checked && _values.Count > _maxPoints)
                {
                    _values = _values.Skip(_values.Count - _maxPoints).ToList();
                }
                _needsRedraw = true;
            };

            // Третья строка - генерация функции (Y = 65)
            currentY = 65;

            var lblFunction = new Label
            {
                Text = "Функция:",
                Location = new Point(10, currentY + 3),
                Size = new Size(55, 20),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            _cmbFunction = new ComboBox
            {
                Location = new Point(70, currentY),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };
            _cmbFunction.Items.AddRange(new object[] {
                "Синус: sin(x)",
                "Косинус: cos(x)",
                "Квадрат: x^2",
                "Корень: sqrt(x)",
                "Экспонента: e^x",
                "Логарифм: ln(x+1)",
                "Линейная: 2*x + 5",
                "Кубическая: x^3",
                "Модуль: |x|",
                "Пилообразная: x % 10"
            });
            _cmbFunction.SelectedIndex = 0;

            var lblTimeRange = new Label
            {
                Text = "От:",
                Location = new Point(260, currentY + 3),
                Size = new Size(25, 20),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            _nudStartTime = new NumericUpDown
            {
                Location = new Point(290, currentY),
                Size = new Size(70, 25),
                DecimalPlaces = 2,
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };

            var lblTo = new Label
            {
                Text = "До:",
                Location = new Point(365, currentY + 3),
                Size = new Size(25, 20),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            _nudEndTime = new NumericUpDown
            {
                Location = new Point(395, currentY),
                Size = new Size(70, 25),
                DecimalPlaces = 2,
                Minimum = -100,
                Maximum = 100,
                Value = 10,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };

            var lblStep = new Label
            {
                Text = "Шаг:",
                Location = new Point(475, currentY + 3),
                Size = new Size(35, 20),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            _nudStep = new NumericUpDown
            {
                Location = new Point(515, currentY),
                Size = new Size(70, 25),
                DecimalPlaces = 3,
                Minimum = 0.001M,
                Maximum = 10,
                Value = 0.1M,
                Increment = 0.1M,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };

            _btnGenerate = new Button
            {
                Text = "Сгенерировать",
                Location = new Point(595, currentY),
                Size = new Size(110, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnGenerate.Click += BtnGenerate_Click;

            // Четвертая строка - ручной ввод (Y = 95)
            currentY = 95;

            var lblManual = new Label
            {
                Text = "Ручной ввод:",
                Location = new Point(10, currentY + 3),
                Size = new Size(75, 20),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            _txtManualValue = new TextBox
            {
                Location = new Point(90, currentY),
                Size = new Size(100, 25),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                Text = "0"
            };

            _btnAddPoint = new Button
            {
                Text = "Добавить точку",
                Location = new Point(200, currentY),
                Size = new Size(110, 25),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnAddPoint.Click += BtnAddPoint_Click;

            _lblStatus = new Label
            {
                Text = "Готов",
                Location = new Point(320, currentY + 3),
                Size = new Size(400, 20),
                ForeColor = Color.LightGreen,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8)
            };

            topPanel.Controls.AddRange(new Control[] {
                _chkAutoScroll, _chkShowGrid, chkAutoScale, _cmbLineColor, btnClear, btnSave,
                lblDisplayMode, _rbProgressive, _rbInstant,
                lblFunction, _cmbFunction, lblTimeRange, _nudStartTime, lblTo, _nudEndTime, lblStep, _nudStep, _btnGenerate,
                lblManual, _txtManualValue, _btnAddPoint, _lblStatus
            });

            this.Controls.Add(topPanel);
        }

        private double EvaluateFunction(double x)
        {
            int selectedIndex = _cmbFunction.SelectedIndex;

            switch (selectedIndex)
            {
                case 0: // sin(x)
                    return Math.Sin(x);
                case 1: // cos(x)
                    return Math.Cos(x);
                case 2: // x^2
                    return x * x;
                case 3: // sqrt(x)
                    return x >= 0 ? Math.Sqrt(x) : double.NaN;
                case 4: // e^x
                    return Math.Exp(x);
                case 5: // ln(x+1)
                    return x > -1 ? Math.Log(x + 1) : double.NaN;
                case 6: // 2x + 5
                    return 2 * x + 5;
                case 7: // x^3
                    return x * x * x;
                case 8: // |x|
                    return Math.Abs(x);
                case 9: // пилообразная
                    return x % 10;
                default:
                    return Math.Sin(x);
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                double startTime = (double)_nudStartTime.Value;
                double endTime = (double)_nudEndTime.Value;
                double step = (double)_nudStep.Value;

                if (step <= 0)
                {
                    MessageBox.Show("Шаг должен быть больше 0!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (startTime >= endTime)
                {
                    MessageBox.Show("Начальное время должно быть меньше конечного!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Очищаем текущие данные
                _values.Clear();

                int pointsGenerated = 0;
                int invalidPoints = 0;

                // Генерируем точки
                for (double x = startTime; x <= endTime; x += step)
                {
                    double y = EvaluateFunction(x);

                    if (!double.IsNaN(y) && !double.IsInfinity(y))
                    {
                        _values.Add(y);
                        pointsGenerated++;
                    }
                    else
                    {
                        invalidPoints++;
                    }
                }

                _lblStatus.Text = $"Сгенерировано {pointsGenerated} точек. " +
                                 (invalidPoints > 0 ? $"Пропущено {invalidPoints} недопустимых точек." : "");

                // Ограничиваем количество точек если нужно
                if (_rbInstant.Checked && _values.Count > _maxPoints)
                {
                    _values = _values.Skip(_values.Count - _maxPoints).ToList();
                    _lblStatus.Text += $" (ограничено до {_maxPoints} точек)";
                }

                _needsRedraw = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddPoint_Click(object sender, EventArgs e)
        {
            try
            {
                double value = double.Parse(_txtManualValue.Text);
                AddValue(value);
                _lblStatus.Text = $"Добавлена точка: {value:F4}";
                _txtManualValue.Clear();
                _txtManualValue.Text = "0";
            }
            catch (FormatException)
            {
                MessageBox.Show("Введите корректное число!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void AddValue(double value)
        {
            _values.Add(value);

            if (_rbInstant.Checked)
            {
                while (_values.Count > _maxPoints)
                    _values.RemoveAt(0);
            }
            else
            {
                while (_values.Count > _maxPoints * 2)
                    _values.RemoveRange(0, _values.Count - _maxPoints);
            }

            _needsRedraw = true;
        }

        private void CreateBackBuffer()
        {
            if (this.Width <= 0 || this.Height <= 130) return;

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
            DrawModeInfo(_backGraphics, topOffset);
        }

        private void DrawGrid(Graphics g, int topOffset, int bottomOffset, int leftOffset, int rightOffset)
        {
            using (var pen = new Pen(Color.FromArgb(60, 60, 65), 1))
            {
                int gridCount = 8;
                int graphWidth = this.Width - leftOffset - rightOffset;
                for (int i = 0; i <= gridCount; i++)
                {
                    int x = leftOffset + (i * graphWidth / gridCount);
                    g.DrawLine(pen, x, topOffset, x, this.Height - bottomOffset);
                }

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
                g.DrawLine(pen, leftOffset, this.Height - bottomOffset,
                    this.Width - rightOffset, this.Height - bottomOffset);
                g.DrawLine(pen, leftOffset, topOffset, leftOffset, this.Height - bottomOffset);
            }

            using (var arrowPen = new Pen(Color.White, 2))
            {
                g.DrawLine(arrowPen, this.Width - rightOffset - 5, this.Height - bottomOffset - 3,
                    this.Width - rightOffset, this.Height - bottomOffset);
                g.DrawLine(arrowPen, this.Width - rightOffset - 5, this.Height - bottomOffset + 3,
                    this.Width - rightOffset, this.Height - bottomOffset);
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
                string xLabel = _rbInstant.Checked ? "Номер точки (последние)" : "Время (точки)";
                SizeF xLabelSize = g.MeasureString(xLabel, labelFont);
                float xLabelX = (this.Width - xLabelSize.Width) / 2;
                float xLabelY = this.Height - bottomOffset + 15;
                g.DrawString(xLabel, labelFont, brush, xLabelX, xLabelY);

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

                if (_values.Count > 0)
                {
                    int verticalGridCount = 8;
                    int graphWidth = this.Width - leftOffset - rightOffset;
                    int startIndex = 0;
                    int endIndex = _values.Count - 1;

                    if (_rbProgressive.Checked && _chkAutoScroll.Checked && _values.Count > _autoScrollThreshold)
                    {
                        startIndex = _values.Count - _maxPoints;
                        if (startIndex < 0) startIndex = 0;
                    }
                    else if (_rbInstant.Checked)
                    {
                        startIndex = 0;
                    }

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

            if (_rbProgressive.Checked)
            {
                if (_chkAutoScroll.Checked && _values.Count > _autoScrollThreshold)
                {
                    startIndex = _values.Count - _maxPoints;
                    if (startIndex < 0) startIndex = 0;
                }
            }
            else
            {
                startIndex = 0;
            }

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
                g.DrawString("Ожидание данных...\n\nИспользуйте 'Сгенерировать' для построения графика функции\nили 'Добавить точку' для ручного ввода",
                    new Font("Segoe UI", 12, FontStyle.Bold),
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

        private void DrawModeInfo(Graphics g, int topOffset)
        {
            string modeText = _rbInstant.Checked ?
                "Режим: Моментальный (показывает последние " + _maxPoints + " точек)" :
                "Режим: Постепенный (авто-скролл после " + _autoScrollThreshold + " точек)";

            g.DrawString(modeText,
                new Font("Segoe UI", 8, FontStyle.Italic),
                Brushes.LightBlue, 10, topOffset - 10);
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
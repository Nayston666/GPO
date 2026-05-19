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
        private List<double> _timeValues = new List<double>();
        private string _sourceName;
        private static int _windowCounter = 0;

        private double _viewXMin, _viewXMax, _viewYMin, _viewYMax;

        private Point _dragStart;
        private bool _isPanning = false;

        private Button _btnExport, _btnResetView, _btnAutoScale;
        private Label _lblCoordinates;

        private NumericUpDown _numRangeStart;
        private NumericUpDown _numRangeEnd;

        private GroupBox _gbInterpolation;
        private NumericUpDown _numInterpX;
        private Button _btnGetValue;
        private Label _lblInterpResult;
        private NumericUpDown _numResampleStep;
        private Button _btnResample;
        private ComboBox _cmbInterpType;

        private Bitmap _backBuffer;
        private Graphics _backGraphics;
        private bool _needsRedraw = true;
        private Color _lineColor = Color.Cyan;

        private double _currentStep = 0.001;

        private List<PointF> _worldPoints = new List<PointF>();

        private static List<GraphForm> _openWindows = new List<GraphForm>();

        private const int LEFT_MARGIN = 70;
        private const int RIGHT_MARGIN = 50;
        private const int TOP_MARGIN = 60;
        private const int BOTTOM_MARGIN = 100;

        // Новые поля для временной оси
        private DateTime _startTime;
        private bool _useTimeAxis = false;
        private CheckBox _chkTimeAxis;

        // Статическая дата Unix эпохи для совместимости со старыми версиями .NET
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public List<double> TimeValues => _timeValues;
        public List<double> Values => _values;
        public string SourceName => _sourceName;

        public void SetDataDirectly(List<double> timeValues, List<double> values)
        {
            if (timeValues == null || values == null)
                return;

            _timeValues = new List<double>(timeValues);
            _values = new List<double>(values);

            // Определяем, нужно ли использовать временную ось
            DetermineTimeAxisUsage();

            UpdateWorldPoints();
            AutoScaleGraph();

            _needsRedraw = true;
            Invalidate();
            Refresh();
            Application.DoEvents();
        }

        private void DetermineTimeAxisUsage()
        {
            if (_timeValues.Count == 0)
            {
                _useTimeAxis = false;
                return;
            }

            // Проверяем флаг из чекбокса, если он существует
            if (_chkTimeAxis != null && _chkTimeAxis.Checked)
            {
                _useTimeAxis = true;
                SetupTimeAxis();
                return;
            }

            // Автоматическое определение: если значения времени большие (Unix timestamp)
            double firstValue = _timeValues[0];
            if (firstValue > 1000000000 && firstValue < 10000000000)
            {
                _useTimeAxis = true;
                SetupTimeAxis();
            }
            // Или если название источника содержит "time" или "date"
            else if (_sourceName.ToLower().Contains("time") || _sourceName.ToLower().Contains("date"))
            {
                _useTimeAxis = true;
                SetupTimeAxis();
            }
            else
            {
                _useTimeAxis = false;
            }
        }

        private void SetupTimeAxis()
        {
            if (_timeValues.Count > 0)
            {
                // Определяем начальную точку отсчета
                double minTime = _timeValues.Min();

                // Если это Unix timestamp в секундах
                if (minTime > 1000000000 && minTime < 10000000000)
                {
                    _startTime = UnixEpoch.AddSeconds(minTime);
                }
                // Если в миллисекундах
                else if (minTime > 1000000000000)
                {
                    _startTime = UnixEpoch.AddMilliseconds(minTime);
                }
                else
                {
                    // Относительное время - начинаем с текущего момента
                    _startTime = DateTime.Now;
                }
            }
        }

        private string FormatTimeValue(double timeValue)
        {
            if (!_useTimeAxis)
                return timeValue.ToString("F6");

            try
            {
                DateTime dateTime;

                // Определяем тип временной метки
                if (timeValue > 1000000000 && timeValue < 10000000000)
                {
                    // Unix timestamp в секундах
                    dateTime = UnixEpoch.AddSeconds(timeValue);
                }
                else if (timeValue > 1000000000000)
                {
                    // Unix timestamp в миллисекундах
                    dateTime = UnixEpoch.AddMilliseconds(timeValue);
                }
                else
                {
                    // Относительное время (секунды от начала)
                    dateTime = _startTime.AddSeconds(timeValue);
                }

                // Автоматически выбираем формат в зависимости от диапазона
                double range = _viewXMax - _viewXMin;

                if (range < 0.001) // Меньше 1 мс
                    return dateTime.ToString("HH:mm:ss.ffffff");
                else if (range < 0.01) // Меньше 10 мс
                    return dateTime.ToString("HH:mm:ss.fffff");
                else if (range < 0.1) // Меньше 100 мс
                    return dateTime.ToString("HH:mm:ss.ffff");
                else if (range < 1) // Меньше секунды
                    return dateTime.ToString("HH:mm:ss.fff");
                else if (range < 60) // Меньше минуты
                    return dateTime.ToString("HH:mm:ss");
                else if (range < 3600) // Меньше часа
                    return dateTime.ToString("HH:mm");
                else if (range < 86400) // Меньше дня
                    return dateTime.ToString("dd.MM HH:mm");
                else
                    return dateTime.ToString("dd.MM.yyyy");
            }
            catch
            {
                return timeValue.ToString("F6");
            }
        }

        private string FormatTimeValueForStatus(double timeValue)
        {
            if (!_useTimeAxis)
                return timeValue.ToString("F6");

            try
            {
                if (timeValue > 1000000000 && timeValue < 10000000000)
                    return UnixEpoch.AddSeconds(timeValue).ToString("HH:mm:ss.ffffff");
                else if (timeValue > 1000000000000)
                    return UnixEpoch.AddMilliseconds(timeValue).ToString("HH:mm:ss.ffffff");
                else
                    return _startTime.AddSeconds(timeValue).ToString("HH:mm:ss.ffffff");
            }
            catch
            {
                return timeValue.ToString("F6");
            }
        }

        public GraphForm(string source)
        {
            _sourceName = source;
            _windowCounter++;

            _viewXMin = 0;
            _viewXMax = 10;
            _viewYMin = -2;
            _viewYMax = 2;

            InitializeComponent();

            _openWindows.Add(this);
            this.FormClosed += (s, e) => _openWindows.Remove(this);

            var timer = new Timer { Interval = 16 };
            timer.Tick += (s, e) => { if (_needsRedraw) { DrawToBuffer(); Invalidate(); _needsRedraw = false; } };
            timer.Start();
            CreateBackBuffer();
        }

        public static List<GraphForm> GetOpenWindows() => new List<GraphForm>(_openWindows);

        private void AutoScaleGraph()
        {
            if (_worldPoints.Count == 0)
            {
                _viewXMin = 0;
                _viewXMax = 10;
                _viewYMin = -2;
                _viewYMax = 2;
                return;
            }

            double minX = _worldPoints.Min(p => p.X);
            double maxX = _worldPoints.Max(p => p.X);
            double minY = _worldPoints.Min(p => p.Y);
            double maxY = _worldPoints.Max(p => p.Y);

            if (Math.Abs(maxX - minX) < 0.0001)
            {
                minX = minX - 1;
                maxX = maxX + 1;
            }

            if (Math.Abs(maxY - minY) < 0.0001)
            {
                minY = minY - 1;
                maxY = maxY + 1;
            }

            // Для X: добавляем отступы в процентах
            double xMargin = Math.Max((maxX - minX) * 0.05, 0.01);
            _viewXMin = Math.Max(0, minX - xMargin);
            _viewXMax = maxX + xMargin;

            // Для Y: более умное масштабирование
            double yRange = maxY - minY;
            double yMargin = Math.Max(yRange * 0.1, Math.Abs(maxY) * 0.05);

            if (minY >= 0)
            {
                _viewYMin = Math.Max(0, minY - yMargin);
                _viewYMax = maxY + yMargin;
            }
            else if (maxY <= 0)
            {
                _viewYMin = minY - yMargin;
                _viewYMax = Math.Min(0, maxY + yMargin);
            }
            else
            {
                double maxAbsY = Math.Max(Math.Abs(minY), Math.Abs(maxY));
                yMargin = Math.Max(maxAbsY * 0.1, yRange * 0.1);
                _viewYMin = minY - yMargin;
                _viewYMax = maxY + yMargin;
            }

            // Обновляем NumericUpDown
            if (_numRangeStart != null && _numRangeEnd != null)
            {
                _numRangeStart.Value = (decimal)_viewXMin;
                _numRangeEnd.Value = (decimal)_viewXMax;
            }
        }

        private void UpdateYRange()
        {
            var visiblePoints = _worldPoints.Where(p => p.X >= _viewXMin && p.X <= _viewXMax).ToList();

            if (visiblePoints.Count > 0)
            {
                double minY = visiblePoints.Min(p => p.Y);
                double maxY = visiblePoints.Max(p => p.Y);
                double yRange = maxY - minY;
                double yMargin = Math.Max(yRange * 0.1, 0.1);

                if (minY >= 0)
                {
                    _viewYMin = Math.Max(0, minY - yMargin);
                    _viewYMax = maxY + yMargin;
                }
                else if (maxY <= 0)
                {
                    _viewYMin = minY - yMargin;
                    _viewYMax = Math.Min(0, maxY + yMargin);
                }
                else
                {
                    _viewYMin = minY - yMargin;
                    _viewYMax = maxY + yMargin;
                }
            }
        }

        public void SetRange(double start, double end)
        {
            if (start < 0) start = 0;
            if (end <= start) end = start + 1;

            _viewXMin = start;
            _viewXMax = end;

            if (_numRangeStart != null)
            {
                _numRangeStart.Value = (decimal)start;
                _numRangeEnd.Value = (decimal)end;
            }

            UpdateYRange();

            _needsRedraw = true;
            Invalidate();
            Refresh();
        }

        public (double start, double end) GetRange()
        {
            return (_viewXMin, _viewXMax);
        }

        private int GraphWidth => Width - LEFT_MARGIN - RIGHT_MARGIN;
        private int GraphHeight => Height - TOP_MARGIN - BOTTOM_MARGIN;

        private int WorldToScreenX(double x)
        {
            if (Math.Abs(_viewXMax - _viewXMin) < 0.0001) return LEFT_MARGIN + GraphWidth / 2;
            return LEFT_MARGIN + (int)((x - _viewXMin) / (_viewXMax - _viewXMin) * GraphWidth);
        }

        private int WorldToScreenY(double y)
        {
            if (Math.Abs(_viewYMax - _viewYMin) < 0.0001) return TOP_MARGIN + GraphHeight / 2;
            return TOP_MARGIN + GraphHeight - (int)((y - _viewYMin) / (_viewYMax - _viewYMin) * GraphHeight);
        }

        private double ScreenToWorldX(int screenX)
        {
            double t = (screenX - LEFT_MARGIN) / (double)GraphWidth;
            double x = _viewXMin + t * (_viewXMax - _viewXMin);
            return Math.Max(0, x);
        }

        private double ScreenToWorldY(int screenY)
        {
            double t = (screenY - TOP_MARGIN) / (double)GraphHeight;
            return _viewYMax - t * (_viewYMax - _viewYMin);
        }

        private bool IsMouseInGraphArea(Point mouse)
        {
            return mouse.X >= LEFT_MARGIN && mouse.X <= LEFT_MARGIN + GraphWidth &&
                   mouse.Y >= TOP_MARGIN && mouse.Y <= TOP_MARGIN + GraphHeight;
        }

        private double InterpolateLinear(double x1, double y1, double x2, double y2, double x)
        {
            if (Math.Abs(x2 - x1) < 1e-10) return y1;
            double t = (x - x1) / (x2 - x1);
            return y1 + t * (y2 - y1);
        }

        public double GetValueAt(double x)
        {
            if (_timeValues == null || _timeValues.Count == 0 || _values == null || _values.Count == 0)
                throw new InvalidOperationException("Нет данных для интерполяции");

            if (_timeValues.Count == 1)
                return _values[0];

            if (x <= _timeValues[0])
                return _values[0];

            if (x >= _timeValues[_timeValues.Count - 1])
                return _values[_timeValues.Count - 1];

            for (int i = 0; i < _timeValues.Count - 1; i++)
            {
                if (x >= _timeValues[i] && x <= _timeValues[i + 1])
                {
                    return InterpolateLinear(
                        _timeValues[i], _values[i],
                        _timeValues[i + 1], _values[i + 1],
                        x);
                }
            }

            return 0;
        }

        public void ResampleWithInterpolation(double newStep)
        {
            if (_timeValues.Count < 2) return;

            double startX = _timeValues[0];
            double endX = _timeValues[_timeValues.Count - 1];

            var newTimeValues = new List<double>();
            var newValues = new List<double>();

            for (double x = startX; x <= endX + newStep / 2; x += newStep)
            {
                newTimeValues.Add(x);
                newValues.Add(GetValueAt(x));
            }

            _timeValues = newTimeValues;
            _values = newValues;
            _currentStep = newStep;

            UpdateWorldPoints();
            AutoScaleGraph();
            _needsRedraw = true;
            Invalidate();
        }

        private void ResetView()
        {
            AutoScaleGraph();
            _needsRedraw = true;
            Invalidate();
        }

        private void InitializeComponent()
        {
            Text = $"График - {_sourceName}";
            Size = new Size(1200, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(30, 30, 32);
            DoubleBuffered = true;

            var topPanel = new Panel { Height = 40, Dock = DockStyle.Top, BackColor = Color.FromArgb(45, 45, 48) };
            topPanel.Padding = new Padding(5);

            int currentX = 10;

            var lblColor = new Label { Text = "Цвет:", Location = new Point(currentX, 10), Size = new Size(40, 20), ForeColor = Color.White };
            topPanel.Controls.Add(lblColor);
            currentX += 40;

            var cmbLineColor = new ComboBox { Location = new Point(currentX, 5), Size = new Size(90, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White };
            cmbLineColor.Items.AddRange(new[] { "Голубой", "Зеленый", "Красный", "Желтый", "Белый" });
            cmbLineColor.SelectedIndex = 0;
            cmbLineColor.SelectedIndexChanged += (s, e) =>
            {
                int idx = cmbLineColor.SelectedIndex;
                if (idx == 0) _lineColor = Color.Cyan;
                else if (idx == 1) _lineColor = Color.LightGreen;
                else if (idx == 2) _lineColor = Color.Orange;
                else if (idx == 3) _lineColor = Color.Yellow;
                else _lineColor = Color.White;
                _needsRedraw = true;
                Invalidate();
            };
            topPanel.Controls.Add(cmbLineColor);
            currentX += 100;

            // Чекбокс для временной оси
            _chkTimeAxis = new CheckBox
            {
                Text = "Временная ось X",
                Location = new Point(currentX, 8),
                Size = new Size(120, 25),
                ForeColor = Color.White,
                Checked = false
            };
            _chkTimeAxis.CheckedChanged += (s, e) =>
            {
                _useTimeAxis = _chkTimeAxis.Checked;
                if (_useTimeAxis) SetupTimeAxis();
                _needsRedraw = true;
                Invalidate();
            };
            topPanel.Controls.Add(_chkTimeAxis);
            currentX += 130;

            _btnResetView = new Button
            {
                Text = "Сброс вида",
                Location = new Point(currentX, 5),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnResetView.Click += (s, e) => ResetView();
            topPanel.Controls.Add(_btnResetView);
            currentX += 90;

            _btnAutoScale = new Button
            {
                Text = "Автомасштаб Y",
                Location = new Point(currentX, 5),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnAutoScale.Click += (s, e) => { UpdateYRange(); _needsRedraw = true; Invalidate(); };
            topPanel.Controls.Add(_btnAutoScale);
            currentX += 110;

            _btnExport = new Button
            {
                Text = "💾 Экспорт",
                Location = new Point(currentX, 5),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnExport.Click += (s, e) => ExportData();
            topPanel.Controls.Add(_btnExport);
            currentX += 90;

            _lblCoordinates = new Label
            {
                Text = "x: --  y: --",
                Location = new Point(currentX, 10),
                Size = new Size(550, 25),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9)
            };
            topPanel.Controls.Add(_lblCoordinates);

            Controls.Add(topPanel);

            var rangePanel = new Panel { Height = 40, Dock = DockStyle.Top, BackColor = Color.FromArgb(35, 35, 38) };
            rangePanel.Padding = new Padding(10, 5, 10, 5);

            var lblRangeTitle = new Label
            {
                Text = "X от:",
                Location = new Point(15, 10),
                Size = new Size(35, 22),
                ForeColor = Color.White
            };

            _numRangeStart = new NumericUpDown
            {
                Location = new Point(50, 7),
                Size = new Size(100, 25),
                Minimum = 0,
                Maximum = 100000,
                DecimalPlaces = 6,
                Increment = 0.001M,
                Value = 0,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };

            var lblTo = new Label
            {
                Text = "до:",
                Location = new Point(155, 10),
                Size = new Size(25, 22),
                ForeColor = Color.White
            };

            _numRangeEnd = new NumericUpDown
            {
                Location = new Point(180, 7),
                Size = new Size(100, 25),
                Minimum = 0.001M,
                Maximum = 100000,
                DecimalPlaces = 6,
                Increment = 0.001M,
                Value = 10,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };

            var btnApplyRange = new Button
            {
                Text = "Применить",
                Location = new Point(290, 5),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnApplyRange.Click += (s, e) =>
            {
                double newStart = (double)_numRangeStart.Value;
                double newEnd = (double)_numRangeEnd.Value;
                if (newEnd > newStart)
                {
                    SetRange(newStart, newEnd);
                }
            };

            rangePanel.Controls.AddRange(new Control[] { lblRangeTitle, _numRangeStart, lblTo, _numRangeEnd, btnApplyRange });
            Controls.Add(rangePanel);

            var interpPanel = new Panel { Height = 80, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(45, 45, 48) };
            interpPanel.Padding = new Padding(10, 5, 10, 5);

            _gbInterpolation = new GroupBox
            {
                Text = "Линейная интерполяция",
                Location = new Point(10, 0),
                Size = new Size(Width - 20, 75),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            var lblInterpType = new Label
            {
                Text = "Тип:",
                Location = new Point(15, 28),
                Size = new Size(35, 23),
                ForeColor = Color.White
            };
            _cmbInterpType = new ComboBox
            {
                Location = new Point(55, 25),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };
            _cmbInterpType.Items.AddRange(new[] { "Линейная (без экстраполяции)", "С экстраполяцией" });
            _cmbInterpType.SelectedIndex = 0;

            var lblX = new Label
            {
                Text = "X =",
                Location = new Point(220, 28),
                Size = new Size(25, 23),
                ForeColor = Color.White
            };
            _numInterpX = new NumericUpDown
            {
                Location = new Point(250, 25),
                Size = new Size(120, 25),
                Minimum = 0,
                Maximum = 100000,
                DecimalPlaces = 6,
                Increment = 0.001M,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };

            _btnGetValue = new Button
            {
                Text = "Получить Y",
                Location = new Point(380, 23),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnGetValue.Click += (s, e) => ShowInterpolatedValue();

            _lblInterpResult = new Label
            {
                Text = "Y = ---",
                Location = new Point(490, 28),
                Size = new Size(300, 25),
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var lblResample = new Label
            {
                Text = "Новый шаг:",
                Location = new Point(800, 28),
                Size = new Size(70, 23),
                ForeColor = Color.White
            };
            _numResampleStep = new NumericUpDown
            {
                Location = new Point(875, 25),
                Size = new Size(100, 25),
                Minimum = 0.000001M,
                Maximum = 10,
                DecimalPlaces = 6,
                Increment = 0.001M,
                Value = 0.01M,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };
            _btnResample = new Button
            {
                Text = "Ресемплинг",
                Location = new Point(985, 23),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(60, 120, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnResample.Click += (s, e) =>
            {
                if (_timeValues.Count > 0)
                {
                    ResampleWithInterpolation((double)_numResampleStep.Value);
                    MessageBox.Show($"Данные пересэмплированы с шагом {_numResampleStep.Value}\n" +
                                    $"Новое количество точек: {_values.Count:N0}", "Ресемплинг", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            _gbInterpolation.Controls.AddRange(new Control[] {
                lblInterpType, _cmbInterpType,
                lblX, _numInterpX,
                _btnGetValue, _lblInterpResult,
                lblResample, _numResampleStep, _btnResample
            });

            interpPanel.Controls.Add(_gbInterpolation);
            Controls.Add(interpPanel);

            this.MouseWheel += OnMouseWheel;
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            Zoom(e.Delta > 0 ? 0.9 : 1.1, e.Location);
        }

        private void ShowInterpolatedValue()
        {
            if (_timeValues.Count == 0)
            {
                _lblInterpResult.Text = "Нет данных!";
                return;
            }

            try
            {
                double x = (double)_numInterpX.Value;
                double y;
                bool useExtrapolation = _cmbInterpType.SelectedIndex == 1;

                if (useExtrapolation)
                    y = GetValueAt(x);
                else if (x >= _timeValues[0] && x <= _timeValues[_timeValues.Count - 1])
                    y = GetValueAt(x);
                else
                {
                    _lblInterpResult.Text = "X вне диапазона!";
                    return;
                }

                string xDisplay = _useTimeAxis ? FormatTimeValueForStatus(x) : x.ToString("F6");
                _lblInterpResult.Text = $"Y = {y:F6} (X = {xDisplay})";
            }
            catch (Exception ex)
            {
                _lblInterpResult.Text = $"Ошибка: {ex.Message}";
            }
        }

        // Левая кнопка мыши для панорамирования
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (IsMouseInGraphArea(e.Location) && e.Button == MouseButtons.Left)
            {
                _isPanning = true;
                _dragStart = e.Location;
                this.Cursor = Cursors.SizeAll;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            double x = ScreenToWorldX(e.X);
            double y = ScreenToWorldY(e.Y);

            if (_timeValues.Count > 0)
            {
                try
                {
                    double interpY = GetValueAt(x);
                    string xDisplay = _useTimeAxis ? FormatTimeValueForStatus(x) : x.ToString("F6");
                    _lblCoordinates.Text = $"X: {xDisplay} | Y(интерп): {interpY:F6} | Диапазон X: [{(_useTimeAxis ? FormatTimeValueForStatus(_viewXMin) : _viewXMin.ToString("F6"))}:{(_useTimeAxis ? FormatTimeValueForStatus(_viewXMax) : _viewXMax.ToString("F6"))}]";
                }
                catch
                {
                    _lblCoordinates.Text = $"X: {(_useTimeAxis ? FormatTimeValueForStatus(x) : x.ToString("F6"))} | Y(экран): {y:F6}";
                }
            }
            else
            {
                _lblCoordinates.Text = $"X: {(_useTimeAxis ? FormatTimeValueForStatus(x) : x.ToString("F6"))} | Y: {y:F6}";
            }

            if (_isPanning)
            {
                Pan(e.Location);
            }

            _needsRedraw = true;
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _isPanning = false;
            this.Cursor = Cursors.Default;
        }

        private void UpdateWorldPoints()
        {
            _worldPoints.Clear();
            if (_timeValues.Count == 0 || _values.Count == 0) return;

            int count = Math.Min(_timeValues.Count, _values.Count);
            for (int i = 0; i < count; i++)
            {
                if (_timeValues[i] >= 0 && !double.IsNaN(_values[i]) && !double.IsInfinity(_values[i]))
                    _worldPoints.Add(new PointF((float)_timeValues[i], (float)_values[i]));
            }
        }

        private void Zoom(double factor, Point mouse)
        {
            double xMouse = ScreenToWorldX(mouse.X);
            double yMouse = ScreenToWorldY(mouse.Y);

            double newWidth = (_viewXMax - _viewXMin) * factor;
            double newHeight = (_viewYMax - _viewYMin) * factor;

            double tX = (xMouse - _viewXMin) / (_viewXMax - _viewXMin);
            double tY = (yMouse - _viewYMin) / (_viewYMax - _viewYMin);

            double newMinX = xMouse - newWidth * tX;
            if (newMinX >= 0)
            {
                _viewXMin = newMinX;
                _viewXMax = _viewXMin + newWidth;
            }

            _viewYMin = yMouse - newHeight * tY;
            _viewYMax = _viewYMin + newHeight;

            _needsRedraw = true;
            Invalidate();
        }

        private void Pan(Point mouse)
        {
            double dx = ScreenToWorldX(_dragStart.X) - ScreenToWorldX(mouse.X);
            double dy = ScreenToWorldY(_dragStart.Y) - ScreenToWorldY(mouse.Y);

            double newMin = _viewXMin + dx;
            if (newMin >= 0)
            {
                _viewXMin = newMin;
                _viewXMax += dx;
            }

            _viewYMin += dy;
            _viewYMax += dy;
            _dragStart = mouse;
            _needsRedraw = true;
        }

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

            using (var pen = new Pen(Color.FromArgb(80, 80, 85), 1))
                _backGraphics.DrawRectangle(pen, LEFT_MARGIN, TOP_MARGIN, GraphWidth, GraphHeight);

            DrawGrid();
            DrawAxes();
            DrawGraphStatic();
            DrawZeroLine();

            if (_values.Count == 0)
                _backGraphics.DrawString("Нет данных", new Font("Segoe UI", 14), Brushes.Gray, LEFT_MARGIN + GraphWidth / 2 - 50, TOP_MARGIN + GraphHeight / 2);

            DrawStats();
        }

        private void DrawZeroLine()
        {
            if (_viewYMin <= 0 && _viewYMax >= 0)
            {
                int zeroY = WorldToScreenY(0);
                using (var pen = new Pen(Color.FromArgb(200, 255, 100, 100), 1.5f))
                {
                    pen.DashStyle = DashStyle.Dash;
                    _backGraphics.DrawLine(pen, LEFT_MARGIN, zeroY, LEFT_MARGIN + GraphWidth, zeroY);
                }
            }
        }

        private void DrawGraphStatic()
        {
            var visiblePoints = _worldPoints.Where(p => p.X >= _viewXMin && p.X <= _viewXMax).ToList();

            if (visiblePoints.Count < 2) return;

            List<PointF> screenPoints = new List<PointF>();

            foreach (var worldPoint in visiblePoints)
            {
                double x = worldPoint.X;
                double y = worldPoint.Y;

                if (y < _viewYMin || y > _viewYMax)
                    continue;

                float sx = WorldToScreenX(x);
                float sy = WorldToScreenY(y);
                screenPoints.Add(new PointF(sx, sy));
            }

            if (screenPoints.Count < 2) return;

            using (var pen = new Pen(_lineColor, 2))
            {
                for (int i = 0; i < screenPoints.Count - 1; i++)
                {
                    _backGraphics.DrawLine(pen, screenPoints[i], screenPoints[i + 1]);
                }
            }
        }

        private void DrawGrid()
        {
            using (var pen = new Pen(Color.FromArgb(60, 60, 65), 1))
            {
                // Автоматическое определение количества вертикальных линий в зависимости от ширины
                double xRange = _viewXMax - _viewXMin;
                int numVerticalLines;

                if (xRange < 0.001)
                    numVerticalLines = 5;
                else if (xRange < 0.01)
                    numVerticalLines = 6;
                else if (xRange < 0.1)
                    numVerticalLines = 8;
                else if (xRange < 1)
                    numVerticalLines = 10;
                else
                    numVerticalLines = Math.Min(12, Math.Max(6, GraphWidth / 80));

                for (int i = 0; i <= numVerticalLines; i++)
                {
                    double x = _viewXMin + xRange * i / numVerticalLines;
                    int sx = WorldToScreenX(x);

                    if (sx >= LEFT_MARGIN && sx <= LEFT_MARGIN + GraphWidth)
                    {
                        // Рисуем вертикальную линию сетки
                        _backGraphics.DrawLine(pen, sx, TOP_MARGIN, sx, TOP_MARGIN + GraphHeight);

                        // Форматируем метку
                        string label = FormatTimeValue(x);
                        SizeF labelSize = _backGraphics.MeasureString(label, new Font("Segoe UI", 8));

                        // Определяем, нужно ли поворачивать метки
                        float availableSpace = GraphWidth / (float)numVerticalLines;
                        bool needRotate = label.Length > 8 && availableSpace < 80;

                        if (needRotate)
                        {
                            _backGraphics.TranslateTransform(sx - labelSize.Width / 2, TOP_MARGIN + GraphHeight + 2);
                            _backGraphics.RotateTransform(-45);
                            _backGraphics.DrawString(label, new Font("Segoe UI", 7), Brushes.Gray, 0, 0);
                            _backGraphics.ResetTransform();
                        }
                        else
                        {
                            _backGraphics.DrawString(label, new Font("Segoe UI", 8), Brushes.Gray,
                                sx - labelSize.Width / 2, TOP_MARGIN + GraphHeight + 5);
                        }
                    }
                }

                // Горизонтальные линии сетки
                int numHorizontalLines = 8;
                for (int i = 0; i <= numHorizontalLines; i++)
                {
                    double y = _viewYMin + (_viewYMax - _viewYMin) * i / numHorizontalLines;
                    int sy = WorldToScreenY(y);

                    if (sy >= TOP_MARGIN && sy <= TOP_MARGIN + GraphHeight)
                    {
                        _backGraphics.DrawLine(pen, LEFT_MARGIN, sy, LEFT_MARGIN + GraphWidth, sy);

                        string label = y.ToString("F6");
                        SizeF labelSize = _backGraphics.MeasureString(label, new Font("Segoe UI", 8));
                        _backGraphics.DrawString(label, new Font("Segoe UI", 8), Brushes.Gray,
                            LEFT_MARGIN - labelSize.Width - 5, sy - labelSize.Height / 2);
                    }
                }
            }

            // Дополнительная координатная сетка - более частые линии для маленьких диапазонов
            double xRangeExtra = _viewXMax - _viewXMin;
            if (xRangeExtra < 0.1 && xRangeExtra > 0.001)
            {
                using (var penLight = new Pen(Color.FromArgb(40, 60, 60, 65), 1))
                {
                    int extraLines = (xRangeExtra < 0.01) ? 20 : 10;
                    for (int i = 0; i <= extraLines; i++)
                    {
                        double x = _viewXMin + xRangeExtra * i / extraLines;
                        int sx = WorldToScreenX(x);
                        if (sx >= LEFT_MARGIN && sx <= LEFT_MARGIN + GraphWidth)
                        {
                            _backGraphics.DrawLine(penLight, sx, TOP_MARGIN, sx, TOP_MARGIN + GraphHeight);
                        }
                    }
                }
            }
        }

        private void DrawAxes()
        {
            using (var pen = new Pen(Color.White, 2))
            {
                // Ось X (внизу или на нуле)
                int yAxisPos = WorldToScreenY(0);
                if (yAxisPos < TOP_MARGIN) yAxisPos = TOP_MARGIN + GraphHeight;
                if (yAxisPos > TOP_MARGIN + GraphHeight) yAxisPos = TOP_MARGIN + GraphHeight;

                _backGraphics.DrawLine(pen, LEFT_MARGIN, yAxisPos, LEFT_MARGIN + GraphWidth, yAxisPos);
                DrawArrowHead(_backGraphics, LEFT_MARGIN + GraphWidth, yAxisPos, 0);

                string xAxisLabel = _useTimeAxis ? "Время" : "t";
                _backGraphics.DrawString(xAxisLabel, new Font("Segoe UI", 11, FontStyle.Italic), Brushes.White,
                    LEFT_MARGIN + GraphWidth + 5, yAxisPos - 8);

                // Ось Y
                _backGraphics.DrawLine(pen, LEFT_MARGIN, TOP_MARGIN, LEFT_MARGIN, TOP_MARGIN + GraphHeight);
                DrawArrowHead(_backGraphics, LEFT_MARGIN, TOP_MARGIN, 270);
                _backGraphics.DrawString("y", new Font("Segoe UI", 11, FontStyle.Italic), Brushes.White,
                    LEFT_MARGIN + 5, TOP_MARGIN - 15);
            }
        }

        private void DrawArrowHead(Graphics g, int x, int y, int angle)
        {
            int arrowSize = 8;
            Point[] arrowPoints;

            switch (angle)
            {
                case 0:
                    arrowPoints = new Point[]
                    {
                        new Point(x, y),
                        new Point(x - arrowSize, y - arrowSize / 2),
                        new Point(x - arrowSize, y + arrowSize / 2)
                    };
                    break;
                case 270:
                default:
                    arrowPoints = new Point[]
                    {
                        new Point(x, y),
                        new Point(x - arrowSize / 2, y + arrowSize),
                        new Point(x + arrowSize / 2, y + arrowSize)
                    };
                    break;
            }

            g.FillPolygon(Brushes.White, arrowPoints);
        }

        private void DrawStats()
        {
            string xRangeDisplay;
            if (_useTimeAxis)
            {
                xRangeDisplay = $"[{FormatTimeValueForStatus(_viewXMin)}:{FormatTimeValueForStatus(_viewXMax)}]";
            }
            else
            {
                xRangeDisplay = $"[{_viewXMin:F6}:{_viewXMax:F6}]";
            }

            string stats = $"Точек: {_values.Count:N0} | Шаг: {_currentStep:F6} | " +
                           $"Мин Y: {(_values.Count > 0 ? _values.Min() : 0):F6} | " +
                           $"Макс Y: {(_values.Count > 0 ? _values.Max() : 0):F6} | " +
                           $"X: {xRangeDisplay} | Y: [{_viewYMin:F6}:{_viewYMax:F6}] | (Зажмите ЛКМ для перемещения)";

            _backGraphics.DrawString(stats, new Font("Segoe UI", 8), Brushes.LightGreen, LEFT_MARGIN, Height - BOTTOM_MARGIN + 10);
        }

        private void ExportData()
        {
            if (_values.Count == 0) return;

            string safeDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var dlg = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                FileName = $"graph_{_sourceName}_{safeDate}.csv"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                using (var w = new System.IO.StreamWriter(dlg.FileName))
                {
                    if (_useTimeAxis)
                        w.WriteLine("Time (Formatted),Time (Raw),Value");
                    else
                        w.WriteLine("Time,Value");

                    for (int i = 0; i < _values.Count; i++)
                    {
                        if (_useTimeAxis)
                        {
                            string formattedTime = FormatTimeValueForStatus(_timeValues[i]);
                            w.WriteLine($"{formattedTime},{_timeValues[i]:F6},{_values[i]:F6}");
                        }
                        else
                        {
                            w.WriteLine($"{_timeValues[i]:F6},{_values[i]:F6}");
                        }
                    }
                    MessageBox.Show($"Сохранено {_values.Count} точек!", "Успех");
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e) { if (_backBuffer != null) e.Graphics.DrawImage(_backBuffer, 0, 0); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); CreateBackBuffer(); _needsRedraw = true; }
        protected override void Dispose(bool disposing) { if (disposing) { _backGraphics?.Dispose(); _backBuffer?.Dispose(); } base.Dispose(disposing); }
    }
}
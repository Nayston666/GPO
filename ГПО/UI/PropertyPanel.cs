using MathApp.Helpers;
using MathApp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MathApp.UI
{
    public class PropertyPanel : UserControl
    {
        private MathTool _selectedTool;
        private FlowLayoutPanel _mainPanel;

        // ВСЕ ПОЛЯ - ТЕПЕРЬ TextBox (вместо NumericUpDown)
        private TextBox _txtValueA;
        private TextBox _txtValueB;
        private TextBox _txtChartPoints;
        private TextBox _txtFrequency;
        private TextBox _txtAmplitude;
        private TextBox _txtPhase;
        private TextBox _txtStepDuration;
        private TextBox _txtRiseTime;
        private TextBox _txtFallTime;
        private TextBox _txtGain;
        private TextBox _txtEffectiveArea;
        private TextBox _txtDistance;
        private TextBox _txtAttenuation;
        private TextBox _txtRadarCrossSection;
        private TextBox _txtTimeConstant;
        private TextBox _txtBitResolution;
        private TextBox _txtSamplingRate;
        private TextBox _txtReferenceVoltage;
        private TextBox _txtStepSize;

        private ComboBox _cmbFileMode;
        private Button _btnSelectInputFile;
        private Button _btnSelectOutputFile;
        private Button _btnExportResult;
        private Label _lblInputFile;
        private Label _lblOutputFile;
        private Label _lblPointsCount;

        public event EventHandler ParametersChanged;

        public PropertyPanel()
        {
            InitializeComponent();
            this.Visible = false;
        }

        private void InitializeComponent()
        {
            this.Size = new Size(340, 700);
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.AutoScroll = true;

            _mainPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(5, 5, 5, 5),
                BackColor = Color.FromArgb(248, 249, 250)
            };
            this.Controls.Add(_mainPanel);

            // Создаём все TextBox с поддержкой инженерного ввода
            _txtValueA = CreateEngineeringTextBox("0");
            _txtValueB = CreateEngineeringTextBox("0");
            _txtChartPoints = CreateNumericTextBox("200", 10, 5000);
            _txtFrequency = CreateEngineeringTextBox("1", "Hz");
            _txtAmplitude = CreateEngineeringTextBox("1", "V");
            _txtPhase = CreateNumericTextBox("0", 0, 360);
            _txtStepDuration = CreateEngineeringTextBox("0.001", "s");
            _txtRiseTime = CreateEngineeringTextBox("0.1", "s");
            _txtFallTime = CreateEngineeringTextBox("0.1", "s");
            _txtGain = CreateEngineeringTextBox("10");
            _txtEffectiveArea = CreateEngineeringTextBox("0.1", "m²");
            _txtDistance = CreateEngineeringTextBox("1000", "m");
            _txtAttenuation = CreateEngineeringTextBox("0.01", "dB/m");
            _txtRadarCrossSection = CreateEngineeringTextBox("1", "m²");
            _txtTimeConstant = CreateEngineeringTextBox("1", "s");
            _txtBitResolution = CreateNumericTextBox("12", 1, 24);
            _txtSamplingRate = CreateEngineeringTextBox("10000", "Hz");
            _txtReferenceVoltage = CreateEngineeringTextBox("5", "V");
            _txtStepSize = CreateEngineeringTextBox("0.01", "s");

            _cmbFileMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.White, ForeColor = Color.Black };
            _cmbFileMode.Items.AddRange(new object[] { "Режим чтения", "Режим записи" });
            _cmbFileMode.SelectedIndex = 0;
            _btnSelectInputFile = new Button { Text = "Выбрать файл", BackColor = Color.FromArgb(240, 240, 240), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
            _btnSelectOutputFile = new Button { Text = "Выбрать файл", BackColor = Color.FromArgb(240, 240, 240), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
            _btnExportResult = new Button { Text = "Сохранить", BackColor = Color.FromArgb(240, 240, 240), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
            _lblInputFile = new Label { Text = "Файл: не выбран", ForeColor = Color.Black, Font = new Font("Segoe UI", 8) };
            _lblOutputFile = new Label { Text = "Файл: не выбран", ForeColor = Color.Black, Font = new Font("Segoe UI", 8) };
            _lblPointsCount = new Label { Text = "Данных: 0", ForeColor = Color.Black, Font = new Font("Segoe UI", 8) };

            _cmbFileMode.SelectedIndexChanged += (s, e) => UpdateFileModeVisibility();
            _btnSelectInputFile.Click += (s, e) => SelectInputFile();
            _btnSelectOutputFile.Click += (s, e) => SelectOutputFile();
            _btnExportResult.Click += (s, e) => ExportResult();

            // Подписка на события
            _txtValueA.Leave += (s, e) => ApplyValueA();
            _txtValueB.Leave += (s, e) => ApplyValueB();
            _txtChartPoints.Leave += (s, e) => ApplyChartPoints();
            _txtFrequency.Leave += (s, e) => ApplyFrequency();
            _txtStepDuration.Leave += (s, e) => ApplyStepDuration();
            _txtAmplitude.Leave += (s, e) => ApplyAmplitude();
            _txtPhase.Leave += (s, e) => ApplyPhase();
            _txtRiseTime.Leave += (s, e) => ApplyRiseTime();
            _txtFallTime.Leave += (s, e) => ApplyFallTime();
            _txtGain.Leave += (s, e) => ApplyGain();
            _txtEffectiveArea.Leave += (s, e) => ApplyEffectiveArea();
            _txtDistance.Leave += (s, e) => ApplyDistance();
            _txtAttenuation.Leave += (s, e) => ApplyAttenuation();
            _txtRadarCrossSection.Leave += (s, e) => ApplyRadarCrossSection();
            _txtTimeConstant.Leave += (s, e) => ApplyTimeConstant();
            _txtBitResolution.Leave += (s, e) => ApplyBitResolution();
            _txtSamplingRate.Leave += (s, e) => ApplySamplingRate();
            _txtReferenceVoltage.Leave += (s, e) => ApplyReferenceVoltage();
            _txtStepSize.Leave += (s, e) => ApplyStepSize();
        }

        // ============ СОЗДАНИЕ КОНТРОЛОВ ============

        private TextBox CreateEngineeringTextBox(string defaultValue, string unit = "")
        {
            var tb = new TextBox
            {
                Text = defaultValue,
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                Width = 100,
                Tag = unit
            };
            return tb;
        }

        private TextBox CreateNumericTextBox(string defaultValue, double min, double max)
        {
            var tb = new TextBox
            {
                Text = defaultValue,
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                Width = 100,
                Tag = new { min, max }
            };
            return tb;
        }

        // ============ ИНЖЕНЕРНЫЙ ПАРСИНГ ============

        private double ParseEngineering(string text, double defaultValue = 0)
        {
            if (EngineeringParser.TryParse(text, out double result))
                return result;
            return defaultValue;
        }

        private string FormatEngineering(double value, string unit = "")
        {
            return EngineeringParser.ToEngineeringString(value, unit);
        }

        private int ParseInt(string text, int defaultValue = 0, int min = 1, int max = 10000)
        {
            if (int.TryParse(text, out int result))
                return Math.Max(min, Math.Min(max, result));
            return defaultValue;
        }

        // ============ ПРИМЕНЕНИЕ ЗНАЧЕНИЙ ============

        private void ApplyValueA()
        {
            if (_selectedTool != null)
            {
                double v = ParseEngineering(_txtValueA.Text, _selectedTool.CustomValueA);
                _selectedTool.CustomValueA = v;
                _txtValueA.Text = FormatEngineering(v);
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyValueB()
        {
            if (_selectedTool != null)
            {
                double v = ParseEngineering(_txtValueB.Text, _selectedTool.CustomValueB);
                _selectedTool.CustomValueB = v;
                _txtValueB.Text = FormatEngineering(v);
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyChartPoints()
        {
            if (_selectedTool != null)
            {
                _selectedTool.MaxHistorySize = ParseInt(_txtChartPoints.Text, 200, 10, 5000);
                _txtChartPoints.Text = _selectedTool.MaxHistorySize.ToString();
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyFrequency()
        {
            if (_selectedTool != null)
            {
                _selectedTool.Frequency = ParseEngineering(_txtFrequency.Text, _selectedTool.Frequency);
                _txtFrequency.Text = FormatEngineering(_selectedTool.Frequency, "Hz");
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyAmplitude()
        {
            if (_selectedTool != null)
            {
                _selectedTool.Amplitude = ParseEngineering(_txtAmplitude.Text, _selectedTool.Amplitude);
                _txtAmplitude.Text = FormatEngineering(_selectedTool.Amplitude, "V");
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyPhase()
        {
            if (_selectedTool != null)
            {
                _selectedTool.Phase = ParseEngineering(_txtPhase.Text, _selectedTool.Phase);
                _txtPhase.Text = FormatEngineering(_selectedTool.Phase, "°");
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private void ApplyStepDuration()
        {
            if (_selectedTool != null)
            {
                double value = ParseEngineering(
                    _txtStepDuration.Text,
                    _selectedTool.StepDuration);

                value = Math.Max(0.0, value);

                _selectedTool.StepDuration = value;

                _txtStepDuration.Text =
                    FormatEngineering(
                        _selectedTool.StepDuration,
                        "s");
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyRiseTime()
        {
            if (_selectedTool == null) return;

            double value = Math.Max(0, ParseEngineering(_txtRiseTime.Text, _selectedTool.RiseTime));
            _selectedTool.RiseTime = value;
            _txtRiseTime.Text = FormatEngineering(value, "s");
            ParametersChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyFallTime()
        {
            if (_selectedTool == null) return;

            double value = Math.Max(0, ParseEngineering(_txtFallTime.Text, _selectedTool.FallTime));
            _selectedTool.FallTime = value;
            _txtFallTime.Text = FormatEngineering(value, "s");
            ParametersChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyGain()
        {
            if (_selectedTool != null)
            {
                _selectedTool.Gain = ParseEngineering(_txtGain.Text, _selectedTool.Gain);
                _txtGain.Text = FormatEngineering(_selectedTool.Gain);
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyEffectiveArea()
        {
            if (_selectedTool != null)
            {
                _selectedTool.EffectiveArea = ParseEngineering(_txtEffectiveArea.Text, _selectedTool.EffectiveArea);
                _txtEffectiveArea.Text = FormatEngineering(_selectedTool.EffectiveArea, "m²");
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyDistance()
        {
            if (_selectedTool != null)
            {
                _selectedTool.Distance = ParseEngineering(_txtDistance.Text, _selectedTool.Distance);
                _txtDistance.Text = FormatEngineering(_selectedTool.Distance, "m");
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyAttenuation()
        {
            if (_selectedTool != null)
            {
                _selectedTool.Attenuation = ParseEngineering(_txtAttenuation.Text, _selectedTool.Attenuation);
                _txtAttenuation.Text = FormatEngineering(_selectedTool.Attenuation, "dB/m");
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyRadarCrossSection()
        {
            if (_selectedTool != null)
            {
                _selectedTool.RadarCrossSection = ParseEngineering(_txtRadarCrossSection.Text, _selectedTool.RadarCrossSection);
                _txtRadarCrossSection.Text = FormatEngineering(_selectedTool.RadarCrossSection, "m²");
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyTimeConstant()
        {
            if (_selectedTool != null)
            {
                _selectedTool.TimeConstant = ParseEngineering(_txtTimeConstant.Text, _selectedTool.TimeConstant);
                _txtTimeConstant.Text = FormatEngineering(_selectedTool.TimeConstant, "s");
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyStepSize()
        {
            if (_selectedTool != null)
            {
                _selectedTool.StepSize = ParseEngineering(_txtStepSize.Text, _selectedTool.StepSize);
                _txtStepSize.Text = FormatEngineering(_selectedTool.StepSize, "s");
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyBitResolution()
        {
            if (_selectedTool != null)
            {
                _selectedTool.BitResolution = ParseInt(_txtBitResolution.Text, 12, 1, 24);
                _txtBitResolution.Text = _selectedTool.BitResolution.ToString();
                _selectedTool.QuantizationStep = _selectedTool.ReferenceVoltage / Math.Pow(2, _selectedTool.BitResolution);
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplySamplingRate()
        {
            if (_selectedTool != null)
            {
                _selectedTool.SamplingRate = ParseEngineering(_txtSamplingRate.Text, _selectedTool.SamplingRate);
                _txtSamplingRate.Text = FormatEngineering(_selectedTool.SamplingRate, "Hz");
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyReferenceVoltage()
        {
            if (_selectedTool != null)
            {
                _selectedTool.ReferenceVoltage = ParseEngineering(_txtReferenceVoltage.Text, _selectedTool.ReferenceVoltage);
                _txtReferenceVoltage.Text = FormatEngineering(_selectedTool.ReferenceVoltage, "V");
                _selectedTool.QuantizationStep = _selectedTool.ReferenceVoltage / Math.Pow(2, _selectedTool.BitResolution);
                ParametersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // ============ UI ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ============

        private Panel CreateBlockPanel(string title, int blockId)
        {
            var panel = new Panel
            {
                Width = 270,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(0)
            };

            var headerPanel = new Panel
            {
                Width = panel.Width - 2,
                Height = 40,
                BackColor = Color.FromArgb(52, 58, 64),
                Location = new Point(0, 0)
            };

            var titleLabel = new Label
            {
                Text = title,
                Location = new Point(8, 10),
                Size = new Size(140, 22),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            var idLabel = new Label
            {
                Text = $"ID: {blockId}",
                Location = new Point(155, 12),
                Size = new Size(105, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 100),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(idLabel);
            panel.Controls.Add(headerPanel);
            return panel;
        }

        private void AddParameter(Panel panel, string labelText, Control control, ref int yPosition)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(10, yPosition),
                Size = new Size(170, 28),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(52, 58, 64),
                BackColor = Color.White
            };
            control.Location = new Point(185, yPosition);
            control.Width = 75;
            control.Visible = true;
            panel.Controls.Add(label);
            panel.Controls.Add(control);
            yPosition += 35;
        }

        private void AddLabel(Panel panel, Label label, ref int yPosition)
        {
            label.Location = new Point(10, yPosition);
            label.Width = 250;
            label.Visible = true;
            panel.Controls.Add(label);
            yPosition += 25;
        }

        private void AddButton(Panel panel, Button button, ref int yPosition)
        {
            button.Location = new Point(10, yPosition);
            button.Width = 250;
            button.Height = 30;
            button.Visible = true;
            panel.Controls.Add(button);
            yPosition += 38;
        }

        private void ClearAllBlocks()
        {
            _mainPanel.Controls.Clear();
        }

        private string GetBlockTitle(MathTool tool)
        {
            if (tool.Type == ToolType.Operation)
            {
                switch (tool.Operation)
                {
                    case MathOperation.Addition: return "➕ Сложение";
                    case MathOperation.Subtraction: return "➖ Вычитание";
                    case MathOperation.Multiplication: return "✖️ Умножение";
                    case MathOperation.Division: return "➗ Деление";
                    case MathOperation.Integrator: return "∫ Интегратор";
                    case MathOperation.Differentiator: return "d/dt Дифференциатор";
                    case MathOperation.Interpolator: return "f(x) Интерполятор";
                    case MathOperation.FileIO: return "📁 Файловый ввод/вывод";
                    default: return "Математическая операция";
                }
            }
            switch (tool.Type)
            {
                case ToolType.Generator:
                    switch (tool.Waveform)
                    {
                        case GeneratorWaveform.Trapezoid: return "▰ Трапециевидный сигнал";
                        case GeneratorWaveform.Square: return "▮ Меандр";
                        case GeneratorWaveform.Sawtooth: return "◢ Пиловидный сигнал";
                        case GeneratorWaveform.Step: return "Единичный сигнал";
                        default: return "📈 Генератор синусоиды";
                    }
                case ToolType.Chart: return "📊 График";
                case ToolType.Amplifier: return "🔊 Усилитель";
                case ToolType.Antenna: return "📡 Антенна";
                case ToolType.Channel: return "🌐 Канал связи";
                case ToolType.Object: return "🎯 Объект (РЛС)";
                case ToolType.ADC: return "🔢 АЦП";
                case ToolType.SubSystem: return "🧩 Подсистема";
                default: return "Блок";
            }
        }

        public void SetSelectedTool(MathTool tool)
        {
            _selectedTool = tool;
            ClearAllBlocks();
            if (tool == null) { this.Visible = false; return; }
            this.Visible = true;

            string title = GetBlockTitle(tool);
            var blockPanel = CreateBlockPanel(title, tool.BlockId);
            int yPos = 50;

            if (tool.Type == ToolType.Operation && tool.Operation != MathOperation.FileIO &&
                tool.Operation != MathOperation.Integrator && tool.Operation != MathOperation.Differentiator &&
                tool.Operation != MathOperation.Interpolator)
            {
                _txtValueA.Text = FormatEngineering(tool.CustomValueA);
                _txtValueB.Text = FormatEngineering(tool.CustomValueB);
                AddParameter(blockPanel, "Значение A:", _txtValueA, ref yPos);
                AddParameter(blockPanel, "Значение B:", _txtValueB, ref yPos);
            }

            // Дифференциатор
            else if (tool.Type == ToolType.Operation && (tool.Operation == MathOperation.Integrator || tool.Operation == MathOperation.Differentiator))
            {
                _txtValueA.Text = FormatEngineering(tool.CustomValueA);
                _txtValueB.Text = FormatEngineering(tool.CustomValueB);
                _txtStepSize.Text = FormatEngineering(tool.StepSize, "s");
                AddParameter(blockPanel, "Значение A:", _txtValueA, ref yPos);
                AddParameter(blockPanel, "Значение B:", _txtValueB, ref yPos);
                AddParameter(blockPanel, "Шаг (с):", _txtStepSize, ref yPos);
            }

            // Интерполятор
            else if (tool.Type == ToolType.Operation && tool.Operation == MathOperation.Interpolator)
            {
                _txtValueA.Text = FormatEngineering(tool.CustomValueA);
                _txtValueB.Text = FormatEngineering(tool.CustomValueB);
                AddParameter(blockPanel, "Значение A:", _txtValueA, ref yPos);
                AddParameter(blockPanel, "Значение B:", _txtValueB, ref yPos);

                // Поле для количества точек (простое и понятное)
                var txtPointsCount = new TextBox
                {
                    Text = (tool.InterpolationPoints?.Count ?? 0).ToString(),
                    Location = new Point(185, yPos),
                    Width = 75,
                    BackColor = Color.White,
                    ForeColor = Color.Black,
                    BorderStyle = BorderStyle.FixedSingle
                };

                var lblPoints = new Label
                {
                    Text = "Количество точек:",
                    Location = new Point(10, yPos),
                    Size = new Size(170, 28),
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(52, 58, 64),
                    BackColor = Color.White
                };

                txtPointsCount.Leave += (s, e) =>
                {
                    if (int.TryParse(txtPointsCount.Text, out int newCount) && newCount >= 0 && newCount <= 1000)
                    {
                        var points = tool.InterpolationPoints;
                        if (points == null)
                        {
                            tool.InterpolationPoints = new List<System.Drawing.PointF>();
                            points = tool.InterpolationPoints;
                        }

                        if (newCount > points.Count)
                        {
                            // Генерируем равномерно распределённые точки (синусоида по умолчанию)
                            Random rand = new Random();
                            while (points.Count < newCount)
                            {
                                if (points.Count == 0)
                                {
                                    points.Add(new System.Drawing.PointF(0, 0));
                                }
                                else
                                {
                                    float lastX = points.Last().X;
                                    points.Add(new System.Drawing.PointF(lastX + 0.5f, (float)Math.Sin(lastX + 0.5f)));
                                }
                            }
                        }
                        else if (newCount < points.Count)
                        {
                            points.RemoveRange(newCount, points.Count - newCount);
                        }
                        txtPointsCount.Text = points.Count.ToString();
                        ParametersChanged?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        txtPointsCount.Text = tool.InterpolationPoints?.Count.ToString() ?? "0";
                    }
                };

                blockPanel.Controls.Add(lblPoints);
                blockPanel.Controls.Add(txtPointsCount);
                yPos += 35;

                // Подсказка для пользователя
                var hintLabel = new Label
                {
                    Text = "Точки генерируются автоматически\n(синусоида)",
                    Location = new Point(10, yPos),
                    Width = 250,
                    Height = 35,
                    Font = new Font("Segoe UI", 7, FontStyle.Italic),
                    ForeColor = Color.FromArgb(108, 117, 125),
                    BackColor = Color.White
                };
                blockPanel.Controls.Add(hintLabel);
                yPos += 40;
            }

            // ????
            else if (tool.Type == ToolType.Chart)
            {
                _txtChartPoints.Text = tool.MaxHistorySize.ToString();
                AddParameter(blockPanel, "Точек истории:", _txtChartPoints, ref yPos);
            }

            // Генератор
            else if (tool.Type == ToolType.Generator)
            {
                if (tool.Waveform == GeneratorWaveform.Step)
                {
                    _txtAmplitude.Text =
                        FormatEngineering(tool.Amplitude, "V");

                    _txtStepDuration.Text =
                        FormatEngineering(tool.StepDuration, "s");

                    AddParameter(
                        blockPanel,
                        "Высота сигнала (В):",
                        _txtAmplitude,
                        ref yPos);

                    AddParameter(
                        blockPanel,
                        "Длительность сигнала:",
                        _txtStepDuration,
                        ref yPos);
                }
                else
                {
                _txtFrequency.Text = FormatEngineering(tool.Frequency, "Hz");
                _txtAmplitude.Text = FormatEngineering(tool.Amplitude, "V");
                AddParameter(blockPanel, "Частота (Гц):", _txtFrequency, ref yPos);
                AddParameter(blockPanel, "Амплитуда (В):", _txtAmplitude, ref yPos);

                if (tool.Waveform == GeneratorWaveform.Sine)
                {
                    _txtPhase.Text = FormatEngineering(tool.Phase, "°");
                    AddParameter(blockPanel, "Фаза (град):", _txtPhase, ref yPos);
                }
                else if (tool.Waveform == GeneratorWaveform.Trapezoid)
                {
                    _txtRiseTime.Text = FormatEngineering(tool.RiseTime, "s");
                    _txtFallTime.Text = FormatEngineering(tool.FallTime, "s");
                    AddParameter(blockPanel, "Фронт нарастания:", _txtRiseTime, ref yPos);
                    AddParameter(blockPanel, "Фронт спада:", _txtFallTime, ref yPos);
                }
                }
            }

            // Усилитель
            else if (tool.Type == ToolType.Amplifier)
            {
                _txtGain.Text = FormatEngineering(tool.Gain);
                AddParameter(blockPanel, "Коэффициент усиления:", _txtGain, ref yPos);
            }

            // Антенна
            else if (tool.Type == ToolType.Antenna)
            {
                _txtGain.Text = FormatEngineering(tool.Gain);
                _txtEffectiveArea.Text = FormatEngineering(tool.EffectiveArea, "m²");
                AddParameter(blockPanel, "Усиление (дБ):", _txtGain, ref yPos);
                AddParameter(blockPanel, "Эффективная площадь (м²):", _txtEffectiveArea, ref yPos);
            }

            // Канал
            else if (tool.Type == ToolType.Channel)
            {
                _txtDistance.Text = FormatEngineering(tool.Distance, "m");
                _txtAttenuation.Text = FormatEngineering(tool.Attenuation, "dB/m");
                AddParameter(blockPanel, "Расстояние (м):", _txtDistance, ref yPos);
                AddParameter(blockPanel, "Затухание (дБ/м):", _txtAttenuation, ref yPos);
            }

            // Объект
            else if (tool.Type == ToolType.Object)
            {
                _txtRadarCrossSection.Text = FormatEngineering(tool.RadarCrossSection, "m²");
                _txtTimeConstant.Text = FormatEngineering(tool.TimeConstant, "s");
                AddParameter(blockPanel, "ЭПР (м²):", _txtRadarCrossSection, ref yPos);
                AddParameter(blockPanel, "Постоянная времени (с):", _txtTimeConstant, ref yPos);
            }

            // АЦП
            else if (tool.Type == ToolType.ADC)
            {
                _txtBitResolution.Text = tool.BitResolution.ToString();
                _txtSamplingRate.Text = FormatEngineering(tool.SamplingRate, "Hz");
                _txtReferenceVoltage.Text = FormatEngineering(tool.ReferenceVoltage, "V");
                AddParameter(blockPanel, "Разрядность (бит):", _txtBitResolution, ref yPos);
                AddParameter(blockPanel, "Частота (Гц):", _txtSamplingRate, ref yPos);
                AddParameter(blockPanel, "Опорное напряжение (В):", _txtReferenceVoltage, ref yPos);
            }

            // Подсистема
            else if (tool.Type == ToolType.SubSystem)
            {
                var infoLabel = new Label { Text = $"Входов: {tool.SubSystemData?.InputPorts?.Count ?? 0} | Выходов: {tool.SubSystemData?.OutputPorts?.Count ?? 0}", Location = new Point(10, yPos), Width = 250, Height = 25, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(52, 58, 64), BackColor = Color.White };
                blockPanel.Controls.Add(infoLabel);
                yPos += 35;
                var hintLabel = new Label { Text = "💡 Двойной клик для редактирования", Location = new Point(10, yPos), Width = 250, Height = 25, Font = new Font("Segoe UI", 8, FontStyle.Italic), ForeColor = Color.FromArgb(108, 117, 125), BackColor = Color.White };
                blockPanel.Controls.Add(hintLabel);
                yPos += 30;
            }

            // Математические операции
            else if (tool.Type == ToolType.Operation && tool.Operation == MathOperation.FileIO)
            {
                _cmbFileMode.SelectedIndex = tool.IsReading ? 0 : 1;
                _lblInputFile.Text = $"Файл: {System.IO.Path.GetFileName(tool.InputFilePath)}";
                _lblOutputFile.Text = $"Файл: {System.IO.Path.GetFileName(tool.OutputFilePath)}";
                _lblPointsCount.Text = $"Данных: {tool.FileData.Count}";
                AddParameter(blockPanel, "Режим работы:", _cmbFileMode, ref yPos);
                AddLabel(blockPanel, _lblInputFile, ref yPos);
                AddLabel(blockPanel, _lblOutputFile, ref yPos);
                AddLabel(blockPanel, _lblPointsCount, ref yPos);
                AddButton(blockPanel, _btnSelectInputFile, ref yPos);
                AddButton(blockPanel, _btnSelectOutputFile, ref yPos);
                AddButton(blockPanel, _btnExportResult, ref yPos);
                UpdateFileModeVisibility();
            }

            blockPanel.Height = yPos + 15;
            _mainPanel.Controls.Add(blockPanel);
        }

        private void UpdateFileModeVisibility()
        {
            if (_selectedTool == null) return;
            bool isReading = _cmbFileMode.SelectedIndex == 0;
            _selectedTool.IsReading = isReading;
            _btnSelectInputFile.Visible = isReading;
            _lblInputFile.Visible = isReading;
            _btnSelectOutputFile.Visible = !isReading;
            _btnExportResult.Visible = !isReading;
            _lblOutputFile.Visible = !isReading;
        }

        private void SelectInputFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Текстовые файлы (*.txt)|*.txt|CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedTool.InputFilePath = dialog.FileName;
                    _lblInputFile.Text = $"Файл: {System.IO.Path.GetFileName(dialog.FileName)}";
                    LoadFileData();
                }
            }
        }

        private void SelectOutputFile()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV файлы (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt";
                dialog.FileName = $"result_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedTool.OutputFilePath = dialog.FileName;
                    _lblOutputFile.Text = $"Файл: {System.IO.Path.GetFileName(dialog.FileName)}";
                }
            }
        }

        private void LoadFileData()
        {
            try
            {
                _selectedTool.FileData.Clear();
                string[] lines = System.IO.File.ReadAllLines(_selectedTool.InputFilePath);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    string[] parts = trimmed.Split(new char[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string part in parts)
                        if (double.TryParse(part, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double value))
                            _selectedTool.FileData.Add(value);
                }
                _selectedTool.CurrentFileIndex = 0;
                _lblPointsCount.Text = $"Данных: {_selectedTool.FileData.Count} значений";
                UpdateConnectedGraph();
                MessageBox.Show($"Загружено {_selectedTool.FileData.Count} значений", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ExportResult()
        {
            if (_selectedTool?.FileData.Count == 0) { MessageBox.Show("Нет данных для экспорта.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV файлы (*.csv)|*.csv";
                dialog.FileName = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    using (var writer = new System.IO.StreamWriter(dialog.FileName))
                    {
                        writer.WriteLine("Index,Value");
                        for (int i = 0; i < _selectedTool.FileData.Count; i++)
                            writer.WriteLine($"{i},{_selectedTool.FileData[i].ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    }
                    MessageBox.Show($"Сохранено {_selectedTool.FileData.Count} точек", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void UpdateConnectedGraph()
        {
            var form = Application.OpenForms.OfType<Form1>().FirstOrDefault();
            if (form != null && _selectedTool != null) form.LoadStaticFileData(_selectedTool.Id);
        }

        public void ApplyChanges(MathTool tool) { }
    }
}



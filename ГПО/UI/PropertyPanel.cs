using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MathApp.Models;

namespace MathApp.UI
{
    public class PropertyPanel : UserControl
    {
        private MathTool _selectedTool;

        // Элементы управления для значений A и B
        private TextBox _txtValueA;
        private TextBox _txtValueB;
        private Label _lblValueA;
        private Label _lblValueB;

        // Для графика
        private NumericUpDown _numChartPoints;
        private Label _lblChartPoints;

        // Для генератора синусоиды
        private NumericUpDown _numFrequency;
        private NumericUpDown _numAmplitude;
        private NumericUpDown _numPhase;
        private Label _lblFrequency;
        private Label _lblAmplitude;
        private Label _lblPhase;

        // Для усилителя
        private NumericUpDown _numGain;
        private Label _lblGain;

        // Для антенны
        private NumericUpDown _numEffectiveArea;
        private Label _lblEffectiveArea;

        // Для канала
        private NumericUpDown _numDistance;
        private NumericUpDown _numAttenuation;
        private Label _lblDistance;
        private Label _lblAttenuation;

        // Для объекта
        private NumericUpDown _numRadarCrossSection;
        private NumericUpDown _numTimeConstant;
        private Label _lblRadarCrossSection;
        private Label _lblTimeConstant;

        // Для АЦП
        private NumericUpDown _numBitResolution;
        private NumericUpDown _numSamplingRate;
        private NumericUpDown _numReferenceVoltage;
        private Label _lblBitResolution;
        private Label _lblSamplingRate;
        private Label _lblReferenceVoltage;

        // Для файлового блока
        private ComboBox _cmbFileMode;
        private Button _btnSelectInputFile;
        private Button _btnSelectOutputFile;
        private Button _btnExportResult;
        private Label _lblInputFile;
        private Label _lblOutputFile;
        private Label _lblPointsCount;

        private Button _btnApply;

        public event EventHandler ApplyClicked;

        public PropertyPanel()
        {
            InitializeComponent();
            this.Visible = false;
        }

        private void InitializeComponent()
        {
            this.Size = new Size(260, 700);
            this.BackColor = Color.Transparent;
            this.AutoScroll = true;

            var titleLabel = new Label
            {
                Text = "✏️ РЕДАКТИРОВАНИЕ",
                Location = new Point(5, 0),
                Size = new Size(250, 20),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            int currentY = 30;
            int labelX = 15;
            int controlX = 100;
            int controlWidth = 150;

            // ---- Значение A ----
            _lblValueA = CreateLabel("Значение A:", new Point(labelX, currentY));
            _txtValueA = CreateTextBox("0", new Point(controlX, currentY - 3));
            currentY += 35;

            // ---- Значение B ----
            _lblValueB = CreateLabel("Значение B:", new Point(labelX, currentY));
            _txtValueB = CreateTextBox("0", new Point(controlX, currentY - 3));
            currentY += 35;

            // ---- Точки на графике ----
            _lblChartPoints = CreateLabel("Точек:", new Point(labelX, currentY));
            _numChartPoints = CreateNumericUpDown(200, 10, 500, new Point(controlX, currentY - 3), 10);
            currentY += 40;

            // ---- Частота (генератор) ----
            _lblFrequency = CreateLabel("Частота (Гц):", new Point(labelX, currentY));
            _numFrequency = CreateNumericUpDown(1.0m, 0.1m, 100.0m, new Point(controlX, currentY - 3), 0.1m);
            currentY += 35;

            // ---- Амплитуда (генератор) ----
            _lblAmplitude = CreateLabel("Амплитуда:", new Point(labelX, currentY));
            _numAmplitude = CreateNumericUpDown(1.0m, 0.1m, 10.0m, new Point(controlX, currentY - 3), 0.1m);
            currentY += 35;

            // ---- Фаза (генератор) ----
            _lblPhase = CreateLabel("Фаза (град):", new Point(labelX, currentY));
            _numPhase = CreateNumericUpDown(0, 0, 360, new Point(controlX, currentY - 3), 15);
            currentY += 45;

            // ---- Усилитель (Gain) ----
            _lblGain = CreateLabel("Коэфф. усиления:", new Point(labelX, currentY));
            _numGain = CreateNumericUpDown(10.0m, 0.1m, 1000.0m, new Point(controlX, currentY - 3), 1.0m);
            currentY += 35;

            // ---- Антенна (Effective Area) ----
            _lblEffectiveArea = CreateLabel("Эфф. площадь (м²):", new Point(labelX, currentY));
            _numEffectiveArea = CreateNumericUpDown(0.1m, 0.01m, 100.0m, new Point(controlX, currentY - 3), 0.1m);
            currentY += 35;

            // ---- Канал (Distance) ----
            _lblDistance = CreateLabel("Расстояние (м):", new Point(labelX, currentY));
            _numDistance = CreateNumericUpDown(1000m, 0, 100000m, new Point(controlX, currentY - 3), 100m);
            currentY += 35;

            // ---- Канал (Attenuation) ----
            _lblAttenuation = CreateLabel("Затухание (дБ/м):", new Point(labelX, currentY));
            _numAttenuation = CreateNumericUpDown(0.01m, 0, 1.0m, new Point(controlX, currentY - 3), 0.01m);
            currentY += 35;

            // ---- Объект (Radar Cross Section) ----
            _lblRadarCrossSection = CreateLabel("ЭПР (м²):", new Point(labelX, currentY));
            _numRadarCrossSection = CreateNumericUpDown(1.0m, 0.01m, 1000.0m, new Point(controlX, currentY - 3), 0.1m);
            currentY += 35;

            // ---- Объект (Time Constant) ----
            _lblTimeConstant = CreateLabel("Пост. времени (с):", new Point(labelX, currentY));
            _numTimeConstant = CreateNumericUpDown(1.0m, 0.01m, 10.0m, new Point(controlX, currentY - 3), 0.1m);
            currentY += 35;

            // ---- АЦП (Bit Resolution) ----
            _lblBitResolution = CreateLabel("Разрядность (бит):", new Point(labelX, currentY));
            _numBitResolution = CreateNumericUpDown(12, 1, 24, new Point(controlX, currentY - 3), 1);
            currentY += 35;

            // ---- АЦП (Sampling Rate) ----
            _lblSamplingRate = CreateLabel("Частота (Гц):", new Point(labelX, currentY));
            _numSamplingRate = CreateNumericUpDown(10000m, 100m, 1000000m, new Point(controlX, currentY - 3), 1000m);
            currentY += 35;

            // ---- АЦП (Reference Voltage) ----
            _lblReferenceVoltage = CreateLabel("Опорное напр. (В):", new Point(labelX, currentY));
            _numReferenceVoltage = CreateNumericUpDown(5.0m, 1.0m, 10.0m, new Point(controlX, currentY - 3), 0.5m);
            currentY += 45;

            // ---- Файловый блок ----
            _cmbFileMode = new ComboBox
            {
                Location = new Point(labelX, currentY),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.Black,
                Visible = false
            };
            _cmbFileMode.Items.AddRange(new object[] { "Режим чтения", "Режим записи" });
            _cmbFileMode.SelectedIndex = 0;
            _cmbFileMode.SelectedIndexChanged += (s, e) => UpdateFileModeVisibility();
            currentY += 30;

            _btnSelectInputFile = new Button
            {
                Text = "Выбрать файл для чтения",
                Location = new Point(labelX, currentY),
                Size = new Size(controlWidth, 30),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            _btnSelectInputFile.Click += (s, e) => SelectInputFile();
            currentY += 35;

            _btnSelectOutputFile = new Button
            {
                Text = "Выбрать файл для записи",
                Location = new Point(labelX, currentY),
                Size = new Size(controlWidth, 30),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            _btnSelectOutputFile.Click += (s, e) => SelectOutputFile();
            currentY += 35;

            _btnExportResult = new Button
            {
                Text = "💾 Сохранить результат",
                Location = new Point(labelX, currentY),
                Size = new Size(controlWidth, 30),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            _btnExportResult.Click += (s, e) => ExportResult();
            currentY += 35;

            _lblInputFile = new Label
            {
                Text = "Файл чтения: не выбран",
                Location = new Point(labelX, currentY),
                Size = new Size(controlWidth, 25),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 8),
                Visible = false
            };
            currentY += 30;

            _lblOutputFile = new Label
            {
                Text = "Файл записи: не выбран",
                Location = new Point(labelX, currentY),
                Size = new Size(controlWidth, 25),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 8),
                Visible = false
            };
            currentY += 30;

            _lblPointsCount = new Label
            {
                Text = "Данных: 0",
                Location = new Point(labelX, currentY),
                Size = new Size(controlWidth, 25),
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 8),
                Visible = false
            };
            currentY += 45;

            // ---- Кнопка применения ----
            _btnApply = new Button
            {
                Text = "✓ ПРИМЕНИТЬ",
                Location = new Point(labelX, currentY),
                Size = new Size(controlWidth, 35),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _btnApply.Click += (s, e) => ApplyClicked?.Invoke(s, e);

            // Добавляем все контролы
            this.Controls.AddRange(new Control[] {
                titleLabel,
                _lblValueA, _txtValueA,
                _lblValueB, _txtValueB,
                _lblChartPoints, _numChartPoints,
                _lblFrequency, _numFrequency,
                _lblAmplitude, _numAmplitude,
                _lblPhase, _numPhase,
                _lblGain, _numGain,
                _lblEffectiveArea, _numEffectiveArea,
                _lblDistance, _numDistance,
                _lblAttenuation, _numAttenuation,
                _lblRadarCrossSection, _numRadarCrossSection,
                _lblTimeConstant, _numTimeConstant,
                _lblBitResolution, _numBitResolution,
                _lblSamplingRate, _numSamplingRate,
                _lblReferenceVoltage, _numReferenceVoltage,
                _cmbFileMode,
                _btnSelectInputFile, _btnSelectOutputFile, _btnExportResult,
                _lblInputFile, _lblOutputFile, _lblPointsCount,
                _btnApply
            });

            HideAllControls();
        }

        private Label CreateLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                Location = location,
                Size = new Size(120, 20),
                ForeColor = Color.Black,  // Белый цвет для читаемости на тёмном фоне
                Font = new Font("Segoe UI", 9),
                BackColor = Color.Transparent
            };
        }

        private TextBox CreateTextBox(string defaultValue, Point location)
        {
            return new TextBox
            {
                Location = location,
                Size = new Size(150, 25),
                Text = defaultValue,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
        }

        private NumericUpDown CreateNumericUpDown(decimal value, decimal min, decimal max, Point location, decimal increment)
        {
            return new NumericUpDown
            {
                Location = location,
                Size = new Size(150, 25),
                Minimum = min,
                Maximum = max,
                Value = value,
                Increment = increment,
                DecimalPlaces = 3,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void HideAllControls()
        {
            var allControls = new Control[] {
                _txtValueA, _txtValueB, _lblValueA, _lblValueB,
                _numChartPoints, _lblChartPoints,
                _numFrequency, _numAmplitude, _numPhase,
                _lblFrequency, _lblAmplitude, _lblPhase,
                _numGain, _lblGain,
                _numEffectiveArea, _lblEffectiveArea,
                _numDistance, _lblDistance,
                _numAttenuation, _lblAttenuation,
                _numRadarCrossSection, _lblRadarCrossSection,
                _numTimeConstant, _lblTimeConstant,
                _numBitResolution, _lblBitResolution,
                _numSamplingRate, _lblSamplingRate,
                _numReferenceVoltage, _lblReferenceVoltage,
                _cmbFileMode, _btnSelectInputFile, _btnSelectOutputFile,
                _btnExportResult, _lblInputFile, _lblOutputFile, _lblPointsCount
            };
            foreach (var c in allControls)
                if (c != null) c.Visible = false;
        }

        public void SetSelectedTool(MathTool tool)
        {
            _selectedTool = tool;
            HideAllControls();

            if (tool == null)
            {
                this.Visible = false;
                return;
            }

            this.Visible = true;

            // ---- Блок операции (сложение, вычитание и т.д.) ----
            if (tool.Type == ToolType.Operation && tool.Operation != MathOperation.FileIO)
            {
                _txtValueA.Visible = true;
                _txtValueB.Visible = true;
                _lblValueA.Visible = true;
                _lblValueB.Visible = true;
                _txtValueA.Text = tool.CustomValueA.ToString("F3");
                _txtValueB.Text = tool.CustomValueB.ToString("F3");
            }
            // ---- График ----
            else if (tool.Type == ToolType.Chart)
            {
                _numChartPoints.Visible = true;
                _lblChartPoints.Visible = true;
                _numChartPoints.Value = tool.MaxHistorySize;
            }
            // ---- Генератор синусоиды ----
            else if (tool.Type == ToolType.Generator)
            {
                _numFrequency.Visible = true;
                _numAmplitude.Visible = true;
                _numPhase.Visible = true;
                _lblFrequency.Visible = true;
                _lblAmplitude.Visible = true;
                _lblPhase.Visible = true;
                _numFrequency.Value = (decimal)tool.Frequency;
                _numAmplitude.Value = (decimal)tool.Amplitude;
                _numPhase.Value = (decimal)tool.Phase;
            }
            // ---- Усилитель ----
            else if (tool.Type == ToolType.Amplifier)
            {
                _numGain.Visible = true;
                _lblGain.Visible = true;
                _numGain.Value = (decimal)tool.Gain;
            }
            // ---- Антенна ----
            else if (tool.Type == ToolType.Antenna)
            {
                _numEffectiveArea.Visible = true;
                _lblEffectiveArea.Visible = true;
                _numEffectiveArea.Value = (decimal)tool.EffectiveArea;
            }
            // ---- Канал ----
            else if (tool.Type == ToolType.Channel)
            {
                _numDistance.Visible = true;
                _numAttenuation.Visible = true;
                _lblDistance.Visible = true;
                _lblAttenuation.Visible = true;
                _numDistance.Value = (decimal)tool.Distance;
                _numAttenuation.Value = (decimal)tool.Attenuation;
            }
            // ---- Объект ----
            else if (tool.Type == ToolType.Object)
            {
                _numRadarCrossSection.Visible = true;
                _numTimeConstant.Visible = true;
                _lblRadarCrossSection.Visible = true;
                _lblTimeConstant.Visible = true;
                _numRadarCrossSection.Value = (decimal)tool.RadarCrossSection;
                _numTimeConstant.Value = (decimal)tool.TimeConstant;
            }
            // ---- АЦП ----
            else if (tool.Type == ToolType.ADC)
            {
                _numBitResolution.Visible = true;
                _numSamplingRate.Visible = true;
                _numReferenceVoltage.Visible = true;
                _lblBitResolution.Visible = true;
                _lblSamplingRate.Visible = true;
                _lblReferenceVoltage.Visible = true;
                _numBitResolution.Value = tool.BitResolution;
                _numSamplingRate.Value = (decimal)tool.SamplingRate;
                _numReferenceVoltage.Value = (decimal)tool.ReferenceVoltage;
            }
            // ---- Файловый ввод/вывод ----
            else if (tool.Type == ToolType.Operation && tool.Operation == MathOperation.FileIO)
            {
                _cmbFileMode.Visible = true;
                _cmbFileMode.SelectedIndex = tool.IsReading ? 0 : 1;
                _lblInputFile.Text = $"Файл чтения: {System.IO.Path.GetFileName(tool.InputFilePath)}";
                _lblOutputFile.Text = $"Файл записи: {System.IO.Path.GetFileName(tool.OutputFilePath)}";
                _lblPointsCount.Text = $"Данных: {tool.FileData.Count}";
                _lblPointsCount.Visible = true;
                UpdateFileModeVisibility();
            }
        }

        private void UpdateFileModeVisibility()
        {
            if (_selectedTool == null) return;
            bool isReading = _cmbFileMode.SelectedIndex == 0;
            _selectedTool.IsReading = isReading;

            if (isReading)
            {
                _btnSelectInputFile.Visible = true;
                _btnSelectOutputFile.Visible = false;
                _btnExportResult.Visible = false;
                _lblInputFile.Visible = true;
                _lblOutputFile.Visible = false;
            }
            else
            {
                _btnSelectInputFile.Visible = false;
                _btnSelectOutputFile.Visible = true;
                _btnExportResult.Visible = true;
                _lblInputFile.Visible = false;
                _lblOutputFile.Visible = true;
            }
        }

        private void SelectInputFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Текстовые файлы (*.txt)|*.txt|CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedTool.InputFilePath = dialog.FileName;
                    _lblInputFile.Text = $"Файл чтения: {System.IO.Path.GetFileName(dialog.FileName)}";
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
                    _lblOutputFile.Text = $"Файл записи: {System.IO.Path.GetFileName(dialog.FileName)}";
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
                    {
                        if (double.TryParse(part, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out double value))
                        {
                            _selectedTool.FileData.Add(value);
                        }
                    }
                }

                _selectedTool.CurrentFileIndex = 0;
                _lblPointsCount.Text = $"Данных: {_selectedTool.FileData.Count} значений";

                UpdateConnectedGraph();

                MessageBox.Show($"Загружено {_selectedTool.FileData.Count} значений из файла", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportResult()
        {
            if (_selectedTool?.FileData.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
                    MessageBox.Show($"Данные сохранены в файл:\n{dialog.FileName}", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void UpdateConnectedGraph()
        {
            var form = Application.OpenForms.OfType<Form1>().FirstOrDefault();
            if (form != null && _selectedTool != null)
            {
                form.LoadStaticFileData(_selectedTool.Id);
            }
        }

        public void ApplyChanges(MathTool tool)
        {
            if (tool == null) return;

            try
            {
                if (tool.Type == ToolType.Operation && tool.Operation != MathOperation.FileIO)
                {
                    tool.CustomValueA = double.Parse(_txtValueA.Text);
                    tool.CustomValueB = double.Parse(_txtValueB.Text);
                }
                else if (tool.Type == ToolType.Chart)
                {
                    tool.MaxHistorySize = (int)_numChartPoints.Value;
                }
                else if (tool.Type == ToolType.Generator)
                {
                    tool.Frequency = (double)_numFrequency.Value;
                    tool.Amplitude = (double)_numAmplitude.Value;
                    tool.Phase = (int)_numPhase.Value;
                }
                else if (tool.Type == ToolType.Amplifier)
                {
                    tool.Gain = (double)_numGain.Value;
                }
                else if (tool.Type == ToolType.Antenna)
                {
                    tool.EffectiveArea = (double)_numEffectiveArea.Value;
                }
                else if (tool.Type == ToolType.Channel)
                {
                    tool.Distance = (double)_numDistance.Value;
                    tool.Attenuation = (double)_numAttenuation.Value;
                }
                else if (tool.Type == ToolType.Object)
                {
                    tool.RadarCrossSection = (double)_numRadarCrossSection.Value;
                    tool.TimeConstant = (double)_numTimeConstant.Value;
                }
                else if (tool.Type == ToolType.ADC)
                {
                    tool.BitResolution = (int)_numBitResolution.Value;
                    tool.SamplingRate = (double)_numSamplingRate.Value;
                    tool.ReferenceVoltage = (double)_numReferenceVoltage.Value;
                }
            }
            catch
            {
                MessageBox.Show("Ошибка ввода данных", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
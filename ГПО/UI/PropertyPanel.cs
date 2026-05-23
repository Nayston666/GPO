using MathApp.Models;
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace MathApp.UI
{
    public class PropertyPanel : UserControl
    {
        // Для генератора ступенчатого сигнала
        private NumericUpDown numStepAmplitude;
        private NumericUpDown numStepDelay;
        private NumericUpDown numStepRiseTime;
        private NumericUpDown numStepDuration;
        private NumericUpDown numStepOffset;
        private NumericUpDown numStepPoints;      // Количество точек
        private NumericUpDown numStepTimeEnd;     // Конечное время
        private Label lblStepAmp, lblStepDelay, lblStepRise, lblStepDur, lblStepOffset;
        private MathTool _selectedTool;
        private TextBox txtValueA;
        private TextBox txtValueB;
        private NumericUpDown numChartPoints;
        private NumericUpDown numFrequency;
        private NumericUpDown numAmplitude;
        private NumericUpDown numPhase;
        private NumericUpDown numStepSize;
        private Button btnApply;
        private Button btnAddPoint;
        private Button btnSelectInputFile;
        private Button btnSelectOutputFile;
        private Button btnExportResult;
        private Button btnExportBatchResults;
        private Label lblStep;
        private Label lblPointsCount;
        private Label lblInputFile;
        private Label lblOutputFile;
        private ComboBox cmbFileMode;

        public event EventHandler ApplyClicked;

        // Культура для парсинга чисел с точкой и экспоненциальной формой
        private CultureInfo _culture = CultureInfo.InvariantCulture;

        public PropertyPanel()
        {
            this.Size = new Size(260, 600);
            this.BackColor = Color.Transparent;
            this.AutoScroll = true;
            this.Visible = false;
            InitializeControls();
        }

        private void InitializeControls()
        {
            int y = 10;

            var lblTitle = new Label
            {
                Text = "✏️ РЕДАКТИРОВАНИЕ",
                Location = new Point(5, y),
                Size = new Size(250, 25),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(lblTitle);
            y += 35;

            // Значение A
            var lblA = new Label { Text = "Значение A:", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray };
            txtValueA = CreateScientificTextBox("0", new Point(100, y));
            this.Controls.Add(lblA);
            this.Controls.Add(txtValueA);
            y += 35;

            // Значение B
            var lblB = new Label { Text = "Значение B:", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray };
            txtValueB = CreateScientificTextBox("0", new Point(100, y));
            this.Controls.Add(lblB);
            this.Controls.Add(txtValueB);
            y += 45;

            // Для графика
            var lblChart = new Label { Text = "Точек на графике:", Location = new Point(15, y), Size = new Size(120, 25), ForeColor = Color.LightGray };
            numChartPoints = new NumericUpDown
            {
                Location = new Point(140, y),
                Size = new Size(100, 25),
                Minimum = 100,
                Maximum = 10000,
                Value = 1000,
                Increment = 500,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };
            this.Controls.Add(lblChart);
            this.Controls.Add(numChartPoints);
            y += 35;

            // Для синусоиды
            var lblFreq = new Label { Text = "Частота:", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray };
            numFrequency = CreateScientificNumericUpDown(1.0m, 0.000001m, 10000m, new Point(100, y), 0.1m);
            this.Controls.Add(lblFreq);
            this.Controls.Add(numFrequency);
            y += 35;

            var lblAmp = new Label { Text = "Амплитуда:", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray };
            numAmplitude = CreateScientificNumericUpDown(1.0m, 0.000001m, 10000m, new Point(100, y), 0.1m);
            this.Controls.Add(lblAmp);
            this.Controls.Add(numAmplitude);
            y += 35;

            var lblPhase = new Label { Text = "Фаза (град):", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray };
            numPhase = new NumericUpDown { Location = new Point(100, y), Size = new Size(140, 25), Minimum = 0, Maximum = 360, Value = 0, BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White };
            this.Controls.Add(lblPhase);
            this.Controls.Add(numPhase);
            y += 45;

            // Для интегратора 
            lblStep = new Label { Text = "Шаг (h):", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray, Visible = false };
            numStepSize = CreateScientificNumericUpDown(0.01m, 1e-15m, 1m, new Point(100, y), 1e-9m);
            numStepSize.DecimalPlaces = 15;
            numStepSize.Visible = false;
            this.Controls.Add(lblStep);
            this.Controls.Add(numStepSize);
            y += 35;

            // Для интерполятора
            btnAddPoint = new Button { Text = "➕ Добавить точку", Location = new Point(15, y), Size = new Size(230, 30), BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = false };
            btnAddPoint.Click += (s, e) => AddInterpolationPoint();
            this.Controls.Add(btnAddPoint);
            y += 35;

            lblPointsCount = new Label { Text = "Точек: 0", Location = new Point(15, y), Size = new Size(230, 25), ForeColor = Color.LightGray, Visible = false };
            this.Controls.Add(lblPointsCount);
            y += 45;

            // Для файлового блока
            cmbFileMode = new ComboBox { Location = new Point(15, y), Size = new Size(230, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White, Visible = false };
            cmbFileMode.Items.AddRange(new object[] { "Режим чтения", "Режим записи" });
            cmbFileMode.SelectedIndexChanged += (s, e) => { if (_selectedTool != null) _selectedTool.IsReading = cmbFileMode.SelectedIndex == 0; UpdateFileModeVisibility(); };
            this.Controls.Add(cmbFileMode);
            y += 35;

            btnSelectInputFile = new Button { Text = "Выбрать файл для чтения", Location = new Point(15, y), Size = new Size(230, 30), BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = false };
            btnSelectInputFile.Click += (s, e) => SelectInputFile();
            this.Controls.Add(btnSelectInputFile);
            y += 35;

            btnSelectOutputFile = new Button { Text = "Выбрать файл для записи", Location = new Point(15, y), Size = new Size(230, 30), BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = false };
            btnSelectOutputFile.Click += (s, e) => SelectOutputFile();
            this.Controls.Add(btnSelectOutputFile);
            y += 35;

            btnExportResult = new Button { Text = "💾 Сохранить результат", Location = new Point(15, y), Size = new Size(230, 30), BackColor = Color.FromArgb(100, 100, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = false };
            btnExportResult.Click += (s, e) => ExportResult();
            this.Controls.Add(btnExportResult);
            y += 35;

            btnExportBatchResults = new Button { Text = "📊 Экспорт всех результатов", Location = new Point(15, y), Size = new Size(230, 30), BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = false };
            btnExportBatchResults.Click += (s, e) => ExportBatchResults();
            this.Controls.Add(btnExportBatchResults);
            y += 35;

            lblInputFile = new Label { Text = "Файл чтения: не выбран", Location = new Point(15, y), Size = new Size(230, 25), ForeColor = Color.LightGray, Font = new Font("Segoe UI", 8), Visible = false };
            this.Controls.Add(lblInputFile);
            y += 30;

            lblOutputFile = new Label { Text = "Файл записи: не выбран", Location = new Point(15, y), Size = new Size(230, 25), ForeColor = Color.LightGray, Font = new Font("Segoe UI", 8), Visible = false };
            this.Controls.Add(lblOutputFile);
            y += 45;

            // Для генератора ступенчатого сигнала
            var lblStepAmp = new Label { Text = "Амплитуда:", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray, Visible = false };
            numStepAmplitude = CreateScientificNumericUpDown(1.0m, 0.000001m, 10000m, new Point(100, y), 0.1m);
            numStepAmplitude.Visible = false;
            this.Controls.Add(lblStepAmp);
            this.Controls.Add(numStepAmplitude);
            y += 35;

            // Задержка
            var lblStepDelay = new Label { Text = "Задержка:", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray, Visible = false };
            numStepDelay = new NumericUpDown
            {
                Location = new Point(100, y),
                Size = new Size(140, 25),
                Minimum = 0m,
                Maximum = 1000m,
                Value = 0m,
                DecimalPlaces = 15,
                Increment = 1e-9m,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                Visible = false
            };
            this.Controls.Add(lblStepDelay);
            this.Controls.Add(numStepDelay);
            y += 35;

            // Время нарастания
            var lblStepRise = new Label { Text = "Время нараст.:", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray, Visible = false };
            numStepRiseTime = new NumericUpDown
            {
                Location = new Point(100, y),
                Size = new Size(140, 25),
                Minimum = 0m,
                Maximum = 1000m,
                Value = 0m,
                DecimalPlaces = 15,
                Increment = 1e-9m,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                Visible = false
            };
            this.Controls.Add(lblStepRise);
            this.Controls.Add(numStepRiseTime);
            y += 35;

            // Длительность - расширяем диапазон
            var lblStepDur = new Label { Text = "Длительность:", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray, Visible = false };
            numStepDuration = new NumericUpDown
            {
                Location = new Point(100, y),
                Size = new Size(140, 25),
                Minimum = 0m,
                Maximum = 1000m,
                Value = 500e-9m,       // 500 наносекунд (5e-7)
                DecimalPlaces = 15,
                Increment = 1e-9m,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                Visible = false
            };
            this.Controls.Add(lblStepDur);
            this.Controls.Add(numStepDuration);
            y += 35;

            var lblStepOffset = new Label { Text = "Смещение:", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray, Visible = false };
            numStepOffset = CreateScientificNumericUpDown(0.0m, -1000m, 1000m, new Point(100, y), 0.1m);
            numStepOffset.Visible = false;
            this.Controls.Add(lblStepOffset);
            this.Controls.Add(numStepOffset);
            y += 35;

            var lblStepPoints = new Label { Text = "Кол-во точек:", Location = new Point(15, y), Size = new Size(90, 25), ForeColor = Color.LightGray, Visible = false };
            numStepPoints = new NumericUpDown
            {
                Location = new Point(110, y),
                Size = new Size(130, 25),
                Minimum = 50,
                Maximum = 10000,
                Value = 500,
                Increment = 50,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                Visible = false
            };
            this.Controls.Add(lblStepPoints);
            this.Controls.Add(numStepPoints);
            y += 35;

            var lblStepTimeEnd = new Label { Text = "Конечное время:", Location = new Point(15, y), Size = new Size(95, 25), ForeColor = Color.LightGray, Visible = false };
            numStepTimeEnd = new NumericUpDown
            {
                Location = new Point(115, y),
                Size = new Size(130, 25),
                Minimum = 1e-12m,      // 1 пикосекунда (1e-12)
                Maximum = 1000m,       // 1000 секунд
                Value = 1e-6m,         // 1 микросекунда (1e-6)
                DecimalPlaces = 15,    // 15 знаков после запятой
                Increment = 1e-9m,     // шаг 1 наносекунда
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                Visible = false
            };
            this.Controls.Add(lblStepTimeEnd);
            this.Controls.Add(numStepTimeEnd);
            y += 35;

            btnApply = new Button
            {
                Text = "✓ ПРИМЕНИТЬ",
                Location = new Point(15, y),
                Size = new Size(230, 35),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnApply.Click += (s, e) => ApplyClicked?.Invoke(s, e);
            this.Controls.Add(btnApply);

            HideAllControls();
        }

        /// <summary>
        /// Создаёт TextBox с поддержкой экспоненциальной записи
        /// </summary>
        private TextBox CreateScientificTextBox(string defaultValue, Point location)
        {
            var tb = new TextBox
            {
                Location = location,
                Size = new Size(140, 25),
                Text = defaultValue,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };

            // Валидация ввода
            tb.Validating += (s, e) =>
            {
                double result;
                if (!double.TryParse(tb.Text, NumberStyles.Any, _culture, out result))
                {
                    MessageBox.Show("Введите корректное число (например: 0.5, 1e-6, 2.3e-5)",
                        "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tb.Text = "0";
                }
            };

            return tb;
        }

        /// <summary>
        /// Создаёт NumericUpDown с поддержкой очень маленьких значений и экспоненциальной формы
        /// </summary>
        private NumericUpDown CreateScientificNumericUpDown(decimal value, decimal min, decimal max, Point location, decimal increment)
        {
            var nud = new NumericUpDown
            {
                Location = location,
                Size = new Size(140, 25),
                Minimum = min,
                Maximum = max,
                Value = value,
                Increment = increment,
                DecimalPlaces = 15,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Добавляем возможность ввода экспоненциальной формы через Text
            nud.Validating += (s, e) =>
            {
                double result;
                string text = nud.Text.Replace(',', '.');

                if (double.TryParse(text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out result))
                {
                    if (result >= (double)min && result <= (double)max)
                    {
                        nud.Value = (decimal)result;
                    }
                    else
                    {
                        MessageBox.Show($"Значение должно быть в диапазоне от {min:E2} до {max:E2}",
                            "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        nud.Value = value;
                    }
                }
                else
                {
                    MessageBox.Show("Введите корректное число (например: 0.01, 1e-6, 2.5e-9)",
                        "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    nud.Value = value;
                }
            };

            return nud;
        }

        private void HideAllControls()
        {
            txtValueA.Visible = false;
            txtValueB.Visible = false;
            numChartPoints.Visible = false;
            numFrequency.Visible = false;
            numAmplitude.Visible = false;
            numPhase.Visible = false;
            lblStep.Visible = false;
            numStepSize.Visible = false;
            btnAddPoint.Visible = false;
            lblPointsCount.Visible = false;
            cmbFileMode.Visible = false;
            btnSelectInputFile.Visible = false;
            btnSelectOutputFile.Visible = false;
            btnExportResult.Visible = false;
            btnExportBatchResults.Visible = false;
            lblInputFile.Visible = false;
            lblOutputFile.Visible = false;

            // Скрываем элементы ступенчатого генератора
            if (numStepAmplitude != null) numStepAmplitude.Visible = false;
            if (numStepDelay != null) numStepDelay.Visible = false;
            if (numStepRiseTime != null) numStepRiseTime.Visible = false;
            if (numStepDuration != null) numStepDuration.Visible = false;
            if (numStepOffset != null) numStepOffset.Visible = false;
            if (numStepPoints != null) numStepPoints.Visible = false;
            if (numStepTimeEnd != null) numStepTimeEnd.Visible = false;

            foreach (Control c in this.Controls)
                if (c is Label lbl && (lbl.Text == "Значение A:" || lbl.Text == "Значение B:" ||
                    lbl.Text == "Амплитуда:" || lbl.Text == "Задержка:" ||
                    lbl.Text == "Время нараст.:" || lbl.Text == "Длительность:" ||
                    lbl.Text == "Смещение:" || lbl.Text == "Кол-во точек:" || lbl.Text == "Конечное время:"))
                    lbl.Visible = false;
        }

        public void SetSelectedTool(MathTool tool)
        {
            _selectedTool = tool;
            HideAllControls();
            if (tool == null) { this.Visible = false; return; }
            this.Visible = true;

            bool isSimple = tool.Type == ToolType.Operation &&
                (tool.Operation == MathOperation.Addition ||
                 tool.Operation == MathOperation.Subtraction ||
                 tool.Operation == MathOperation.Multiplication ||
                 tool.Operation == MathOperation.Division);

            if (isSimple)
            {
                foreach (Control c in this.Controls)
                    if (c is Label lbl && (lbl.Text == "Значение A:" || lbl.Text == "Значение B:"))
                        lbl.Visible = true;
                txtValueA.Visible = true;
                txtValueB.Visible = true;
                txtValueA.Text = tool.CustomValueA.ToString("E6", _culture);
                txtValueB.Text = tool.CustomValueB.ToString("E6", _culture);
            }
            else if (tool.Type == ToolType.Operation && tool.Operation == MathOperation.Integrator)
            {
                lblStep.Visible = true;
                numStepSize.Visible = true;
                numStepSize.Value = (decimal)tool.StepSize;
            }
            else if (tool.Type == ToolType.Operation && tool.Operation == MathOperation.Interpolator)
            {
                btnAddPoint.Visible = true;
                lblPointsCount.Visible = true;
                lblPointsCount.Text = $"Точек: {tool.InterpolationPoints?.Count ?? 0}";
            }
            else if (tool.Type == ToolType.Operation && tool.Operation == MathOperation.FileIO)
            {
                cmbFileMode.Visible = true;
                cmbFileMode.SelectedIndex = tool.IsReading ? 0 : 1;
                UpdateFileModeVisibility();
                lblInputFile.Text = $"Файл чтения: {System.IO.Path.GetFileName(tool.InputFilePath)}";
                lblOutputFile.Text = $"Файл записи: {System.IO.Path.GetFileName(tool.OutputFilePath)}";
                lblPointsCount.Text = $"Данных: {tool.FileData.Count}";
                lblPointsCount.Visible = true;
            }
            else if (tool.Type == ToolType.Chart)
            {
                numChartPoints.Visible = true;
                numChartPoints.Value = tool.MaxHistorySize;
            }
            else if (tool.Type == ToolType.SineGenerator)
            {
                numFrequency.Visible = true;
                numAmplitude.Visible = true;
                numPhase.Visible = true;
                numFrequency.Value = (decimal)tool.Frequency;
                numAmplitude.Value = (decimal)tool.Amplitude;
                numPhase.Value = tool.Phase;
            }
            else if (tool.Type == ToolType.Operation && tool.Operation == MathOperation.Integrator)
            {
                lblStep.Visible = true;
                numStepSize.Visible = true;
                // Отображаем значение в экспоненциальной форме, если оно очень маленькое
                if (tool.StepSize < 0.0001 && tool.StepSize != 0)
                {
                    numStepSize.Text = tool.StepSize.ToString("E6", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    numStepSize.Value = (decimal)tool.StepSize;
                }
            }
            else if (tool.Type == ToolType.StepGenerator)
            {
                numStepAmplitude.Visible = true;
                numStepDelay.Visible = true;
                numStepRiseTime.Visible = true;
                numStepDuration.Visible = true;
                numStepOffset.Visible = true;
                numStepPoints.Visible = true;
                numStepTimeEnd.Visible = true;

                // Показываем метки
                foreach (Control c in this.Controls)
                    if (c is Label lbl && (lbl.Text == "Амплитуда:" || lbl.Text == "Задержка:" ||
                        lbl.Text == "Время нараст.:" || lbl.Text == "Длительность:" ||
                        lbl.Text == "Смещение:" || lbl.Text == "Кол-во точек:" || lbl.Text == "Конечное время:"))
                        lbl.Visible = true;

                numStepAmplitude.Value = (decimal)tool.StepAmplitude;
                numStepDelay.Value = (decimal)tool.StepDelay;
                numStepRiseTime.Value = (decimal)tool.StepRiseTime;
                numStepDuration.Value = (decimal)tool.StepDuration;
                numStepOffset.Value = (decimal)tool.StepOffset;
                numStepPoints.Value = tool.StepPoints;
                numStepTimeEnd.Value = (decimal)tool.StepTimeEnd;

            }
        }

        private void UpdateFileModeVisibility()
        {
            if (_selectedTool == null) return;
            if (_selectedTool.IsReading)
            {
                btnSelectInputFile.Visible = true;
                btnSelectOutputFile.Visible = false;
                btnExportResult.Visible = false;
                btnExportBatchResults.Visible = true;
                lblInputFile.Visible = true;
                lblOutputFile.Visible = false;
            }
            else
            {
                btnSelectInputFile.Visible = false;
                btnSelectOutputFile.Visible = true;
                btnExportResult.Visible = true;
                btnExportBatchResults.Visible = false;
                lblInputFile.Visible = false;
                lblOutputFile.Visible = true;
            }
        }

        private void AddInterpolationPoint()
        {
            if (_selectedTool?.Operation != MathOperation.Interpolator) return;

            var form = new Form
            {
                Text = "Добавить точку интерполяции",
                Size = new Size(280, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };

            var txtX = new TextBox { Location = new Point(130, 20), Size = new Size(120, 25), Text = "0" };
            var txtY = new TextBox { Location = new Point(130, 55), Size = new Size(120, 25), Text = "0" };
            var btnOk = new Button { Text = "OK", Location = new Point(60, 100), Size = new Size(70, 30), DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Отмена", Location = new Point(140, 100), Size = new Size(70, 30), DialogResult = DialogResult.Cancel };

            form.Controls.AddRange(new Control[] {
                new Label { Text = "X:", Location = new Point(20, 20), Size = new Size(30, 25) }, txtX,
                new Label { Text = "Y:", Location = new Point(20, 55), Size = new Size(30, 25) }, txtY, btnOk, btnCancel
            });

            if (form.ShowDialog() == DialogResult.OK &&
                double.TryParse(txtX.Text, NumberStyles.Any, _culture, out double x) &&
                double.TryParse(txtY.Text, NumberStyles.Any, _culture, out double y))
            {
                _selectedTool.InterpolationPoints.Add(new PointF((float)x, (float)y));
                lblPointsCount.Text = $"Точек: {_selectedTool.InterpolationPoints.Count}";
                MessageBox.Show($"Точка ({x.ToString("E3", _culture)}, {y.ToString("E3", _culture)}) добавлена!", "Успех");
            }
        }

        private void SelectInputFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Текстовые файлы (*.txt)|*.txt|CSV файлы (*.csv)|*.csv";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedTool.InputFilePath = dialog.FileName;
                    lblInputFile.Text = $"Файл чтения: {System.IO.Path.GetFileName(dialog.FileName)}";
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
                    lblOutputFile.Text = $"Файл записи: {System.IO.Path.GetFileName(dialog.FileName)}";
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
                        if (double.TryParse(part, NumberStyles.Any, _culture, out double value))
                        {
                            _selectedTool.FileData.Add(value);
                        }
                    }
                }

                _selectedTool.CurrentFileIndex = 0;
                lblPointsCount.Text = $"Данных: {_selectedTool.FileData.Count} значений";

                var form = Application.OpenForms.OfType<Form1>().FirstOrDefault();
                form?.RefreshGraphs();

                MessageBox.Show($"Загружено {_selectedTool.FileData.Count} значений из файла", "Успех");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка"); }
        }

        private void ExportResult()
        {
            if (_selectedTool?.FileData.Count == 0 && _selectedTool?.LastResult == null) return;

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV файлы (*.csv)|*.csv";
                dialog.FileName = $"result_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    using (var writer = new System.IO.StreamWriter(dialog.FileName))
                    {
                        writer.WriteLine("Index,Value");
                        if (_selectedTool.FileData.Count > 0)
                        {
                            for (int i = 0; i < _selectedTool.FileData.Count; i++)
                                writer.WriteLine($"{i},{_selectedTool.FileData[i].ToString("E10", _culture)}");
                        }
                        else if (_selectedTool.LastResult.HasValue)
                        {
                            writer.WriteLine($"0,{_selectedTool.LastResult.Value.ToString("E10", _culture)}");
                        }
                    }
                    MessageBox.Show($"Данные сохранены:\n{dialog.FileName}", "Успех");
                }
            }
        }

        private void ExportBatchResults()
        {
            if (_selectedTool?.FileData.Count == 0) return;

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV файлы (*.csv)|*.csv";
                dialog.FileName = $"batch_result_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    using (var writer = new System.IO.StreamWriter(dialog.FileName))
                    {
                        writer.WriteLine("Index,Value");
                        for (int i = 0; i < _selectedTool.FileData.Count; i++)
                        {
                            writer.WriteLine($"{i},{_selectedTool.FileData[i].ToString("E10", _culture)}");
                        }
                    }
                    MessageBox.Show($"Экспортировано {_selectedTool.FileData.Count} значений!\n{dialog.FileName}", "Успех");
                }
            }
        }

        public void ApplyChanges(MathTool tool)
        {
            if (tool == null) return;
            try
            {
                bool isSimple = tool.Type == ToolType.Operation &&
                    (tool.Operation == MathOperation.Addition ||
                     tool.Operation == MathOperation.Subtraction ||
                     tool.Operation == MathOperation.Multiplication ||
                     tool.Operation == MathOperation.Division);

                if (isSimple)
                {
                    tool.CustomValueA = double.Parse(txtValueA.Text, NumberStyles.Any, _culture);
                    tool.CustomValueB = double.Parse(txtValueB.Text, NumberStyles.Any, _culture);
                }
                else if (tool.Type == ToolType.Operation && tool.Operation == MathOperation.Integrator)
                {
                    tool.StepSize = (double)numStepSize.Value;
                }
                else if (tool.Type == ToolType.Chart)
                {
                    tool.MaxHistorySize = (int)numChartPoints.Value;
                }
                else if (tool.Type == ToolType.SineGenerator)
                {
                    tool.Frequency = (double)numFrequency.Value;
                    tool.Amplitude = (double)numAmplitude.Value;
                    tool.Phase = (int)numPhase.Value;
                }
                else if (tool.Type == ToolType.StepGenerator)
                {
                    tool.StepAmplitude = (double)numStepAmplitude.Value;
                    tool.StepDelay = (double)numStepDelay.Value;
                    tool.StepRiseTime = (double)numStepRiseTime.Value;
                    tool.StepDuration = (double)numStepDuration.Value;
                    tool.StepOffset = (double)numStepOffset.Value;
                    tool.StepPoints = (int)numStepPoints.Value;
                    tool.StepTimeEnd = (double)numStepTimeEnd.Value;
                }
            }
            catch { MessageBox.Show("Ошибка ввода данных", "Ошибка"); }
        }
    }
}
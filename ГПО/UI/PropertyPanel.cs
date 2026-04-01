using MathApp.Models;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MathApp.UI
{
    public class PropertyPanel : UserControl
    {
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
        private Label lblStep;
        private Label lblPointsCount;
        private Label lblInputFile;
        private Label lblOutputFile;
        private ComboBox cmbFileMode;

        public event EventHandler ApplyClicked;

        public PropertyPanel()
        {
            this.Size = new Size(260, 550);
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
            txtValueA = new TextBox { Location = new Point(100, y), Size = new Size(140, 25), Text = "0", BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White };
            this.Controls.Add(lblA);
            this.Controls.Add(txtValueA);
            y += 35;

            // Значение B
            var lblB = new Label { Text = "Значение B:", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray };
            txtValueB = new TextBox { Location = new Point(100, y), Size = new Size(140, 25), Text = "0", BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White };
            this.Controls.Add(lblB);
            this.Controls.Add(txtValueB);
            y += 45;

            // Для графика - увеличен до 10000 точек
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
            numFrequency = new NumericUpDown { Location = new Point(100, y), Size = new Size(140, 25), Minimum = 0.1m, Maximum = 5.0m, Value = 1.0m, DecimalPlaces = 1, BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White };
            this.Controls.Add(lblFreq);
            this.Controls.Add(numFrequency);
            y += 35;

            var lblAmp = new Label { Text = "Амплитуда:", Location = new Point(15, y), Size = new Size(80, 25), ForeColor = Color.LightGray };
            numAmplitude = new NumericUpDown { Location = new Point(100, y), Size = new Size(140, 25), Minimum = 0.1m, Maximum = 10.0m, Value = 1.0m, DecimalPlaces = 1, BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White };
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
            numStepSize = new NumericUpDown { Location = new Point(100, y), Size = new Size(140, 25), Minimum = 0.001m, Maximum = 0.1m, Value = 0.01m, DecimalPlaces = 3, BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White, Visible = false };
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
            cmbFileMode.SelectedIndex = 0;
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

            lblInputFile = new Label { Text = "Файл чтения: не выбран", Location = new Point(15, y), Size = new Size(230, 25), ForeColor = Color.LightGray, Font = new Font("Segoe UI", 8), Visible = false };
            this.Controls.Add(lblInputFile);
            y += 30;

            lblOutputFile = new Label { Text = "Файл записи: не выбран", Location = new Point(15, y), Size = new Size(230, 25), ForeColor = Color.LightGray, Font = new Font("Segoe UI", 8), Visible = false };
            this.Controls.Add(lblOutputFile);
            y += 45;

            btnApply = new Button { Text = "✓ ПРИМЕНИТЬ", Location = new Point(15, y), Size = new Size(230, 35), BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnApply.Click += (s, e) => ApplyClicked?.Invoke(s, e);
            this.Controls.Add(btnApply);

            HideAllControls();
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
            lblInputFile.Visible = false;
            lblOutputFile.Visible = false;

            foreach (Control c in this.Controls)
                if (c is Label lbl && (lbl.Text == "Значение A:" || lbl.Text == "Значение B:"))
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
                txtValueA.Text = tool.CustomValueA.ToString("F2");
                txtValueB.Text = tool.CustomValueB.ToString("F2");
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
        }

        private void UpdateFileModeVisibility()
        {
            if (_selectedTool == null) return;
            if (_selectedTool.IsReading)
            {
                btnSelectInputFile.Visible = true;
                btnSelectOutputFile.Visible = false;
                btnExportResult.Visible = false;
                lblInputFile.Visible = true;
                lblOutputFile.Visible = false;
            }
            else
            {
                btnSelectInputFile.Visible = false;
                btnSelectOutputFile.Visible = true;
                btnExportResult.Visible = true;
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

            if (form.ShowDialog() == DialogResult.OK && double.TryParse(txtX.Text, out double x) && double.TryParse(txtY.Text, out double y))
            {
                _selectedTool.InterpolationPoints.Add(new PointF((float)x, (float)y));
                lblPointsCount.Text = $"Точек: {_selectedTool.InterpolationPoints.Count}";
                MessageBox.Show($"Точка ({x}, {y}) добавлена!", "Успех");
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

                    // Поддержка разных разделителей
                    string[] parts = trimmed.Split(new char[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string part in parts)
                    {
                        // Используем InvariantCulture для правильного парсинга чисел с точкой
                        if (double.TryParse(part, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out double value))
                        {
                            _selectedTool.FileData.Add(value);
                        }
                    }
                }

                _selectedTool.CurrentFileIndex = 0;
                lblPointsCount.Text = $"Данных: {_selectedTool.FileData.Count} значений";

                // Автоматически обновляем график, если он подключен
                UpdateConnectedGraph();

                MessageBox.Show($"Загружено {_selectedTool.FileData.Count} значений из файла", "Успех");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка"); }
        }

        private void UpdateConnectedGraph()
        {
            // Находим форму, к которой подключён этот блок
            var form = Application.OpenForms.OfType<Form1>().FirstOrDefault();
            if (form != null)
            {
                // Вызываем обновление графиков
                var method = form.GetType().GetMethod("UpdateGraphsBatch",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(form, null);
            }
        }

        private void ExportResult()
        {
            if (_selectedTool?.FileData.Count == 0) return;
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
                    MessageBox.Show($"Данные сохранены:\n{dialog.FileName}", "Успех");
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
                    tool.CustomValueA = double.Parse(txtValueA.Text);
                    tool.CustomValueB = double.Parse(txtValueB.Text);
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
            }
            catch { MessageBox.Show("Ошибка ввода данных", "Ошибка"); }
        }
    }
}
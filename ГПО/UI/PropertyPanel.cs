using System;
using System.Drawing;
using System.Windows.Forms;
using MathApp.Models;

namespace MathApp.UI
{
    public class PropertyPanel : UserControl
    {
        private MathTool _selectedTool;

        // Элементы управления
        private TextBox _txtValueA;
        private TextBox _txtValueB;
        private NumericUpDown _numChartPoints;
        private NumericUpDown _numFrequency;
        private NumericUpDown _numAmplitude;
        private NumericUpDown _numPhase;
        private NumericUpDown _numStepSize;
        private Button _btnApply;
        private Button _btnAddPoint;
        private Label _lblStep;
        private Label _lblPointsCount;

        public event EventHandler ApplyClicked;

        public PropertyPanel()
        {
            InitializeComponent();
            this.Visible = false;
        }

        private void InitializeComponent()
        {
            this.Size = new Size(260, 400);
            this.BackColor = Color.Transparent;

            var titleLabel = new Label
            {
                Text = "✏️ РЕДАКТИРОВАНИЕ",
                Location = new Point(5, 0),
                Size = new Size(250, 20),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            // Значение A
            var lblValueA = CreateLabel("Значение A:", new Point(15, 33));
            _txtValueA = CreateTextBox("0", new Point(100, 30));

            // Значение B
            var lblValueB = CreateLabel("Значение B:", new Point(15, 68));
            _txtValueB = CreateTextBox("0", new Point(100, 65));

            // Точки на графике
            var lblChartPoints = CreateLabel("Точек:", new Point(15, 103));
            _numChartPoints = CreateNumericUpDown(200, 10, 500, new Point(120, 100), 10);

            // Частота
            var lblFrequency = CreateLabel("Частота:", new Point(15, 103));
            _numFrequency = CreateNumericUpDown(1.0m, 0.1m, 5.0m, new Point(100, 100), 0.1m);

            // Амплитуда
            var lblAmplitude = CreateLabel("Амплитуда:", new Point(15, 138));
            _numAmplitude = CreateNumericUpDown(1.0m, 0.1m, 10.0m, new Point(100, 135), 0.1m);

            // Фаза
            var lblPhase = CreateLabel("Фаза:", new Point(15, 173));
            _numPhase = CreateNumericUpDown(0, 0, 360, new Point(100, 170), 15);

            // Шаг интегрирования
            _lblStep = CreateLabel("Шаг (h):", new Point(15, 208));
            _numStepSize = CreateNumericUpDown(0.01m, 0.001m, 0.1m, new Point(100, 205), 0.001m);
            _numStepSize.DecimalPlaces = 3;

            // Кнопка добавления точек для интерполятора
            _btnAddPoint = new Button
            {
                Text = "➕ Добавить точку",
                Location = new Point(15, 205),
                Size = new Size(230, 30),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Visible = false
            };
            _btnAddPoint.Click += BtnAddPoint_Click;

            // Количество точек интерполяции
            _lblPointsCount = CreateLabel("Точек: 0", new Point(15, 240));
            _lblPointsCount.Visible = false;

            // Кнопка применения
            _btnApply = new Button
            {
                Text = "✓ ПРИМЕНИТЬ",
                Location = new Point(15, 270),
                Size = new Size(230, 35),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _btnApply.Click += (s, e) => ApplyClicked?.Invoke(s, e);

            // Добавляем контролы
            this.Controls.AddRange(new Control[] {
                titleLabel, lblValueA, _txtValueA, lblValueB, _txtValueB,
                lblChartPoints, _numChartPoints, lblFrequency, _numFrequency,
                lblAmplitude, _numAmplitude, lblPhase, _numPhase,
                _lblStep, _numStepSize, _btnAddPoint, _lblPointsCount, _btnApply
            });

            HideAllControls();
        }

        private Label CreateLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                Location = location,
                Size = new Size(80, 20),
                ForeColor = Color.LightGray,
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
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
        }

        private NumericUpDown CreateNumericUpDown(decimal value, decimal min,
                                                   decimal max, Point location,
                                                   decimal increment)
        {
            return new NumericUpDown
            {
                Location = location,
                Size = new Size(130, 25),
                Minimum = min,
                Maximum = max,
                Value = value,
                Increment = increment,
                DecimalPlaces = value % 1 == 0 ? 0 : 1,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void HideAllControls()
        {
            _txtValueA.Visible = false;
            _txtValueB.Visible = false;
            _numChartPoints.Visible = false;
            _numFrequency.Visible = false;
            _numAmplitude.Visible = false;
            _numPhase.Visible = false;
            _lblStep.Visible = false;
            _numStepSize.Visible = false;
            _btnAddPoint.Visible = false;
            _lblPointsCount.Visible = false;
        }

        /// <summary>
        /// Отображает свойства для выбранного блока
        /// </summary>
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

            switch (tool.Type)
            {
                case ToolType.Operation:
                    _txtValueA.Visible = true;
                    _txtValueB.Visible = true;
                    _txtValueA.Text = tool.CustomValueA.ToString("F2");
                    _txtValueB.Text = tool.CustomValueB.ToString("F2");

                    // Дополнительные настройки для специальных блоков
                    if (tool.Operation == MathOperation.Integrator)
                    {
                        _lblStep.Visible = true;
                        _numStepSize.Visible = true;
                        _numStepSize.Value = (decimal)tool.StepSize;
                        _numStepSize.ValueChanged += (s, e) =>
                        {
                            if (_selectedTool != null)
                                _selectedTool.StepSize = (double)_numStepSize.Value;
                        };
                    }
                    else if (tool.Operation == MathOperation.Interpolator)
                    {
                        _btnAddPoint.Visible = true;
                        _lblPointsCount.Visible = true;
                        UpdatePointsCountLabel(tool);
                    }
                    break;

                case ToolType.Chart:
                    _numChartPoints.Visible = true;
                    _numChartPoints.Value = tool.MaxHistorySize;
                    break;

                case ToolType.SineGenerator:
                    _numFrequency.Visible = true;
                    _numAmplitude.Visible = true;
                    _numPhase.Visible = true;
                    _numFrequency.Value = (decimal)tool.Frequency;
                    _numAmplitude.Value = (decimal)tool.Amplitude;
                    _numPhase.Value = tool.Phase;
                    break;
            }
        }

        private void UpdatePointsCountLabel(MathTool tool)
        {
            if (tool.InterpolationPoints != null)
            {
                _lblPointsCount.Text = $"Точек: {tool.InterpolationPoints.Count}";
            }
        }

        private void BtnAddPoint_Click(object sender, EventArgs e)
        {
            if (_selectedTool == null || _selectedTool.Operation != MathOperation.Interpolator)
                return;

            var form = new Form
            {
                Text = "Добавить точку интерполяции",
                Size = new Size(280, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent
            };

            var lblX = new Label { Text = "X (координата):", Location = new Point(20, 20), Size = new Size(100, 25) };
            var txtX = new TextBox { Location = new Point(130, 20), Size = new Size(120, 25), Text = "0" };

            var lblY = new Label { Text = "Y (значение):", Location = new Point(20, 55), Size = new Size(100, 25) };
            var txtY = new TextBox { Location = new Point(130, 55), Size = new Size(120, 25), Text = "0" };

            var btnOk = new Button
            {
                Text = "OK",
                Location = new Point(60, 100),
                Size = new Size(70, 30),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(140, 100),
                Size = new Size(70, 30),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            form.Controls.AddRange(new Control[] { lblX, txtX, lblY, txtY, btnOk, btnCancel });

            if (form.ShowDialog() == DialogResult.OK)
            {
                if (double.TryParse(txtX.Text, out double x) && double.TryParse(txtY.Text, out double y))
                {
                    _selectedTool.InterpolationPoints.Add(new PointF((float)x, (float)y));
                    UpdatePointsCountLabel(_selectedTool);
                    MessageBox.Show($"Точка ({x}, {y}) добавлена!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Введите корректные числа!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// Применяет изменения к выбранному блоку
        /// </summary>
        public void ApplyChanges(MathTool tool)
        {
            if (tool == null) return;

            try
            {
                switch (tool.Type)
                {
                    case ToolType.Operation:
                        tool.CustomValueA = double.Parse(_txtValueA.Text);
                        tool.CustomValueB = double.Parse(_txtValueB.Text);

                        if (tool.Operation == MathOperation.Integrator)
                        {
                            tool.StepSize = (double)_numStepSize.Value;
                        }
                        break;

                    case ToolType.Chart:
                        tool.MaxHistorySize = (int)_numChartPoints.Value;
                        break;

                    case ToolType.SineGenerator:
                        tool.Frequency = (double)_numFrequency.Value;
                        tool.Amplitude = (double)_numAmplitude.Value;
                        tool.Phase = (int)_numPhase.Value;
                        break;
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
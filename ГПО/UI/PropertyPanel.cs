using System;
using System.Drawing;
using System.Windows.Forms;
using MathApp.Models;

namespace MathApp.UI
{
    public class PropertyPanel : UserControl
    {
        private MathTool _selectedTool;
        private Panel _workPanel;
        private Button _btnApply;

        public event EventHandler ApplyClicked;

        public PropertyPanel()
        {
            this.Size = new Size(260, 300);
            this.BackColor = Color.FromArgb(45, 45, 50);
            this.Visible = false;

            _workPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(260, 260),
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            this.Controls.Add(_workPanel);

            var title = new Label
            {
                Text = "РЕДАКТИРОВАНИЕ",
                Location = new Point(10, 8),
                Size = new Size(240, 25),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            _workPanel.Controls.Add(title);

            _btnApply = new Button
            {
                Text = "ПРИМЕНИТЬ",
                Location = new Point(10, 220),
                Size = new Size(240, 35),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _btnApply.Click += (s, e) => ApplyClicked?.Invoke(s, e);
            _workPanel.Controls.Add(_btnApply);
        }

        public void SetSelectedTool(MathTool tool)
        {
            _selectedTool = tool;

            for (int i = _workPanel.Controls.Count - 1; i >= 0; i--)
            {
                var c = _workPanel.Controls[i];
                if (c != _workPanel.Controls[0] && c != _btnApply)
                {
                    _workPanel.Controls.RemoveAt(i);
                }
            }

            if (tool == null)
            {
                this.Visible = false;
                return;
            }

            this.Visible = true;
            int y = 40;

            switch (tool.Type)
            {
                case ToolType.Operation:
                    AddBox("Значение A:", tool.CustomValueA.ToString(), ref y);
                    AddBox("Значение B:", tool.CustomValueB.ToString(), ref y);
                    break;

                case ToolType.SineGenerator:
                    AddBox("Частота (Гц):", tool.Frequency.ToString(), ref y);
                    AddBox("Амплитуда:", tool.Amplitude.ToString(), ref y);
                    AddBox("Фаза (град):", tool.Phase.ToString(), ref y);
                    break;

                case ToolType.Generator:
                    AddBox("Частота (Гц):", tool.Frequency.ToString(), ref y);
                    AddBox("Амплитуда:", tool.Amplitude.ToString(), ref y);
                    break;

                case ToolType.Amplifier:
                    AddBox("Коэффициент усиления:", tool.Gain.ToString(), ref y);
                    break;

                case ToolType.Antenna:
                    AddBox("Частота (МГц):", tool.Frequency.ToString(), ref y);
                    AddBox("Усиление (дБ):", tool.Gain.ToString(), ref y);
                    AddBox("Эффективная площадь (м²):", tool.CustomValueA.ToString(), ref y);
                    AddBox("КПД (%):", tool.CustomValueB.ToString(), ref y);
                    break;

                case ToolType.Channel:
                    AddBox("Расстояние (м):", tool.CustomValueA.ToString(), ref y);
                    AddBox("Затухание (дБ/м):", tool.Attenuation.ToString(), ref y);
                    break;

                case ToolType.Object:
                    AddBox("Постоянная времени (с):", tool.TimeConstant.ToString(), ref y);
                    break;

                case ToolType.ADC:
                    AddBox("Разрядность (бит):", tool.BitResolution.ToString(), ref y);
                    AddBox("Опорное напряжение (В):", tool.ReferenceVoltage.ToString(), ref y);
                    break;

                case ToolType.Chart:
                    AddBox("Точек истории:", tool.MaxHistorySize.ToString(), ref y);
                    break;
            }

            _btnApply.Location = new Point(10, y + 10);
            _workPanel.AutoScrollMinSize = new Size(0, y + 60);
        }

        private void AddBox(string text, string val, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(10, y),
                Size = new Size(120, 25),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9)
            };
            var txt = new TextBox
            {
                Text = val,
                Location = new Point(135, y),
                Size = new Size(115, 25),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };
            _workPanel.Controls.Add(lbl);
            _workPanel.Controls.Add(txt);
            y += 35;
        }

        public void ApplyChanges(MathTool tool)
        {
            if (tool == null) return;

            try
            {
                int idx = 1;
                switch (tool.Type)
                {
                    case ToolType.Operation:
                        tool.CustomValueA = double.Parse(GetBox(idx++));
                        tool.CustomValueB = double.Parse(GetBox(idx++));
                        break;
                    case ToolType.SineGenerator:
                        tool.Frequency = double.Parse(GetBox(idx++));
                        tool.Amplitude = double.Parse(GetBox(idx++));
                        tool.Phase = int.Parse(GetBox(idx++));
                        break;
                    case ToolType.Generator:
                        tool.Frequency = double.Parse(GetBox(idx++));
                        tool.Amplitude = double.Parse(GetBox(idx++));
                        break;
                    case ToolType.Amplifier:
                        tool.Gain = double.Parse(GetBox(idx++));
                        break;
                    case ToolType.Antenna:
                        tool.Frequency = double.Parse(GetBox(idx++));
                        tool.Gain = double.Parse(GetBox(idx++));
                        tool.CustomValueA = double.Parse(GetBox(idx++));
                        tool.CustomValueB = double.Parse(GetBox(idx++));
                        break;
                    case ToolType.Channel:
                        tool.CustomValueA = double.Parse(GetBox(idx++));
                        tool.Attenuation = double.Parse(GetBox(idx++));
                        break;
                    case ToolType.Object:
                        tool.TimeConstant = double.Parse(GetBox(idx++));
                        break;
                    case ToolType.ADC:
                        tool.BitResolution = int.Parse(GetBox(idx++));
                        tool.ReferenceVoltage = double.Parse(GetBox(idx++));
                        break;
                    case ToolType.Chart:
                        tool.MaxHistorySize = int.Parse(GetBox(idx++));
                        break;
                }
            }
            catch { MessageBox.Show("Ошибка ввода", "Ошибка"); }
        }

        private string GetBox(int idx)
        {
            int c = 0;
            foreach (Control x in _workPanel.Controls)
                if (x is TextBox && c++ == idx - 1) return ((TextBox)x).Text;
            return "0";
        }
    }
}
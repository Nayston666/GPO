using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MathApp.Models;
using MathApp.Core;
using MathApp.UI;
using MathApp.Rendering;

namespace MathApp
{
    public partial class Form1 : Form
    {
        // ============ КОМПОНЕНТЫ ИНТЕРФЕЙСА ============
        private Panel leftPanel;
        private Panel workArea;
        private Panel propertiesPanel;
        private FlowLayoutPanel propertiesContent;

        private TextBox samplingRateInput;
        private TextBox durationInput;
        private Button runButton;
        private Button clearButton;
        private FlowLayoutPanel libraryPanel;

        // ============ ДАННЫЕ ============
        private List<MathTool> tools = new List<MathTool>();
        private List<Connection> connections = new List<Connection>();
        private CalculationEngine calculator = new CalculationEngine();
        private ConnectionManager connectionManager;

        private MathTool selectedTool = null;
        private MathTool draggedTool = null;
        private bool isDragging = false;
        private Point dragStartPoint;

        private bool isConnecting = false;
        private ConnectionPoint? sourcePoint = null;
        private Point tempEnd;

        private Timer simulationTimer;
        private bool isSimulating = false;
        private double simulationTime = 0;

        private Dictionary<Guid, GraphForm> graphs = new Dictionary<Guid, GraphForm>();
        private Dictionary<Guid, Panel> blockContainers = new Dictionary<Guid, Panel>();

        public Form1()
        {
            connectionManager = new ConnectionManager(connections);
            InitializeForm();
            SetupTimer();
        }

        private void InitializeForm()
        {
            this.Text = "САПР структурного моделирования РТС";
            this.Size = new Size(1300, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 242, 245);

            // Левая панель
            leftPanel = new Panel
            {
                Width = 280,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            int y = 15;

            leftPanel.Controls.Add(new Label
            {
                Text = "УПРАВЛЕНИЕ СИСТЕМОЙ",
                Location = new Point(15, y),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 58, 64)
            });
            y += 35;

            runButton = new Button
            {
                Text = "ЗАПУСТИТЬ СИСТЕМУ",
                Location = new Point(15, y),
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            runButton.Click += RunSimulation;
            leftPanel.Controls.Add(runButton);
            y += 55;

            leftPanel.Controls.Add(new Label
            {
                Text = "Глобальные параметры системы",
                Location = new Point(15, y),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 58, 64)
            });
            y += 30;

            leftPanel.Controls.Add(new Label
            {
                Text = "Частота дискретизации, Гц:",
                Location = new Point(15, y),
                Size = new Size(180, 23),
                Font = new Font("Segoe UI", 9)
            });
            samplingRateInput = new TextBox
            {
                Location = new Point(200, y),
                Size = new Size(65, 23),
                Text = "10000",
                Font = new Font("Segoe UI", 9)
            };
            samplingRateInput.KeyPress += (s, e) => FilterNumberInput(e);
            leftPanel.Controls.Add(samplingRateInput);
            y += 30;

            leftPanel.Controls.Add(new Label
            {
                Text = "Длительность сигнала, с:",
                Location = new Point(15, y),
                Size = new Size(180, 23),
                Font = new Font("Segoe UI", 9)
            });
            durationInput = new TextBox
            {
                Location = new Point(200, y),
                Size = new Size(65, 23),
                Text = "0.01",
                Font = new Font("Segoe UI", 9)
            };
            durationInput.KeyPress += (s, e) => FilterNumberInput(e);
            leftPanel.Controls.Add(durationInput);
            y += 45;

            leftPanel.Controls.Add(new Label
            {
                Text = "Библиотека блоков",
                Location = new Point(15, y),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 58, 64)
            });
            y += 30;

            libraryPanel = new FlowLayoutPanel
            {
                Location = new Point(15, y),
                Size = new Size(250, 300),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            string[] blocks = { "Генератор", "Усилитель", "Антенна", "Канал", "Объект", "АЦП", "График", "Сложение", "Вычитание", "Умножение", "Деление" };
            foreach (var block in blocks)
            {
                var btn = new Button
                {
                    Text = block,
                    Size = new Size(235, 32),
                    BackColor = Color.FromArgb(108, 117, 125),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(10, 0, 0, 0)
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                        DoDragDrop(new DataObject("ToolType", block), DragDropEffects.Copy);
                };
                libraryPanel.Controls.Add(btn);
            }
            leftPanel.Controls.Add(libraryPanel);
            y += 310;

            clearButton = new Button
            {
                Text = "ОЧИСТИТЬ ВСЁ",
                Location = new Point(15, y),
                Size = new Size(250, 35),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            clearButton.Click += ClearAll;
            leftPanel.Controls.Add(clearButton);

            // Правая панель свойств
            propertiesPanel = new Panel
            {
                Width = 320,
                Dock = DockStyle.Right,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            propertiesPanel.Controls.Add(new Label
            {
                Text = "ПАРАМЕТРЫ БЛОКОВ",
                Location = new Point(10, 10),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 58, 64)
            });

            propertiesContent = new FlowLayoutPanel
            {
                Location = new Point(10, 45),
                Width = 300,
                Height = propertiesPanel.ClientSize.Height - 55,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            propertiesPanel.Controls.Add(propertiesContent);

            propertiesPanel.Resize += (s, e) =>
            {
                propertiesContent.Height = propertiesPanel.ClientSize.Height - 55;
            };

            this.Controls.Add(propertiesPanel);
            this.Controls.Add(leftPanel);

            // Рабочая область
            workArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            workArea.GetType().GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(workArea, true);

            workArea.Paint += WorkArea_Paint;
            workArea.MouseDown += WorkArea_MouseDown;
            workArea.MouseMove += WorkArea_MouseMove;
            workArea.MouseUp += WorkArea_MouseUp;
            workArea.MouseDoubleClick += WorkArea_MouseDoubleClick;
            workArea.AllowDrop = true;
            workArea.DragEnter += (s, e) => { if (e.Data.GetDataPresent("ToolType")) e.Effect = DragDropEffects.Copy; };
            workArea.DragDrop += WorkArea_DragDrop;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("🗑 Удалить блок", null, DeleteTool_Click);
            contextMenu.Items.Add("🔗 Удалить соединения", null, DeleteConnections_Click);
            workArea.ContextMenuStrip = contextMenu;

            this.Controls.Add(workArea);
        }

        private void FilterNumberInput(KeyPressEventArgs e)
        {
            if (e.KeyChar == ',')
            {
                e.Handled = true;
            }
        }

        private void WorkArea_DragDrop(object sender, DragEventArgs e)
        {
            var mousePos = workArea.PointToClient(new Point(e.X, e.Y));
            string toolType = e.Data.GetData("ToolType") as string;
            if (string.IsNullOrEmpty(toolType)) return;

            var tool = new MathTool
            {
                Position = mousePos,
                Size = new Size(100, 60),
                Name = toolType,
                Id = Guid.NewGuid(),
                Rotated = false
            };

            if (toolType.Contains("Генератор"))
            {
                tool.Type = ToolType.Generator;
                tool.Amplitude = 10;
                tool.Frequency = 1000;
                tool.PhaseRad = 0;
            }
            else if (toolType.Contains("Усилитель"))
            {
                tool.Type = ToolType.Amplifier;
                tool.Gain = 10;
                tool.Size = new Size(80, 80);
            }
            else if (toolType.Contains("Антенна"))
            {
                tool.Type = ToolType.Antenna;
                tool.Gain = 10;
                tool.EffectiveArea = 0.1;
            }
            else if (toolType.Contains("Канал"))
            {
                tool.Type = ToolType.Channel;
                tool.Distance = 1000;
                tool.Attenuation = 0.01;
            }
            else if (toolType.Contains("Объект"))
            {
                tool.Type = ToolType.Object;
                tool.RadarCrossSection = 1;
            }
            else if (toolType.Contains("АЦП"))
            {
                tool.Type = ToolType.ADC;
                tool.BitResolution = 12;
                tool.SamplingRate = 10000;
                tool.QuantizationStep = 0.001;
                tool.DynamicRange = 120;
            }
            else if (toolType.Contains("График"))
            {
                tool.Type = ToolType.Chart;
                tool.Size = new Size(120, 60);
            }
            else if (toolType.Contains("Сложение"))
            {
                tool.Type = ToolType.Operation;
                tool.Operation = MathOperation.Addition;
            }
            else if (toolType.Contains("Вычитание"))
            {
                tool.Type = ToolType.Operation;
                tool.Operation = MathOperation.Subtraction;
            }
            else if (toolType.Contains("Умножение"))
            {
                tool.Type = ToolType.Operation;
                tool.Operation = MathOperation.Multiplication;
            }
            else if (toolType.Contains("Деление"))
            {
                tool.Type = ToolType.Operation;
                tool.Operation = MathOperation.Division;
            }

            tools.Add(tool);
            workArea.Invalidate();
            CreateBlockParameterPanel(tool);
        }

        private void CreateBlockParameterPanel(MathTool tool)
        {
            int idNumber = tools.IndexOf(tool) + 1;

            Panel blockPanel = new Panel
            {
                Width = 280,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 5),
                Tag = tool.Id
            };

            Panel headerPanel = new Panel
            {
                Width = blockPanel.Width - 2,
                Height = 35,
                BackColor = Color.FromArgb(52, 58, 64)
            };
            Label headerLabel = new Label
            {
                Text = $"{tool.Name}  ID: {idNumber}",
                Location = new Point(8, 8),
                Size = new Size(270, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White
            };
            headerPanel.Controls.Add(headerLabel);
            blockPanel.Controls.Add(headerPanel);

            Panel paramsPanel = new Panel
            {
                Width = blockPanel.Width - 2,
                Height = 10,
                AutoSize = true,
                Location = new Point(0, 35)
            };

            int currentY = 5;

            if (tool.Type == ToolType.Generator)
            {
                currentY = AddParameterRow(paramsPanel, "Амплитуда (В):", tool.Amplitude, v => tool.Amplitude = v, currentY);
                currentY = AddParameterRow(paramsPanel, "Частота (Гц):", tool.Frequency, v => tool.Frequency = v, currentY);
                currentY = AddParameterRow(paramsPanel, "Фаза (рад):", tool.PhaseRad, v => tool.PhaseRad = v, currentY);
            }
            else if (tool.Type == ToolType.Amplifier)
            {
                currentY = AddParameterRow(paramsPanel, "Коэфф. усиления:", tool.Gain, v => tool.Gain = v, currentY);
            }
            else if (tool.Type == ToolType.Antenna)
            {
                currentY = AddParameterRow(paramsPanel, "Усиление (дБ):", tool.Gain, v => tool.Gain = v, currentY);
                currentY = AddParameterRow(paramsPanel, "Эфф. площадь (м²):", tool.EffectiveArea, v => tool.EffectiveArea = v, currentY);
            }
            else if (tool.Type == ToolType.Channel)
            {
                currentY = AddParameterRow(paramsPanel, "Расстояние (м):", tool.Distance, v => tool.Distance = v, currentY);
                currentY = AddParameterRow(paramsPanel, "Затухание (дБ/м):", tool.Attenuation, v => tool.Attenuation = v, currentY);
            }
            else if (tool.Type == ToolType.Object)
            {
                currentY = AddParameterRow(paramsPanel, "ЭПР (м²):", tool.RadarCrossSection, v => tool.RadarCrossSection = v, currentY);
            }
            else if (tool.Type == ToolType.ADC)
            {
                currentY = AddParameterRow(paramsPanel, "Разрядность (бит):", tool.BitResolution, v => tool.BitResolution = (int)v, currentY);
                currentY = AddParameterRow(paramsPanel, "Частота дискр. (Гц):", tool.SamplingRate, v => tool.SamplingRate = v, currentY);
                currentY = AddParameterRow(paramsPanel, "Шаг квант. (В):", tool.QuantizationStep, v => tool.QuantizationStep = v, currentY);
                currentY = AddParameterRow(paramsPanel, "Дин. диапазон (дБ):", tool.DynamicRange, v => tool.DynamicRange = v, currentY);
            }
            else if (tool.Type == ToolType.Chart)
            {
                currentY = AddParameterRow(paramsPanel, "Точек истории:", tool.MaxHistorySize, v => tool.MaxHistorySize = (int)v, currentY);
            }

            paramsPanel.Height = currentY + 10;
            blockPanel.Height = headerPanel.Height + paramsPanel.Height;
            blockPanel.Controls.Add(paramsPanel);
            propertiesContent.Controls.Add(blockPanel);
            blockContainers[tool.Id] = blockPanel;
        }

        private int AddParameterRow(Panel container, string labelText, double value, Action<double> setter, int y)
        {
            Panel row = new Panel
            {
                Width = container.Width - 10,
                Height = 32,
                Location = new Point(5, y)
            };

            Label lbl = new Label
            {
                Text = labelText,
                Location = new Point(5, 7),
                Width = 130,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(52, 58, 64)
            };

            TextBox txt = new TextBox
            {
                Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Location = new Point(140, 4),
                Width = row.Width - 150
            };

            txt.KeyPress += (s, e) =>
            {
                if (e.KeyChar == ',')
                {
                    e.Handled = true;
                }
            };

            txt.TextChanged += (s, e) =>
            {
                if (double.TryParse(txt.Text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double result))
                {
                    setter(result);
                    workArea.Invalidate();
                }
            };

            row.Controls.Add(lbl);
            row.Controls.Add(txt);
            container.Controls.Add(row);

            return y + 34;
        }

        private int AddParameterRow(Panel container, string labelText, int value, Action<int> setter, int y)
        {
            Panel row = new Panel
            {
                Width = container.Width - 10,
                Height = 32,
                Location = new Point(5, y)
            };

            Label lbl = new Label
            {
                Text = labelText,
                Location = new Point(5, 7),
                Width = 130,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(52, 58, 64)
            };

            TextBox txt = new TextBox
            {
                Text = value.ToString(),
                Location = new Point(140, 4),
                Width = row.Width - 150
            };

            txt.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };

            txt.TextChanged += (s, e) =>
            {
                if (int.TryParse(txt.Text, out int result))
                {
                    setter(result);
                    workArea.Invalidate();
                }
            };

            row.Controls.Add(lbl);
            row.Controls.Add(txt);
            container.Controls.Add(row);

            return y + 34;
        }

        private void UpdateAllBlockIds()
        {
            int index = 0;
            foreach (var tool in tools)
            {
                index++;
                if (blockContainers.ContainsKey(tool.Id))
                {
                    Panel blockPanel = blockContainers[tool.Id];
                    if (blockPanel.Controls[0] is Panel headerPanel && headerPanel.Controls[0] is Label headerLabel)
                    {
                        headerLabel.Text = $"{tool.Name}  ID: {index}";
                    }
                }
            }
        }

        private void WorkArea_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Отрисовка соединений
            foreach (var conn in connections)
            {
                var source = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                var target = tools.FirstOrDefault(t => t.Id == conn.TargetToolId);
                if (source != null && target != null)
                {
                    Point start, end;

                    // Определяем позицию выхода источника
                    if (source.Type == ToolType.Generator)
                    {
                        // У генератора выход только с одной стороны
                        if (!source.Rotated)
                            start = new Point(source.Position.X + source.Size.Width + 5, source.Position.Y + source.Size.Height / 2);
                        else
                            start = new Point(source.Position.X - 5, source.Position.Y + source.Size.Height / 2);
                    }
                    else if (source.Type == ToolType.Operation)
                    {
                        // У операций выход справа или слева
                        if (!source.Rotated)
                            start = new Point(source.Position.X + source.Size.Width + 5, source.Position.Y + source.Size.Height / 2);
                        else
                            start = new Point(source.Position.X - 5, source.Position.Y + source.Size.Height / 2);
                    }
                    else if (source.Type == ToolType.Amplifier)
                    {
                        if (!source.Rotated)
                            start = new Point(source.Position.X + source.Size.Width + 5, source.Position.Y + source.Size.Height / 2);
                        else
                            start = new Point(source.Position.X - 5, source.Position.Y + source.Size.Height / 2);
                    }
                    else
                    {
                        if (!source.Rotated)
                            start = new Point(source.Position.X + source.Size.Width + 5, source.Position.Y + source.Size.Height / 2);
                        else
                            start = new Point(source.Position.X - 5, source.Position.Y + source.Size.Height / 2);
                    }

                    // Определяем позицию входа цели
                    if (target.Type == ToolType.Operation)
                    {
                        if (!target.Rotated)
                        {
                            if (conn.TargetInput == InputType.A)
                                end = new Point(target.Position.X - 5, target.Position.Y + target.Size.Height / 3);
                            else
                                end = new Point(target.Position.X - 5, target.Position.Y + 2 * target.Size.Height / 3);
                        }
                        else
                        {
                            if (conn.TargetInput == InputType.A)
                                end = new Point(target.Position.X + target.Size.Width + 5, target.Position.Y + target.Size.Height / 3);
                            else
                                end = new Point(target.Position.X + target.Size.Width + 5, target.Position.Y + 2 * target.Size.Height / 3);
                        }
                    }
                    else if (target.Type == ToolType.Chart)
                    {
                        if (!target.Rotated)
                            end = new Point(target.Position.X - 5, target.Position.Y + target.Size.Height / 2);
                        else
                            end = new Point(target.Position.X + target.Size.Width + 5, target.Position.Y + target.Size.Height / 2);
                    }
                    else if (target.Type == ToolType.Amplifier)
                    {
                        if (!target.Rotated)
                            end = new Point(target.Position.X - 5, target.Position.Y + target.Size.Height / 2);
                        else
                            end = new Point(target.Position.X + target.Size.Width + 5, target.Position.Y + target.Size.Height / 2);
                    }
                    else
                    {
                        if (!target.Rotated)
                            end = new Point(target.Position.X - 5, target.Position.Y + target.Size.Height / 2);
                        else
                            end = new Point(target.Position.X + target.Size.Width + 5, target.Position.Y + target.Size.Height / 2);
                    }

                    using (var pen = new Pen(Color.FromArgb(0, 120, 215), 1.5f))
                    {
                        int offset = Math.Min(40, Math.Abs(end.X - start.X) / 2);
                        Point c1 = new Point(start.X + offset, start.Y);
                        Point c2 = new Point(end.X - offset, end.Y);
                        g.DrawBezier(pen, start, c1, c2, end);
                    }

                    // Рисуем значение на соединении
                    if (conn.CurrentValue.HasValue)
                    {
                        int midX = (start.X + end.X) / 2;
                        int midY = (start.Y + end.Y) / 2 - 15;
                        string valueStr = conn.CurrentValue.Value.ToString("F2");
                        using (var font = new Font("Segoe UI", 8, FontStyle.Bold))
                        {
                            SizeF textSize = g.MeasureString(valueStr, font);
                            using (var bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                                g.FillRectangle(bgBrush, midX - textSize.Width / 2 - 2, midY - 6, textSize.Width + 4, textSize.Height + 2);
                            g.DrawString(valueStr, font, Brushes.Yellow, midX - textSize.Width / 2, midY - 6);
                        }
                    }
                }
            }

            // Временное соединение
            if (isConnecting && sourcePoint.HasValue)
            {
                var source = tools.FirstOrDefault(t => t.Id == sourcePoint.Value.ToolId);
                if (source != null)
                {
                    Point start;
                    if (source.Type == ToolType.Operation)
                    {
                        if (!source.Rotated)
                            start = new Point(source.Position.X + source.Size.Width + 5, source.Position.Y + source.Size.Height / 2);
                        else
                            start = new Point(source.Position.X - 5, source.Position.Y + source.Size.Height / 2);
                    }
                    else if (source.Type == ToolType.Generator)
                    {
                        if (!source.Rotated)
                            start = new Point(source.Position.X + source.Size.Width + 5, source.Position.Y + source.Size.Height / 2);
                        else
                            start = new Point(source.Position.X - 5, source.Position.Y + source.Size.Height / 2);
                    }
                    else
                    {
                        if (!source.Rotated)
                            start = new Point(source.Position.X + source.Size.Width + 5, source.Position.Y + source.Size.Height / 2);
                        else
                            start = new Point(source.Position.X - 5, source.Position.Y + source.Size.Height / 2);
                    }

                    using (var pen = new Pen(Color.Gray, 1f) { DashStyle = DashStyle.Dash })
                    {
                        int offset = Math.Min(40, Math.Abs(tempEnd.X - start.X) / 2);
                        Point c1 = new Point(start.X + offset, start.Y);
                        Point c2 = new Point(tempEnd.X - offset, tempEnd.Y);
                        g.DrawBezier(pen, start, c1, c2, tempEnd);
                    }
                }
            }

            // Отрисовка блоков
            var renderer = new BlockRenderer();

            foreach (var tool in tools)
            {
                int idNumber = tools.IndexOf(tool) + 1;

                switch (tool.Type)
                {
                    case ToolType.Generator:
                        renderer.DrawGeneratorTool(g, tool, selectedTool, idNumber);
                        break;
                    case ToolType.Amplifier:
                        renderer.DrawAmplifierTool(g, tool, selectedTool, idNumber);
                        break;
                    case ToolType.Antenna:
                        renderer.DrawAntennaTool(g, tool, selectedTool, idNumber);
                        break;
                    case ToolType.Channel:
                        renderer.DrawChannelTool(g, tool, selectedTool, idNumber);
                        break;
                    case ToolType.Object:
                        renderer.DrawObjectTool(g, tool, selectedTool, idNumber);
                        break;
                    case ToolType.ADC:
                        renderer.DrawADCTool(g, tool, selectedTool, idNumber);
                        break;
                    case ToolType.Chart:
                        renderer.DrawChartTool(g, tool, selectedTool, idNumber);
                        break;
                    case ToolType.Operation:
                        switch (tool.Operation)
                        {
                            case MathOperation.Addition:
                                renderer.DrawAdditionTool(g, tool, selectedTool, idNumber);
                                break;
                            case MathOperation.Subtraction:
                                renderer.DrawSubtractionTool(g, tool, selectedTool, idNumber);
                                break;
                            case MathOperation.Multiplication:
                                renderer.DrawMultiplicationTool(g, tool, selectedTool, idNumber);
                                break;
                            case MathOperation.Division:
                                renderer.DrawDivisionTool(g, tool, selectedTool, idNumber);
                                break;
                        }
                        break;
                }

                renderer.DrawConnectionPoints(g, tool, connections, tools);
            }
        }

        private void WorkArea_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Если мы НЕ в режиме соединения - проверяем нажатие на выходной пин (начало нового соединения)
                if (!isConnecting)
                {
                    foreach (var tool in tools)
                    {
                        Point output;

                        // У графика нет выхода - пропускаем
                        if (tool.Type == ToolType.Chart)
                        {
                            continue;
                        }

                        // Определяем выход для разных типов блоков
                        if (tool.Type == ToolType.Operation)
                        {
                            if (!tool.Rotated)
                                output = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                            else
                                output = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                        }
                        else if (tool.Type == ToolType.Generator)
                        {
                            if (!tool.Rotated)
                                output = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                            else
                                output = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                        }
                        else if (tool.Type == ToolType.Amplifier)
                        {
                            if (!tool.Rotated)
                                output = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                            else
                                output = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                        }
                        else
                        {
                            if (!tool.Rotated)
                                output = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                            else
                                output = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                        }

                        if (Math.Sqrt(Math.Pow(e.X - output.X, 2) + Math.Pow(e.Y - output.Y, 2)) < 10)
                        {
                            isConnecting = true;
                            sourcePoint = new ConnectionPoint(tool.Id, ConnectionPointType.Output);
                            tempEnd = e.Location;
                            workArea.Invalidate();
                            return;
                        }
                    }
                }

                // Если мы в режиме соединения - проверяем нажатие на входной пин (завершение соединения)
                if (isConnecting && sourcePoint.HasValue)
                {
                    foreach (var tool in tools)
                    {
                        // У генератора нет входа - пропускаем
                        if (tool.Type == ToolType.Generator)
                        {
                            continue;
                        }

                        // Для операций - два входа
                        if (tool.Type == ToolType.Operation)
                        {
                            Point inputA, inputB;

                            if (!tool.Rotated)
                            {
                                inputA = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 3);
                                inputB = new Point(tool.Position.X - 5, tool.Position.Y + 2 * tool.Size.Height / 3);
                            }
                            else
                            {
                                inputA = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 3);
                                inputB = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + 2 * tool.Size.Height / 3);
                            }

                            // Проверка нажатия на вход A
                            if (Math.Sqrt(Math.Pow(e.X - inputA.X, 2) + Math.Pow(e.Y - inputA.Y, 2)) < 10)
                            {
                                // Нельзя соединять блок сам с собой
                                if (sourcePoint.Value.ToolId == tool.Id)
                                {
                                    isConnecting = false;
                                    sourcePoint = null;
                                    workArea.Invalidate();
                                    return;
                                }

                                // Источник должен иметь выход (не график)
                                var sourceTool = tools.FirstOrDefault(t => t.Id == sourcePoint.Value.ToolId);
                                if (sourceTool != null && sourceTool.Type == ToolType.Chart)
                                {
                                    isConnecting = false;
                                    sourcePoint = null;
                                    workArea.Invalidate();
                                    return;
                                }

                                connectionManager.CreateConnection(sourcePoint.Value, new ConnectionPoint(tool.Id, ConnectionPointType.Input, InputType.A));
                                isConnecting = false;
                                sourcePoint = null;
                                workArea.Invalidate();
                                return;
                            }

                            // Проверка нажатия на вход B
                            if (Math.Sqrt(Math.Pow(e.X - inputB.X, 2) + Math.Pow(e.Y - inputB.Y, 2)) < 10)
                            {
                                // Нельзя соединять блок сам с собой
                                if (sourcePoint.Value.ToolId == tool.Id)
                                {
                                    isConnecting = false;
                                    sourcePoint = null;
                                    workArea.Invalidate();
                                    return;
                                }

                                // Источник должен иметь выход (не график)
                                var sourceTool = tools.FirstOrDefault(t => t.Id == sourcePoint.Value.ToolId);
                                if (sourceTool != null && sourceTool.Type == ToolType.Chart)
                                {
                                    isConnecting = false;
                                    sourcePoint = null;
                                    workArea.Invalidate();
                                    return;
                                }

                                connectionManager.CreateConnection(sourcePoint.Value, new ConnectionPoint(tool.Id, ConnectionPointType.Input, InputType.B));
                                isConnecting = false;
                                sourcePoint = null;
                                workArea.Invalidate();
                                return;
                            }
                        }
                        else
                        {
                            // Для остальных блоков - один вход
                            Point input;

                            if (!tool.Rotated)
                                input = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                            else
                                input = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);

                            if (Math.Sqrt(Math.Pow(e.X - input.X, 2) + Math.Pow(e.Y - input.Y, 2)) < 10)
                            {
                                // Нельзя соединять блок сам с собой
                                if (sourcePoint.Value.ToolId == tool.Id)
                                {
                                    isConnecting = false;
                                    sourcePoint = null;
                                    workArea.Invalidate();
                                    return;
                                }

                                // Источник должен иметь выход (не график)
                                var sourceTool = tools.FirstOrDefault(t => t.Id == sourcePoint.Value.ToolId);
                                if (sourceTool != null && sourceTool.Type == ToolType.Chart)
                                {
                                    isConnecting = false;
                                    sourcePoint = null;
                                    workArea.Invalidate();
                                    return;
                                }

                                connectionManager.CreateConnection(sourcePoint.Value, new ConnectionPoint(tool.Id, ConnectionPointType.Input, InputType.A));
                                isConnecting = false;
                                sourcePoint = null;
                                workArea.Invalidate();
                                return;
                            }
                        }
                    }
                }

                // Если не нажали на пины - проверяем нажатие на блок
                var hitTool = GetToolAtPosition(e.Location);
                if (hitTool != null)
                {
                    selectedTool = hitTool;
                    workArea.Invalidate();
                    isDragging = true;
                    draggedTool = hitTool;
                    dragStartPoint = new Point(e.Location.X - hitTool.Position.X, e.Location.Y - hitTool.Position.Y);
                }
                else
                {
                    selectedTool = null;
                    workArea.Invalidate();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Отмена соединения
                isConnecting = false;
                sourcePoint = null;
                workArea.Invalidate();
            }
        }

        private void WorkArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && draggedTool != null)
            {
                draggedTool.Position = new Point(e.Location.X - dragStartPoint.X, e.Location.Y - dragStartPoint.Y);
                workArea.Invalidate();
            }
            else if (isConnecting)
            {
                tempEnd = e.Location;
                workArea.Invalidate();
            }
        }

        private void WorkArea_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                draggedTool = null;
                workArea.Invalidate();
            }
        }

        private void WorkArea_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var tool = GetToolAtPosition(e.Location);
            if (tool != null)
            {
                if (tool.Type == ToolType.Chart)
                {
                    if (graphs.ContainsKey(tool.Id) && !graphs[tool.Id].IsDisposed)
                    {
                        graphs[tool.Id].WindowState = FormWindowState.Normal;
                        graphs[tool.Id].BringToFront();
                    }
                    else
                    {
                        var graph = new GraphForm(tool.Name);
                        graph.Show();
                        graphs[tool.Id] = graph;
                        foreach (var val in tool.ValueHistory)
                            graph.AddValue(val);
                    }
                }
                else
                {
                    tool.Rotated = !tool.Rotated;
                    workArea.Invalidate();
                }
            }
        }

        private MathTool GetToolAtPosition(Point point)
        {
            foreach (var tool in tools)
            {
                if (new Rectangle(tool.Position, tool.Size).Contains(point))
                    return tool;
            }
            return null;
        }

        private void DeleteTool_Click(object sender, EventArgs e)
        {
            if (selectedTool != null)
            {
                if (blockContainers.ContainsKey(selectedTool.Id))
                {
                    propertiesContent.Controls.Remove(blockContainers[selectedTool.Id]);
                    blockContainers.Remove(selectedTool.Id);
                }

                if (selectedTool.Type == ToolType.Chart && graphs.ContainsKey(selectedTool.Id))
                {
                    if (!graphs[selectedTool.Id].IsDisposed) graphs[selectedTool.Id].Close();
                    graphs.Remove(selectedTool.Id);
                }

                tools.Remove(selectedTool);
                selectedTool = null;
                UpdateAllBlockIds();
                workArea.Invalidate();
            }
        }

        private void DeleteConnections_Click(object sender, EventArgs e)
        {
            if (selectedTool != null)
            {
                connections.RemoveAll(c => c.TargetToolId == selectedTool.Id);
                workArea.Invalidate();
            }
        }

        private void SetupTimer()
        {
            simulationTimer = new Timer { Interval = 50 };
            simulationTimer.Tick += SimulationTick;
        }

        private void RunSimulation(object sender, EventArgs e)
        {
            isSimulating = true;
            simulationTime = 0;
            simulationTimer.Start();
        }

        private void ClearAll(object sender, EventArgs e)
        {
            foreach (var graph in graphs.Values)
                if (!graph.IsDisposed) graph.Close();
            graphs.Clear();
            tools.Clear();
            connections.Clear();
            selectedTool = null;
            propertiesContent.Controls.Clear();
            blockContainers.Clear();
            workArea.Invalidate();
        }

        private void SimulationTick(object sender, EventArgs e)
        {
            if (!isSimulating) return;

            simulationTime += 0.001;

            foreach (var tool in tools.Where(t => t.Type == ToolType.Generator))
            {
                double value = tool.Amplitude * Math.Sin(2 * Math.PI * tool.Frequency * simulationTime + tool.PhaseRad);
                tool.LastResult = value;
            }

            foreach (var tool in tools.Where(t => t.Type == ToolType.Amplifier))
            {
                var conn = connections.FirstOrDefault(c => c.TargetToolId == tool.Id);
                if (conn != null)
                {
                    var source = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                    if (source != null && source.LastResult.HasValue)
                        tool.LastResult = source.LastResult.Value * tool.Gain;
                }
            }

            // Для операций - вычисляем результат
            foreach (var tool in tools.Where(t => t.Type == ToolType.Operation))
            {
                var connA = connections.FirstOrDefault(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.A);
                var connB = connections.FirstOrDefault(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.B);

                double valueA = 0, valueB = 0;
                bool hasA = false, hasB = false;

                if (connA != null)
                {
                    var sourceA = tools.FirstOrDefault(t => t.Id == connA.SourceToolId);
                    if (sourceA != null && sourceA.LastResult.HasValue)
                    {
                        valueA = sourceA.LastResult.Value;
                        hasA = true;
                    }
                }

                if (connB != null)
                {
                    var sourceB = tools.FirstOrDefault(t => t.Id == connB.SourceToolId);
                    if (sourceB != null && sourceB.LastResult.HasValue)
                    {
                        valueB = sourceB.LastResult.Value;
                        hasB = true;
                    }
                }

                if (hasA && hasB)
                {
                    switch (tool.Operation)
                    {
                        case MathOperation.Addition:
                            tool.LastResult = valueA + valueB;
                            break;
                        case MathOperation.Subtraction:
                            tool.LastResult = valueA - valueB;
                            break;
                        case MathOperation.Multiplication:
                            tool.LastResult = valueA * valueB;
                            break;
                        case MathOperation.Division:
                            tool.LastResult = valueB != 0 ? valueA / valueB : 0;
                            break;
                    }

                    foreach (var conn in connections.Where(c => c.SourceToolId == tool.Id))
                        conn.CurrentValue = tool.LastResult;
                }
            }

            foreach (var chart in tools.Where(t => t.Type == ToolType.Chart))
            {
                var conn = connections.FirstOrDefault(c => c.TargetToolId == chart.Id);
                if (conn != null)
                {
                    var source = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                    if (source != null && source.LastResult.HasValue)
                    {
                        chart.AddToHistory(source.LastResult.Value);
                        if (graphs.ContainsKey(chart.Id) && !graphs[chart.Id].IsDisposed)
                            graphs[chart.Id].AddValue(source.LastResult.Value);
                    }
                }
            }

            workArea.Invalidate();
        }
    }
}
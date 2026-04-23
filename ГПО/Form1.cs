using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MathApp.Models;
using MathApp.Core;
using MathApp.UI;

namespace MathApp
{
    public partial class Form1 : Form
    {
        // ============ КОМПОНЕНТЫ ИНТЕРФЕЙСА ============
        private Panel leftPanel;
        private Panel workArea;
        private Panel propertiesPanel;
        private FlowLayoutPanel propertiesContent;

        // Левая панель
        private NumericUpDown samplingRateInput;
        private NumericUpDown durationInput;
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

        // Хранилище последних выбранных параметров для каждого блока
        private Dictionary<Guid, List<Control>> blockParameterPanels = new Dictionary<Guid, List<Control>>();

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

            // ============ ЛЕВАЯ ПАНЕЛЬ ============
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
            samplingRateInput = new NumericUpDown
            {
                Location = new Point(200, y),
                Size = new Size(65, 23),
                Minimum = 100,
                Maximum = 100000,
                Value = 10000,
                Font = new Font("Segoe UI", 9)
            };
            leftPanel.Controls.Add(samplingRateInput);
            y += 30;

            leftPanel.Controls.Add(new Label
            {
                Text = "Длительность сигнала, с:",
                Location = new Point(15, y),
                Size = new Size(180, 23),
                Font = new Font("Segoe UI", 9)
            });
            durationInput = new NumericUpDown
            {
                Location = new Point(200, y),
                Size = new Size(65, 23),
                Minimum = 0.001m,
                Maximum = 10m,
                Value = 0.01m,
                DecimalPlaces = 3,
                Increment = 0.001m,
                Font = new Font("Segoe UI", 9)
            };
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

            // ============ ПРАВАЯ ПАНЕЛЬ (параметры блоков) ============
            propertiesPanel = new Panel
            {
                Width = 300,
                Dock = DockStyle.Right,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            propertiesPanel.Controls.Add(new Label
            {
                Text = "Параметры блоков",
                Location = new Point(10, 10),
                Size = new Size(280, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 58, 64)
            });

            propertiesContent = new FlowLayoutPanel
            {
                Location = new Point(10, 45),
                Size = new Size(280, 600),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };
            propertiesPanel.Controls.Add(propertiesContent);

            // ============ РАБОЧАЯ ОБЛАСТЬ ============
            workArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            workArea.Paint += WorkArea_Paint;
            workArea.MouseDown += WorkArea_MouseDown;
            workArea.MouseMove += WorkArea_MouseMove;
            workArea.MouseUp += WorkArea_MouseUp;
            workArea.MouseDoubleClick += WorkArea_MouseDoubleClick;
            workArea.AllowDrop = true;
            workArea.DragEnter += (s, e) => { if (e.Data.GetDataPresent("ToolType")) e.Effect = DragDropEffects.Copy; };
            workArea.DragDrop += WorkArea_DragDrop;

            // Контекстное меню (ПРАВАЯ КНОПКА МЫШИ)
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("🗑 Удалить блок", null, DeleteTool_Click);
            contextMenu.Items.Add("🔗 Удалить соединения", null, DeleteConnections_Click);
            workArea.ContextMenuStrip = contextMenu;

            this.Controls.Add(workArea);
            this.Controls.Add(propertiesPanel);
            this.Controls.Add(leftPanel);
        }

        private void WorkArea_DragDrop(object sender, DragEventArgs e)
        {
            var mousePos = workArea.PointToClient(new Point(e.X, e.Y));
            string toolType = e.Data.GetData("ToolType") as string;
            if (string.IsNullOrEmpty(toolType)) return;

            var tool = new MathTool
            {
                Position = mousePos,
                Size = new Size(120, 60),
                Name = toolType,
                Id = Guid.NewGuid(),
                Rotated = false
            };

            if (toolType.Contains("Генератор"))
            {
                tool.Type = ToolType.Generator;
                tool.Frequency = 10;
                tool.Amplitude = 1;
                tool.DutyCycle = 50;
            }
            else if (toolType.Contains("Усилитель"))
            {
                tool.Type = ToolType.Amplifier;
                tool.Gain = 20;
            }
            else if (toolType.Contains("Антенна"))
            {
                tool.Type = ToolType.Antenna;
                tool.Frequency = 433;
                tool.Gain = 10;
                tool.CustomValueA = 0.1;
                tool.CustomValueB = 70;
            }
            else if (toolType.Contains("Канал"))
            {
                tool.Type = ToolType.Channel;
                tool.CustomValueA = 1000;
                tool.Attenuation = 0.01;
                tool.CustomValueB = 290;
            }
            else if (toolType.Contains("Объект"))
            {
                tool.Type = ToolType.Object;
                tool.TimeConstant = 0.1;
            }
            else if (toolType.Contains("АЦП"))
            {
                tool.Type = ToolType.ADC;
                tool.BitResolution = 10;
                tool.ReferenceVoltage = 5;
            }
            else if (toolType.Contains("График"))
            {
                tool.Type = ToolType.Chart;
                tool.Size = new Size(140, 60);
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

            // Добавляем параметры блока в правую панель (накапливаем)
            AddBlockParametersToPanel(tool);
        }

        private void AddBlockParametersToPanel(MathTool tool)
        {
            int idNumber = tools.IndexOf(tool) + 1;

            // Заголовок блока
            var headerPanel = new Panel
            {
                Size = new Size(260, 35),
                BackColor = Color.FromArgb(240, 242, 245),
                Margin = new Padding(0, 5, 0, 0)
            };
            var headerLabel = new Label
            {
                Text = $"{tool.Name} ID-{idNumber}",
                Location = new Point(5, 8),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 58, 64)
            };
            headerPanel.Controls.Add(headerLabel);
            propertiesContent.Controls.Add(headerPanel);

            // Параметры блока
            if (tool.Type == ToolType.Generator)
            {
                AddPropertyCell(tool, "Частота (Гц):", tool.Frequency.ToString(), (val) => tool.Frequency = double.Parse(val));
                AddPropertyCell(tool, "Амплитуда (В):", tool.Amplitude.ToString(), (val) => tool.Amplitude = double.Parse(val));
                AddPropertyCell(tool, "Скважность (%):", tool.DutyCycle.ToString(), (val) => tool.DutyCycle = double.Parse(val));
            }
            else if (tool.Type == ToolType.Amplifier)
            {
                AddPropertyCell(tool, "Коэффициент усиления:", tool.Gain.ToString(), (val) => tool.Gain = double.Parse(val));
            }
            else if (tool.Type == ToolType.Antenna)
            {
                AddPropertyCell(tool, "Частота (МГц):", tool.Frequency.ToString(), (val) => tool.Frequency = double.Parse(val));
                AddPropertyCell(tool, "Усиление (дБ):", tool.Gain.ToString(), (val) => tool.Gain = double.Parse(val));
                AddPropertyCell(tool, "Эффект. площадь (дБ м²):", tool.CustomValueA.ToString(), (val) => tool.CustomValueA = double.Parse(val));
                AddPropertyCell(tool, "КПД (%):", tool.CustomValueB.ToString(), (val) => tool.CustomValueB = double.Parse(val));
            }
            else if (tool.Type == ToolType.Channel)
            {
                AddPropertyCell(tool, "Расстояние (м):", tool.CustomValueA.ToString(), (val) => tool.CustomValueA = double.Parse(val));
                AddPropertyCell(tool, "Затухание (дБ/м):", tool.Attenuation.ToString(), (val) => tool.Attenuation = double.Parse(val));
                AddPropertyCell(tool, "Темп. шума (К):", tool.CustomValueB.ToString(), (val) => tool.CustomValueB = double.Parse(val));
            }
            else if (tool.Type == ToolType.Object)
            {
                AddPropertyCell(tool, "Постоянная времени (с):", tool.TimeConstant.ToString(), (val) => tool.TimeConstant = double.Parse(val));
            }
            else if (tool.Type == ToolType.ADC)
            {
                AddPropertyCell(tool, "Разрядность (бит):", tool.BitResolution.ToString(), (val) => tool.BitResolution = int.Parse(val));
                AddPropertyCell(tool, "Опорное напряжение (В):", tool.ReferenceVoltage.ToString(), (val) => tool.ReferenceVoltage = double.Parse(val));
            }
            else if (tool.Type == ToolType.Operation)
            {
                AddPropertyCell(tool, "Значение A:", tool.CustomValueA.ToString(), (val) => tool.CustomValueA = double.Parse(val));
                AddPropertyCell(tool, "Значение B:", tool.CustomValueB.ToString(), (val) => tool.CustomValueB = double.Parse(val));
            }
            else if (tool.Type == ToolType.Chart)
            {
                AddPropertyCell(tool, "Точек истории:", tool.MaxHistorySize.ToString(), (val) => tool.MaxHistorySize = int.Parse(val));
            }
        }

        private void AddPropertyCell(MathTool tool, string label, string value, Action<string> setter)
        {
            var panel = new Panel
            {
                Size = new Size(260, 38),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 3),
                Tag = tool.Id
            };

            var lbl = new Label
            {
                Text = label,
                Location = new Point(8, 10),
                Size = new Size(140, 23),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.White
            };

            var txt = new TextBox
            {
                Text = value,
                Location = new Point(150, 8),
                Size = new Size(100, 23),
                Tag = setter
            };

            txt.TextChanged += (s, e) =>
            {
                try { (txt.Tag as Action<string>)?.Invoke(txt.Text); }
                catch { }
                workArea.Invalidate();
            };

            panel.Controls.Add(lbl);
            panel.Controls.Add(txt);
            propertiesContent.Controls.Add(panel);
        }

        private void RemoveBlockParametersFromPanel(Guid blockId)
        {
            var toRemove = propertiesContent.Controls.Cast<Control>()
                .Where(c => c.Tag != null && c.Tag.ToString() == blockId.ToString())
                .ToList();

            foreach (var control in toRemove)
                propertiesContent.Controls.Remove(control);

            // Также удаляем заголовок
            var headers = propertiesContent.Controls.Cast<Control>()
                .Where(c => c is Panel && c.Controls.Count > 0 && c.Controls[0] is Label)
                .ToList();

            foreach (var header in headers)
            {
                var label = header.Controls[0] as Label;
                if (label != null && label.Text.Contains(blockId.ToString().Substring(0, 4)))
                {
                    propertiesContent.Controls.Remove(header);
                    break;
                }
            }
        }

        private void WorkArea_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Рисуем соединения
            foreach (var conn in connections)
            {
                var source = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                var target = tools.FirstOrDefault(t => t.Id == conn.TargetToolId);
                if (source != null && target != null)
                {
                    Point start, end;

                    if (!source.Rotated)
                        start = new Point(source.Position.X + source.Size.Width + 5, source.Position.Y + source.Size.Height / 2);
                    else
                        start = new Point(source.Position.X - 5, source.Position.Y + source.Size.Height / 2);

                    if (!target.Rotated)
                        end = new Point(target.Position.X - 5, target.Position.Y + target.Size.Height / 2);
                    else
                        end = new Point(target.Position.X + target.Size.Width + 5, target.Position.Y + target.Size.Height / 2);

                    using (var pen = new Pen(Color.FromArgb(0, 120, 215), 1.5f))
                    {
                        int offset = Math.Min(40, Math.Abs(end.X - start.X) / 2);
                        Point c1 = new Point(start.X + offset, start.Y);
                        Point c2 = new Point(end.X - offset, end.Y);
                        g.DrawBezier(pen, start, c1, c2, end);
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
                    if (!source.Rotated)
                        start = new Point(source.Position.X + source.Size.Width + 5, source.Position.Y + source.Size.Height / 2);
                    else
                        start = new Point(source.Position.X - 5, source.Position.Y + source.Size.Height / 2);

                    using (var pen = new Pen(Color.Gray, 1f) { DashStyle = DashStyle.Dash })
                    {
                        int offset = Math.Min(40, Math.Abs(tempEnd.X - start.X) / 2);
                        Point c1 = new Point(start.X + offset, start.Y);
                        Point c2 = new Point(tempEnd.X - offset, tempEnd.Y);
                        g.DrawBezier(pen, start, c1, c2, tempEnd);
                    }
                }
            }

            // Рисуем блоки
            foreach (var tool in tools)
            {
                Rectangle rect = new Rectangle(tool.Position, tool.Size);
                bool isSelected = (selectedTool == tool);

                // Тень
                using (var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0)))
                    g.FillRectangle(shadowBrush, rect.X + 2, rect.Y + 2, rect.Width, rect.Height);

                // Фон блока (светло-серый)
                using (var brush = new SolidBrush(Color.FromArgb(245, 245, 245)))
                    g.FillRectangle(brush, rect);

                // Рамка
                using (var pen = new Pen(isSelected ? Color.FromArgb(0, 120, 215) : Color.FromArgb(200, 200, 200), isSelected ? 2 : 1))
                    g.DrawRectangle(pen, rect);

                // Название и ID (всегда сверху, независимо от поворота)
                int idNumber = tools.IndexOf(tool) + 1;
                using (var nameFont = new Font("Segoe UI", 9, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.FromArgb(52, 58, 64)))
                    g.DrawString(tool.Name, nameFont, brush, rect.X + 8, rect.Y + 8);

                using (var idFont = new Font("Segoe UI", 8))
                using (var brush = new SolidBrush(Color.FromArgb(120, 120, 120)))
                    g.DrawString($"ID-{idNumber}", idFont, brush, rect.X + 8, rect.Y + 30);

                // Значение
                if (tool.LastResult.HasValue)
                {
                    using (var valFont = new Font("Segoe UI", 7))
                    using (var brush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                        g.DrawString($"{tool.LastResult.Value:F2}", valFont, brush, rect.X + 8, rect.Bottom - 18);
                }

                // Точки подключения (с учётом поворота)
                if (!tool.Rotated)
                {
                    DrawPort(g, new Point(rect.Right + 5, rect.Y + rect.Height / 2), connections.Any(c => c.SourceToolId == tool.Id), false);
                    DrawPort(g, new Point(rect.X - 5, rect.Y + rect.Height / 2), connections.Any(c => c.TargetToolId == tool.Id), true);
                }
                else
                {
                    // Повёрнутый: выход слева, вход справа
                    DrawPort(g, new Point(rect.X - 5, rect.Y + rect.Height / 2), connections.Any(c => c.SourceToolId == tool.Id), false);
                    DrawPort(g, new Point(rect.Right + 5, rect.Y + rect.Height / 2), connections.Any(c => c.TargetToolId == tool.Id), true);
                }
            }
        }

        private void DrawPort(Graphics g, Point point, bool hasConnection, bool isInput)
        {
            int size = 7;
            Color color = isInput ? Color.FromArgb(40, 167, 69) : Color.FromArgb(255, 140, 0);
            if (hasConnection) color = Color.Gold;

            using (var brush = new SolidBrush(color))
                g.FillEllipse(brush, point.X - size / 2, point.Y - size / 2, size, size);
            using (var pen = new Pen(Color.White, 1))
                g.DrawEllipse(pen, point.X - size / 2, point.Y - size / 2, size, size);
        }

        private void WorkArea_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Проверка на порт
                foreach (var tool in tools)
                {
                    Point output, input;

                    if (!tool.Rotated)
                    {
                        output = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                        input = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                    }
                    else
                    {
                        output = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                        input = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                    }

                    if (Math.Sqrt(Math.Pow(e.X - output.X, 2) + Math.Pow(e.Y - output.Y, 2)) < 10)
                    {
                        isConnecting = true;
                        sourcePoint = new ConnectionPoint(tool.Id, ConnectionPointType.Output);
                        tempEnd = e.Location;
                        workArea.Invalidate();
                        return;
                    }

                    if (Math.Sqrt(Math.Pow(e.X - input.X, 2) + Math.Pow(e.Y - input.Y, 2)) < 10 && isConnecting && sourcePoint.HasValue)
                    {
                        connectionManager.CreateConnection(sourcePoint.Value, new ConnectionPoint(tool.Id, ConnectionPointType.Input, InputType.A));
                        isConnecting = false;
                        sourcePoint = null;
                        workArea.Invalidate();
                        return;
                    }
                }

                // Выбор блока для перетаскивания
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
                    // Поворот блока при двойном клике (название остаётся на месте)
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

        // ============ КОНТЕКСТНОЕ МЕНЮ ============
        private void DeleteTool_Click(object sender, EventArgs e)
        {
            if (selectedTool != null)
            {
                if (selectedTool.Type == ToolType.Chart && graphs.ContainsKey(selectedTool.Id))
                {
                    if (!graphs[selectedTool.Id].IsDisposed) graphs[selectedTool.Id].Close();
                    graphs.Remove(selectedTool.Id);
                }
                connections.RemoveAll(c => c.SourceToolId == selectedTool.Id || c.TargetToolId == selectedTool.Id);
                RemoveBlockParametersFromPanel(selectedTool.Id);
                tools.Remove(selectedTool);
                selectedTool = null;
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

        private void StopSimulation(object sender, EventArgs e)
        {
            isSimulating = false;
            simulationTimer.Stop();
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
            workArea.Invalidate();
        }

        private void SimulationTick(object sender, EventArgs e)
        {
            if (!isSimulating) return;

            simulationTime += 0.001;

            foreach (var tool in tools.Where(t => t.Type == ToolType.Generator))
            {
                double period = 1.0 / tool.Frequency;
                double duty = tool.DutyCycle / 100.0;
                double pulseWidth = period * duty;
                double cyclePos = simulationTime % period;
                tool.LastResult = (cyclePos < pulseWidth) ? tool.Amplitude : 0;
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
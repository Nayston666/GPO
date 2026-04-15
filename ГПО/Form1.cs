using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MathApp.Models;
using MathApp.Core;
using MathApp.Rendering;
using MathApp.UI;
using MathApp.Helpers;

namespace MathApp
{
    public partial class Form1 : Form
    {
        // Компоненты UI
        private DoubleBufferedPanel whiteboardPanel;
        private Panel sidePanel;
        private Label resultLabel;
        private ToolboxControl toolboxControl;
        private PropertyPanel propertyPanel;

        // Данные
        private List<MathTool> whiteboardTools = new List<MathTool>();
        private List<Connection> connections = new List<Connection>();

        // Менеджеры
        private CalculationEngine calculator = new CalculationEngine();
        private ConnectionManager connectionManager;
        private BlockRenderer blockRenderer = new BlockRenderer();

        // Состояние
        private MathTool selectedTool = null;
        private MathTool draggedTool = null;
        private bool isDragging = false;
        private Point dragStartPoint;
        private Point lastMousePosition;

        private bool isConnecting = false;
        private ConnectionPoint? sourceConnectionPoint = null;
        private Point tempConnectionEnd;

        private Timer realTimeTimer;
        private Timer renderTimer;
        private bool needsRedraw = true;
        private double time = 0;

        // Цветовая схема
        private Color primaryColor = Color.FromArgb(0, 120, 212);
        private Color accentColor = Color.FromArgb(0, 200, 255);
        private Color backgroundColor = Color.FromArgb(28, 28, 30);
        private Color panelColor = Color.FromArgb(38, 38, 40);
        private Color textColor = Color.White;

        // Словарь для окон графиков
        private Dictionary<Guid, GraphForm> graphWindows = new Dictionary<Guid, GraphForm>();

        public Form1()
        {
            InitializeComponent();

            connectionManager = new ConnectionManager(connections);

            // Таймер для рендеринга (60 FPS)
            renderTimer = new Timer();
            renderTimer.Interval = 16;
            renderTimer.Tick += (s, e) =>
            {
                if (needsRedraw)
                {
                    whiteboardPanel.Invalidate();
                    needsRedraw = false;
                }
            };
            renderTimer.Start();

            // Таймер для реального времени
            realTimeTimer = new Timer();
            realTimeTimer.Interval = 16;
            realTimeTimer.Tick += RealTimeTimerTick;

            // Двойной клик для открытия подсистемы
            whiteboardPanel.DoubleClick += WhiteboardPanel_DoubleClick;
        }

        private void InitializeComponent()
        {
            this.Text = "Математический конструктор";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = backgroundColor;
            this.DoubleBuffered = true;

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);

            // ============ БОКОВАЯ ПАНЕЛЬ ============
            sidePanel = new Panel
            {
                Width = 300,
                Height = this.ClientSize.Height,
                Location = new Point(0, 0),
                BackColor = panelColor,
                BorderStyle = BorderStyle.None
            };

            // Тень для боковой панели
            sidePanel.Paint += (s, e) =>
            {
                using (var shadow = new LinearGradientBrush(
                    new Rectangle(sidePanel.Width - 10, 0, 10, sidePanel.Height),
                    Color.FromArgb(50, 0, 0, 0),
                    Color.Transparent,
                    LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(shadow, sidePanel.Width - 10, 0, 10, sidePanel.Height);
                }
            };

            // Контент боковой панели с прокруткой
            var contentPanel = new Panel
            {
                Width = 280,
                Height = 800,
                Location = new Point(10, 0),
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            // ============ ПАНЕЛЬ ИНСТРУМЕНТОВ ============
            toolboxControl = new ToolboxControl
            {
                Location = new Point(0, 10)
            };
            toolboxControl.ItemMouseDown += ToolboxControl_ItemMouseDown;

            // ============ ПАНЕЛЬ РЕАЛЬНОГО ВРЕМЕНИ ============
            var realTimePanel = CreateSimplePanel("⚡ РЕЖИМ РЕАЛЬНОГО ВРЕМЕНИ", new Point(0, 230));

            var chkRealTime = new CheckBox
            {
                Text = "Включить",
                Location = new Point(15, 35),
                Size = new Size(100, 25),
                ForeColor = textColor,
                Checked = false,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat
            };

            var btnStart = CreateSimpleButton("▶ Старт", new Point(120, 35), primaryColor);
            var btnStop = CreateSimpleButton("⏹ Стоп", new Point(200, 35), Color.FromArgb(200, 70, 70));

            btnStart.Enabled = false;
            btnStop.Enabled = false;

            chkRealTime.CheckedChanged += (s, e) =>
            {
                btnStart.Enabled = chkRealTime.Checked;
                btnStop.Enabled = false;
            };

            btnStart.Click += (s, e) =>
            {
                realTimeTimer.Start();
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            };

            btnStop.Click += (s, e) =>
            {
                realTimeTimer.Stop();
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            };

            realTimePanel.Controls.AddRange(new Control[] { chkRealTime, btnStart, btnStop });

            // ============ ПАНЕЛЬ СВОЙСТВ ============
            propertyPanel = new PropertyPanel
            {
                Location = new Point(0, 320)
            };
            propertyPanel.ApplyClicked += PropertyPanel_ApplyClicked;

            // ============ ПАНЕЛЬ ИНФОРМАЦИИ ============
            var infoPanel = CreateSimplePanel("ℹ️ ИНФОРМАЦИЯ", new Point(0, 660));

            var infoLabel = new Label
            {
                Text = "• Клик на блок - редактирование\r\n• Оранж. точка - выход\r\n• Зел. точка - вход\r\n• Для соединения: выход → вход\r\n• ПКМ на блоке - удалить\r\n• Двойной клик по подсистеме - открыть",
                Location = new Point(15, 35),
                Size = new Size(250, 120),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.Transparent
            };
            infoPanel.Controls.Add(infoLabel);

            // ============ ПАНЕЛЬ РЕЗУЛЬТАТА ============
            var resultPanel = new Panel
            {
                Location = new Point(10, 770),
                Size = new Size(260, 70),
                BackColor = Color.FromArgb(45, 45, 50)
            };

            resultLabel = new Label
            {
                Text = "0.00",
                Location = new Point(10, 25),
                Size = new Size(240, 40),
                ForeColor = Color.FromArgb(0, 255, 128),
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };

            var resultTitle = new Label
            {
                Text = "РЕЗУЛЬТАТ",
                Location = new Point(10, 5),
                Size = new Size(100, 20),
                ForeColor = accentColor,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            resultPanel.Controls.AddRange(new Control[] { resultTitle, resultLabel });

            // Добавляем все на боковую панель
            contentPanel.Controls.AddRange(new Control[] {
                toolboxControl, realTimePanel, propertyPanel, infoPanel
            });

            sidePanel.Controls.Add(contentPanel);
            sidePanel.Controls.Add(resultPanel);

            // ============ РАБОЧАЯ ОБЛАСТЬ ============
            whiteboardPanel = new DoubleBufferedPanel
            {
                Location = new Point(310, 10),
                Size = new Size(this.ClientSize.Width - 320, this.ClientSize.Height - 20),
                BackColor = Color.FromArgb(30, 30, 35),
                BorderStyle = BorderStyle.None,
                AllowDrop = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Подписка на события
            whiteboardPanel.Paint += WhiteboardPanel_Paint;
            whiteboardPanel.MouseDown += WhiteboardPanel_MouseDown;
            whiteboardPanel.MouseMove += WhiteboardPanel_MouseMove;
            whiteboardPanel.MouseUp += WhiteboardPanel_MouseUp;
            whiteboardPanel.DragEnter += WhiteboardPanel_DragEnter;
            whiteboardPanel.DragDrop += WhiteboardPanel_DragDrop;
            whiteboardPanel.Scroll += (s, e) => needsRedraw = true;

            // Контекстное меню
            var contextMenu = new ContextMenuStrip();
            contextMenu.BackColor = Color.FromArgb(45, 45, 50);
            contextMenu.ForeColor = textColor;
            contextMenu.Items.Add("🗑️ Удалить блок", null, DeleteTool_Click);
            contextMenu.Items.Add("🔗 Удалить соединения", null, DeleteConnections_Click);
            contextMenu.Items.Add("🧹 Очистить всё", null, ClearAll_Click);
            whiteboardPanel.ContextMenuStrip = contextMenu;

            // Добавляем контролы на форму
            this.Controls.Add(whiteboardPanel);
            this.Controls.Add(sidePanel);
        }

        // ============ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ============

        private Panel CreateSimplePanel(string title, Point location)
        {
            var panel = new Panel
            {
                Location = location,
                Size = new Size(260, 80),
                BackColor = Color.Transparent
            };

            var titleLabel = new Label
            {
                Text = title,
                Location = new Point(5, 0),
                Size = new Size(250, 20),
                ForeColor = accentColor,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            panel.Controls.Add(titleLabel);

            panel.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 20, panel.Width, panel.Height - 21);
                using (var brush = new SolidBrush(Color.FromArgb(45, 45, 50)))
                using (var path = GraphicsExtensions.CreateRoundedRectangle(rect, 8))
                {
                    e.Graphics.FillPath(brush, path);
                }
            };

            return panel;
        }

        private Button CreateSimpleButton(string text, Point location, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(70, 25),
                BackColor = color,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ============ ОБРАБОТЧИКИ СОБЫТИЙ ============

        private void ToolboxControl_ItemMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && toolboxControl.GetSelectedItem() != null)
            {
                DoDragDrop(new DataObject("MathTool", toolboxControl.GetSelectedItem()), DragDropEffects.Copy);
            }
        }

        private void WhiteboardPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("MathTool"))
                e.Effect = DragDropEffects.Copy;
        }

        private void WhiteboardPanel_DragDrop(object sender, DragEventArgs e)
        {
            var mousePos = whiteboardPanel.PointToClient(new Point(e.X, e.Y));
            var realPos = whiteboardPanel.GetRealMouseLocation(mousePos);

            var type = e.Data.GetData("MathTool") as string;

            var tool = new MathTool { Position = realPos };

            if (type.Contains("График"))
            {
                tool.Type = ToolType.Chart;
                tool.Size = new Size(160, 80);
                tool.Name = $"График {whiteboardTools.Count + 1}";

                // Создаём и показываем окно графика
                var graph = new GraphForm(tool.Name);
                graph.Show();
                graphWindows[tool.Id] = graph;
            }
            else if (type.Contains("Синусоида"))
            {
                tool.Type = ToolType.SineGenerator;
                tool.Size = new Size(160, 100);
                tool.Name = "Синусоида";
                tool.Frequency = 1.0;
                tool.Amplitude = 1.0;
                tool.Phase = 0;
            }
            else if (type.Contains("Подсистема"))
            {
                tool.Type = ToolType.SubSystem;
                tool.Size = new Size(180, 120);
                tool.Name = $"Подсистема {whiteboardTools.Count + 1}";

                // Создаем пустые данные для подсхемы
                tool.SubSystemData = new SubSystemData
                {
                    Name = tool.Name,
                    InternalTools = new List<MathTool>(),
                    InternalConnections = new List<Connection>(),
                    InputPorts = new List<SubSystemPort>(),
                    OutputPorts = new List<SubSystemPort>()
                };
            }
            else  // Математические операции
            {
                tool.Type = ToolType.Operation;
                tool.Size = new Size(140, 80);

                if (type.Contains("Сложение"))
                {
                    tool.Operation = MathOperation.Addition;
                    tool.Name = "Сложение";
                }
                else if (type.Contains("Вычитание"))
                {
                    tool.Operation = MathOperation.Subtraction;
                    tool.Name = "Вычитание";
                }
                else if (type.Contains("Умножение"))
                {
                    tool.Operation = MathOperation.Multiplication;
                    tool.Name = "Умножение";
                }
                else if (type.Contains("Деление"))
                {
                    tool.Operation = MathOperation.Division;
                    tool.Name = "Деление";
                }
            }

            whiteboardTools.Add(tool);
            needsRedraw = true;
        }

        private void WhiteboardPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Рисуем сетку
            GridRenderer.Draw(g, whiteboardPanel);

            // Рисуем соединения
            foreach (var conn in connections)
            {
                var source = whiteboardTools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                var target = whiteboardTools.FirstOrDefault(t => t.Id == conn.TargetToolId);

                if (source != null && target != null)
                {
                    ConnectionRenderer.Draw(g, conn, source, target);
                }
            }

            // Рисуем временное соединение
            if (isConnecting && sourceConnectionPoint.HasValue)
            {
                var sourceTool = whiteboardTools.FirstOrDefault(t =>
                    t.Id == sourceConnectionPoint.Value.ToolId);
                if (sourceTool != null)
                {
                    ConnectionRenderer.DrawTemp(g, sourceTool, tempConnectionEnd);
                }
            }

            // Рисуем блоки
            foreach (var tool in whiteboardTools)
            {
                if (tool.Type == ToolType.Chart)
                    blockRenderer.DrawChartTool(g, tool, selectedTool);
                else if (tool.Type == ToolType.SineGenerator)
                    blockRenderer.DrawSineTool(g, tool, selectedTool, time);
                else if (tool.Type == ToolType.SubSystem)
                    blockRenderer.DrawSubSystemTool(g, tool, selectedTool);
                else
                    blockRenderer.DrawMathTool(g, tool, selectedTool);

                blockRenderer.DrawConnectionPoints(g, tool, connections);
            }
        }

        private void WhiteboardPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var realMousePos = whiteboardPanel.GetRealMouseLocation(e.Location);

                var hitPoint = HitTestConnectionPoint(e.Location);

                if (hitPoint.HasValue)
                {
                    var hit = hitPoint.Value;
                    if (hit.Type == ConnectionPointType.Output)
                    {
                        isConnecting = true;
                        sourceConnectionPoint = hit;
                        tempConnectionEnd = e.Location;
                        needsRedraw = true;
                    }
                    else if (hit.Type == ConnectionPointType.Input &&
                             isConnecting && sourceConnectionPoint.HasValue)
                    {
                        connectionManager.CreateConnection(sourceConnectionPoint.Value, hit);
                        isConnecting = false;
                        sourceConnectionPoint = null;
                        needsRedraw = true;
                    }
                }
                else
                {
                    draggedTool = GetToolAtPosition(realMousePos);
                    if (draggedTool != null)
                    {
                        SelectTool(draggedTool);
                        isDragging = true;
                        dragStartPoint = new Point(
                            realMousePos.X - draggedTool.Position.X,
                            realMousePos.Y - draggedTool.Position.Y
                        );
                        lastMousePosition = realMousePos;
                        needsRedraw = true;
                    }
                    else
                    {
                        SelectTool(null);
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                isConnecting = false;
                sourceConnectionPoint = null;
                needsRedraw = true;
            }
        }

        private void WhiteboardPanel_MouseMove(object sender, MouseEventArgs e)
        {
            var realMousePos = whiteboardPanel.GetRealMouseLocation(e.Location);

            if (isDragging && draggedTool != null)
            {
                lastMousePosition = realMousePos;
                draggedTool.Position = new Point(
                    realMousePos.X - dragStartPoint.X,
                    realMousePos.Y - dragStartPoint.Y
                );
                needsRedraw = true;
            }
            else if (isConnecting)
            {
                tempConnectionEnd = e.Location;
                needsRedraw = true;
            }
        }

        private void WhiteboardPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isDragging)
            {
                isDragging = false;
                draggedTool = null;
                needsRedraw = true;
            }
        }

        private void WhiteboardPanel_DoubleClick(object sender, EventArgs e)
        {
            var mousePos = whiteboardPanel.PointToClient(MousePosition);
            var realPos = whiteboardPanel.GetRealMouseLocation(mousePos);
            var tool = GetToolAtPosition(realPos);

            if (tool != null)
            {
                if (tool.Type == ToolType.SubSystem)
                {
                    // Если у подсистемы нет данных, создаём пустые
                    if (tool.SubSystemData == null)
                    {
                        tool.SubSystemData = new SubSystemData
                        {
                            Name = tool.Name,
                            InternalTools = new List<MathTool>(),
                            InternalConnections = new List<Connection>(),
                            InputPorts = new List<SubSystemPort>(),
                            OutputPorts = new List<SubSystemPort>()
                        };
                    }

                    var form = new SubSystemForm(tool.SubSystemData);
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        tool.SubSystemData = form.GetResult();
                        tool.Name = tool.SubSystemData.Name;
                        // Обновляем размер блока подсистемы
                        tool.Size = blockRenderer.CalculateSubSystemSize(tool.SubSystemData);
                        needsRedraw = true;
                    }
                }
                else if (tool.Type == ToolType.Chart)
                {
                    // Открываем окно графика
                    if (!graphWindows.ContainsKey(tool.Id) || graphWindows[tool.Id].IsDisposed)
                    {
                        var graph = new GraphForm(tool.Name);
                        graph.Show();
                        graphWindows[tool.Id] = graph;
                    }
                    else
                    {
                        // Если уже открыто, просто активируем
                        graphWindows[tool.Id].Activate();
                    }
                }
            }
        }

        private ConnectionPoint? HitTestConnectionPoint(Point mousePos)
        {
            var realPoint = whiteboardPanel.GetRealMouseLocation(mousePos);

            foreach (var tool in whiteboardTools)
            {
                // Для подсистемы - проверяем все порты
                if (tool.Type == ToolType.SubSystem && tool.SubSystemData != null)
                {
                    int inputCount = tool.SubSystemData.InputPorts?.Count ?? 0;
                    int outputCount = tool.SubSystemData.OutputPorts?.Count ?? 0;

                    // Проверяем входные порты подсистемы
                    for (int i = 0; i < inputCount; i++)
                    {
                        int yOffset = 35 + i * 20;
                        Point inputPoint = new Point(
                            tool.Position.X - 5,
                            tool.Position.Y + yOffset
                        );
                        if (Distance(realPoint, inputPoint) < 12)
                        {
                            return new ConnectionPoint
                            {
                                ToolId = tool.Id,
                                Type = ConnectionPointType.Input,
                                InputType = InputType.A
                            };
                        }
                    }

                    // Проверяем выходные порты подсистемы 
                    for (int i = 0; i < outputCount; i++)
                    {
                        int yOffset = 35 + i * 20;
                        Point outputPoint = new Point(
                            tool.Position.X + tool.Size.Width + 5,
                            tool.Position.Y + yOffset
                        );
                        if (Distance(realPoint, outputPoint) < 12)
                        {
                            return new ConnectionPoint
                            {
                                ToolId = tool.Id,
                                Type = ConnectionPointType.Output
                            };
                        }
                    }
                }

                // Проверяем выходную точку обычного блока
                if (tool.Type != ToolType.Chart && tool.Type != ToolType.SubSystem)
                {
                    var output = GetOutputPoint(tool);
                    if (Distance(realPoint, output) < 12)
                    {
                        return new ConnectionPoint
                        {
                            ToolId = tool.Id,
                            Type = ConnectionPointType.Output
                        };
                    }
                }

                // Проверяем входные точки обычных блоков
                if (tool.Type == ToolType.Operation)
                {
                    var inputA = GetInputPoint(tool, InputType.A);
                    if (Distance(realPoint, inputA) < 12)
                    {
                        return new ConnectionPoint
                        {
                            ToolId = tool.Id,
                            Type = ConnectionPointType.Input,
                            InputType = InputType.A
                        };
                    }

                    var inputB = GetInputPoint(tool, InputType.B);
                    if (Distance(realPoint, inputB) < 12)
                    {
                        return new ConnectionPoint
                        {
                            ToolId = tool.Id,
                            Type = ConnectionPointType.Input,
                            InputType = InputType.B
                        };
                    }
                }
                else if (tool.Type == ToolType.Chart || tool.Type == ToolType.SineGenerator)
                {
                    var input = GetInputPoint(tool, InputType.A);
                    if (Distance(realPoint, input) < 12)
                    {
                        return new ConnectionPoint
                        {
                            ToolId = tool.Id,
                            Type = ConnectionPointType.Input,
                            InputType = InputType.A
                        };
                    }
                }
            }

            return null;
        }

        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        private Point GetOutputPoint(MathTool tool)
        {
            if (tool.Type == ToolType.SubSystem && tool.SubSystemData != null)
            {
                // Для подсистемы возвращаем позицию первого выходного порта
                 return new Point(
                    tool.Position.X + tool.Size.Width + 5,
                    tool.Position.Y + tool.Size.Height / 2
                );
            }

            return new Point(
                tool.Position.X + tool.Size.Width + 5,
                tool.Position.Y + tool.Size.Height / 2
            );
        }

        private Point GetInputPoint(MathTool tool, InputType input)
        {
            if (tool.Type == ToolType.SubSystem && tool.SubSystemData != null)
            {
                // Для подсистемы возвращаем позицию первого входного порта
                return new Point(
                    tool.Position.X - 5,
                    tool.Position.Y + tool.Size.Height / 2
                );
            }

            if (tool.Type == ToolType.Chart || tool.Type == ToolType.SineGenerator)
            {
                return new Point(
                    tool.Position.X - 5,
                    tool.Position.Y + tool.Size.Height / 2
                );
            }

            if (input == InputType.A)
            {
                return new Point(tool.Position.X - 5, tool.Position.Y + 20);
            }
            else
            {
                return new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height - 20);
            }
        }

        private MathTool GetToolAtPosition(Point point)
        {
            foreach (var tool in whiteboardTools)
            {
                var rect = new Rectangle(tool.Position, tool.Size);
                if (rect.Contains(point))
                    return tool;
            }
            return null;
        }

        private void SelectTool(MathTool tool)
        {
            selectedTool = tool;
            propertyPanel.SetSelectedTool(tool);
            needsRedraw = true;
        }

        private void PropertyPanel_ApplyClicked(object sender, EventArgs e)
        {
            propertyPanel.ApplyChanges(selectedTool);
            needsRedraw = true;
        }

        private void RealTimeTimerTick(object sender, EventArgs e)
        {
            time += 0.05;

            var results = calculator.CalculateAll(whiteboardTools, connections);

            // Обновляем графики
            foreach (var chart in whiteboardTools.Where(t => t.Type == ToolType.Chart))
            {
                var conn = connections.FirstOrDefault(c => c.TargetToolId == chart.Id);
                if (conn != null && graphWindows.ContainsKey(chart.Id) &&
                    !graphWindows[chart.Id].IsDisposed)
                {
                    if (results.ContainsKey(conn.SourceToolId))
                    {
                        graphWindows[chart.Id].AddValue(results[conn.SourceToolId]);
                    }
                    else
                    {
                        var source = whiteboardTools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                        if (source != null && source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
                        {
                            graphWindows[chart.Id].AddValue(source.LastResult.Value);
                        }
                        // Добавляем поддержку подсистемы
                        else if (source != null && source.Type == ToolType.SubSystem && source.LastResult.HasValue)
                        {
                            graphWindows[chart.Id].AddValue(source.LastResult.Value);
                        }
                    }
                }
            }

            // Обновляем результат на главной форме 
            var lastTool = whiteboardTools
                .Where(t => t.Type == ToolType.Operation || t.Type == ToolType.SubSystem)
                .OrderByDescending(t => t.Position.X)
                .FirstOrDefault();

            if (lastTool != null && lastTool.LastResult.HasValue)
            {
                resultLabel.Text = lastTool.LastResult.Value.ToString("F2");
            }

            needsRedraw = true;
        }

        private void DeleteTool_Click(object sender, EventArgs e)
        {
            var mousePos = whiteboardPanel.PointToClient(MousePosition);
            var realPos = whiteboardPanel.GetRealMouseLocation(mousePos);
            var tool = GetToolAtPosition(realPos);

            if (tool != null)
            {
                if (selectedTool == tool)
                    SelectTool(null);

                if (tool.Type == ToolType.Chart && graphWindows.ContainsKey(tool.Id))
                {
                    if (!graphWindows[tool.Id].IsDisposed)
                    {
                        graphWindows[tool.Id].Close();
                    }
                    graphWindows.Remove(tool.Id);
                }

                connectionManager.RemoveConnectionsForTool(tool.Id);
                whiteboardTools.Remove(tool);
                needsRedraw = true;
            }
        }

        private void DeleteConnections_Click(object sender, EventArgs e)
        {
            var mousePos = whiteboardPanel.PointToClient(MousePosition);
            var realPos = whiteboardPanel.GetRealMouseLocation(mousePos);
            var tool = GetToolAtPosition(realPos);

            if (tool != null)
            {
                connectionManager.RemoveIncomingConnections(tool.Id);
                needsRedraw = true;
            }
        }

        private void ClearAll_Click(object sender, EventArgs e)
        {
            foreach (var graph in graphWindows.Values)
            {
                if (!graph.IsDisposed)
                    graph.Close();
            }
            graphWindows.Clear();

            whiteboardTools.Clear();
            connections.Clear();
            SelectTool(null);
            resultLabel.Text = "0.00";
            needsRedraw = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (whiteboardPanel != null)
            {
                whiteboardPanel.Size = new Size(
                    this.ClientSize.Width - 320,
                    this.ClientSize.Height - 20
                );
                needsRedraw = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (renderTimer != null)
                {
                    renderTimer.Stop();
                    renderTimer.Dispose();
                }
                if (realTimeTimer != null)
                {
                    realTimeTimer.Stop();
                    realTimeTimer.Dispose();
                }

                foreach (var graph in graphWindows.Values)
                {
                    if (!graph.IsDisposed)
                        graph.Close();
                }
            }
            base.Dispose(disposing);
        }
    }
}
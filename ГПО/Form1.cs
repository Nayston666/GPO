using MathApp.Core;
using MathApp.Helpers;
using MathApp.Models;
using MathApp.Rendering;
using MathApp.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;


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

        private Timer renderTimer;
        private bool needsRedraw = true;

        // Цветовая схема
        private Color primaryColor = Color.FromArgb(0, 120, 212);
        private Color accentColor = Color.FromArgb(0, 200, 255);
        private Color backgroundColor = Color.FromArgb(28, 28, 30);
        private Color panelColor = Color.FromArgb(38, 38, 40);
        private Color textColor = Color.White;

        // Словарь для окон графиков
        private Dictionary<Guid, GraphForm> graphWindows = new Dictionary<Guid, GraphForm>();

        // Окно результата
        private ResultForm _resultForm;

        public Form1()
        {
            // Настройка формы
            this.Text = "Математический конструктор";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = backgroundColor;
            this.DoubleBuffered = true;

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);

            InitializeComponent();

            connectionManager = new ConnectionManager(connections);

            // Создаем окно результата
            _resultForm = new ResultForm();

            // Таймер для рендеринга (60 FPS)
            renderTimer = new Timer();
            renderTimer.Interval = 16; // 60 FPS
            renderTimer.Tick += (s, e) =>
            {
                needsRedraw = true;
                whiteboardPanel.Invalidate();
            };
            renderTimer.Start();
        }

        private void InitializeComponent()
        {
            // ============ БОКОВАЯ ПАНЕЛЬ ============
            sidePanel = new Panel
            {
                Width = 300,
                Dock = DockStyle.Left,
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
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom
            };

            // ============ ПАНЕЛЬ ИНСТРУМЕНТОВ ============
            toolboxControl = new ToolboxControl
            {
                Location = new Point(0, 10),
                Width = 280
            };
            toolboxControl.ItemMouseDown += ToolboxControl_ItemMouseDown;

            // ============ ПАНЕЛЬ СВОЙСТВ ============
            propertyPanel = new PropertyPanel
            {
                Location = new Point(0, 200),
                Width = 280
            };
            propertyPanel.ApplyClicked += PropertyPanel_ApplyClicked;

            // ============ ПАНЕЛЬ ИНФОРМАЦИИ ============
            var infoPanel = new Panel
            {
                Location = new Point(0, 500),
                Size = new Size(260, 120),
                BackColor = Color.FromArgb(45, 45, 50)
            };

            // Закругленные углы для информационной панели
            infoPanel.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, infoPanel.Width - 1, infoPanel.Height - 1);
                using (var path = GraphicsExtensions.CreateRoundedRectangle(rect, 8))
                {
                    using (var brush = new SolidBrush(Color.FromArgb(45, 45, 50)))
                        e.Graphics.FillPath(brush, path);
                    using (var pen = new Pen(Color.FromArgb(60, 60, 65), 1))
                        e.Graphics.DrawPath(pen, path);
                }
            };

            var infoTitle = new Label
            {
                Text = "ℹ️ ИНФОРМАЦИЯ",
                Location = new Point(10, 5),
                Size = new Size(240, 20),
                ForeColor = accentColor,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            var infoLabel = new Label
            {
                Text = "• Клик на блок - редактирование\r\n• Оранж. точка - выход\r\n• Зел. точка - вход\r\n• Для соединения: выход → вход\r\n• ПКМ на блоке - удалить\r\n• Кнопка 'Вычислить' - расчет",
                Location = new Point(10, 30),
                Size = new Size(240, 85),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 8),
                BackColor = Color.Transparent
            };

            infoPanel.Controls.AddRange(new Control[] { infoTitle, infoLabel });

            // ============ ПАНЕЛЬ РЕЗУЛЬТАТА ============
            var resultPanel = new Panel
            {
                Location = new Point(0, 630),
                Size = new Size(260, 100),
                BackColor = Color.FromArgb(45, 45, 50)
            };

            // Закругленные углы для панели результата
            resultPanel.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, resultPanel.Width - 1, resultPanel.Height - 1);
                using (var path = GraphicsExtensions.CreateRoundedRectangle(rect, 8))
                {
                    using (var brush = new SolidBrush(Color.FromArgb(45, 45, 50)))
                        e.Graphics.FillPath(brush, path);
                    using (var pen = new Pen(Color.FromArgb(60, 60, 65), 1))
                        e.Graphics.DrawPath(pen, path);
                }
            };

            var resultTitle = new Label
            {
                Text = "ТЕКУЩИЙ РЕЗУЛЬТАТ",
                Location = new Point(10, 5),
                Size = new Size(240, 20),
                ForeColor = accentColor,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            resultLabel = new Label
            {
                Text = "0.00",
                Location = new Point(10, 28),
                Size = new Size(240, 35),
                ForeColor = Color.FromArgb(0, 255, 128),
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };

            // Кнопка вычисления
            var btnCalculate = new Button
            {
                Text = "🧮 ВЫЧИСЛИТЬ",
                Location = new Point(10, 68),
                Size = new Size(150, 28),
                BackColor = primaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCalculate.Click += BtnCalculate_Click;

            // Кнопка для показа отдельного окна результата
            var btnShowResult = new Button
            {
                Text = "📊 ОКНО",
                Location = new Point(165, 68),
                Size = new Size(85, 28),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnShowResult.Click += (s, e) => ShowResultWindow();

            resultPanel.Controls.AddRange(new Control[] {
                resultTitle, resultLabel, btnCalculate, btnShowResult
            });

            // Добавляем все на боковую панель
            contentPanel.Controls.AddRange(new Control[] {
                toolboxControl, propertyPanel, infoPanel, resultPanel
            });

            sidePanel.Controls.Add(contentPanel);

            // ============ РАБОЧАЯ ОБЛАСТЬ ============
            whiteboardPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 35),
                BorderStyle = BorderStyle.None,
                AllowDrop = true
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
            else if (type.Contains("Интегратор"))
            {
                tool.Type = ToolType.Operation;
                tool.Operation = MathOperation.Integrator;
                tool.Size = new Size(140, 100);
                tool.Name = "Интегратор";
                tool.IntegralValue = 0;
                tool.PreviousInput = 0;
                tool.StepSize = 0.01;
            }
            else if (type.Contains("Дифференциатор"))
            {
                tool.Type = ToolType.Operation;
                tool.Operation = MathOperation.Differentiator;
                tool.Size = new Size(140, 100);
                tool.Name = "Дифференциатор";
                tool.PreviousTime = 0;
                tool.PreviousOutput = 0;
            }
            else if (type.Contains("Интерполятор"))
            {
                tool.Type = ToolType.Operation;
                tool.Operation = MathOperation.Interpolator;
                tool.Size = new Size(140, 100);
                tool.Name = "Интерполятор";
                tool.InterpolationPoints = new List<PointF>();
                for (int i = 0; i <= 10; i++)
                {
                    float x = i / 2f;
                    float y = (float)Math.Sin(x);
                    tool.InterpolationPoints.Add(new PointF(x, y));
                }
            }
            else
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
                    blockRenderer.DrawSineTool(g, tool, selectedTool, 0);
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

        private ConnectionPoint? HitTestConnectionPoint(Point mousePos)
        {
            var realPoint = whiteboardPanel.GetRealMouseLocation(mousePos);

            foreach (var tool in whiteboardTools)
            {
                if (tool.Type != ToolType.Chart)
                {
                    var output = GetOutputPoint(tool);
                    if (Distance(realPoint, output) < 10)
                    {
                        return new ConnectionPoint
                        {
                            ToolId = tool.Id,
                            Type = ConnectionPointType.Output
                        };
                    }
                }

                if (tool.Type == ToolType.Operation)
                {
                    var inputA = GetInputPoint(tool, InputType.A);
                    if (Distance(realPoint, inputA) < 10)
                    {
                        return new ConnectionPoint
                        {
                            ToolId = tool.Id,
                            Type = ConnectionPointType.Input,
                            InputType = InputType.A
                        };
                    }

                    var inputB = GetInputPoint(tool, InputType.B);
                    if (Distance(realPoint, inputB) < 10)
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
                    if (Distance(realPoint, input) < 10)
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
            return new Point(
                tool.Position.X + tool.Size.Width + 5,
                tool.Position.Y + tool.Size.Height / 2
            );
        }

        private Point GetInputPoint(MathTool tool, InputType input)
        {
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

        private void BtnCalculate_Click(object sender, EventArgs e)
        {
            try
            {
                var results = calculator.CalculateAll(whiteboardTools, connections);

                var lastTool = whiteboardTools
                    .Where(t => t.Type == ToolType.Operation)
                    .OrderByDescending(t => t.Position.X)
                    .FirstOrDefault();

                if (lastTool != null && lastTool.LastResult.HasValue)
                {
                    double value = lastTool.LastResult.Value;
                    resultLabel.Text = value.ToString("F2");

                    if (_resultForm != null && _resultForm.Visible)
                    {
                        _resultForm.SetValueImmediate(value);
                    }
                }

                UpdateGraphsBatch();
                needsRedraw = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при вычислении: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateGraphsBatch()
        {
            foreach (var chart in whiteboardTools.Where(t => t.Type == ToolType.Chart))
            {
                var conn = connections.FirstOrDefault(c => c.TargetToolId == chart.Id);
                if (conn == null) continue;

                var source = whiteboardTools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                if (source == null) continue;

                double start = 0, end = 10;
                int points = 300;

                if (source.Type == ToolType.SineGenerator)
                {
                    double period = 2 * Math.PI / source.Frequency;
                    end = 3 * period;
                }

                Func<double, double> calcFunc = (x) =>
                {
                    if (source.Type == ToolType.SineGenerator)
                    {
                        return source.Amplitude * Math.Sin(2 * Math.PI * source.Frequency * x + source.Phase * Math.PI / 180.0);
                    }
                    return source.LastResult ?? 0;
                };

                GraphForm graphForm;
                if (graphWindows.ContainsKey(chart.Id) && !graphWindows[chart.Id].IsDisposed)
                {
                    graphForm = graphWindows[chart.Id];
                }
                else
                {
                    graphForm = new GraphForm(chart.Name);
                    graphWindows[chart.Id] = graphForm;
                }

                graphForm.SetCalculationFunction(calcFunc, start, end, points);
                graphForm.Show();
            }
        }

        private void ShowResultWindow()
        {
            if (_resultForm == null || _resultForm.IsDisposed)
            {
                _resultForm = new ResultForm();
            }

            _resultForm.Show();
            _resultForm.Focus();

            var lastTool = whiteboardTools
                .Where(t => t.Type == ToolType.Operation)
                .OrderByDescending(t => t.Position.X)
                .FirstOrDefault();

            if (lastTool != null && lastTool.LastResult.HasValue)
            {
                _resultForm.SetValueImmediate(lastTool.LastResult.Value);
            }
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
                        graphWindows[tool.Id].Close();
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
                if (!graph.IsDisposed) graph.Close();
            }
            graphWindows.Clear();

            whiteboardTools.Clear();
            connections.Clear();
            SelectTool(null);
            resultLabel.Text = "0.00";
            needsRedraw = true;
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

                foreach (var graph in graphWindows.Values)
                {
                    if (!graph.IsDisposed) graph.Close();
                }

                if (_resultForm != null && !_resultForm.IsDisposed)
                {
                    _resultForm.Close();
                    _resultForm.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
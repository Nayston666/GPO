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
        private DoubleBufferedPanel whiteboardPanel;
        private Panel sidePanel;
        private Label resultLabel;
        private ToolboxControl toolboxControl;
        private PropertyPanel propertyPanel;

        private List<MathTool> whiteboardTools = new List<MathTool>();
        private List<Connection> connections = new List<Connection>();

        private CalculationEngine calculator = new CalculationEngine();
        private ConnectionManager connectionManager;
        private BlockRenderer blockRenderer = new BlockRenderer();
        private BatchProcessor _batchProcessor;

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

        private Color primaryColor = Color.FromArgb(0, 120, 212);
        private Color accentColor = Color.FromArgb(0, 200, 255);
        private Color backgroundColor = Color.FromArgb(28, 28, 30);
        private Color panelColor = Color.FromArgb(38, 38, 40);
        private Color textColor = Color.White;

        private Dictionary<Guid, GraphForm> graphWindows = new Dictionary<Guid, GraphForm>();
        private ResultForm _resultForm;

        public Form1()
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

            InitializeComponent();

            connectionManager = new ConnectionManager(connections);
            _resultForm = new ResultForm();
            _batchProcessor = new BatchProcessor(calculator, whiteboardTools, connections);

            renderTimer = new Timer();
            renderTimer.Interval = 16;
            renderTimer.Tick += (s, e) =>
            {
                needsRedraw = true;
                whiteboardPanel.Invalidate();
            };
            renderTimer.Start();
        }

        private void InitializeComponent()
        {
            sidePanel = new Panel
            {
                Width = 300,
                Dock = DockStyle.Left,
                BackColor = panelColor,
                BorderStyle = BorderStyle.None
            };

            var contentPanel = new Panel
            {
                Width = 280,
                Height = 800,
                Location = new Point(10, 0),
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            toolboxControl = new ToolboxControl
            {
                Location = new Point(0, 10),
                Width = 280
            };
            toolboxControl.ItemMouseDown += ToolboxControl_ItemMouseDown;

            propertyPanel = new PropertyPanel
            {
                Location = new Point(0, 80),
                Width = 280
            };
            propertyPanel.ApplyClicked += PropertyPanel_ApplyClicked;

            var infoPanel = new Panel
            {
                Location = new Point(0, 400),
                Size = new Size(260, 120),
                BackColor = Color.FromArgb(45, 45, 50)
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
                Text = "• Клик на блок - редактирование\r\n• Оранж. точка - выход\r\n• Зел. точка - вход\r\n• Для соединения: выход → вход\r\n• ПКМ на блоке - удалить",
                Location = new Point(10, 30),
                Size = new Size(240, 85),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 8),
                BackColor = Color.Transparent
            };

            infoPanel.Controls.AddRange(new Control[] { infoTitle, infoLabel });

            var resultPanel = new Panel
            {
                Location = new Point(0, 530),
                Size = new Size(260, 100),
                BackColor = Color.FromArgb(45, 45, 50)
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

            resultPanel.Controls.AddRange(new Control[] { resultTitle, resultLabel, btnCalculate, btnShowResult });

            contentPanel.Controls.AddRange(new Control[] {
                toolboxControl, propertyPanel, infoPanel, resultPanel
            });
            sidePanel.Controls.Add(contentPanel);

            whiteboardPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 35),
                BorderStyle = BorderStyle.None,
                AllowDrop = true
            };

            whiteboardPanel.Paint += WhiteboardPanel_Paint;
            whiteboardPanel.MouseDown += WhiteboardPanel_MouseDown;
            whiteboardPanel.MouseMove += WhiteboardPanel_MouseMove;
            whiteboardPanel.MouseUp += WhiteboardPanel_MouseUp;
            whiteboardPanel.DragEnter += (s, e) => { if (e.Data.GetDataPresent("MathTool")) e.Effect = DragDropEffects.Copy; };
            whiteboardPanel.DragDrop += WhiteboardPanel_DragDrop;
            whiteboardPanel.Scroll += (s, e) => needsRedraw = true;

            var contextMenu = new ContextMenuStrip();
            contextMenu.BackColor = Color.FromArgb(45, 45, 50);
            contextMenu.ForeColor = textColor;
            contextMenu.Items.Add("🗑️ Удалить блок", null, DeleteTool_Click);
            contextMenu.Items.Add("🔗 Удалить соединения", null, DeleteConnections_Click);
            contextMenu.Items.Add("🧹 Очистить всё", null, ClearAll_Click);
            whiteboardPanel.ContextMenuStrip = contextMenu;

            this.Controls.Add(whiteboardPanel);
            this.Controls.Add(sidePanel);
        }

        private void ToolboxControl_ItemMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && toolboxControl.GetSelectedItem() != null)
                DoDragDrop(new DataObject("MathTool", toolboxControl.GetSelectedItem()), DragDropEffects.Copy);
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

                var graphForm = new GraphForm(tool.Name);
                graphForm.Show();
                graphWindows[tool.Id] = graphForm;
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
            else if (type.Contains("Файловый"))
            {
                tool.Type = ToolType.Operation;
                tool.Operation = MathOperation.FileIO;
                tool.Size = new Size(160, 100);
                tool.Name = "Файловый ввод/вывод";
                tool.IsReading = true;
                tool.FileData = new List<double>();
            }
            else if (type.Contains("Ступенька"))
            {
                tool.Type = ToolType.StepGenerator;
                tool.Operation = MathOperation.StepGenerator;
                tool.Size = new Size(160, 100);
                tool.Name = "Ступенька";
                tool.StepAmplitude = 0.1;           // Амплитуда 0.1
                tool.StepDelay = 0.0;               // Без задержки
                tool.StepRiseTime = 0.0;            // Мгновенный фронт
                tool.StepDuration = 5e-7;           // 500 наносекунд = 0.0000005
                tool.StepOffset = 0.0;              // Смещение 0
                tool.StepPoints = 160;              // 160 точек
                tool.StepTimeEnd = 1e-6;            // 1 микросекунда = 0.000001
            }
            else
            {
                tool.Type = ToolType.Operation;
                tool.Size = new Size(140, 80);
                if (type.Contains("Сложение")) tool.Operation = MathOperation.Addition;
                else if (type.Contains("Вычитание")) tool.Operation = MathOperation.Subtraction;
                else if (type.Contains("Умножение")) tool.Operation = MathOperation.Multiplication;
                else if (type.Contains("Деление")) tool.Operation = MathOperation.Division;
            }

            whiteboardTools.Add(tool);
            needsRedraw = true;
        }

        private void WhiteboardPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            GridRenderer.Draw(g, whiteboardPanel);

            foreach (var conn in connections)
            {
                var source = whiteboardTools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                var target = whiteboardTools.FirstOrDefault(t => t.Id == conn.TargetToolId);
                if (source != null && target != null)
                    ConnectionRenderer.Draw(g, conn, source, target);
            }

            if (isConnecting && sourceConnectionPoint.HasValue)
            {
                var sourceTool = whiteboardTools.FirstOrDefault(t => t.Id == sourceConnectionPoint.Value.ToolId);
                if (sourceTool != null)
                    ConnectionRenderer.DrawTemp(g, sourceTool, tempConnectionEnd);
            }

            foreach (var tool in whiteboardTools)
            {
                if (tool.Type == ToolType.Chart)
                    blockRenderer.DrawChartTool(g, tool, selectedTool);
                else if (tool.Type == ToolType.SineGenerator)
                    blockRenderer.DrawSineTool(g, tool, selectedTool, 0);
                else if (tool.Type == ToolType.StepGenerator)
                    blockRenderer.DrawMathTool(g, tool, selectedTool);
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
                    else if (hit.Type == ConnectionPointType.Input && isConnecting && sourceConnectionPoint.HasValue)
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
                        dragStartPoint = new Point(realMousePos.X - draggedTool.Position.X, realMousePos.Y - draggedTool.Position.Y);
                        lastMousePosition = realMousePos;
                        needsRedraw = true;
                    }
                    else SelectTool(null);
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
                draggedTool.Position = new Point(realMousePos.X - dragStartPoint.X, realMousePos.Y - dragStartPoint.Y);
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
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Output };
                }

                if (tool.Type == ToolType.Operation)
                {
                    var inputA = GetInputPoint(tool, InputType.A);
                    if (Distance(realPoint, inputA) < 10)
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Input, InputType = InputType.A };

                    var inputB = GetInputPoint(tool, InputType.B);
                    if (Distance(realPoint, inputB) < 10)
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Input, InputType = InputType.B };
                }
                else if (tool.Type == ToolType.Chart)
                {
                    var input = GetInputPoint(tool, InputType.A);
                    if (Distance(realPoint, input) < 10)
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Input, InputType = InputType.A };
                }
            }
            return null;
        }

        private double Distance(Point p1, Point p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));

        private Point GetOutputPoint(MathTool tool) => new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);

        private Point GetInputPoint(MathTool tool, InputType input)
        {
            if (tool.Type == ToolType.SineGenerator) return Point.Empty;
            if (tool.Type == ToolType.Chart) return new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
            return input == InputType.A ? new Point(tool.Position.X - 5, tool.Position.Y + 20) : new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height - 20);
        }

        private MathTool GetToolAtPosition(Point point)
        {
            foreach (var tool in whiteboardTools)
                if (new Rectangle(tool.Position, tool.Size).Contains(point))
                    return tool;
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

        /// <summary>
        /// Вычисляет значение ступенчатого генератора в заданный момент времени
        /// </summary>
        private double CalculateStepValue(MathTool tool, double time)
        {
            double offset = tool.StepOffset;
            double amplitude = tool.StepAmplitude;
            double delay = tool.StepDelay;
            double riseTime = tool.StepRiseTime;
            double duration = tool.StepDuration;

            if (time < delay)
                return offset;

            if (riseTime > 0 && time < delay + riseTime)
            {
                double ratio = (time - delay) / riseTime;
                return offset + amplitude * ratio;
            }

            if (duration <= 0 || time < delay + riseTime + duration)
                return offset + amplitude;

            return offset;
        }

        private void UpdateGraphsBatch()
        {
            foreach (var chart in whiteboardTools.Where(t => t.Type == ToolType.Chart))
            {
                var conn = connections.FirstOrDefault(c => c.TargetToolId == chart.Id);

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

                if (conn != null)
                {
                    var source = whiteboardTools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                    if (source != null)
                    {
                        // Для ступенчатого генератора
                        if (source.Type == ToolType.StepGenerator)
                        {
                            var timeValues = new List<double>();
                            var dataValues = new List<double>();

                            int points = source.StepPoints;
                            double endTime = source.StepTimeEnd;
                            double step = endTime / points;

                            for (int i = 0; i <= points; i++)
                            {
                                double t = i * step;
                                timeValues.Add(t);
                                dataValues.Add(CalculateStepValue(source, t));
                            }

                            graphForm.SetDataDirectly(timeValues, dataValues);
                        }
                        else if (source.Type == ToolType.Operation && source.Operation == MathOperation.FileIO && source.IsReading)
                        {
                            var timeValues = new List<double>();
                            var dataValues = new List<double>();

                            for (int i = 0; i < source.FileData.Count; i++)
                            {
                                timeValues.Add(i);
                                dataValues.Add(source.FileData[i]);
                            }

                            graphForm.SetDataDirectly(timeValues, dataValues);
                        }
                        else if (source.Type == ToolType.SineGenerator)
                        {
                            double start = 0;
                            double period = 2 * Math.PI / source.Frequency;
                            double end = 5 * period;
                            int points = 5000;

                            Func<double, double> calcFunc = (x) =>
                            {
                                return source.Amplitude * Math.Sin(2 * Math.PI * source.Frequency * x + source.Phase * Math.PI / 180.0);
                            };

                            graphForm.SetCalculationFunction(calcFunc, start, end, points);
                        }
                        else
                        {
                            double start = 0, end = 10;
                            int points = 5000;

                            Func<double, double> calcFunc = (x) =>
                            {
                                return source.LastResult ?? 0;
                            };

                            graphForm.SetCalculationFunction(calcFunc, start, end, points);
                        }
                    }
                }

                graphForm.Show();
            }
        }

        private void UpdateGraphsFromBatch()
        {
            var results = _batchProcessor.GetResults();

            foreach (var chart in whiteboardTools.Where(t => t.Type == ToolType.Chart))
            {
                var conn = connections.FirstOrDefault(c => c.TargetToolId == chart.Id);
                if (conn != null && graphWindows.ContainsKey(chart.Id) && !graphWindows[chart.Id].IsDisposed)
                {
                    var timeValues = new List<double>();
                    for (int i = 0; i < results.Count; i++)
                        timeValues.Add(i * 0.01);

                    graphWindows[chart.Id].SetDataDirectly(timeValues, results);
                }
            }
        }

        private void BtnCalculate_Click(object sender, EventArgs e)
        {
            try
            {
                // Проверяем наличие файловых блоков или генератора ступеньки
                var fileSources = whiteboardTools.Where(t =>
                    t.Type == ToolType.Operation && t.Operation == MathOperation.FileIO && t.IsReading && t.FileData.Count > 0)
                    .ToList();

                var stepGenerator = whiteboardTools.FirstOrDefault(t =>
                    t.Type == ToolType.StepGenerator);

                if (fileSources.Count > 0 || stepGenerator != null)
                {
                    // Пакетная обработка
                    _batchProcessor.ProcessBatch();
                    var results = _batchProcessor.GetResults();

                    // Обновляем графики
                    foreach (var chart in whiteboardTools.Where(t => t.Type == ToolType.Chart))
                    {
                        var conn = connections.FirstOrDefault(c => c.TargetToolId == chart.Id);
                        if (conn != null && graphWindows.ContainsKey(chart.Id) && !graphWindows[chart.Id].IsDisposed)
                        {
                            var timeValues = new List<double>();
                            double timeStep = 0.01;

                            if (stepGenerator != null)
                            {
                                timeStep = stepGenerator.StepTimeEnd / stepGenerator.StepPoints;
                            }

                            for (int i = 0; i < results.Count; i++)
                                timeValues.Add(i * timeStep);

                            graphWindows[chart.Id].SetDataDirectly(timeValues, results);
                        }
                    }

                    if (results.Count > 0)
                    {
                        resultLabel.Text = $"Обработано {results.Count} значений. Последнее: {results.Last():E4}";
                        if (_resultForm != null && _resultForm.Visible)
                            _resultForm.SetValueImmediate(results.Last());
                    }
                }
                else
                {
                    // Обычный разовый расчёт
                    var results = calculator.CalculateAll(whiteboardTools, connections);

                    var lastTool = whiteboardTools.Where(t => t.Type == ToolType.Operation)
                                                  .OrderByDescending(t => t.Position.X)
                                                  .FirstOrDefault();

                    if (lastTool != null && lastTool.LastResult.HasValue)
                    {
                        resultLabel.Text = lastTool.LastResult.Value.ToString("F2");
                        if (_resultForm != null && _resultForm.Visible)
                            _resultForm.SetValueImmediate(lastTool.LastResult.Value);
                    }

                    UpdateGraphsBatch();
                }

                needsRedraw = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowResultWindow()
        {
            if (_resultForm == null || _resultForm.IsDisposed) _resultForm = new ResultForm();
            _resultForm.Show();
            _resultForm.Focus();

            var lastTool = whiteboardTools.Where(t => t.Type == ToolType.Operation).OrderByDescending(t => t.Position.X).FirstOrDefault();
            if (lastTool != null && lastTool.LastResult.HasValue) _resultForm.SetValueImmediate(lastTool.LastResult.Value);
        }

        public void RefreshGraphs()
        {
            UpdateGraphsBatch();
        }

        private void DeleteTool_Click(object sender, EventArgs e)
        {
            var mousePos = whiteboardPanel.PointToClient(MousePosition);
            var realPos = whiteboardPanel.GetRealMouseLocation(mousePos);
            var tool = GetToolAtPosition(realPos);

            if (tool != null)
            {
                if (selectedTool == tool) SelectTool(null);
                if (tool.Type == ToolType.Chart && graphWindows.ContainsKey(tool.Id))
                {
                    if (!graphWindows[tool.Id].IsDisposed) graphWindows[tool.Id].Close();
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
            foreach (var graph in graphWindows.Values) if (!graph.IsDisposed) graph.Close();
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
                whiteboardPanel.Size = new Size(this.ClientSize.Width - 300, this.ClientSize.Height - 20);
                needsRedraw = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                renderTimer?.Stop();
                renderTimer?.Dispose();
                foreach (var graph in graphWindows.Values) if (!graph.IsDisposed) graph.Close();
                _resultForm?.Close();
                _resultForm?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
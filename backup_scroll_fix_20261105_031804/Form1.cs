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
        // ============ КОМПОНЕНТЫ ИНТЕРФЕЙСА ============
        private Panel leftPanel;
        private Panel workArea;
        private Panel propertiesPanel;
        private FlowLayoutPanel propertiesContent;

        private TextBox samplingRateInput;
        private TextBox durationInput;
        private Button runButton;
        private Button clearButton;
        private Button allGraphsButton;
        private TreeView libraryPanel;

        // ============ ДАННЫЕ ============
        private List<MathTool> whiteboardTools = new List<MathTool>();
        private List<Connection> connections = new List<Connection>();
        private CalculationEngine calculator = new CalculationEngine();
        private ConnectionManager connectionManager;
        private BlockRenderer blockRenderer = new BlockRenderer();

        private MathTool selectedTool = null;
        private MathTool draggedTool = null;
        private MathTool contextMenuTool = null;
        private bool isDragging = false;
        private Point dragStartPoint;

        private bool isConnecting = false;
        private ConnectionPoint? sourceConnectionPoint = null;
        private Point tempConnectionEnd;

        private Dictionary<Guid, GraphForm> graphWindows = new Dictionary<Guid, GraphForm>();
        private PropertyPanel propertyPanel;
        private Timer renderTimer;
        private bool needsRedraw = true;

        private static int _nextBlockId = 1;

        public Form1()
        {
            connectionManager = new ConnectionManager(connections);
            InitializeForm();
            SetupTimers();
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
            leftPanel.Controls.Add(new Label { Text = "УПРАВЛЕНИЕ СИСТЕМОЙ", Location = new Point(15, y), Size = new Size(250, 25), Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(52, 58, 64) });
            y += 35;
            runButton = new Button { Text = "ЗАПУСТИТЬ СИСТЕМУ", Location = new Point(15, y), Size = new Size(250, 40), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            runButton.Click += RunSimulation;
            leftPanel.Controls.Add(runButton);
            y += 55;

            leftPanel.Controls.Add(new Label { Text = "Глобальные параметры системы", Location = new Point(15, y), Size = new Size(250, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(52, 58, 64) });
            y += 30;
            leftPanel.Controls.Add(new Label { Text = "Частота дискретизации, Гц:", Location = new Point(15, y), Size = new Size(180, 23), Font = new Font("Segoe UI", 9) });
            samplingRateInput = new TextBox { Location = new Point(200, y), Size = new Size(65, 23), Text = "10000", Font = new Font("Segoe UI", 9) };
            samplingRateInput.KeyPress += (s, e) => { if (e.KeyChar == ',') e.Handled = true; };
            leftPanel.Controls.Add(samplingRateInput);
            y += 30;
            leftPanel.Controls.Add(new Label { Text = "Длительность окна, с:", Location = new Point(15, y), Size = new Size(180, 23), Font = new Font("Segoe UI", 9) });
            durationInput = new TextBox { Location = new Point(200, y), Size = new Size(65, 23), Text = "0.01", Font = new Font("Segoe UI", 9) };
            durationInput.KeyPress += (s, e) => { if (e.KeyChar == ',') e.Handled = true; };
            leftPanel.Controls.Add(durationInput);
            y += 45;

            allGraphsButton = new Button
            {
                Text = "ВСЕ ГРАФИКИ",
                Location = new Point(15, y),
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            allGraphsButton.Click += ShowAllGraphs;
            leftPanel.Controls.Add(allGraphsButton);
            y += 55;

            leftPanel.Controls.Add(new Label { Text = "Библиотека блоков", Location = new Point(15, y), Size = new Size(250, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(52, 58, 64) });
            y += 30;
            libraryPanel = BlockLibraryTree.Create();
            libraryPanel.Dock = DockStyle.None;
            libraryPanel.Location = new Point(15, y);
            libraryPanel.Size = new Size(250, 300);
            leftPanel.Controls.Add(libraryPanel);
            y += 310;
            clearButton = new Button { Text = "ОЧИСТИТЬ ВСЁ", Location = new Point(15, y), Size = new Size(250, 35), BackColor = Color.FromArgb(108, 117, 125), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            clearButton.Click += ClearAll;
            leftPanel.Controls.Add(clearButton);

            // Правая панель свойств
            propertiesPanel = new Panel { Width = 320, Dock = DockStyle.Right, BackColor = Color.FromArgb(248, 249, 250), BorderStyle = BorderStyle.FixedSingle };
            propertiesPanel.Controls.Add(new Label { Text = "ПАРАМЕТРЫ БЛОКОВ", Location = new Point(10, 10), Size = new Size(300, 30), Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(52, 58, 64) });
            propertiesContent = new FlowLayoutPanel { Location = new Point(10, 45), Width = 300, Height = propertiesPanel.ClientSize.Height - 55, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            propertiesPanel.Controls.Add(propertiesContent);

            propertyPanel = new PropertyPanel { Width = 280, Visible = false };
            propertyPanel.ParametersChanged += (s, e) => { needsRedraw = true; };
            propertiesContent.Controls.Add(propertyPanel);

            propertiesPanel.Resize += (s, e) => propertiesContent.Height = propertiesPanel.ClientSize.Height - 55;
            this.Controls.Add(propertiesPanel);
            this.Controls.Add(leftPanel);

            // Рабочая область
            workArea = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            workArea.Paint += WorkArea_Paint;
            workArea.MouseDown += WorkArea_MouseDown;
            workArea.MouseMove += WorkArea_MouseMove;
            workArea.MouseUp += WorkArea_MouseUp;
            workArea.MouseDoubleClick += WorkArea_MouseDoubleClick;
            workArea.AllowDrop = true;
            workArea.DragEnter += (s, e) => { if (e.Data.GetDataPresent("ToolType")) e.Effect = DragDropEffects.Copy; };
            workArea.DragDrop += WorkArea_DragDrop;

            var contextMenu = new ContextMenuStrip();
            contextMenu.BackColor = Color.FromArgb(45, 45, 50);
            contextMenu.ForeColor = Color.White;
            contextMenu.Items.Add("🔄 Отзеркалить блок", null, FlipTool_Click);
            contextMenu.Items.Add("🗑️ Удалить блок", null, DeleteTool_Click);
            contextMenu.Items.Add("🔗 Удалить соединения", null, DeleteConnections_Click);
            contextMenu.Items.Add("🧹 Очистить всё", null, ClearAll_Click);


            workArea.ContextMenuStrip = contextMenu;

            this.Controls.Add(workArea);
        }

        private void SetupTimers()
        {
            renderTimer = new Timer { Interval = 16 };
            renderTimer.Tick += (s, e) => { if (needsRedraw) { workArea.Invalidate(); needsRedraw = false; } };
            renderTimer.Start();
        }

        private void WorkArea_DragDrop(object sender, DragEventArgs e)
        {
            var mousePos = workArea.PointToClient(new Point(e.X, e.Y));
            var realPos = ((DoubleBufferedPanel)workArea).GetRealMouseLocation(mousePos);
            string toolType = e.Data.GetData("ToolType") as string;
            if (string.IsNullOrEmpty(toolType)) return;

            MathTool tool = null;
            if (toolType.Contains("Генератор синусоиды"))
                tool = new MathTool { Position = realPos, Type = ToolType.Generator, Waveform = GeneratorWaveform.Sine, Size = new Size(160, 100), Name = "Синусоида", Frequency = 1.0, Amplitude = 1.0, Phase = 0 };
            else if (toolType.Contains("Трапециевидный"))
                tool = new MathTool { Position = realPos, Type = ToolType.Generator, Waveform = GeneratorWaveform.Trapezoid, Size = new Size(160, 100), Name = "Трапеция", Frequency = 1.0, Amplitude = 1.0, RiseTime = 0.1, FallTime = 0.1 };
            else if (toolType.Contains("Меандр"))
                tool = new MathTool { Position = realPos, Type = ToolType.Generator, Waveform = GeneratorWaveform.Square, Size = new Size(160, 100), Name = "Меандр", Frequency = 1.0, Amplitude = 1.0, RiseTime = 0.01, FallTime = 0.01 };
            else if (toolType.Contains("Треугольный"))
                tool = new MathTool { Position = realPos, Type = ToolType.Generator, Waveform = GeneratorWaveform.Sawtooth, Size = new Size(160, 100), Name = "Треугольный импульс", Frequency = 1.0, Amplitude = 1.0, RiseTime = 0.5, FallTime = 0.5 };
            else if (toolType.Contains("Единичный"))
                tool = new MathTool
                {
                    Position = realPos,
                    Type = ToolType.Generator,
                    Waveform = GeneratorWaveform.Step,
                    Size = new Size(160, 100),
                    Name = "Единичный сигнал",
                    Amplitude = 1.0,
                    StepDuration = 0.001,
                    RiseTime = 0.0001,
                    FallTime = 0.0001,
                    Frequency = 1.0
                };
            else if (toolType.Contains("Сложение"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Addition, Size = new Size(140, 80), Name = "Сложение" };
            else if (toolType.Contains("Вычитание"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Subtraction, Size = new Size(140, 80), Name = "Вычитание" };
            else if (toolType.Contains("Умножение"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Multiplication, Size = new Size(140, 80), Name = "Умножение" };
            else if (toolType.Contains("Деление"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Division, Size = new Size(140, 80), Name = "Деление" };
            else if (toolType.Contains("Интегратор"))
            {
                double samplingRate = 10000;
                double.TryParse(samplingRateInput.Text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out samplingRate);
                double dt = 1.0 / samplingRate;

                tool = new MathTool
                {
                    Position = realPos,
                    Type = ToolType.Operation,
                    Operation = MathOperation.Integrator,
                    Size = new Size(140, 100),
                    Name = "Интегратор",
                    IntegralValue = 0,
                    PreviousInput = 0,
                    StepSize = dt
                };
            }
            else if (toolType.Contains("Дифференциатор"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Differentiator, Size = new Size(140, 100), Name = "Дифференциатор", PreviousTime = 0, PreviousOutput = 0 };
            else if (toolType.Contains("Интерполятор"))
            {
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Interpolator, Size = new Size(140, 100), Name = "Интерполятор", InterpolationPoints = new List<PointF>() };
                for (int i = 0; i <= 10; i++) tool.InterpolationPoints.Add(new PointF(i / 2f, (float)Math.Sin(i / 2f)));
            }
            else if (toolType.Contains("Нелинейный фильтр"))
            {
                tool = new MathTool
                {
                    Position = realPos,
                    Type = ToolType.NonlinearFirstOrderFilter,
                    Size = new Size(170, 100),
                    Name = "Нелинейный фильтр 1 порядка",
                    FilterKVH = new List<FilterPoint>(),
                    FilterVAH = new List<FilterPoint>()
                };
            }
            else if (toolType.Contains("Обратная связь"))
                tool = new MathTool { Position = realPos, Type = ToolType.FeedbackDelay, Size = new Size(150, 80), Name = "Обратная связь", CustomValueA = 0.0 };
            else if (toolType.Contains("Файловый"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.FileIO, Size = new Size(160, 100), Name = "Файловый ввод/вывод", IsReading = true, FileData = new List<double>() };
            else if (toolType.Contains("График"))
                tool = new MathTool { Position = realPos, Type = ToolType.Chart, Size = new Size(160, 80), Name = $"График {whiteboardTools.Count + 1}" };
            else if (toolType.Contains("Подсистема"))
                tool = new MathTool { Position = realPos, Type = ToolType.SubSystem, Size = new Size(180, 120), Name = $"Подсистема {whiteboardTools.Count + 1}", SubSystemData = new SubSystemData { Name = $"Подсистема {whiteboardTools.Count + 1}", InternalTools = new List<MathTool>(), InternalConnections = new List<Connection>(), InputPorts = new List<SubSystemPort>(), OutputPorts = new List<SubSystemPort>() } };
            else if (toolType.Contains("Усилитель"))
                tool = new MathTool { Position = realPos, Type = ToolType.Amplifier, Size = new Size(80, 80), Name = "Усилитель", Gain = 10 };
            else if (toolType.Contains("Антенна"))
                tool = new MathTool { Position = realPos, Type = ToolType.Antenna, Size = new Size(100, 80), Name = "Антенна", Gain = 10, EffectiveArea = 0.1 };
            else if (toolType.Contains("Канал"))
                tool = new MathTool { Position = realPos, Type = ToolType.Channel, Size = new Size(100, 80), Name = "Канал", Distance = 1000, Attenuation = 0.01 };
            else if (toolType.Contains("Объект"))
                tool = new MathTool { Position = realPos, Type = ToolType.Object, Size = new Size(100, 80), Name = "Объект", RadarCrossSection = 1, TimeConstant = 1.0 };
            else if (toolType.Contains("АЦП"))
                tool = new MathTool { Position = realPos, Type = ToolType.ADC, Size = new Size(100, 80), Name = "АЦП", BitResolution = 12, SamplingRate = 10000, QuantizationStep = 0.001, DynamicRange = 120 };

            if (tool != null)
            {
                tool.BlockId = _nextBlockId++;
                whiteboardTools.Add(tool);
                needsRedraw = true;
            }
        }

        // ============ ОТРИСОВКА ============
        private void WorkArea_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            GridRenderer.Draw(g, (Panel)workArea);

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
                else if (tool.Type == ToolType.Generator)
                    blockRenderer.DrawSineTool(g, tool, selectedTool, 0);
                else if (tool.Type == ToolType.SubSystem)
                    blockRenderer.DrawSubSystemTool(g, tool, selectedTool);
                else if (tool.Type == ToolType.Amplifier)
                    blockRenderer.DrawAmplifierTool(g, tool, selectedTool);
                else if (tool.Type == ToolType.Antenna)
                    blockRenderer.DrawAntennaTool(g, tool, selectedTool);
                else if (tool.Type == ToolType.Channel)
                    blockRenderer.DrawChannelTool(g, tool, selectedTool);
                else if (tool.Type == ToolType.Object)
                    blockRenderer.DrawObjectTool(g, tool, selectedTool);
                else if (tool.Type == ToolType.ADC)
                    blockRenderer.DrawADCTool(g, tool, selectedTool);
                else
                    blockRenderer.DrawMathTool(g, tool, selectedTool);

                blockRenderer.DrawConnectionPoints(g, tool, connections);
            }
        }

        // ============ ОБРАБОТКА МЫШИ ============
        private void WorkArea_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var hit = HitTestConnectionPoint(e.Location);
                if (hit.HasValue)
                {
                    if (hit.Value.Type == ConnectionPointType.Output)
                    {
                        isConnecting = true;
                        sourceConnectionPoint = hit;
                        tempConnectionEnd = e.Location;
                        needsRedraw = true;
                    }
                    else if (hit.Value.Type == ConnectionPointType.Input && isConnecting && sourceConnectionPoint.HasValue)
                    {
                        connectionManager.CreateConnection(sourceConnectionPoint.Value, hit.Value);
                        isConnecting = false;
                        sourceConnectionPoint = null;
                        needsRedraw = true;
                    }
                }
                else
                {
                    var realPos = ((DoubleBufferedPanel)workArea).GetRealMouseLocation(e.Location);
                    draggedTool = GetToolAtPosition(realPos);
                    if (draggedTool != null)
                    {
                        SelectTool(draggedTool);
                        isDragging = true;
                        dragStartPoint = new Point(realPos.X - draggedTool.Position.X, realPos.Y - draggedTool.Position.Y);
                        needsRedraw = true;
                    }
                    else SelectTool(null);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                var realPos =
                    ((DoubleBufferedPanel)workArea)
                    .GetRealMouseLocation(e.Location);

                contextMenuTool =
                    GetToolAtPosition(realPos);

                isConnecting = false;
                sourceConnectionPoint = null;
                needsRedraw = true;
            }
        }

        private void WorkArea_MouseMove(object sender, MouseEventArgs e)
        {
            var realPos = ((DoubleBufferedPanel)workArea).GetRealMouseLocation(e.Location);
            if (isDragging && draggedTool != null)
            {
                draggedTool.Position = new Point(realPos.X - dragStartPoint.X, realPos.Y - dragStartPoint.Y);
                needsRedraw = true;
            }
            else if (isConnecting)
            {
                tempConnectionEnd = e.Location;
                needsRedraw = true;
            }
        }

        private void WorkArea_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                draggedTool = null;
                needsRedraw = true;
            }
        }

        private void ShowAllGraphs(object sender, EventArgs e)
        {
            var charts = whiteboardTools.Where(t => t.Type == ToolType.Chart).ToList();
            if (charts.Count == 0)
            {
                MessageBox.Show("Нет графиков на схеме", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var graphsWindow = new AllGraphsWindow(charts, graphWindows);

            double samplingRate = 10000;
            EngineeringParser.TryParse(samplingRateInput.Text, out samplingRate);
            if (samplingRate <= 0) samplingRate = 10000;
            graphsWindow.SetTimeStep(1.0 / samplingRate);

            graphsWindow.Show();
        }

        private void WorkArea_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var realPos = ((DoubleBufferedPanel)workArea).GetRealMouseLocation(e.Location);
            var tool = GetToolAtPosition(realPos);
            if (tool != null)
            {
                if (tool.Type == ToolType.SubSystem)
                {
                    if (tool.SubSystemData == null) tool.SubSystemData = new SubSystemData();
                    var form = new SubSystemForm(tool.SubSystemData);
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        tool.SubSystemData = form.GetResult();
                        tool.Name = tool.SubSystemData.Name;
                        tool.Size = blockRenderer.CalculateSubSystemSize(tool.SubSystemData);
                        needsRedraw = true;
                    }
                }
                else if (tool.Type == ToolType.Chart)
                {
                    if (!graphWindows.ContainsKey(tool.Id) || graphWindows[tool.Id].IsDisposed)
                    {
                        var graph = new GraphForm(tool.Name);
                        graph.Show();
                        graphWindows[tool.Id] = graph;
                    }
                    else graphWindows[tool.Id].Activate();
                }
            }
        }

        private ConnectionPoint? HitTestConnectionPoint(Point mousePos)
        {
            var realPoint = ((DoubleBufferedPanel)workArea).GetRealMouseLocation(mousePos);
            foreach (var tool in whiteboardTools)
            {
                if (tool.Type == ToolType.SubSystem && tool.SubSystemData != null)
                {
                    int inCnt = tool.SubSystemData.InputPorts?.Count ?? 0;
                    int outCnt = tool.SubSystemData.OutputPorts?.Count ?? 0;
                    for (int i = 0; i < inCnt; i++)
                    {
                        Point pt = blockRenderer.GetInputPoint(tool, InputType.A, i);
                        if (Distance(realPoint, pt) < 12)
                            return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Input, InputType = null, PortIndex = i };
                    }
                    for (int i = 0; i < outCnt; i++)
                    {
                        Point pt = blockRenderer.GetOutputPoint(tool, i);
                        if (Distance(realPoint, pt) < 12)
                            return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Output, PortIndex = i };
                    }
                }

                if (tool.Type != ToolType.Chart)
                {
                    Point outPt = blockRenderer.GetOutputPoint(tool);
                    if (Distance(realPoint, outPt) < 12)
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Output };
                }

                if (tool.Type == ToolType.Operation)
                {
                    Point inA = blockRenderer.GetInputPoint(tool, InputType.A);
                    Point inB = blockRenderer.GetInputPoint(tool, InputType.B);
                    if (Distance(realPoint, inA) < 12)
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Input, InputType = InputType.A };
                    if (Distance(realPoint, inB) < 12)
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Input, InputType = InputType.B };
                }
                else if (tool.Type == ToolType.Chart)
                {
                    Point inPt = blockRenderer.GetInputPoint(tool, InputType.A);
                    if (Distance(realPoint, inPt) < 12)
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Input, InputType = InputType.A };
                }
                else if (tool.Type != ToolType.Generator)
                {
                    Point inPt = blockRenderer.GetInputPoint(tool, InputType.A);
                    if (Distance(realPoint, inPt) < 12)
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Input, InputType = InputType.A };
                }
            }
            return null;
        }

        private MathTool GetToolAtPosition(Point point)
        {
            foreach (var tool in whiteboardTools)
                if (new Rectangle(tool.Position, tool.Size).Contains(point))
                    return tool;
            return null;
        }

        private double Distance(Point p1, Point p2) => Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));

        private void SelectTool(MathTool tool)
        {
            selectedTool = tool;
            if (tool != null)
            {
                propertyPanel.SetSelectedTool(tool);
                propertyPanel.Visible = true;
            }
            else propertyPanel.Visible = false;
            needsRedraw = true;
        }

        // ============ КОНТЕКСТНОЕ МЕНЮ ============
        private void FlipTool_Click(object sender, EventArgs e)
        {
            var tool = contextMenuTool;

            if (tool != null)
            {
                tool.Flipped = !tool.Flipped;
                needsRedraw = true;
            }
        }

        private void DeleteTool_Click(object sender, EventArgs e)
        {
            var tool = contextMenuTool;

            if (tool != null)
            {
                if (selectedTool == tool)
                    SelectTool(null);

                if (tool.Type == ToolType.Chart &&
                    graphWindows.ContainsKey(tool.Id))
                {
                    if (!graphWindows[tool.Id].IsDisposed)
                        graphWindows[tool.Id].Close();

                    graphWindows.Remove(tool.Id);
                }

                connectionManager.RemoveConnectionsForTool(tool.Id);
                whiteboardTools.Remove(tool);

                int newId = 1;

                foreach (var t in whiteboardTools.OrderBy(t => t.BlockId))
                    t.BlockId = newId++;

                _nextBlockId = newId;

                needsRedraw = true;
            }
        }

        private void DeleteConnections_Click(object sender, EventArgs e)
        {
            var tool = contextMenuTool;

            if (tool == null)
                return;

            int removed = connections.RemoveAll(
                c =>
                    c.SourceToolId == tool.Id ||
                    c.TargetToolId == tool.Id);

            // If a temporary wire was started from this block, cancel it too.
            if (isConnecting &&
                sourceConnectionPoint.HasValue &&
                sourceConnectionPoint.Value.ToolId == tool.Id)
            {
                isConnecting = false;
                sourceConnectionPoint = null;
            }

            needsRedraw = true;
            workArea.Invalidate();
            workArea.Update();

            System.Diagnostics.Debug.WriteLine(
                $"[Connections] Removed {removed} connection(s) for block #{tool.BlockId}");
        }
        private void ClearAll(object sender, EventArgs e)
        {
            foreach (var g in graphWindows.Values)
                if (!g.IsDisposed) g.Close();
            graphWindows.Clear();
            whiteboardTools.Clear();
            connections.Clear();
            _nextBlockId = 1;
            SelectTool(null);
            needsRedraw = true;
        }
        private void ClearAll_Click(object sender, EventArgs e) => ClearAll(null, null);

        // ============ СТАТИЧЕСКАЯ СИМУЛЯЦИЯ ============
        private bool ConfirmSamplingWarnings(
            double samplingRate,
            double dt)
        {
            var warnings = new List<string>();

            foreach (var tool in whiteboardTools)
            {
                bool isGenerator =
                    tool.Type == ToolType.Generator;

                bool isFileSource =
                    tool.Type == ToolType.Operation &&
                    tool.Operation == MathOperation.FileIO &&
                    tool.IsReading;

                if (!isGenerator && !isFileSource)
                    continue;

                string title =
                    !string.IsNullOrWhiteSpace(tool.Name)
                        ? $"{tool.Name} #{tool.BlockId}"
                        : $"Блок #{tool.BlockId}";

                // A source shorter than one sample can be lost almost entirely.
                if (tool.SignalDuration > 0.0 &&
                    tool.SignalDuration < dt)
                {
                    warnings.Add(
                        $"{title}: длительность сигнала ({tool.SignalDuration:G6} с) " +
                        $"меньше шага дискретизации dt ({dt:G6} с). " +
                        "Сигнал может не попасть в отсчёты или выглядеть как мгновенный.");
                }

                if (!isGenerator)
                    continue;

                bool hasFronts =
                    tool.Waveform == GeneratorWaveform.Trapezoid ||
                    tool.Waveform == GeneratorWaveform.Square ||
                    tool.Waveform == GeneratorWaveform.Sawtooth;

                if (hasFronts)
                {
                    if (tool.RiseTime > 0.0 &&
                        tool.RiseTime < dt)
                    {
                        warnings.Add(
                            $"{title}: фронт нарастания ({tool.RiseTime:G6} с) " +
                            $"короче шага дискретизации dt ({dt:G6} с). " +
                            "На графике он будет выглядеть практически мгновенным.");
                    }

                    if (tool.FallTime > 0.0 &&
                        tool.FallTime < dt)
                    {
                        warnings.Add(
                            $"{title}: фронт спада ({tool.FallTime:G6} с) " +
                            $"короче шага дискретизации dt ({dt:G6} с). " +
                            "На графике он будет выглядеть практически мгновенным.");
                    }
                }

                // Do not change the sine generator itself; only warn about sampling quality.
                if (tool.Waveform == GeneratorWaveform.Sine &&
                    tool.Frequency > 0.0 &&
                    !double.IsNaN(tool.Frequency) &&
                    !double.IsInfinity(tool.Frequency))
                {
                    double samplesPerPeriod =
                        samplingRate / tool.Frequency;

                    if (samplesPerPeriod <= 2.0)
                    {
                        warnings.Add(
                            $"{title}: частота сигнала {tool.Frequency:G6} Гц " +
                            $"слишком высока для Fs={samplingRate:G6} Гц. " +
                            "Нарушено условие Найквиста (нужно Fs > 2·f), " +
                            "поэтому возможен алиасинг.");
                    }
                    else if (samplesPerPeriod < 10.0)
                    {
                        warnings.Add(
                            $"{title}: на один период синусоиды приходится только " +
                            $"{samplesPerPeriod:F2} отсчёта. Сигнал будет рассчитан, " +
                            "но форма на графике может выглядеть грубо. " +
                            "Для наглядного графика желательно около 10 и более отсчётов на период.");
                    }
                }
            }

            if (warnings.Count == 0)
                return true;

            string message =
                "Обнаружены параметры, при которых дискретизация может заметно " +
                "исказить отображение сигнала:`n`n" +
                string.Join("`n`n", warnings) +
                "`n`nПродолжить моделирование?";

            return MessageBox.Show(
                message,
                "Предупреждение о дискретизации",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes;
        }
        private bool ValidateGeneratorSignalParameters()
        {
            const double eps = 1e-15;

            foreach (var tool in whiteboardTools.Where(
                t => t.Type == ToolType.Generator))
            {
                if (tool.SignalDuration <= 0.0 ||
                    double.IsNaN(tool.SignalDuration) ||
                    double.IsInfinity(tool.SignalDuration))
                {
                    MessageBox.Show(
                        $"{tool.Name} #{tool.BlockId}: длительность сигнала должна быть больше нуля.",
                        "Некорректные параметры сигнала",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    SelectTool(tool);
                    return false;
                }

                if (tool.RiseTime < 0.0 ||
                    tool.FallTime < 0.0 ||
                    double.IsNaN(tool.RiseTime) ||
                    double.IsNaN(tool.FallTime) ||
                    double.IsInfinity(tool.RiseTime) ||
                    double.IsInfinity(tool.FallTime))
                {
                    MessageBox.Show(
                        $"{tool.Name} #{tool.BlockId}: длительности фронтов должны быть " +
                        "неотрицательными конечными числами.",
                        "Некорректные параметры сигнала",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    SelectTool(tool);
                    return false;
                }

                // Trapezoid and triangular/sawtooth single-period signal:
                // both fronts must fit into the signal duration.
                if (tool.Waveform == GeneratorWaveform.Trapezoid ||
                    tool.Waveform == GeneratorWaveform.Sawtooth)
                {
                    if (tool.RiseTime + tool.FallTime >
                        tool.SignalDuration + eps)
                    {
                        MessageBox.Show(
                            $"{tool.Name} #{tool.BlockId}: сумма фронта нарастания " +
                            $"и фронта спада ({tool.RiseTime + tool.FallTime:G6} с) " +
                            $"не может превышать длительность сигнала " +
                            $"({tool.SignalDuration:G6} с).",
                            "Некорректные параметры сигнала",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        SelectTool(tool);
                        return false;
                    }
                }

                // Current Square block models one high/low interval.
                // Each transition must fit into its half of the signal.
                if (tool.Waveform == GeneratorWaveform.Square)
                {
                    double half = tool.SignalDuration * 0.5;

                    if (tool.RiseTime > half + eps)
                    {
                        MessageBox.Show(
                            $"{tool.Name} #{tool.BlockId}: фронт нарастания " +
                            $"({tool.RiseTime:G6} с) не может быть больше половины " +
                            $"длительности сигнала ({half:G6} с).",
                            "Некорректные параметры сигнала",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        SelectTool(tool);
                        return false;
                    }

                    if (tool.FallTime > half + eps)
                    {
                        MessageBox.Show(
                            $"{tool.Name} #{tool.BlockId}: фронт спада " +
                            $"({tool.FallTime:G6} с) не может быть больше половины " +
                            $"длительности сигнала ({half:G6} с).",
                            "Некорректные параметры сигнала",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        SelectTool(tool);
                        return false;
                    }
                }
            }

            return true;
        }
        private void RunSimulation(object sender, EventArgs e)
        {
            if (!ValidateGeneratorSignalParameters())
                return;

            // ---------- 1. Парсинг параметров с ЖЁСТКОЙ проверкой ----------
            double samplingRate, duration;

            if (!EngineeringParser.TryParse(samplingRateInput.Text, out samplingRate) ||
                samplingRate <= 0 || double.IsNaN(samplingRate) || double.IsInfinity(samplingRate))
            {
                samplingRate = 10000;
                samplingRateInput.Text = "10000";
            }

            if (!EngineeringParser.TryParse(durationInput.Text, out duration) ||
                duration <= 0 || double.IsNaN(duration) || double.IsInfinity(duration))
            {
                duration = 0.01;
                durationInput.Text = "0.01";
            }

            double dt = 1.0 / samplingRate;

            if (!ConfirmSamplingWarnings(
                    samplingRate,
                    dt))
            {
                return;
            }

            // Guard against overflow before converting the sample count to Int32.
            double requestedSamples = Math.Round(duration * samplingRate);
            if (double.IsNaN(requestedSamples) || double.IsInfinity(requestedSamples) ||
                requestedSamples > int.MaxValue || requestedSamples < 0)
            {
                MessageBox.Show("Число отсчётов превышает допустимый предел. Уменьшите длительность или частоту дискретизации.",
                    "Слишком много отсчётов", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int totalSamples = (int)requestedSamples;
            if (totalSamples < 2) totalSamples = 2;

            if (totalSamples > 5_000_000)
            {
                var r = MessageBox.Show(
                    $"Будет рассчитано {totalSamples:N0} точек.\n" +
                    $"Это может занять несколько секунд. Продолжить?",
                    "Очень много точек",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
            }

            System.Diagnostics.Debug.WriteLine($"=== СТАТИЧЕСКИЙ РАСЧЁТ ===");
            System.Diagnostics.Debug.WriteLine($"Fs={samplingRate:G6} Гц, T={duration:G6} с, dt={dt:E3} с, N={totalSamples:N0}");

            runButton.Enabled = false;
            allGraphsButton.Enabled = false;
            var oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 2. Сброс состояния
                calculator.ResetAll(whiteboardTools);
                foreach (var c in connections) c.CurrentValue = null;

                // 3. dt для интеграторов / дифференциаторов
                foreach (var t in whiteboardTools)
                {
                    if (t.Type == ToolType.Operation &&
                        (t.Operation == MathOperation.Integrator ||
                         t.Operation == MathOperation.Differentiator))
                    {
                        t.StepSize = dt;
                    }
                }
                calculator.TimeStep = dt;
                calculator.SimulationDuration = duration;

                // 4. Привязка графиков к источникам
                var chartBindings = new List<Tuple<MathTool, MathTool, Connection>>();
                foreach (var chart in whiteboardTools.Where(t => t.Type == ToolType.Chart))
                {
                    chart.ValueHistory.Clear();
                    chart.CurrentValue = 0;

                    var conn = connections.FirstOrDefault(c => c.TargetToolId == chart.Id);
                    if (conn == null) { chartBindings.Add(null); continue; }

                    var src = whiteboardTools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                    chartBindings.Add(src == null ? null : Tuple.Create(chart, src, conn));
                }

                // 5. ГЛАВНЫЙ ЦИКЛ — без таймера, без DoEvents
                for (int i = 0; i < totalSamples; i++)
                {
                    calculator.CurrentTime = i * dt;

                    var results = calculator.CalculateAll(whiteboardTools, connections);

                    for (int k = 0; k < chartBindings.Count; k++)
                    {
                        var b = chartBindings[k];
                        if (b == null) continue;

                        MathTool chart = b.Item1;
                        MathTool src = b.Item2;
                        Connection conn = b.Item3;

                        double value = 0;
                        if (src.Type == ToolType.SubSystem)
                        {
                            if (src.OutputPortResults.TryGetValue(conn.SourcePortIndex, out var v))
                                value = v;
                            else if (src.LastResult.HasValue)
                                value = src.LastResult.Value;
                        }
                        else if (results.TryGetValue(src.Id, out var rv))
                        {
                            value = rv;
                        }
                        else if (src.LastResult.HasValue)
                        {
                            value = src.LastResult.Value;
                        }

                        chart.ValueHistory.Add(value);
                        chart.CurrentValue = value;
                    }
                }

                sw.Stop();
                System.Diagnostics.Debug.WriteLine(
                    $"[Sim] {totalSamples:N0} точек за {sw.ElapsedMilliseconds} мс " +
                    $"({(totalSamples > 0 ? sw.ElapsedMilliseconds / (double)totalSamples : 0):F4} мс/точку)");

                // 6. Показываем графики сразу
                DisplayAllGraphsStatic();
                needsRedraw = true;
                workArea.Invalidate();
                workArea.Update();

                MessageBox.Show(
                    $"Моделирование завершено.\n" +
                    $"Шагов: {totalSamples:N0}\n" +
                    $"dt: {dt:E3} с\n" +
                    $"Fs: {samplingRate:G6} Гц\n" +
                    $"T: {duration:G6} с\n" +
                    $"Время расчёта: {sw.ElapsedMilliseconds} мс",
                    "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчёта: {ex.Message}\n{ex.StackTrace}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = oldCursor;
                runButton.Enabled = true;
                allGraphsButton.Enabled = true;
            }
        }

        private void DisplayAllGraphsStatic()
        {
            double samplingRate = 10000;
            double duration = 0.01;

            EngineeringParser.TryParse(
                samplingRateInput.Text,
                out samplingRate);

            EngineeringParser.TryParse(
                durationInput.Text,
                out duration);

            if (samplingRate <= 0)
                samplingRate = 10000;

            if (duration <= 0)
                duration = 0.01;

            double dt =
                1.0 /
                samplingRate;

            foreach (
                var chart
                in whiteboardTools.Where(
                    t => t.Type == ToolType.Chart))
            {
                GraphForm gf;

                if (graphWindows.ContainsKey(chart.Id) &&
                    !graphWindows[chart.Id].IsDisposed)
                {
                    gf =
                        graphWindows[chart.Id];
                }
                else
                {
                    gf =
                        new GraphForm(
                            chart.Name);

                    gf.Show();

                    graphWindows[chart.Id] =
                        gf;
                }

                if (chart.ValueHistory == null ||
                    chart.ValueHistory.Count == 0)
                {
                    continue;
                }

                var connection =
                    connections.FirstOrDefault(
                        c =>
                            c.TargetToolId ==
                            chart.Id);

                MathTool source =
                    null;

                if (connection != null)
                {
                    source =
                        whiteboardTools.FirstOrDefault(
                            t =>
                                t.Id ==
                                connection.SourceToolId);
                }

                // Только идеальный Step рисуем ступенчато.
                bool stepStyle =
                    source != null &&
                    source.Type == ToolType.Generator &&
                    source.Waveform == GeneratorWaveform.Step;

                gf.SetSignal(
                    chart.ValueHistory,
                    dt,
                    duration,
                    stepStyle);
            }
        }

        public void LoadStaticFileData(Guid fileToolId)
        {
            var fileTool = whiteboardTools.FirstOrDefault(t => t.Id == fileToolId);
            if (fileTool != null && fileTool.Operation == MathOperation.FileIO && fileTool.FileData.Count > 0)
            {
                var timeValues = Enumerable.Range(0, fileTool.FileData.Count).Select(i => (double)i).ToList();
                foreach (var conn in connections.Where(c => c.SourceToolId == fileToolId))
                {
                    var chart = whiteboardTools.FirstOrDefault(t => t.Id == conn.TargetToolId && t.Type == ToolType.Chart);
                    if (chart != null && graphWindows.ContainsKey(chart.Id) && !graphWindows[chart.Id].IsDisposed)
                        graphWindows[chart.Id].SetDataDirectly(timeValues, fileTool.FileData);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (propertiesContent != null)
                propertiesContent.Height = propertiesPanel.ClientSize.Height - 55;
            needsRedraw = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                renderTimer?.Stop();
                renderTimer?.Dispose();
                foreach (var g in graphWindows.Values) if (!g.IsDisposed) g.Close();
            }
            base.Dispose(disposing);
        }
    }
}



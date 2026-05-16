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
        private FlowLayoutPanel libraryPanel;

        // ============ ДАННЫЕ ============
        private List<MathTool> whiteboardTools = new List<MathTool>();
        private List<Connection> connections = new List<Connection>();
        private CalculationEngine calculator = new CalculationEngine();
        private ConnectionManager connectionManager;
        private BlockRenderer blockRenderer = new BlockRenderer();

        private MathTool selectedTool = null;
        private MathTool draggedTool = null;
        private bool isDragging = false;
        private Point dragStartPoint;

        private bool isConnecting = false;
        private ConnectionPoint? sourceConnectionPoint = null;
        private Point tempConnectionEnd;

        private Timer simulationTimer;
        private bool isSimulating = false;
        private double simulationTime = 0;

        private Dictionary<Guid, GraphForm> graphWindows = new Dictionary<Guid, GraphForm>();
        private PropertyPanel propertyPanel;
        private Timer renderTimer;
        private bool needsRedraw = true;

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
            leftPanel.Controls.Add(new Label { Text = "Длительность сигнала, с:", Location = new Point(15, y), Size = new Size(180, 23), Font = new Font("Segoe UI", 9) });
            durationInput = new TextBox { Location = new Point(200, y), Size = new Size(65, 23), Text = "0.01", Font = new Font("Segoe UI", 9) };
            durationInput.KeyPress += (s, e) => { if (e.KeyChar == ',') e.Handled = true; };
            leftPanel.Controls.Add(durationInput);
            y += 45;

            leftPanel.Controls.Add(new Label { Text = "Библиотека блоков", Location = new Point(15, y), Size = new Size(250, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(52, 58, 64) });
            y += 30;
            libraryPanel = new FlowLayoutPanel { Location = new Point(15, y), Size = new Size(250, 300), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            string[] blocks = { "Генератор синусоиды", "Сложение", "Вычитание", "Умножение", "Деление", "Интегратор", "Дифференциатор", "Интерполятор", "Файловый ввод/вывод", "График", "Подсистема", "Усилитель", "Антенна", "Канал", "Объект", "АЦП" };
            foreach (var block in blocks)
            {
                var btn = new Button { Text = block, Size = new Size(235, 32), BackColor = Color.FromArgb(108, 117, 125), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) };
                btn.FlatAppearance.BorderSize = 0;
                btn.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) DoDragDrop(new DataObject("ToolType", block), DragDropEffects.Copy); };
                libraryPanel.Controls.Add(btn);
            }
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
            propertyPanel.ApplyClicked += (s, e) => { if (selectedTool != null) { propertyPanel.ApplyChanges(selectedTool); needsRedraw = true; } };
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
            simulationTimer = new Timer { Interval = 50 };
            simulationTimer.Tick += SimulationTick;
        }

        private void WorkArea_DragDrop(object sender, DragEventArgs e)
        {
            var mousePos = workArea.PointToClient(new Point(e.X, e.Y));
            var realPos = ((DoubleBufferedPanel)workArea).GetRealMouseLocation(mousePos);
            string toolType = e.Data.GetData("ToolType") as string;
            if (string.IsNullOrEmpty(toolType)) return;

            MathTool tool = null;
            if (toolType.Contains("Генератор синусоиды"))
                tool = new MathTool { Position = realPos, Type = ToolType.Generator, Size = new Size(160, 100), Name = "Синусоида", Frequency = 1.0, Amplitude = 1.0, Phase = 0 };
            else if (toolType.Contains("Сложение"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Addition, Size = new Size(140, 80), Name = "Сложение" };
            else if (toolType.Contains("Вычитание"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Subtraction, Size = new Size(140, 80), Name = "Вычитание" };
            else if (toolType.Contains("Умножение"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Multiplication, Size = new Size(140, 80), Name = "Умножение" };
            else if (toolType.Contains("Деление"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Division, Size = new Size(140, 80), Name = "Деление" };
            else if (toolType.Contains("Интегратор"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Integrator, Size = new Size(140, 100), Name = "Интегратор", IntegralValue = 0, PreviousInput = 0, StepSize = 0.01 };
            else if (toolType.Contains("Дифференциатор"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Differentiator, Size = new Size(140, 100), Name = "Дифференциатор", PreviousTime = 0, PreviousOutput = 0 };
            else if (toolType.Contains("Интерполятор"))
            {
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.Interpolator, Size = new Size(140, 100), Name = "Интерполятор", InterpolationPoints = new List<PointF>() };
                for (int i = 0; i <= 10; i++) tool.InterpolationPoints.Add(new PointF(i / 2f, (float)Math.Sin(i / 2f)));
            }
            else if (toolType.Contains("Файловый"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.FileIO, Size = new Size(160, 100), Name = "Файловый ввод/вывод", IsReading = true, FileData = new List<double>() };
            else if (toolType.Contains("График"))
            {
                tool = new MathTool { Position = realPos, Type = ToolType.Chart, Size = new Size(160, 80), Name = $"График {whiteboardTools.Count + 1}" };
                var graph = new GraphForm(tool.Name);
                graph.Show();
                graphWindows[tool.Id] = graph;
            }
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
            // НЕ используем TranslateTransform – GridRenderer сам учитывает прокрутку

            GridRenderer.Draw(g, (Panel)workArea);

            // Соединения
            foreach (var conn in connections)
            {
                var source = whiteboardTools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                var target = whiteboardTools.FirstOrDefault(t => t.Id == conn.TargetToolId);
                if (source != null && target != null)
                    ConnectionRenderer.Draw(g, conn, source, target);
            }

            // Временное соединение
            if (isConnecting && sourceConnectionPoint.HasValue)
            {
                var sourceTool = whiteboardTools.FirstOrDefault(t => t.Id == sourceConnectionPoint.Value.ToolId);
                if (sourceTool != null)
                    ConnectionRenderer.DrawTemp(g, sourceTool, tempConnectionEnd);
            }

            // Блоки
            foreach (var tool in whiteboardTools)
            {
                if (tool.Type == ToolType.Chart)
                    blockRenderer.DrawChartTool(g, tool, selectedTool);
                else if (tool.Type == ToolType.Generator)
                    blockRenderer.DrawSineTool(g, tool, selectedTool, simulationTime);
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

        // ============ HIT TEST – ИСПОЛЬЗУЕТ МЕТОДЫ BlockRenderer ============
        private ConnectionPoint? HitTestConnectionPoint(Point mousePos)
        {
            var realPoint = ((DoubleBufferedPanel)workArea).GetRealMouseLocation(mousePos);
            foreach (var tool in whiteboardTools)
            {
                // Подсистема
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

                // Выход (кроме графика)
                if (tool.Type != ToolType.Chart)
                {
                    Point outPt = blockRenderer.GetOutputPoint(tool);
                    if (Distance(realPoint, outPt) < 12)
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Output };
                }

                // Входы
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
                else if (tool.Type != ToolType.Generator) // у генератора нет входа
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
            var realPos = ((DoubleBufferedPanel)workArea).GetRealMouseLocation(workArea.PointToClient(MousePosition));
            var tool = GetToolAtPosition(realPos);
            if (tool != null)
            {
                tool.Flipped = !tool.Flipped;
                needsRedraw = true;
            }
        }

        private void DeleteTool_Click(object sender, EventArgs e)
        {
            var realPos = ((DoubleBufferedPanel)workArea).GetRealMouseLocation(workArea.PointToClient(MousePosition));
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
            var realPos = ((DoubleBufferedPanel)workArea).GetRealMouseLocation(workArea.PointToClient(MousePosition));
            var tool = GetToolAtPosition(realPos);
            if (tool != null)
            {
                connectionManager.RemoveIncomingConnections(tool.Id);
                needsRedraw = true;
            }
        }

        private void ClearAll(object sender, EventArgs e)
        {
            foreach (var g in graphWindows.Values)
                if (!g.IsDisposed) g.Close();
            graphWindows.Clear();
            whiteboardTools.Clear();
            connections.Clear();
            SelectTool(null);
            needsRedraw = true;
        }
        private void ClearAll_Click(object sender, EventArgs e) => ClearAll(null, null);

        // ============ СИМУЛЯЦИЯ ============
        private void RunSimulation(object sender, EventArgs e)
        {
            isSimulating = true;
            simulationTime = 0;
            simulationTimer.Start();
        }

        private void SimulationTick(object sender, EventArgs e)
        {
            if (!isSimulating) return;
            var results = calculator.CalculateAll(whiteboardTools, connections);
            simulationTime += 0.05;

            foreach (var chart in whiteboardTools.Where(t => t.Type == ToolType.Chart))
            {
                var conn = connections.FirstOrDefault(c => c.TargetToolId == chart.Id);
                if (conn != null && graphWindows.ContainsKey(chart.Id) && !graphWindows[chart.Id].IsDisposed)
                {
                    var source = whiteboardTools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                    if (source != null && source.Type == ToolType.SubSystem && source.OutputPortResults.ContainsKey(conn.SourcePortIndex))
                        graphWindows[chart.Id].AddValue(source.OutputPortResults[conn.SourcePortIndex]);
                    else if (results.ContainsKey(conn.SourceToolId))
                        graphWindows[chart.Id].AddValue(results[conn.SourceToolId]);
                    else if (source != null && source.Type == ToolType.Generator && source.LastResult.HasValue)
                        graphWindows[chart.Id].AddValue(source.LastResult.Value);
                }
            }
            needsRedraw = true;
        }

        public void LoadStaticFileData(Guid fileToolId)
        {
            if (simulationTimer.Enabled) simulationTimer.Stop();
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
                simulationTimer?.Stop();
                simulationTimer?.Dispose();
                foreach (var g in graphWindows.Values) if (!g.IsDisposed) g.Close();
            }
            base.Dispose(disposing);
        }
    }
}
using MathApp.Core;
using MathApp.Helpers;
using MathApp.Models;
using MathApp.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace MathApp.UI
{
    public partial class SubSystemForm : Form
    {
        private SubSystemData _subSystemData;
        private List<MathTool> _tools;
        private List<Connection> _connections;
        private List<PortTool> _inputPorts;
        private List<PortTool> _outputPorts;

        private DoubleBufferedPanel workspace;
        private FlowLayoutPanel toolboxPanel;

        private MathTool _selectedTool;
        private MathTool _draggedTool;
        private bool _isDragging = false;
        private Point _dragStartPoint;

        private bool _isConnecting = false;
        private ConnectionPoint? _sourceConnectionPoint;
        private Point _tempConnectionEnd;

        private CalculationEngine _calculator;
        private ConnectionManager _connectionManager;
        private BlockRenderer _blockRenderer;

        private bool _needsRedraw = true;
        private Timer _renderTimer;
        private bool _isDirty = false;

        private TextBox nameBox;
        private Label lblInputs;
        private Label lblOutputs;
        private Button btnSave;
        private Button btnCancel;

        private Panel _leftPanel;
        private PropertyPanel _propertyPanel;

        private const int PORT_WIDTH = 100;
        private const int PORT_HEIGHT = 40;
        private const int PORT_SPACING = 55;
        private const int PORT_START_Y = 80;
        private static int _nextBlockId = 1;

        public SubSystemForm(SubSystemData data = null)
        {
            if (data == null)
            {
                _subSystemData = new SubSystemData
                {
                    Name = "Новая подсхема",
                    InternalTools = new List<MathTool>(),
                    InternalConnections = new List<Connection>(),
                    InputPorts = new List<SubSystemPort>(),
                    OutputPorts = new List<SubSystemPort>()
                };
            }
            else
            {
                _subSystemData = data;
                foreach (var tool in _subSystemData.InternalTools)
                {
                    if (tool.BlockId == 0) tool.BlockId = _nextBlockId++;
                }
            }

            _tools = _subSystemData.InternalTools;
            _connections = _subSystemData.InternalConnections;
            _inputPorts = new List<PortTool>();
            _outputPorts = new List<PortTool>();

            LoadPortsFromData();
            _calculator = new CalculationEngine();
            _connectionManager = new ConnectionManager(_connections);
            _blockRenderer = new BlockRenderer();

            InitializeComponent();
            SetupWorkspace();
            StartRenderTimer();
            AddPortsToTools();
        }

        private void LoadPortsFromData()
        {
            _inputPorts.Clear();
            for (int i = 0; i < _subSystemData.InputPorts.Count; i++)
            {
                var p = _subSystemData.InputPorts[i];
                _inputPorts.Add(new PortTool
                {
                    Id = p.Id,
                    Name = p.Name,
                    Type = ToolType.Port,
                    PortType = PortType.Input,
                    PortIndex = i,
                    PortName = p.Name,
                    Position = new Point(30, PORT_START_Y + i * PORT_SPACING),
                    Size = new Size(PORT_WIDTH, PORT_HEIGHT),
                    BlockId = -1
                });
            }

            _outputPorts.Clear();
            for (int i = 0; i < _subSystemData.OutputPorts.Count; i++)
            {
                var p = _subSystemData.OutputPorts[i];
                _outputPorts.Add(new PortTool
                {
                    Id = p.Id,
                    Name = p.Name,
                    Type = ToolType.Port,
                    PortType = PortType.Output,
                    PortIndex = i,
                    PortName = p.Name,
                    Position = new Point(workspace != null ? workspace.Width - 130 : 850, PORT_START_Y + i * PORT_SPACING),
                    Size = new Size(PORT_WIDTH, PORT_HEIGHT),
                    BlockId = -1
                });
            }
        }

        private void AddPortsToTools()
        {
            foreach (var port in _inputPorts) _tools.Add(port);
            foreach (var port in _outputPorts) _tools.Add(port);
            UpdatePortLabels();
        }

        private string ShowInputDialog(string text, string caption, string defaultValue = "")
        {
            using (var prompt = new Form())
            {
                prompt.Width = 400;
                prompt.Height = 150;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = caption;
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.BackColor = Color.FromArgb(45, 45, 48);

                var textLabel = new Label { Left = 20, Top = 20, Text = text, Width = 350, ForeColor = Color.White, BackColor = Color.Transparent };
                var textBox = new TextBox { Left = 20, Top = 50, Width = 350, Text = defaultValue, BackColor = Color.FromArgb(60, 60, 65), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
                var confirmation = new Button { Text = "OK", Left = 250, Width = 100, Top = 80, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                var cancel = new Button { Text = "Отмена", Left = 140, Width = 100, Top = 80, DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(70, 70, 75), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                prompt.Controls.AddRange(new Control[] { textLabel, textBox, confirmation, cancel });
                prompt.AcceptButton = confirmation;
                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : defaultValue;
            }
        }

        public SubSystemData GetResult()
        {
            _subSystemData.InputPorts = _inputPorts.Select(p => new SubSystemPort { Id = p.Id, Name = p.PortName ?? $"Вход{p.PortIndex + 1}", Index = p.PortIndex }).ToList();
            _subSystemData.OutputPorts = _outputPorts.Select(p => new SubSystemPort { Id = p.Id, Name = p.PortName ?? $"Выход{p.PortIndex + 1}", Index = p.PortIndex }).ToList();
            _subSystemData.InternalTools = _tools.Where(t => !(t is PortTool)).ToList();
            _subSystemData.InternalConnections = _connections;
            return _subSystemData;
        }

        private void SaveAndClose()
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void InitializeComponent()
        {
            this.Text = _subSystemData.Name;
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.MinimumSize = new Size(1000, 700);

            // TOP PANEL
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(248, 249, 250), BorderStyle = BorderStyle.FixedSingle };

            var nameLabel = new Label { Text = "Имя подсхемы:", Location = new Point(15, 20), Size = new Size(100, 25), ForeColor = Color.FromArgb(52, 58, 64), Font = new Font("Segoe UI", 10) };
            nameBox = new TextBox { Text = _subSystemData.Name, Location = new Point(120, 18), Size = new Size(250, 30), BackColor = Color.White, ForeColor = Color.Black, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            nameBox.TextChanged += (s, e) => { _subSystemData.Name = nameBox.Text; this.Text = nameBox.Text; _isDirty = true; };

            var btnAddInput = new Button { Text = "+ Вход", Location = new Point(400, 15), Size = new Size(90, 38), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnAddInput.Click += (s, e) => AddInputPort();

            var btnAddOutput = new Button { Text = "+ Выход", Location = new Point(500, 15), Size = new Size(90, 38), BackColor = Color.FromArgb(255, 193, 7), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnAddOutput.Click += (s, e) => AddOutputPort();

            btnSave = new Button { Text = "СОХРАНИТЬ", Size = new Size(90, 40), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnSave.Click += (s, e) => SaveAndClose();

            btnCancel = new Button { Text = "ОТМЕНА", Size = new Size(85, 40), BackColor = Color.FromArgb(108, 117, 125), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            topPanel.Controls.AddRange(new Control[] { nameLabel, nameBox, btnAddInput, btnAddOutput, btnSave, btnCancel });

            // LEFT PANEL (Toolbox)
            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 260, BackColor = Color.FromArgb(248, 249, 250), BorderStyle = BorderStyle.FixedSingle, AutoScroll = true };

            var toolsTitle = new Label { Text = "📦 БИБЛИОТЕКА БЛОКОВ", Location = new Point(10, 15), Size = new Size(230, 30), ForeColor = Color.FromArgb(52, 58, 64), Font = new Font("Segoe UI", 11, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };

            toolboxPanel = new FlowLayoutPanel { Location = new Point(10, 50), Size = new Size(230, 400), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.FromArgb(248, 249, 250) };

            var blockCategories = new Dictionary<string, string[]>
            {
                ["📐 МАТЕМАТИКА"] = new[] { "➕ Сложение", "➖ Вычитание", "✖️ Умножение", "➗ Деление", "∫ Интегратор", "d/dt Дифференциатор", "f(x) Интерполятор", "📁 Файловый ввод/вывод" },
                ["📡 ГЕНЕРАТОРЫ"] = new[] { "📈 Генератор синусоиды" },
                ["🔧 СПЕЦ. БЛОКИ"] = new[] { "🔊 Усилитель", "📡 Антенна", "🌐 Канал", "🎯 Объект", "🔢 АЦП" }
            };

            foreach (var category in blockCategories)
            {
                var catLabel = new Label { Text = category.Key, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(52, 58, 64), Width = 210, Height = 25, Margin = new Padding(0, 5, 0, 0) };
                toolboxPanel.Controls.Add(catLabel);

                foreach (var block in category.Value)
                {
                    var btn = new Button { Text = block, Size = new Size(210, 32), BackColor = Color.FromArgb(108, 117, 125), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0), Margin = new Padding(0, 2, 0, 2) };
                    btn.FlatAppearance.BorderSize = 0;
                    btn.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) DoDragDrop(new DataObject("ToolType", block), DragDropEffects.Copy); };
                    toolboxPanel.Controls.Add(btn);
                }
            }

            var portsInfo = new GroupBox { Text = "Порты подсистемы", Location = new Point(10, 560), Size = new Size(230, 80), ForeColor = Color.FromArgb(52, 58, 64), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(248, 249, 250) };

            lblInputs = new Label { Text = $"Входов: {_inputPorts.Count}", Location = new Point(10, 30), Size = new Size(200, 25), ForeColor = Color.FromArgb(40, 167, 69), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            lblOutputs = new Label { Text = $"Выходов: {_outputPorts.Count}", Location = new Point(10, 55), Size = new Size(200, 25), ForeColor = Color.FromArgb(255, 140, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            portsInfo.Controls.AddRange(new Control[] { lblInputs, lblOutputs });
            leftPanel.Controls.Add(toolsTitle);
            leftPanel.Controls.Add(toolboxPanel);
            leftPanel.Controls.Add(portsInfo);

            // RIGHT PANEL (Properties)
            var rightPanel = new Panel { Dock = DockStyle.Right, Width = 320, BackColor = Color.FromArgb(248, 249, 250), BorderStyle = BorderStyle.FixedSingle };

            var propertyTitle = new Label { Text = "ПАРАМЕТРЫ БЛОКА", Location = new Point(10, 15), Size = new Size(300, 30), Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(52, 58, 64), TextAlign = ContentAlignment.MiddleLeft };

            _propertyPanel = new PropertyPanel { Location = new Point(10, 55), Width = 290, Height = rightPanel.Height - 70, Visible = false, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            _propertyPanel.ParametersChanged += (s, e) => { if (_selectedTool != null) { _needsRedraw = true; _isDirty = true; workspace.Invalidate(); } };

            rightPanel.Controls.Add(propertyTitle);
            rightPanel.Controls.Add(_propertyPanel);

            // WORKSPACE
            workspace = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = Color.White };
            workspace.Paint += Workspace_Paint;
            workspace.MouseDown += Workspace_MouseDown;
            workspace.MouseMove += Workspace_MouseMove;
            workspace.MouseUp += Workspace_MouseUp;
            workspace.DragEnter += (s, e) => { if (e.Data.GetDataPresent("ToolType")) e.Effect = DragDropEffects.Copy; };
            workspace.DragDrop += Workspace_DragDrop;

            this.Controls.Add(workspace);
            this.Controls.Add(rightPanel);
            this.Controls.Add(leftPanel);
            this.Controls.Add(topPanel);

            this.Resize += (s, e) => { _needsRedraw = true; UpdateButtonPositions(); };
            this.Shown += (s, e) => UpdateButtonPositions();
        }

        private void UpdateButtonPositions()
        {
            if (btnSave != null && btnCancel != null)
            {
                btnSave.Location = new Point(this.ClientSize.Width - 190, 16);
                btnCancel.Location = new Point(this.ClientSize.Width - 95, 16);
            }
        }

        private void UpdatePortLabels()
        {
            if (lblInputs != null) lblInputs.Text = $"Входов: {_inputPorts.Count}";
            if (lblOutputs != null) lblOutputs.Text = $"Выходов: {_outputPorts.Count}";
        }

        private void AddInputPort()
        {
            int newIndex = _inputPorts.Count;
            var newPort = new PortTool
            {
                Id = Guid.NewGuid(),
                Name = $"Вход{newIndex + 1}",
                Type = ToolType.Port,
                PortType = PortType.Input,
                PortIndex = newIndex,
                PortName = $"Вход{newIndex + 1}",
                Position = new Point(30, PORT_START_Y + newIndex * PORT_SPACING),
                Size = new Size(PORT_WIDTH, PORT_HEIGHT),
                BlockId = -1
            };
            _inputPorts.Add(newPort);
            _tools.Add(newPort);
            UpdatePortLabels();
            _needsRedraw = true;
            _isDirty = true;
        }

        private void AddOutputPort()
        {
            int newIndex = _outputPorts.Count;
            var newPort = new PortTool
            {
                Id = Guid.NewGuid(),
                Name = $"Выход{newIndex + 1}",
                Type = ToolType.Port,
                PortType = PortType.Output,
                PortIndex = newIndex,
                PortName = $"Выход{newIndex + 1}",
                Position = new Point(workspace.Width - 130, PORT_START_Y + newIndex * PORT_SPACING),
                Size = new Size(PORT_WIDTH, PORT_HEIGHT),
                BlockId = -1
            };
            _outputPorts.Add(newPort);
            _tools.Add(newPort);
            UpdatePortLabels();
            _needsRedraw = true;
            _isDirty = true;
        }

        private void SetupWorkspace()
        {
            workspace.AutoScroll = true;
            workspace.AutoScrollMinSize = new Size(3000, 2000);
            workspace.AllowDrop = true;
        }

        private void StartRenderTimer()
        {
            _renderTimer = new Timer { Interval = 16 };
            _renderTimer.Tick += (s, e) => { if (_needsRedraw) { workspace.Invalidate(); _needsRedraw = false; } };
            _renderTimer.Start();
        }

        private void Workspace_DragDrop(object sender, DragEventArgs e)
        {
            var mousePos = workspace.PointToClient(new Point(e.X, e.Y));
            var realPos = GetRealLocation(mousePos);
            string toolType = e.Data.GetData("ToolType") as string;
            if (string.IsNullOrEmpty(toolType)) return;

            MathTool tool = CreateToolFromType(toolType, realPos);
            if (tool != null)
            {
                tool.BlockId = _nextBlockId++;
                _tools.Add(tool);
                _needsRedraw = true;
                _isDirty = true;
            }
        }

        private MathTool CreateToolFromType(string type, Point position)
        {
            if (type.Contains("Сложение")) return new MathTool { Position = position, Type = ToolType.Operation, Size = new Size(140, 80), Operation = MathOperation.Addition, Name = "Сложение" };
            if (type.Contains("Вычитание")) return new MathTool { Position = position, Type = ToolType.Operation, Size = new Size(140, 80), Operation = MathOperation.Subtraction, Name = "Вычитание" };
            if (type.Contains("Умножение")) return new MathTool { Position = position, Type = ToolType.Operation, Size = new Size(140, 80), Operation = MathOperation.Multiplication, Name = "Умножение" };
            if (type.Contains("Деление")) return new MathTool { Position = position, Type = ToolType.Operation, Size = new Size(140, 80), Operation = MathOperation.Division, Name = "Деление" };
            if (type.Contains("Интегратор")) return new MathTool { Position = position, Type = ToolType.Operation, Size = new Size(140, 100), Operation = MathOperation.Integrator, Name = "Интегратор", IntegralValue = 0, PreviousInput = 0, StepSize = 0.01 };
            if (type.Contains("Дифференциатор")) return new MathTool { Position = position, Type = ToolType.Operation, Size = new Size(140, 100), Operation = MathOperation.Differentiator, Name = "Дифференциатор", PreviousTime = 0, PreviousOutput = 0 };
            if (type.Contains("Интерполятор"))
            {
                var tool = new MathTool { Position = position, Type = ToolType.Operation, Size = new Size(140, 100), Operation = MathOperation.Interpolator, Name = "Интерполятор", InterpolationPoints = new List<PointF>() };
                for (int i = 0; i <= 10; i++) tool.InterpolationPoints.Add(new PointF(i / 2f, (float)Math.Sin(i / 2f)));
                return tool;
            }
            if (type.Contains("Файловый")) return new MathTool { Position = position, Type = ToolType.Operation, Size = new Size(160, 100), Operation = MathOperation.FileIO, Name = "Файловый ввод/вывод", IsReading = true, FileData = new List<double>() };
            if (type.Contains("Генератор синусоиды")) return new MathTool { Position = position, Type = ToolType.Generator, Size = new Size(160, 100), Name = "Синусоида", Frequency = 1.0, Amplitude = 1.0, Phase = 0 };
            if (type.Contains("Усилитель")) return new MathTool { Position = position, Type = ToolType.Amplifier, Size = new Size(80, 80), Name = "Усилитель", Gain = 10 };
            if (type.Contains("Антенна")) return new MathTool { Position = position, Type = ToolType.Antenna, Size = new Size(100, 80), Name = "Антенна", Gain = 10, EffectiveArea = 0.1 };
            if (type.Contains("Канал")) return new MathTool { Position = position, Type = ToolType.Channel, Size = new Size(100, 80), Name = "Канал", Distance = 1000, Attenuation = 0.01 };
            if (type.Contains("Объект")) return new MathTool { Position = position, Type = ToolType.Object, Size = new Size(100, 80), Name = "Объект", RadarCrossSection = 1, TimeConstant = 1.0 };
            if (type.Contains("АЦП")) return new MathTool { Position = position, Type = ToolType.ADC, Size = new Size(100, 80), Name = "АЦП", BitResolution = 12, SamplingRate = 10000, QuantizationStep = 0.001, DynamicRange = 120, ReferenceVoltage = 5.0 };
            return null;
        }

        private void Workspace_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            GridRenderer.Draw(g, workspace);

            foreach (var conn in _connections)
            {
                var source = _tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                var target = _tools.FirstOrDefault(t => t.Id == conn.TargetToolId);
                if (source != null && target != null)
                    ConnectionRenderer.Draw(g, conn, source, target);
            }

            if (_isConnecting && _sourceConnectionPoint.HasValue)
            {
                var sourceTool = _tools.FirstOrDefault(t => t.Id == _sourceConnectionPoint.Value.ToolId);
                if (sourceTool != null)
                    ConnectionRenderer.DrawTemp(g, sourceTool, _tempConnectionEnd);
            }

            foreach (var tool in _tools)
            {
                if (tool is PortTool port)
                {
                    DrawPortBlock(g, port);
                    DrawPortConnectionPoints(g, port);
                }
                else
                {
                    DrawToolByType(g, tool);
                    DrawConnectionPoints(g, tool);
                }
            }
        }

        private void DrawToolByType(Graphics g, MathTool tool)
        {
            switch (tool.Type)
            {
                case ToolType.Generator: _blockRenderer.DrawSineTool(g, tool, _selectedTool, 0); break;
                case ToolType.Amplifier: _blockRenderer.DrawAmplifierTool(g, tool, _selectedTool); break;
                case ToolType.Antenna: _blockRenderer.DrawAntennaTool(g, tool, _selectedTool); break;
                case ToolType.Channel: _blockRenderer.DrawChannelTool(g, tool, _selectedTool); break;
                case ToolType.Object: _blockRenderer.DrawObjectTool(g, tool, _selectedTool); break;
                case ToolType.ADC: _blockRenderer.DrawADCTool(g, tool, _selectedTool); break;
                default: _blockRenderer.DrawMathTool(g, tool, _selectedTool); break;
            }
        }

        private void DrawPortBlock(Graphics g, PortTool port)
        {
            Rectangle rect = new Rectangle(port.Position, port.Size);
            Color startColor, endColor;
            if (port.PortType == PortType.Input)
            {
                startColor = Color.FromArgb(220, 240, 220);
                endColor = Color.FromArgb(200, 230, 200);
            }
            else
            {
                startColor = Color.FromArgb(255, 235, 210);
                endColor = Color.FromArgb(255, 220, 180);
            }
            using (var brush = new LinearGradientBrush(rect, startColor, endColor, 45))
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 8);
            using (var pen = new Pen(port.PortType == PortType.Input ? Color.FromArgb(40, 167, 69) : Color.FromArgb(255, 140, 0), 2))
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 8);

            string displayName = port.PortName ?? (port.PortType == PortType.Input ? $"Вход {port.PortIndex + 1}" : $"Выход {port.PortIndex + 1}");
            using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(displayName, font, Brushes.Black, rect, sf);

            string icon = port.PortType == PortType.Input ? "⬅️ ВХОД" : "ВЫХОД ➡️";
            using (var font = new Font("Segoe UI", 7, FontStyle.Bold))
                g.DrawString(icon, font, Brushes.DarkGray, rect.X + 5, rect.Y + 5);
        }

        private void DrawPortConnectionPoints(Graphics g, PortTool port)
        {
            Point point;
            string label;
            Color color;
            bool hasConnection;

            if (port.PortType == PortType.Input)
            {
                // Входной порт: точка СПРАВА (выход) - оранжевая, можно начать соединение
                point = new Point(port.Position.X + port.Size.Width + 5, port.Position.Y + port.Size.Height / 2);
                label = "out";
                color = Color.Orange;
                hasConnection = _connections.Any(c => c.SourceToolId == port.Id);
            }
            else
            {
                // Выходной порт: точка СЛЕВА (вход) - зелёная, можно закончить соединение
                point = new Point(port.Position.X - 5, port.Position.Y + port.Size.Height / 2);
                label = "in";
                color = Color.LightGreen;
                hasConnection = _connections.Any(c => c.TargetToolId == port.Id);
            }

            DrawPoint(g, point, label, color, hasConnection);
        }

        private void DrawConnectionPoints(Graphics g, MathTool tool)
        {
            Point output = tool.Flipped ? new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2) : new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
            bool hasOutput = _connections.Any(c => c.SourceToolId == tool.Id);
            DrawPoint(g, output, "out", Color.Orange, hasOutput);

            Point inputA, inputB;
            if (tool.Flipped)
            {
                inputA = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + 20);
                inputB = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height - 20);
            }
            else
            {
                inputA = new Point(tool.Position.X - 5, tool.Position.Y + 20);
                inputB = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height - 20);
            }
            bool hasA = _connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.A);
            bool hasB = _connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.B);
            DrawPoint(g, inputA, "A", Color.LightGreen, hasA);
            DrawPoint(g, inputB, "B", Color.LightGreen, hasB);
        }

        private void DrawPoint(Graphics g, Point point, string label, Color color, bool hasConnection)
        {
            int size = 10;
            Color finalColor = hasConnection ? Color.Gold : color;
            using (var brush = new SolidBrush(finalColor))
                g.FillEllipse(brush, point.X - size / 2, point.Y - size / 2, size, size);
            using (var pen = new Pen(Color.White, 1))
                g.DrawEllipse(pen, point.X - size / 2, point.Y - size / 2, size, size);
            using (var font = new Font("Segoe UI", 7, FontStyle.Bold))
                g.DrawString(label, font, Brushes.White, point.X - 8, point.Y - 12);
        }

        private void Workspace_MouseDown(object sender, MouseEventArgs e)
        {
            var realPos = GetRealLocation(e.Location);

            if (e.Button == MouseButtons.Left)
            {
                var hitPoint = HitTestConnectionPoint(realPos);
                if (hitPoint.HasValue)
                {
                    if (hitPoint.Value.Type == ConnectionPointType.Output)
                    {
                        _isConnecting = true;
                        _sourceConnectionPoint = hitPoint;
                        _tempConnectionEnd = e.Location;
                        _needsRedraw = true;
                    }
                    else if (hitPoint.Value.Type == ConnectionPointType.Input && _isConnecting && _sourceConnectionPoint.HasValue)
                    {
                        if (ValidateConnection(_sourceConnectionPoint.Value, hitPoint.Value))
                        {
                            _connectionManager.CreateConnection(_sourceConnectionPoint.Value, hitPoint.Value);
                            _isDirty = true;
                        }
                        _isConnecting = false;
                        _sourceConnectionPoint = null;
                        _needsRedraw = true;
                    }
                }
                else
                {
                    var tool = GetToolAtPosition(realPos);
                    if (tool != null)
                    {
                        _selectedTool = tool;
                        _isDragging = true;
                        _draggedTool = tool;
                        _dragStartPoint = new Point(realPos.X - tool.Position.X, realPos.Y - tool.Position.Y);
                        _needsRedraw = true;
                        _propertyPanel.SetSelectedTool(_selectedTool);
                        _propertyPanel.Visible = true;
                    }
                    else
                    {
                        _selectedTool = null;
                        _needsRedraw = true;
                        _propertyPanel.Visible = false;
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                var tool = GetToolAtPosition(realPos);
                if (tool != null)
                {
                    var menu = new ContextMenuStrip();
                    if (!(tool is PortTool))
                    {
                        menu.Items.Add("🔄 Отзеркалить", null, (s, ev) => { tool.Flipped = !tool.Flipped; _needsRedraw = true; _isDirty = true; });
                    }
                    menu.Items.Add("🗑️ Удалить блок", null, (s, ev) => DeleteTool(tool));
                    if (tool is PortTool portTool)
                    {
                        menu.Items.Add("✏️ Переименовать", null, (s, ev) =>
                        {
                            string newName = ShowInputDialog("Введите имя порта:", "Переименование", portTool.PortName ?? (portTool.PortType == PortType.Input ? "Вход" : "Выход"));
                            if (!string.IsNullOrEmpty(newName))
                            {
                                portTool.PortName = newName;
                                portTool.Name = newName;
                                _needsRedraw = true;
                                _isDirty = true;
                            }
                        });
                    }
                    menu.Show(workspace, e.Location);
                }
                else
                {
                    _isConnecting = false;
                    _sourceConnectionPoint = null;
                    _needsRedraw = true;
                }
            }
        }

        private bool ValidateConnection(ConnectionPoint source, ConnectionPoint target)
        {
            var sourceTool = _tools.FirstOrDefault(t => t.Id == source.ToolId);
            var targetTool = _tools.FirstOrDefault(t => t.Id == target.ToolId);

            // Входной порт может быть только ИСТОЧНИКОМ (из него выходят)
            if (sourceTool is PortTool srcPort && srcPort.PortType == PortType.Input)
                return true;

            // Выходной порт может быть только ПРИЁМНИКОМ (в него входят)
            if (targetTool is PortTool tgtPort && tgtPort.PortType == PortType.Output)
                return true;

            // Нельзя выходить из выходного порта
            if (sourceTool is PortTool srcOut && srcOut.PortType == PortType.Output)
                return false;

            // Нельзя входить во входной порт
            if (targetTool is PortTool tgtIn && tgtIn.PortType == PortType.Input)
                return false;

            return true;
        }

        private void DeleteTool(MathTool tool)
        {
            if (tool is PortTool port)
            {
                if (port.PortType == PortType.Input)
                {
                    _inputPorts.Remove(port);
                    UpdatePortLabels();
                }
                else
                {
                    _outputPorts.Remove(port);
                    UpdatePortLabels();
                }
                _propertyPanel.Visible = false;
            }
            _connectionManager.RemoveConnectionsForTool(tool.Id);
            _tools.Remove(tool);
            if (_selectedTool == tool) _selectedTool = null;
            _needsRedraw = true;
            _isDirty = true;
        }

        private void Workspace_MouseMove(object sender, MouseEventArgs e)
        {
            var realPos = GetRealLocation(e.Location);
            if (_isDragging && _draggedTool != null)
            {
                _draggedTool.Position = new Point(realPos.X - _dragStartPoint.X, realPos.Y - _dragStartPoint.Y);
                _needsRedraw = true;
                workspace.Invalidate();
            }
            else if (_isConnecting)
            {
                _tempConnectionEnd = e.Location;
                _needsRedraw = true;
            }
        }

        private void Workspace_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _draggedTool = null;
                _needsRedraw = true;
            }
        }

        private ConnectionPoint? HitTestConnectionPoint(Point point)
        {
            foreach (var tool in _tools)
            {
                if (tool is PortTool port)
                {
                    Point portPoint;
                    if (port.PortType == PortType.Input)
                    {
                        // Входной порт: точка СПРАВА (выход) - можно начать соединение
                        portPoint = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                        if (Distance(point, portPoint) < 12)
                            return new ConnectionPoint(tool.Id, ConnectionPointType.Output, null, port.PortIndex);
                    }
                    else
                    {
                        // Выходной порт: точка СЛЕВА (вход) - можно закончить соединение
                        portPoint = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                        if (Distance(point, portPoint) < 12)
                            return new ConnectionPoint(tool.Id, ConnectionPointType.Input, InputType.A, port.PortIndex);
                    }
                }
                else
                {
                    Point output = tool.Flipped ? new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2) : new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                    if (Distance(point, output) < 12)
                        return new ConnectionPoint(tool.Id, ConnectionPointType.Output);

                    Point inputA, inputB;
                    if (tool.Flipped)
                    {
                        inputA = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + 20);
                        inputB = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height - 20);
                    }
                    else
                    {
                        inputA = new Point(tool.Position.X - 5, tool.Position.Y + 20);
                        inputB = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height - 20);
                    }
                    if (Distance(point, inputA) < 12)
                        return new ConnectionPoint(tool.Id, ConnectionPointType.Input, InputType.A);
                    if (Distance(point, inputB) < 12)
                        return new ConnectionPoint(tool.Id, ConnectionPointType.Input, InputType.B);
                }
            }
            return null;
        }

        private MathTool GetToolAtPosition(Point point)
        {
            foreach (var tool in _tools.Reverse<MathTool>())
            {
                if (new Rectangle(tool.Position, tool.Size).Contains(point))
                    return tool;
            }
            return null;
        }

        private Point GetRealLocation(Point mouseLocation)
        {
            return new Point(mouseLocation.X - workspace.AutoScrollPosition.X, mouseLocation.Y - workspace.AutoScrollPosition.Y);
        }

        private double Distance(Point p1, Point p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.DialogResult != DialogResult.Cancel && _isDirty)
            {
                var result = MessageBox.Show("Сохранить изменения?", "Подсистема", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Yes) this.DialogResult = DialogResult.OK;
                else if (result == DialogResult.Cancel) e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _renderTimer != null)
            {
                _renderTimer.Stop();
                _renderTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
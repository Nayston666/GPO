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
        private List<PortTool> _inputPorts;   // Входные порты 
        private List<PortTool> _outputPorts;  // Выходные порты 

        private DoubleBufferedPanel workspace;
        private ListBox toolboxList;

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

        // Константы для отрисовки портов
        private const int PORT_WIDTH = 100;
        private const int PORT_HEIGHT = 40;
        private const int PORT_SPACING = 55;
        private const int PORT_START_Y = 80;

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
            }

            _tools = _subSystemData.InternalTools;
            _connections = _subSystemData.InternalConnections;

            // Конвертируем SubSystemPort в PortTool
            _inputPorts = new List<PortTool>();
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
                    Size = new Size(PORT_WIDTH, PORT_HEIGHT)
                });
            }

            _outputPorts = new List<PortTool>();
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
                    Position = new Point(workspace != null ? workspace.Width - 130 : 800, PORT_START_Y + i * PORT_SPACING),
                    Size = new Size(PORT_WIDTH, PORT_HEIGHT)
                });
            }

            _calculator = new CalculationEngine();
            _connectionManager = new ConnectionManager(_connections);
            _blockRenderer = new BlockRenderer();

            InitializeComponent();
            SetupWorkspace();
            StartRenderTimer();

            // Добавляем порты в рабочую область
            foreach (var port in _inputPorts)
                _tools.Add(port);
            foreach (var port in _outputPorts)
                _tools.Add(port);
        }

        private string ShowInputDialog(string text, string caption, string defaultValue = "")
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            Label textLabel = new Label()
            {
                Left = 20,
                Top = 20,
                Text = text,
                Width = 350,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            TextBox textBox = new TextBox()
            {
                Left = 20,
                Top = 50,
                Width = 350,
                Text = defaultValue,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Button confirmation = new Button()
            {
                Text = "OK",
                Left = 250,
                Width = 100,
                Top = 80,
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            Button cancel = new Button()
            {
                Text = "Отмена",
                Left = 140,
                Width = 100,
                Top = 80,
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(70, 70, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : defaultValue;
        }

        public SubSystemData GetResult()
        {
            // Сохраняем порты обратно в SubSystemData
            _subSystemData.InputPorts = _inputPorts.Select(p => new SubSystemPort
            {
                Id = p.Id,
                Name = p.PortName ?? $"Вход{p.PortIndex + 1}",
                Index = p.PortIndex
            }).ToList();

            _subSystemData.OutputPorts = _outputPorts.Select(p => new SubSystemPort
            {
                Id = p.Id,
                Name = p.PortName ?? $"Выход{p.PortIndex + 1}",
                Index = p.PortIndex
            }).ToList();

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
            this.BackColor = Color.FromArgb(30, 30, 35);
            this.MinimumSize = new Size(1000, 700);

            // Верхняя панель
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(38, 38, 40)
            };

            var nameLabel = new Label
            {
                Text = "Имя подсхемы:",
                Location = new Point(15, 20),
                Size = new Size(100, 25),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 10)
            };

            nameBox = new TextBox
            {
                Text = _subSystemData.Name,
                Location = new Point(120, 18),
                Size = new Size(250, 30),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            nameBox.TextChanged += (s, e) =>
            {
                _subSystemData.Name = nameBox.Text;
                this.Text = nameBox.Text;
                _isDirty = true;
            };

            // Кнопка "Добавить вход"
            var btnAddInput = new Button
            {
                Text = "+ Вход",
                Location = new Point(400, 15),
                Size = new Size(90, 38),
                BackColor = Color.FromArgb(0, 150, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnAddInput.Click += (s, e) => AddInputPort();

            // Кнопка "Добавить выход"
            var btnAddOutput = new Button
            {
                Text = "+ Выход",
                Location = new Point(500, 15),
                Size = new Size(90, 38),
                BackColor = Color.FromArgb(200, 120, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnAddOutput.Click += (s, e) => AddOutputPort();

            // Кнопка Сохранить
            btnSave = new Button
            {
                Text = "СОХРАНИТЬ",
                Size = new Size(90, 40),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSave.Click += (s, e) => SaveAndClose();

            // Кнопка Отмена
            btnCancel = new Button
            {
                Text = "ОТМЕНА",
                Size = new Size(85, 40),
                BackColor = Color.FromArgb(70, 70, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            topPanel.Controls.AddRange(new Control[] { nameLabel, nameBox, btnAddInput, btnAddOutput, btnSave, btnCancel });

            // Левая панель с инструментами
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            var toolsTitle = new Label
            {
                Text = "🧮 МАТЕМАТИКА",
                Location = new Point(10, 15),
                Size = new Size(200, 30),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            toolboxList = new ListBox
            {
                Location = new Point(10, 55),
                Size = new Size(200, 200),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                ItemHeight = 35
            };

            toolboxList.Items.AddRange(new object[]
            {
                "➕ Сложение",
                "➖ Вычитание",
                "✖️ Умножение",
                "➗ Деление",
                "📈 Синусоида"
            });

            toolboxList.MouseDown += ToolboxList_MouseDown;

            // Информация о портах
            var portsInfo = new GroupBox
            {
                Text = "Порты подсистемы",
                Location = new Point(10, 270),
                Size = new Size(200, 150),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lblInputs = new Label
            {
                Text = $"Входов: {_inputPorts.Count}",
                Location = new Point(10, 30),
                Size = new Size(180, 25),
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9)
            };

            lblOutputs = new Label
            {
                Text = $"Выходов: {_outputPorts.Count}",
                Location = new Point(10, 60),
                Size = new Size(180, 25),
                ForeColor = Color.Orange,
                Font = new Font("Segoe UI", 9)
            };

            var lblHint = new Label
            {
                Text = "Входной порт (зелёный):\n  • Точка СПРАВА (выход)\n  • В него НЕЛЬЗЯ зайти\nВыходной порт (оранжевый):\n  • Точка СЛЕВА (вход)\n  • Из него НЕЛЬЗЯ выйти",
                Location = new Point(10, 95),
                Size = new Size(180, 80),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7)
            };

            portsInfo.Controls.AddRange(new Control[] { lblInputs, lblOutputs, lblHint });

            leftPanel.Controls.AddRange(new Control[] { toolsTitle, toolboxList, portsInfo });

            // Рабочая область
            workspace = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 35, 40)
            };

            workspace.Paint += Workspace_Paint;
            workspace.MouseDown += Workspace_MouseDown;
            workspace.MouseMove += Workspace_MouseMove;
            workspace.MouseUp += Workspace_MouseUp;
            workspace.DragEnter += Workspace_DragEnter;
            workspace.DragDrop += Workspace_DragDrop;

            this.Controls.Add(workspace);
            this.Controls.Add(leftPanel);
            this.Controls.Add(topPanel);

            this.Resize += (s, e) =>
            {
                _needsRedraw = true;
                UpdateButtonPositions();
            };
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
                Size = new Size(PORT_WIDTH, PORT_HEIGHT)
            };
            _inputPorts.Add(newPort);
            _tools.Add(newPort);

            if (lblInputs != null)
                lblInputs.Text = $"Входов: {_inputPorts.Count}";

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
                Size = new Size(PORT_WIDTH, PORT_HEIGHT)
            };
            _outputPorts.Add(newPort);
            _tools.Add(newPort);

            if (lblOutputs != null)
                lblOutputs.Text = $"Выходов: {_outputPorts.Count}";

            _needsRedraw = true;
            _isDirty = true;
        }

        private void SetupWorkspace()
        {
            workspace.AutoScroll = true;
            workspace.AutoScrollMinSize = new Size(2000, 2000);
            workspace.AllowDrop = true;
        }

        private void StartRenderTimer()
        {
            _renderTimer = new Timer();
            _renderTimer.Interval = 16;
            _renderTimer.Tick += (s, e) =>
            {
                if (_needsRedraw)
                {
                    workspace.Invalidate();
                    _needsRedraw = false;
                }
            };
            _renderTimer.Start();
        }

        private void ToolboxList_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && toolboxList.SelectedItem != null)
            {
                DoDragDrop(new DataObject("MathTool", toolboxList.SelectedItem.ToString()), DragDropEffects.Copy);
            }
        }

        private void Workspace_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("MathTool"))
                e.Effect = DragDropEffects.Copy;
        }

        private void Workspace_DragDrop(object sender, DragEventArgs e)
        {
            var mousePos = workspace.PointToClient(new Point(e.X, e.Y));
            var realPos = GetRealLocation(mousePos);
            var type = e.Data.GetData("MathTool") as string;

            MathTool tool = null;

            if (type.Contains("Сложение"))
            {
                tool = new MathTool
                {
                    Position = realPos,
                    Type = ToolType.Operation,
                    Size = new Size(140, 80),
                    Operation = MathOperation.Addition,
                    Name = "Сложение"
                };
            }
            else if (type.Contains("Вычитание"))
            {
                tool = new MathTool
                {
                    Position = realPos,
                    Type = ToolType.Operation,
                    Size = new Size(140, 80),
                    Operation = MathOperation.Subtraction,
                    Name = "Вычитание"
                };
            }
            else if (type.Contains("Умножение"))
            {
                tool = new MathTool
                {
                    Position = realPos,
                    Type = ToolType.Operation,
                    Size = new Size(140, 80),
                    Operation = MathOperation.Multiplication,
                    Name = "Умножение"
                };
            }
            else if (type.Contains("Деление"))
            {
                tool = new MathTool
                {
                    Position = realPos,
                    Type = ToolType.Operation,
                    Size = new Size(140, 80),
                    Operation = MathOperation.Division,
                    Name = "Деление"
                };
            }
            else if (type.Contains("График"))
            {
                tool = new MathTool
                {
                    Position = realPos,
                    Type = ToolType.Chart,
                    Size = new Size(160, 80),
                    Name = "График"
                };
            }
            else if (type.Contains("Синусоида"))
            {
                tool = new MathTool
                {
                    Position = realPos,
                    Type = ToolType.SineGenerator,
                    Size = new Size(160, 100),
                    Name = "Синусоида",
                    Frequency = 1.0,
                    Amplitude = 1.0,
                    Phase = 0
                };
            }

            if (tool != null)
            {
                _tools.Add(tool);
                _needsRedraw = true;
                _isDirty = true;
            }
        }

        private void Workspace_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            GridRenderer.Draw(g, workspace);

            // Рисуем соединения
            foreach (var conn in _connections)
            {
                var source = _tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                var target = _tools.FirstOrDefault(t => t.Id == conn.TargetToolId);
                if (source != null && target != null)
                {
                    ConnectionRenderer.Draw(g, conn, source, target);
                }
            }

            if (_isConnecting && _sourceConnectionPoint.HasValue)
            {
                var sourceTool = _tools.FirstOrDefault(t => t.Id == _sourceConnectionPoint.Value.ToolId);
                if (sourceTool != null)
                {
                    ConnectionRenderer.DrawTemp(g, sourceTool, _tempConnectionEnd);
                }
            }

            // Рисуем блоки
            foreach (var tool in _tools)
            {
                if (tool is PortTool port)
                {
                    DrawPortBlock(g, port);
                    DrawPortConnectionPoints(g, port);
                }
                else
                {
                    _blockRenderer.DrawMathTool(g, tool, _selectedTool);
                    DrawStandardConnectionPoints(g, tool);
                }
            }
        }

        private void DrawPortBlock(Graphics g, PortTool port)
        {
            Rectangle rect = new Rectangle(port.Position, port.Size);

            // Цвета для портов
            Color startColor, endColor;
            if (port.PortType == PortType.Input)
            {
                // Входной порт — зелёный
                startColor = Color.FromArgb(60, 100, 80);
                endColor = Color.FromArgb(40, 70, 55);
            }
            else
            {
                // Выходной порт — оранжевый
                startColor = Color.FromArgb(100, 80, 60);
                endColor = Color.FromArgb(80, 60, 40);
            }

            using (var brush = new LinearGradientBrush(rect, startColor, endColor, 45))
            {
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 8);
            }

            // Рамка
            using (var pen = new Pen(port.PortType == PortType.Input ? Color.LightGreen : Color.Orange, 2))
            {
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 8);
            }

            // Текст
            string displayName = port.PortName ?? (port.PortType == PortType.Input ? $"Вход {port.PortIndex + 1}" : $"Выход {port.PortIndex + 1}");
            using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(displayName, font, Brushes.White, rect, sf);
            }

            // Поясняющая иконка
            string icon = port.PortType == PortType.Input ? "⬅️ ВХОД" : "ВЫХОД ➡️";
            using (var font = new Font("Segoe UI", 7, FontStyle.Bold))
            {
                g.DrawString(icon, font, Brushes.LightYellow, rect.X + 5, rect.Y + 5);
            }
        }

        private void DrawPortConnectionPoints(Graphics g, PortTool port)
        {
            Point point;
            if (port.PortType == PortType.Input)
            {
                // Входной порт
                point = new Point(port.Position.X + port.Size.Width + 5,
                                  port.Position.Y + port.Size.Height / 2);
            }
            else
            {
                // Выходной порт
                point = new Point(port.Position.X - 5,
                                  port.Position.Y + port.Size.Height / 2);
            }

            bool hasConnection = _connections.Any(c =>
                (port.PortType == PortType.Input && c.SourceToolId == port.Id) ||  // Входной порт - источник
                (port.PortType == PortType.Output && c.TargetToolId == port.Id));   // Выходной порт - цель

            string label = port.PortType == PortType.Input ? "out" : "in";
            Color color = port.PortType == PortType.Input ? Color.Orange : Color.LightGreen;

            DrawPoint(g, point, label, color, hasConnection);
        }

        private void DrawStandardConnectionPoints(Graphics g, MathTool tool)
        {
            // Выходная точка (справа)
            Point output = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
            bool hasOutput = _connections.Any(c => c.SourceToolId == tool.Id);
            DrawPoint(g, output, "out", Color.Orange, hasOutput);

            // Входные точки (слева)
            Point inputA = new Point(tool.Position.X - 5, tool.Position.Y + 20);
            Point inputB = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height - 20);

            bool hasA = _connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.A);
            bool hasB = _connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.B);

            DrawPoint(g, inputA, "A", Color.LightGreen, hasA);
            DrawPoint(g, inputB, "B", Color.LightGreen, hasB);
        }

        private void DrawPoint(Graphics g, Point point, string label, Color color, bool hasConnection)
        {
            int size = 10;
            Color finalColor = hasConnection ? Color.Yellow : color;

            using (var brush = new SolidBrush(finalColor))
            {
                g.FillEllipse(brush, point.X - size / 2, point.Y - size / 2, size, size);
            }
            using (var pen = new Pen(Color.White, 1))
            {
                g.DrawEllipse(pen, point.X - size / 2, point.Y - size / 2, size, size);
            }
            using (var font = new Font("Segoe UI", 7, FontStyle.Bold))
            {
                g.DrawString(label, font, Brushes.White, point.X - 8, point.Y - 12);
            }
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
                    else if (hitPoint.Value.Type == ConnectionPointType.Input && _isConnecting)
                    {
                        _connectionManager.CreateConnection(_sourceConnectionPoint.Value, hitPoint.Value);
                        _isConnecting = false;
                        _sourceConnectionPoint = null;
                        _needsRedraw = true;
                        _isDirty = true;
                    }
                }
                else
                {
                    _draggedTool = GetToolAtPosition(realPos);
                    if (_draggedTool != null)
                    {
                        _selectedTool = _draggedTool;
                        _isDragging = true;
                        _dragStartPoint = new Point(realPos.X - _draggedTool.Position.X, realPos.Y - _draggedTool.Position.Y);
                        _needsRedraw = true;
                    }
                    else
                    {
                        _selectedTool = null;
                        _needsRedraw = true;
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                var tool = GetToolAtPosition(realPos);
                if (tool != null)
                {
                    var menu = new ContextMenuStrip();
                    menu.Items.Add("🗑️ Удалить блок", null, (s, ev) =>
                    {
                        if (tool is PortTool port)
                        {
                            if (port.PortType == PortType.Input)
                            {
                                _inputPorts.Remove(port);
                                if (lblInputs != null)
                                    lblInputs.Text = $"Входов: {_inputPorts.Count}";
                            }
                            else
                            {
                                _outputPorts.Remove(port);
                                if (lblOutputs != null)
                                    lblOutputs.Text = $"Выходов: {_outputPorts.Count}";
                            }
                        }
                        _connectionManager.RemoveConnectionsForTool(tool.Id);
                        _tools.Remove(tool);
                        _needsRedraw = true;
                        _isDirty = true;
                    });

                    if (tool is PortTool portTool)
                    {
                        menu.Items.Add("✏️ Переименовать", null, (s, ev) =>
                        {
                            string newName = ShowInputDialog("Введите имя порта:", "Переименование",
                                portTool.PortName ?? (portTool.PortType == PortType.Input ? "Вход" : "Выход"));
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
                    // Сброс соединения при ПКМ
                    _isConnecting = false;
                    _sourceConnectionPoint = null;
                    _needsRedraw = true;
                }
            }
        }

        private void Workspace_MouseMove(object sender, MouseEventArgs e)
        {
            var realPos = GetRealLocation(e.Location);

            if (_isDragging && _draggedTool != null)
            {
                _draggedTool.Position = new Point(realPos.X - _dragStartPoint.X, realPos.Y - _dragStartPoint.Y);
                _needsRedraw = true;
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
                        // Входной порт
                        portPoint = new Point(tool.Position.X + tool.Size.Width + 5,
                                              tool.Position.Y + tool.Size.Height / 2);
                    }
                    else
                    {
                        // Выходной порт
                        portPoint = new Point(tool.Position.X - 5,
                                              tool.Position.Y + tool.Size.Height / 2);
                    }

                    if (Distance(point, portPoint) < 12)
                    {
                        System.Diagnostics.Debug.WriteLine($"Hit port {port.PortName} at {portPoint}, Type={port.PortType}");

                        return new ConnectionPoint
                        {
                            ToolId = tool.Id,
                            Type = port.PortType == PortType.Input ? ConnectionPointType.Output : ConnectionPointType.Input,
                            InputType = port.PortType == PortType.Output ? InputType.A : (InputType?)null,
                            PortIndex = port.PortIndex  
                        };
                    }
                }
                else
                {
                    // Выходная точка обычного блока 
                    Point output = new Point(tool.Position.X + tool.Size.Width + 5,
                                             tool.Position.Y + tool.Size.Height / 2);
                    if (Distance(point, output) < 12)
                    {
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Output };
                    }

                    // Входные точки обычного блока 
                    Point inputA = new Point(tool.Position.X - 5, tool.Position.Y + 20);
                    if (Distance(point, inputA) < 12)
                    {
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Input, InputType = InputType.A };
                    }

                    Point inputB = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height - 20);
                    if (Distance(point, inputB) < 12)
                    {
                        return new ConnectionPoint { ToolId = tool.Id, Type = ConnectionPointType.Input, InputType = InputType.B };
                    }
                }
            }
            return null;
        }

        private MathTool GetToolAtPosition(Point point)
        {
            foreach (var tool in _tools)
            {
                var rect = new Rectangle(tool.Position, tool.Size);
                if (rect.Contains(point))
                    return tool;
            }
            return null;
        }

        private Point GetRealLocation(Point mouseLocation)
        {
            return new Point(
                mouseLocation.X - workspace.AutoScrollPosition.X,
                mouseLocation.Y - workspace.AutoScrollPosition.Y
            );
        }

        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.DialogResult != DialogResult.Cancel && _isDirty)
            {
                var result = MessageBox.Show("Сохранить изменения?", "Подсистема",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    this.DialogResult = DialogResult.OK;
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
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
#requires -version 5.1
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $Root "ГПО"
$Csproj = Join-Path $ProjectDir "ГПО.csproj"
$BackupRoot = Join-Path $Root "_NRF1P_backup_before_install"

function Fail([string]$Message) {
    throw $Message
}

function Read-Normalized([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        Fail "Не найден файл: $Path"
    }

    $text = [System.IO.File]::ReadAllText($Path)
    return $text.Replace("`r`n", "`n").Replace("`r", "`n")
}

function Write-Normalized([string]$Path, [string]$Text) {
    $crlf = $Text.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")
    $utf8Bom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($Path, $crlf, $utf8Bom)
}

function Replace-Once(
    [string]$Text,
    [string]$Old,
    [string]$New,
    [string]$Label
) {
    $first = $Text.IndexOf($Old, [System.StringComparison]::Ordinal)

    if ($first -lt 0) {
        Fail "Не удалось найти место для изменения: $Label.`nСкорее всего локальные файлы отличаются от ветки 30/09/26-version."
    }

    $second = $Text.IndexOf(
        $Old,
        $first + $Old.Length,
        [System.StringComparison]::Ordinal
    )

    if ($second -ge 0) {
        Fail "Найдено несколько одинаковых мест для изменения: $Label."
    }

    return $Text.Substring(0, $first) +
           $New +
           $Text.Substring($first + $Old.Length)
}

if (-not (Test-Path -LiteralPath $Csproj)) {
    Write-Host ""
    Write-Host "ОШИБКА: установщик лежит не в корне проекта." -ForegroundColor Red
    Write-Host ""
    Write-Host "Положи install_NRF1P.ps1 рядом с ГПО.slnx, чтобы структура была:" -ForegroundColor Yellow
    Write-Host "  GPO-30-09-26-version\" -ForegroundColor Yellow
    Write-Host "    install_NRF1P.ps1" -ForegroundColor Yellow
    Write-Host "    ГПО.slnx" -ForegroundColor Yellow
    Write-Host "    ГПО\" -ForegroundColor Yellow
    exit 1
}

if (Test-Path -LiteralPath $BackupRoot) {
    Write-Host ""
    Write-Host "Папка резервной копии уже существует:" -ForegroundColor Yellow
    Write-Host $BackupRoot -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Если это предыдущая попытка, сначала переименуй или удали эту папку." -ForegroundColor Yellow
    exit 1
}

$RelativeFiles = @(
    "Models\Enums.cs",
    "Models\MathTool.cs",
    "Core\CalculationEngine.cs",
    "Form1.cs",
    "Rendering\BlockRenderer.cs",
    "UI\PropertyPanel.cs",
    "ГПО.csproj"
)

$NewFormRelative = "UI\NonlinearFilterCharacteristicsForm.cs"
$NewFormPath = Join-Path $ProjectDir $NewFormRelative

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " Установка НРФ1П в ветку 30/09/26-version" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

# Сначала проверяем наличие всех исходных файлов.
foreach ($relative in $RelativeFiles) {
    $full = Join-Path $ProjectDir $relative
    if (-not (Test-Path -LiteralPath $full)) {
        Fail "Не найден исходный файл: $full"
    }
}

# Резервная копия.
New-Item -ItemType Directory -Path $BackupRoot | Out-Null

foreach ($relative in $RelativeFiles) {
    $src = Join-Path $ProjectDir $relative
    $dst = Join-Path $BackupRoot $relative
    $dstDir = Split-Path -Parent $dst

    if (-not (Test-Path -LiteralPath $dstDir)) {
        New-Item -ItemType Directory -Path $dstDir -Force | Out-Null
    }

    Copy-Item -LiteralPath $src -Destination $dst
}

Write-Host "[1/8] Резервная копия создана." -ForegroundColor Green

try {
    # ============================================================
    # 1. Models\Enums.cs
    # ============================================================
    $path = Join-Path $ProjectDir "Models\Enums.cs"
    $text = Read-Normalized $path

    $old = @'
        ADC,            // Аналого-цифровой преобразователь
        SubSystem,      // Подсистема
'@

    $new = @'
        ADC,            // Аналого-цифровой преобразователь
        NonlinearFirstOrderFilter, // Нелинейный рекурсивный фильтр 1 порядка
        SubSystem,      // Подсистема
'@

    $text = Replace-Once $text $old $new "Models\Enums.cs / ToolType"
    Write-Normalized $path $text
    Write-Host "[2/8] Добавлен тип блока НРФ1П." -ForegroundColor Green


    # ============================================================
    # 2. Models\MathTool.cs
    # ============================================================
    $path = Join-Path $ProjectDir "Models\MathTool.cs"
    $text = Read-Normalized $path

    $old = @'
        // Подсистема
        public SubSystemData SubSystemData { get; set; }
'@

    $new = @'
        // Нелинейный рекурсивный фильтр 1 порядка
        // КВХ: X = заряд q, Y = напряжение U.
        public List<FilterPoint> FilterKVH { get; set; } = new List<FilterPoint>();

        // ВАХ: X = напряжение U, Y = ток I.
        public List<FilterPoint> FilterVAH { get; set; } = new List<FilterPoint>();

        // Внутреннее состояние фильтра между шагами моделирования.
        public double FilterPreviousIC { get; set; } = 0.0;
        public double FilterPreviousIR { get; set; } = 0.0;
        public double FilterCharge { get; set; } = 0.0;
        public double FilterLastUOut { get; set; } = 0.0;

        // Подсистема
        public SubSystemData SubSystemData { get; set; }
'@

    $text = Replace-Once $text $old $new "Models\MathTool.cs / свойства фильтра"

    $old = @'
            CurrentFileIndex = 0;
            OutputPortResults.Clear();
            CurrentValue = 0;
        }
    }
}
'@

    $new = @'
            CurrentFileIndex = 0;
            OutputPortResults.Clear();
            CurrentValue = 0;

            // Сбрасываем только динамическое состояние фильтра.
            // ВАХ и КВХ являются настройками блока и сохраняются.
            FilterPreviousIC = 0.0;
            FilterPreviousIR = 0.0;
            FilterCharge = 0.0;
            FilterLastUOut = 0.0;
        }
    }

    /// <summary>
    /// Одна точка табличной характеристики нелинейного фильтра.
    /// Используется double, чтобы не терять точность на малых физических величинах.
    /// </summary>
    public class FilterPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public FilterPoint()
        {
        }

        public FilterPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
'@

    $text = Replace-Once $text $old $new "Models\MathTool.cs / сброс и FilterPoint"
    Write-Normalized $path $text
    Write-Host "[3/8] Добавлены ВАХ, КВХ и внутреннее состояние." -ForegroundColor Green


    # ============================================================
    # 3. Core\CalculationEngine.cs
    # ============================================================
    $path = Join-Path $ProjectDir "Core\CalculationEngine.cs"
    $text = Read-Normalized $path

    $old = @'
                case ToolType.SubSystem:
                    return CalculateSubSystem(tool, tools, connections, results);
'@

    $new = @'
                case ToolType.NonlinearFirstOrderFilter:
                    {
                        double input = GetInputValue(
                            tool,
                            InputType.A,
                            tools,
                            connections,
                            results);

                        if (double.IsNaN(input))
                            return null;

                        return ProcessNonlinearFirstOrderFilter(tool, input);
                    }

                case ToolType.SubSystem:
                    return CalculateSubSystem(tool, tools, connections, results);
'@

    $text = Replace-Once $text $old $new "CalculationEngine / верхний уровень"

    $old = @'
                case ToolType.SubSystem:
                    return CalculateSubSystem(tool, tools, conns, results);
'@

    $new = @'
                case ToolType.NonlinearFirstOrderFilter:
                    {
                        double input = GetInternalInputValue(
                            tool,
                            InputType.A,
                            tools,
                            conns,
                            results,
                            portValues);

                        if (double.IsNaN(input))
                            return null;

                        return ProcessNonlinearFirstOrderFilter(tool, input);
                    }

                case ToolType.SubSystem:
                    return CalculateSubSystem(tool, tools, conns, results);
'@

    $text = Replace-Once $text $old $new "CalculationEngine / подсистема"

    $old = @'
        public double ProcessSpecialTool(MathTool tool, double input)
        {
'@

    $new = @'
        /// <summary>
        /// Один дискретный шаг нелинейного рекурсивного фильтра первого порядка.
        /// Вход берётся из соединения схемы, dt — из глобального TimeStep.
        /// Наружу возвращается только Uout. Ток iR хранится внутри как обратная связь.
        /// </summary>
        private double ProcessNonlinearFirstOrderFilter(MathTool tool, double input)
        {
            double dt = TimeStep;

            if (dt <= 0.0 || double.IsNaN(dt) || double.IsInfinity(dt))
                throw new InvalidOperationException(
                    "Шаг моделирования dt должен быть больше нуля.");

            ValidateCharacteristic(tool.FilterKVH, "КВХ");
            ValidateCharacteristic(tool.FilterVAH, "ВАХ");

            // A1: iC(j) = iIn(j) - iR(j-1)
            double iC =
                input -
                tool.FilterPreviousIR;

            // A2: q(j) = q(j-1) + dt * (iC(j) + iC(j-1)) / 2
            double q =
                tool.FilterCharge
                +
                dt *
                (iC + tool.FilterPreviousIC)
                / 2.0;

            // A3: КВХ q -> U. Это основной выход блока.
            double uOut =
                InterpolateClamped(
                    q,
                    tool.FilterKVH);

            // A4: ВАХ U -> I.
            // Ток нужен только для обратной связи следующего шага.
            double iR =
                InterpolateClamped(
                    uOut,
                    tool.FilterVAH);

            tool.FilterPreviousIC = iC;
            tool.FilterPreviousIR = iR;
            tool.FilterCharge = q;
            tool.FilterLastUOut = uOut;

            return uOut;
        }

        /// <summary>
        /// Линейная интерполяция между соседними точками.
        /// За пределами диапазона экстраполяции нет:
        /// результат зажимается на первом или последнем Y.
        /// </summary>
        private double InterpolateClamped(
            double x,
            List<FilterPoint> points)
        {
            var sorted =
                points
                .OrderBy(p => p.X)
                .ToList();

            if (x <= sorted[0].X)
                return sorted[0].Y;

            int last =
                sorted.Count - 1;

            if (x >= sorted[last].X)
                return sorted[last].Y;

            int left = 0;
            int right = last;

            while (right - left > 1)
            {
                int middle =
                    (left + right) / 2;

                if (x >= sorted[middle].X)
                    left = middle;
                else
                    right = middle;
            }

            double x1 = sorted[left].X;
            double y1 = sorted[left].Y;

            double x2 = sorted[right].X;
            double y2 = sorted[right].Y;

            double k =
                (x - x1) /
                (x2 - x1);

            return
                y1 +
                k * (y2 - y1);
        }

        private void ValidateCharacteristic(
            List<FilterPoint> points,
            string name)
        {
            if (points == null || points.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Для нелинейного фильтра необходимо задать минимум 2 точки {name}.");
            }

            var sorted =
                points
                .OrderBy(p => p.X)
                .ToList();

            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].X <= sorted[i - 1].X)
                {
                    throw new InvalidOperationException(
                        $"{name}: значения X должны быть уникальными.");
                }
            }
        }

        public double ProcessSpecialTool(MathTool tool, double input)
        {
'@

    $text = Replace-Once $text $old $new "CalculationEngine / математика НРФ1П"
    Write-Normalized $path $text
    Write-Host "[4/8] Добавлен расчёт A1-A4 и линейная интерполяция." -ForegroundColor Green


    # ============================================================
    # 4. Form1.cs
    # ============================================================
    $path = Join-Path $ProjectDir "Form1.cs"
    $text = Read-Normalized $path

    $old = @'
                "Интерполятор",

                "Файловый ввод/вывод",
'@

    $new = @'
                "Интерполятор",
                "Нелинейный фильтр 1 порядка",

                "Файловый ввод/вывод",
'@

    $text = Replace-Once $text $old $new "Form1 / библиотека блоков"

    $old = @'
            else if (toolType.Contains("Файловый"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.FileIO, Size = new Size(160, 100), Name = "Файловый ввод/вывод", IsReading = true, FileData = new List<double>() };
'@

    $new = @'
            else if (toolType.Contains("Нелинейный фильтр"))
                tool = new MathTool
                {
                    Position = realPos,
                    Type = ToolType.NonlinearFirstOrderFilter,
                    Size = new Size(170, 100),
                    Name = "Нелинейный фильтр 1 порядка",
                    FilterKVH = new List<FilterPoint>(),
                    FilterVAH = new List<FilterPoint>()
                };
            else if (toolType.Contains("Файловый"))
                tool = new MathTool { Position = realPos, Type = ToolType.Operation, Operation = MathOperation.FileIO, Size = new Size(160, 100), Name = "Файловый ввод/вывод", IsReading = true, FileData = new List<double>() };
'@

    $text = Replace-Once $text $old $new "Form1 / создание блока"
    Write-Normalized $path $text
    Write-Host "[5/8] НРФ1П добавлен в библиотеку блоков." -ForegroundColor Green


    # ============================================================
    # 5. Rendering\BlockRenderer.cs
    # ============================================================
    $path = Join-Path $ProjectDir "Rendering\BlockRenderer.cs"
    $text = Read-Normalized $path

    $old = @'
            if (tool.Operation == MathOperation.FileIO)
            {
'@

    $new = @'
            if (tool.Type == ToolType.NonlinearFirstOrderFilter)
            {
                DrawCenteredText(
                    g,
                    "НРФ1П",
                    new Font("Segoe UI", 12, FontStyle.Bold),
                    Brushes.DarkBlue,
                    rect);

                g.DrawString(
                    $"КВХ:{tool.FilterKVH?.Count ?? 0}  ВАХ:{tool.FilterVAH?.Count ?? 0}",
                    _paramFont,
                    Brushes.DarkGray,
                    rect.X + 10,
                    rect.Y + 10);

                g.DrawString(
                    "выход: Uout",
                    _paramFont,
                    Brushes.DarkGray,
                    rect.X + 10,
                    rect.Y + rect.Height - 20);
            }
            else if (tool.Operation == MathOperation.FileIO)
            {
'@

    $text = Replace-Once $text $old $new "BlockRenderer / отрисовка"
    Write-Normalized $path $text
    Write-Host "[6/8] Добавлено отображение блока." -ForegroundColor Green


    # ============================================================
    # 6. UI\PropertyPanel.cs
    # ============================================================
    $path = Join-Path $ProjectDir "UI\PropertyPanel.cs"
    $text = Read-Normalized $path

    $old = @'
                case ToolType.ADC: return "🔢 АЦП";
                case ToolType.SubSystem: return "🧩 Подсистема";
'@

    $new = @'
                case ToolType.ADC: return "🔢 АЦП";
                case ToolType.NonlinearFirstOrderFilter:
                    return "НРФ1П Нелинейный фильтр";
                case ToolType.SubSystem: return "🧩 Подсистема";
'@

    $text = Replace-Once $text $old $new "PropertyPanel / заголовок"

    $old = @'
            // ????
            else if (tool.Type == ToolType.Chart)
            {
'@

    $new = @'
            // Нелинейный рекурсивный фильтр 1 порядка
            else if (tool.Type == ToolType.NonlinearFirstOrderFilter)
            {
                var infoLabel = new Label
                {
                    Text =
                        $"КВХ: {tool.FilterKVH?.Count ?? 0} точек\n" +
                        $"ВАХ: {tool.FilterVAH?.Count ?? 0} точек\n" +
                        "Выход блока: Uout",
                    Location = new Point(10, yPos),
                    Width = 250,
                    Height = 55,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(52, 58, 64),
                    BackColor = Color.White
                };

                blockPanel.Controls.Add(infoLabel);
                yPos += 62;

                var editButton = new Button
                {
                    Text = "Редактировать ВАХ / КВХ",
                    Location = new Point(10, yPos),
                    Width = 250,
                    Height = 32,
                    BackColor = Color.FromArgb(0, 120, 212),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                editButton.FlatAppearance.BorderSize = 0;

                editButton.Click += (s, e) =>
                {
                    using (var form =
                        new NonlinearFilterCharacteristicsForm(tool))
                    {
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            ParametersChanged?.Invoke(this, EventArgs.Empty);
                            SetSelectedTool(tool);
                        }
                    }
                };

                blockPanel.Controls.Add(editButton);
                yPos += 40;

                var hintLabel = new Label
                {
                    Text =
                        "dt и входной сигнал берутся из общей схемы.\n" +
                        "За границами ВАХ/КВХ используется крайнее значение.",
                    Location = new Point(10, yPos),
                    Width = 250,
                    Height = 45,
                    Font = new Font("Segoe UI", 7, FontStyle.Italic),
                    ForeColor = Color.FromArgb(108, 117, 125),
                    BackColor = Color.White
                };

                blockPanel.Controls.Add(hintLabel);
                yPos += 50;
            }

            // ????
            else if (tool.Type == ToolType.Chart)
            {
'@

    $text = Replace-Once $text $old $new "PropertyPanel / параметры НРФ1П"
    Write-Normalized $path $text


    # ============================================================
    # 7. Новое окно ручного ввода ВАХ/КВХ
    # ============================================================
    $formText = @'
using MathApp.Helpers;
using MathApp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MathApp.UI
{
    public class NonlinearFilterCharacteristicsForm : Form
    {
        private readonly MathTool _tool;
        private readonly DataGridView _kvhGrid;
        private readonly DataGridView _vahGrid;

        public NonlinearFilterCharacteristicsForm(MathTool tool)
        {
            _tool = tool ?? throw new ArgumentNullException(nameof(tool));

            Text = "Характеристики нелинейного фильтра 1 порядка";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(720, 560);
            MinimumSize = new Size(650, 480);
            BackColor = Color.FromArgb(248, 249, 250);

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(12, 6)
            };

            _kvhGrid = CreateGrid("Заряд q", "Напряжение U");
            _vahGrid = CreateGrid("Напряжение U", "Ток I");

            tabs.TabPages.Add(
                CreateTab(
                    "КВХ (q → U)",
                    _kvhGrid,
                    "X = заряд q, Y = выходное напряжение U."));

            tabs.TabPages.Add(
                CreateTab(
                    "ВАХ (U → I)",
                    _vahGrid,
                    "X = напряжение U, Y = ток I для внутренней обратной связи."));

            FillGrid(_kvhGrid, _tool.FilterKVH);
            FillGrid(_vahGrid, _tool.FilterVAH);

            var bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                Padding = new Padding(10)
            };

            var okButton = new Button
            {
                Text = "Применить",
                Width = 120,
                Height = 32,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Left = 450,
                Top = 12
            };

            okButton.Click += SaveAndClose;

            var cancelButton = new Button
            {
                Text = "Отмена",
                Width = 100,
                Height = 32,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Left = 580,
                Top = 12,
                DialogResult = DialogResult.Cancel
            };

            bottom.Controls.Add(okButton);
            bottom.Controls.Add(cancelButton);

            Controls.Add(tabs);
            Controls.Add(bottom);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private TabPage CreateTab(
            string title,
            DataGridView grid,
            string description)
        {
            var page = new TabPage(title);

            var hint = new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                Text =
                    description +
                    "\nМожно вводить обычную и инженерную запись: 0.001, 1e-3, 500n и т.п.",
                Padding = new Padding(8, 4, 8, 4)
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Padding = new Padding(6),
                FlowDirection = FlowDirection.LeftToRight
            };

            var add = new Button
            {
                Text = "Добавить точку",
                Width = 125,
                Height = 28
            };

            add.Click += (s, e) => grid.Rows.Add();

            var remove = new Button
            {
                Text = "Удалить выбранные",
                Width = 145,
                Height = 28
            };

            remove.Click += (s, e) =>
            {
                foreach (
                    DataGridViewRow row
                    in grid.SelectedRows.Cast<DataGridViewRow>().ToList())
                {
                    if (!row.IsNewRow)
                        grid.Rows.Remove(row);
                }
            };

            buttons.Controls.Add(add);
            buttons.Controls.Add(remove);

            page.Controls.Add(grid);
            page.Controls.Add(hint);
            page.Controls.Add(buttons);

            return page;
        }

        private DataGridView CreateGrid(
            string xHeader,
            string yHeader)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };

            grid.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    HeaderText = xHeader
                });

            grid.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    HeaderText = yHeader
                });

            return grid;
        }

        private void FillGrid(
            DataGridView grid,
            List<FilterPoint> points)
        {
            if (points == null)
                return;

            foreach (var p in points.OrderBy(p => p.X))
            {
                grid.Rows.Add(
                    EngineeringParser.ToEngineeringString(p.X),
                    EngineeringParser.ToEngineeringString(p.Y));
            }
        }

        private void SaveAndClose(
            object sender,
            EventArgs e)
        {
            try
            {
                var kvh = ReadGrid(_kvhGrid, "КВХ");
                var vah = ReadGrid(_vahGrid, "ВАХ");

                if (kvh.Count < 2)
                {
                    throw new InvalidOperationException(
                        "КВХ должна содержать минимум 2 точки.");
                }

                if (vah.Count < 2)
                {
                    throw new InvalidOperationException(
                        "ВАХ должна содержать минимум 2 точки.");
                }

                _tool.FilterKVH = kvh;
                _tool.FilterVAH = vah;

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка характеристики",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private List<FilterPoint> ReadGrid(
            DataGridView grid,
            string name)
        {
            var points = new List<FilterPoint>();

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string xText =
                    Convert.ToString(row.Cells[0].Value)?.Trim();

                string yText =
                    Convert.ToString(row.Cells[1].Value)?.Trim();

                if (
                    string.IsNullOrWhiteSpace(xText)
                    &&
                    string.IsNullOrWhiteSpace(yText))
                {
                    continue;
                }

                if (
                    !EngineeringParser.TryParse(xText, out double x)
                    ||
                    !EngineeringParser.TryParse(yText, out double y))
                {
                    throw new InvalidOperationException(
                        $"{name}: не удалось прочитать точку в строке {row.Index + 1}.");
                }

                if (
                    double.IsNaN(x)
                    ||
                    double.IsInfinity(x)
                    ||
                    double.IsNaN(y)
                    ||
                    double.IsInfinity(y))
                {
                    throw new InvalidOperationException(
                        $"{name}: строка {row.Index + 1} содержит недопустимое число.");
                }

                points.Add(new FilterPoint(x, y));
            }

            points =
                points
                .OrderBy(p => p.X)
                .ToList();

            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].X <= points[i - 1].X)
                {
                    throw new InvalidOperationException(
                        $"{name}: значения X должны быть уникальными.");
                }
            }

            return points;
        }
    }
}
'@

    $newFormDir = Split-Path -Parent $NewFormPath

    if (-not (Test-Path -LiteralPath $newFormDir)) {
        New-Item -ItemType Directory -Path $newFormDir -Force | Out-Null
    }

    Write-Normalized $NewFormPath $formText


    # ============================================================
    # 8. ГПО.csproj
    # ============================================================
    $path = $Csproj
    $text = Read-Normalized $path

    $old = @'
    <Compile Include="UI\GraphForm.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="UI\PropertyPanel.cs">
'@

    $new = @'
    <Compile Include="UI\GraphForm.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="UI\NonlinearFilterCharacteristicsForm.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="UI\PropertyPanel.cs">
'@

    $text = Replace-Once $text $old $new "ГПО.csproj / новое окно"
    Write-Normalized $path $text

    Write-Host "[7/8] Добавлен редактор ВАХ / КВХ." -ForegroundColor Green
    Write-Host "[8/8] Файл подключён к проекту." -ForegroundColor Green

    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Green
    Write-Host " ГОТОВО: НРФ1П установлен" -ForegroundColor Green
    Write-Host "==============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Резервная копия исходных файлов:" -ForegroundColor Cyan
    Write-Host "  $BackupRoot" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Теперь вернись в Visual Studio и нажми:" -ForegroundColor White
    Write-Host "  Ctrl + Shift + B" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "После успешной сборки запускай программу." -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "УСТАНОВКА НЕ ЗАВЕРШЕНА." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Возвращаю исходные файлы из резервной копии..." -ForegroundColor Yellow

    foreach ($relative in $RelativeFiles) {
        $src = Join-Path $BackupRoot $relative
        $dst = Join-Path $ProjectDir $relative

        if (Test-Path -LiteralPath $src) {
            Copy-Item -LiteralPath $src -Destination $dst -Force
        }
    }

    if (Test-Path -LiteralPath $NewFormPath) {
        Remove-Item -LiteralPath $NewFormPath -Force
    }

    Write-Host "Исходные файлы восстановлены." -ForegroundColor Green
    Write-Host ""
    exit 1
}

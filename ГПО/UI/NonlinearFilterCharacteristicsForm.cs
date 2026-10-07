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
            Size = new Size(800, 600);
            MinimumSize = new Size(650, 450);
            BackColor = Color.FromArgb(248, 249, 250);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(8)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));

            var tabs = new TabControl { Dock = DockStyle.Fill };

            _kvhGrid = CreateGrid("Заряд q", "Напряжение U");
            _vahGrid = CreateGrid("Напряжение U", "Ток I");

            tabs.TabPages.Add(CreateTab(
                "КВХ (q → U)",
                _kvhGrid,
                "X = заряд q, Y = выходное напряжение U."));

            tabs.TabPages.Add(CreateTab(
                "ВАХ (U → I)",
                _vahGrid,
                "X = напряжение U, Y = ток I для внутренней обратной связи."));

            FillGrid(_kvhGrid, _tool.FilterKVH);
            FillGrid(_vahGrid, _tool.FilterVAH);

            var bottomPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(5)
            };

            var cancelButton = new Button
            {
                Text = "Отмена",
                Width = 110,
                Height = 34,
                Margin = new Padding(8, 4, 0, 4),
                DialogResult = DialogResult.Cancel
            };

            var applyButton = new Button
            {
                Text = "Применить",
                Width = 130,
                Height = 34,
                Margin = new Padding(8, 4, 0, 4),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            applyButton.FlatAppearance.BorderSize = 0;
            applyButton.Click += SaveAndClose;

            bottomPanel.Controls.Add(cancelButton);
            bottomPanel.Controls.Add(applyButton);

            mainLayout.Controls.Add(tabs, 0, 0);
            mainLayout.Controls.Add(bottomPanel, 0, 1);
            Controls.Add(mainLayout);

            AcceptButton = applyButton;
            CancelButton = cancelButton;
        }

        private TabPage CreateTab(string title, DataGridView grid, string description)
        {
            var page = new TabPage(title);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(6)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));

            var hint = new Label
            {
                Dock = DockStyle.Fill,
                Text = description + Environment.NewLine +
                       "Можно вводить: 0.001, 1e-3, 500n, 2m и т.п.",
                Padding = new Padding(6),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(52, 58, 64)
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(3)
            };

            var addButton = new Button
            {
                Text = "Добавить точку",
                Width = 130,
                Height = 32
            };

            var deleteButton = new Button
            {
                Text = "Удалить выбранные",
                Width = 155,
                Height = 32
            };

            addButton.Click += (s, e) =>
            {
                int rowIndex = grid.Rows.Add();
                if (rowIndex >= 0)
                {
                    grid.CurrentCell = grid.Rows[rowIndex].Cells[0];
                    grid.BeginEdit(true);
                }
            };

            deleteButton.Click += (s, e) =>
            {
                var selectedRows = grid.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Where(r => !r.IsNewRow)
                    .ToList();

                foreach (var row in selectedRows)
                    grid.Rows.Remove(row);
            };

            buttons.Controls.Add(addButton);
            buttons.Controls.Add(deleteButton);

            layout.Controls.Add(hint, 0, 0);
            layout.Controls.Add(grid, 0, 1);
            layout.Controls.Add(buttons, 0, 2);
            page.Controls.Add(layout);

            return page;
        }

        private DataGridView CreateGrid(string xHeader, string yHeader)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                RowHeadersVisible = true,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "X",
                HeaderText = xHeader,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Y",
                HeaderText = yHeader,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            return grid;
        }

        private void FillGrid(DataGridView grid, List<FilterPoint> points)
        {
            if (points == null) return;

            foreach (var point in points.OrderBy(p => p.X))
            {
                grid.Rows.Add(
                    EngineeringParser.ToEngineeringString(point.X),
                    EngineeringParser.ToEngineeringString(point.Y));
            }
        }

        private void SaveAndClose(object sender, EventArgs e)
        {
            try
            {
                _kvhGrid.EndEdit();
                _vahGrid.EndEdit();

                var kvh = ReadGrid(_kvhGrid, "КВХ");
                var vah = ReadGrid(_vahGrid, "ВАХ");

                if (kvh.Count < 2)
                    throw new InvalidOperationException("КВХ должна содержать минимум 2 точки.");
                if (vah.Count < 2)
                    throw new InvalidOperationException("ВАХ должна содержать минимум 2 точки.");

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

        private List<FilterPoint> ReadGrid(DataGridView grid, string name)
        {
            var points = new List<FilterPoint>();

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;

                string xText = Convert.ToString(row.Cells[0].Value)?.Trim();
                string yText = Convert.ToString(row.Cells[1].Value)?.Trim();

                if (string.IsNullOrWhiteSpace(xText) &&
                    string.IsNullOrWhiteSpace(yText))
                    continue;

                if (string.IsNullOrWhiteSpace(xText) ||
                    string.IsNullOrWhiteSpace(yText))
                    throw new InvalidOperationException(
                        $"{name}: строка {row.Index + 1} заполнена не полностью.");

                if (!EngineeringParser.TryParse(xText, out double x) ||
                    !EngineeringParser.TryParse(yText, out double y))
                    throw new InvalidOperationException(
                        $"{name}: не удалось прочитать точку в строке {row.Index + 1}.");

                points.Add(new FilterPoint(x, y));
            }

            points = points.OrderBy(p => p.X).ToList();

            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].X <= points[i - 1].X)
                    throw new InvalidOperationException(
                        $"{name}: значения X должны быть уникальными.");
            }

            return points;
        }
    }
}

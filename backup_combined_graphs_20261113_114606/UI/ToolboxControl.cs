using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MathApp.Helpers;

namespace MathApp.UI
{
    public class ToolboxControl : UserControl
    {
        private ListBox _listBox;

        public event MouseEventHandler ItemMouseDown;

        public ToolboxControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(260, 280); 
            this.BackColor = Color.Transparent;

            var titleLabel = new Label
            {
                Text = "📦 ИНСТРУМЕНТЫ",
                Location = new Point(0, 0),
                Size = new Size(400, 35),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _listBox = new ListBox
            {
                Location = new Point(0, 40),
                Size = new Size(260, 230),  
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                ItemHeight = 30,
                DrawMode = DrawMode.OwnerDrawFixed
            };

            _listBox.DrawItem += ListBox_DrawItem;
            _listBox.MouseDown += (s, e) => ItemMouseDown?.Invoke(s, e);

            _listBox.Items.AddRange(new object[]
            {
                "➕ Сложение",
                "➖ Вычитание",
                "✖️ Умножение",
                "➗ Деление",
                "∫ Интегратор",
                "d/dt Дифференциатор",
                "f(x) Интерполятор",
                "↩ Обратная связь",
                "📁 Файловый ввод/вывод",
                "📊 График",
                "📈 Синусоида",
                "▰ Трапециевидный сигнал",
                "▮ Меандр",
                "◢ Пиловидный сигнал",
                "🧩 Подсистема"
            });

            this.Controls.AddRange(new Control[] { titleLabel, _listBox });
        }

        public string GetSelectedItem()
        {
            return _listBox.SelectedItem?.ToString();
        }

        private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            var rect = e.Bounds;
            rect.Inflate(-2, -2);

            using (var path = GraphicsExtensions.CreateRoundedRectangle(rect, 5))
            {
                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(0, 120, 212)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
                else if ((e.State & DrawItemState.HotLight) == DrawItemState.HotLight)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(60, 60, 65)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                var text = _listBox.Items[e.Index].ToString();
                using (var sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                })
                {
                    e.Graphics.DrawString(text, e.Font, Brushes.White, rect, sf);
                }
            }

            e.DrawFocusRectangle();
        }
    }


    // Единая библиотека блоков в виде папок, как в Проводнике.

    public static class BlockLibraryTree

    {

        public static TreeView Create()

        {

            var tree = new TreeView

            {

                Dock = DockStyle.Fill,

                HideSelection = false,

                ShowLines = false,

                ShowRootLines = false,

                ShowPlusMinus = true,

                BorderStyle = BorderStyle.None,

                FullRowSelect = true,

                ItemHeight = 28,

                Indent = 18,

                Font = new Font("Segoe UI", 9),

                BackColor = Color.FromArgb(248, 249, 250),

                ForeColor = Color.FromArgb(40, 48, 58)

            };



            Add(tree, "Математика", new [] {

                "Сложение", "Вычитание", "Умножение", "Деление",

                "Интегратор", "Дифференциатор", "Интерполятор",

                "Файловый ввод/вывод"

            });

            Add(tree, "Генераторы", new [] {

                "Генератор синусоиды", "Трапециевидный сигнал", "Меандр",

                "Треугольный импульс", "Единичный сигнал", "Постоянный сигнал"

            });

            Add(tree, "Фильтры и обратная связь", new [] {

                "Нелинейный фильтр 1 порядка", "Обратная связь"

            });

            Add(tree, "Визуализация", new [] { "График" });

            Add(tree, "Специальные блоки", new [] {

                "Усилитель", "Антенна", "Канал", "Объект", "АЦП"

            });

            Add(tree, "Структура", new [] { "Подсистема" });



            tree.NodeMouseClick += (sender, e) =>

            {

                if (e.Button == MouseButtons.Left && e.Node.Parent == null)

                {

                    // Узлы раскрываются по одиночному нажатию по всей строке.

                    if (e.Node.IsExpanded) e.Node.Collapse();

                    else e.Node.Expand();

                }

            };

            tree.ItemDrag += (sender, e) =>

            {

                var node = e.Item as TreeNode;

                if (node != null && node.Parent != null)

                    tree.DoDragDrop(new DataObject("ToolType", node.Tag as string), DragDropEffects.Copy);

            };

            return tree;

        }



        private static void Add(TreeView tree, string category, string[] names)

        {

            var folder = new TreeNode("📁 " + category) { NodeFont = new Font("Segoe UI", 9, FontStyle.Bold) };

            foreach (string name in names)

                folder.Nodes.Add(new TreeNode("  " + name) { Tag = name });

            tree.Nodes.Add(folder);

        }

    }
}
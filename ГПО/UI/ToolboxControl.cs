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
            this.Size = new Size(260, 320);
            this.BackColor = Color.Transparent;

            var titleLabel = new Label
            {
                Text = "📦 ИНСТРУМЕНТЫ",
                Location = new Point(0, 0),
                Size = new Size(260, 35),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _listBox = new ListBox
            {
                Location = new Point(0, 40),
                Size = new Size(260, 270),
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = Color.White,
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
                "📊 График",
                "📈 Синусоида",
                "⚡ Генератор",
                "▲ Усилитель",
                "📡 Антенна",
                "〰️ Канал",
                "◻️ Объект",
                "🔢 АЦП"
            });

            this.Controls.AddRange(new Control[] { titleLabel, _listBox });
        }

        public string GetSelectedItem() => _listBox.SelectedItem?.ToString();

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
                        e.Graphics.FillPath(brush, path);
                }
                else if ((e.State & DrawItemState.HotLight) == DrawItemState.HotLight)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(60, 60, 65)))
                        e.Graphics.FillPath(brush, path);
                }

                var text = _listBox.Items[e.Index].ToString();
                using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString(text, e.Font, Brushes.White, rect, sf);
            }
            e.DrawFocusRectangle();
        }
    }
}
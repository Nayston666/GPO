using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MathApp.Helpers;

namespace MathApp.UI
{
    public class ToolboxControl : UserControl
    {
        private ComboBox _comboBox;
        private Label _titleLabel;
        public event MouseEventHandler ItemMouseDown;

        public ToolboxControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(260, 60);
            this.BackColor = Color.Transparent;

            _titleLabel = new Label
            {
                Text = "📦 ИНСТРУМЕНТЫ",
                Location = new Point(0, 5),
                Size = new Size(260, 25),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            _comboBox = new ComboBox
            {
                Location = new Point(0, 35),
                Size = new Size(260, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            _comboBox.Items.AddRange(new object[]
            {
            "➕ Сложение",
            "➖ Вычитание",
            "✖️ Умножение",
            "➗ Деление",
            "∫ Интегратор (трапеции)",
            "d/dt Дифференциатор",
            "f(x) Интерполятор",
            "📁 Файловый ввод/вывод",
            "⤴ Ступенька",
            "[ ] График",
            "~ Синусоида"
            });
            _comboBox.SelectedIndex = 0;

            _comboBox.SelectedIndexChanged += (s, e) =>
            {
                if (_comboBox.SelectedIndex >= 0 && ItemMouseDown != null)
                {
                    ItemMouseDown(_comboBox, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                }
            };

            this.Controls.AddRange(new Control[] { _titleLabel, _comboBox });
        }

        public string GetSelectedItem()
        {
            return _comboBox.SelectedItem?.ToString();
        }

        public void SetSelectedIndex(int index)
        {
            if (index >= 0 && index < _comboBox.Items.Count)
                _comboBox.SelectedIndex = index;
        }
    }
}
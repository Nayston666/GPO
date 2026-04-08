using System;
using System.Drawing;
using System.Windows.Forms;

namespace MathApp.UI
{
    public partial class SubSystemForm : Form
    {
        public SubSystemForm()
        {
            InitializeComponent();
        }

        public SubSystemForm(string name)
        {
            InitializeComponent();
            this.Text = name;
        }

        private void InitializeComponent()
        {
            this.Text = "ПОДСИСТЕМА";
            this.Size = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 35);
            this.MinimumSize = new Size(900, 650);

            // Верхняя панель
            var topPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(this.Width, 90),
                BackColor = Color.FromArgb(38, 38, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var titleLabel = new Label
            {
                Text = "🧩 РЕДАКТОР ПОДСХЕМЫ",
                Location = new Point(15, 15),
                Size = new Size(300, 30),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            var infoLabel = new Label
            {
                Text = "Входные порты (зеленые) - СЛЕВА | Выходные порты (красные) - СПРАВА",
                Location = new Point(15, 50),
                Size = new Size(500, 25),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.Transparent
            };

            topPanel.Controls.AddRange(new Control[] { titleLabel, infoLabel });

            // Правая панель инструментов
            var toolsPanel = new Panel
            {
                Location = new Point(this.Width - 190, 100),
                Size = new Size(175, 360),
                BackColor = Color.FromArgb(45, 45, 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            var toolsTitle = new Label
            {
                Text = "🛠️ ИНСТРУМЕНТЫ",
                Location = new Point(10, 8),
                Size = new Size(155, 25),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var btnAddInput = new Button
            {
                Text = "➕ Входной порт",
                Location = new Point(10, 40),
                Size = new Size(155, 32),
                BackColor = Color.FromArgb(50, 150, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnAddInput.Click += (s, e) => MessageBox.Show("Добавление портов будет в следующей версии", "Info");

            var btnAddOutput = new Button
            {
                Text = "📤 Выходной порт",
                Location = new Point(10, 78),
                Size = new Size(155, 32),
                BackColor = Color.FromArgb(200, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnAddOutput.Click += (s, e) => MessageBox.Show("Добавление портов будет в следующей версии", "Info");

            var separator = new Label
            {
                Text = "──────────────",
                Location = new Point(10, 118),
                Size = new Size(155, 15),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var btnAddAdd = new Button
            {
                Text = "+ Сложение",
                Location = new Point(10, 140),
                Size = new Size(155, 30),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnAddSub = new Button
            {
                Text = "- Вычитание",
                Location = new Point(10, 175),
                Size = new Size(155, 30),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnAddMul = new Button
            {
                Text = "× Умножение",
                Location = new Point(10, 210),
                Size = new Size(155, 30),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnAddDiv = new Button
            {
                Text = "÷ Деление",
                Location = new Point(10, 245),
                Size = new Size(155, 30),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnAddSine = new Button
            {
                Text = "📈 Синусоида",
                Location = new Point(10, 280),
                Size = new Size(155, 30),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnAddChart = new Button
            {
                Text = "📊 График",
                Location = new Point(10, 315),
                Size = new Size(155, 30),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            toolsPanel.Controls.AddRange(new Control[] {
                toolsTitle, btnAddInput, btnAddOutput, separator,
                btnAddAdd, btnAddSub, btnAddMul, btnAddDiv, btnAddSine, btnAddChart
            });

            // Рабочая область
            var workspace = new Panel
            {
                Location = new Point(15, 100),
                Size = new Size(this.Width - 225, this.Height - 270),
                BackColor = Color.FromArgb(35, 35, 40),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            var tempLabel = new Label
            {
                Text = "⚙️ Редактор подсхем (в разработке)\n\n" +
                       "• Нажмите кнопки справа для добавления блоков\n" +
                       "• Перетаскивайте блоки мышкой\n" +
                       "• Соединяйте выходные точки с входными",
                Location = new Point(workspace.Width / 2 - 180, workspace.Height / 2 - 60),
                Size = new Size(360, 100),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.None
            };
            workspace.Controls.Add(tempLabel);

            // Нижняя панель с кнопками
            var bottomPanel = new Panel
            {
                Location = new Point(10, this.Height - 140),
                Size = new Size(this.Width - 20, 100),
                BackColor = Color.FromArgb(40, 40, 45),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            var btnSave = new Button
            {
                Text = "✓ СОХРАНИТЬ",
                Location = new Point(bottomPanel.Width - 180, 32),
                Size = new Size(85, 38),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnSave.Click += (s, e) =>
            {
                MessageBox.Show("Сохранение подсхемы будет доступно в следующей версии",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            var btnCancel = new Button
            {
                Text = "ОТМЕНА",
                Location = new Point(bottomPanel.Width - 90, 32),
                Size = new Size(80, 38),
                BackColor = Color.FromArgb(70, 70, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnCancel.Click += (s, e) => this.Close();

            bottomPanel.Controls.AddRange(new Control[] { btnSave, btnCancel });

            this.Controls.Add(topPanel);
            this.Controls.Add(toolsPanel);
            this.Controls.Add(workspace);
            this.Controls.Add(bottomPanel);

            this.Resize += (s, e) =>
            {
                topPanel.Width = this.Width;
                workspace.Size = new Size(this.Width - 225, this.Height - 270);
                toolsPanel.Location = new Point(this.Width - 190, 100);
                bottomPanel.Location = new Point(10, this.Height - 140);
                bottomPanel.Width = this.Width - 20;
                btnSave.Location = new Point(bottomPanel.Width - 180, 32);
                btnCancel.Location = new Point(bottomPanel.Width - 90, 32);
                tempLabel.Location = new Point(workspace.Width / 2 - 180, workspace.Height / 2 - 60);
            };
        }
    }
}
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MathApp.Helpers;

namespace MathApp.UI
{
    public class ResultForm : Form
    {
        private Label _valueLabel;
        private Label _timestampLabel;
        private Panel _colorPanel;
        private Timer _animationTimer;
        private double _currentValue = 0, _targetValue = 0;

        public ResultForm()
        {
            InitializeComponent();
            _animationTimer = new Timer { Interval = 16 };
            _animationTimer.Tick += (s, e) =>
            {
                if (Math.Abs(_currentValue - _targetValue) > 0.01)
                {
                    _currentValue += (_targetValue - _currentValue) * 0.1;
                    _valueLabel.Text = _currentValue.ToString("F2");
                    UpdateColor();
                }
            };
            _animationTimer.Start();
        }

        private void InitializeComponent()
        {
            Text = "Результат вычислений";
            Size = new Size(400, 320);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(28, 28, 30);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var titleLabel = new Label
            {
                Text = "РЕЗУЛЬТАТ ВЫЧИСЛЕНИЙ",
                Location = new Point(0, 20),
                Size = new Size(400, 30),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _colorPanel = new Panel
            {
                Location = new Point(50, 70),
                Size = new Size(300, 150),
                BackColor = Color.FromArgb(0, 120, 212)
            };
            _colorPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var path = GraphicsExtensions.CreateRoundedRectangle(new Rectangle(0, 0, _colorPanel.Width - 1, _colorPanel.Height - 1), 15))
                using (var brush = new SolidBrush(_colorPanel.BackColor))
                using (var pen = new Pen(Color.FromArgb(60, 60, 65), 2))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }
            };

            _valueLabel = new Label
            {
                Text = "0.00",
                Location = new Point(0, 95),
                Size = new Size(400, 100),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 48, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _timestampLabel = new Label
            {
                Text = $"Обновлено: {DateTime.Now:HH:mm:ss}",
                Location = new Point(0, 230),
                Size = new Size(400, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var chkTopMost = new CheckBox
            {
                Text = "Поверх всех окон",
                Location = new Point(50, 260),
                Size = new Size(150, 25),
                ForeColor = Color.LightGray,
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };
            chkTopMost.CheckedChanged += (s, e) => TopMost = chkTopMost.Checked;

            var btnReset = new Button
            {
                Text = "Сбросить",
                Location = new Point(220, 260),
                Size = new Size(70, 25),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnReset.Click += (s, e) => SetValueImmediate(0);

            var btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(300, 260),
                Size = new Size(70, 25),
                BackColor = Color.FromArgb(200, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.Click += (s, e) => Hide();

            Controls.AddRange(new Control[] { titleLabel, _colorPanel, _valueLabel, _timestampLabel, chkTopMost, btnReset, btnClose });
        }

        private void UpdateColor()
        {
            if (_targetValue > 0) _colorPanel.BackColor = Color.FromArgb(0, Math.Min(255, 120 + (int)Math.Abs(_targetValue) * 10), 100);
            else if (_targetValue < 0) _colorPanel.BackColor = Color.FromArgb(Math.Min(255, 150 + (int)Math.Abs(_targetValue) * 10), 50, 50);
            else _colorPanel.BackColor = Color.FromArgb(0, 120, 212);
            _colorPanel.Invalidate();
        }

        public void SetValueImmediate(double value)
        {
            _targetValue = _currentValue = value;
            _valueLabel.Text = value.ToString("F2");
            _timestampLabel.Text = $"Обновлено: {DateTime.Now:HH:mm:ss}";
            UpdateColor();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing) { if (disposing) _animationTimer?.Dispose(); base.Dispose(disposing); }
    }
}
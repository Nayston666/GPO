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
        private Label _titleLabel;
        private Label _timestampLabel;
        private Panel _colorPanel;
        private Timer _animationTimer;
        private double _currentValue = 0;
        private double _targetValue = 0;
        private double _animationSpeed = 0.1;

        public ResultForm()
        {
            InitializeComponent();

            _animationTimer = new Timer();
            _animationTimer.Interval = 16;
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Text = "Результат вычислений";
            this.Size = new Size(400, 320);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(28, 28, 30);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            _titleLabel = new Label
            {
                Text = "РЕЗУЛЬТАТ ВЫЧИСЛЕНИЙ",
                Location = new Point(0, 20),
                Size = new Size(400, 30),
                ForeColor = Color.FromArgb(0, 200, 255),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            _colorPanel = new Panel
            {
                Location = new Point(50, 70),
                Size = new Size(300, 150),
                BackColor = Color.FromArgb(0, 120, 212),
                BorderStyle = BorderStyle.None
            };

            _colorPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = GraphicsExtensions.CreateRoundedRectangle(
                    new Rectangle(0, 0, _colorPanel.Width - 1, _colorPanel.Height - 1), 15))
                {
                    using (var brush = new SolidBrush(_colorPanel.BackColor))
                        g.FillPath(brush, path);
                    using (var pen = new Pen(Color.FromArgb(60, 60, 65), 2))
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
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            _timestampLabel = new Label
            {
                Text = $"Обновлено: {DateTime.Now:HH:mm:ss}",
                Location = new Point(0, 230),
                Size = new Size(400, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var chkTopMost = new CheckBox
            {
                Text = "Поверх всех окон",
                Location = new Point(50, 260),
                Size = new Size(150, 25),
                ForeColor = Color.LightGray,
                Checked = true,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat
            };
            chkTopMost.CheckedChanged += (s, e) => this.TopMost = chkTopMost.Checked;

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
            btnClose.Click += (s, e) => this.Hide();

            this.Controls.AddRange(new Control[] { _titleLabel, _colorPanel, _valueLabel, _timestampLabel, chkTopMost, btnReset, btnClose });
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (Math.Abs(_currentValue - _targetValue) > 0.01)
            {
                _currentValue += (_targetValue - _currentValue) * _animationSpeed;
                _valueLabel.Text = _currentValue.ToString("F2");
                UpdateColor();
            }
        }

        public void UpdateValue(double value)
        {
            _targetValue = value;
            _timestampLabel.Text = $"Обновлено: {DateTime.Now:HH:mm:ss}";
        }

        public void SetValueImmediate(double value)
        {
            _targetValue = value;
            _currentValue = value;
            _valueLabel.Text = value.ToString("F2");
            _timestampLabel.Text = $"Обновлено: {DateTime.Now:HH:mm:ss}";
            UpdateColor();
        }

        private void UpdateColor()
        {
            if (_targetValue > 0)
            {
                int green = (int)Math.Min(255, 120 + Math.Abs(_targetValue) * 10);
                _colorPanel.BackColor = Color.FromArgb(0, green, 100);
            }
            else if (_targetValue < 0)
            {
                int red = (int)Math.Min(255, 150 + Math.Abs(_targetValue) * 10);
                _colorPanel.BackColor = Color.FromArgb(red, 50, 50);
            }
            else
                _colorPanel.BackColor = Color.FromArgb(0, 120, 212);
            _colorPanel.Invalidate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _animationTimer?.Dispose();
            base.Dispose(disposing);
        }
    }
}
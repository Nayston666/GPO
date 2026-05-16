using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MathApp.Models;

namespace MathApp.UI
{
    public class AllGraphsWindow : Form
    {
        private List<MathTool> charts;
        private Dictionary<Guid, GraphForm> existingGraphs;
        private Timer updateTimer;
        private Panel containerPanel;
        private List<Panel> graphPanels = new List<Panel>();
        private Dictionary<Guid, List<double>> graphData = new Dictionary<Guid, List<double>>();

        public AllGraphsWindow(List<MathTool> charts, Dictionary<Guid, GraphForm> existingGraphs)
        {
            this.charts = charts;
            this.existingGraphs = existingGraphs;

            foreach (var chart in charts)
            {
                graphData[chart.Id] = new List<double>();
            }

            this.Text = "Все графики системы";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 35);
            this.MinimumSize = new Size(800, 600);

            containerPanel = new Panel();
            containerPanel.Dock = DockStyle.Fill;
            containerPanel.AutoScroll = true;
            containerPanel.BackColor = Color.FromArgb(30, 30, 35);

            var topPanel = new Panel();
            topPanel.Height = 45;
            topPanel.Dock = DockStyle.Top;
            topPanel.BackColor = Color.FromArgb(45, 45, 48);

            var refreshBtn = new Button();
            refreshBtn.Text = "🔄 Обновить";
            refreshBtn.Location = new Point(10, 8);
            refreshBtn.Size = new Size(100, 30);
            refreshBtn.BackColor = Color.FromArgb(108, 117, 125);
            refreshBtn.ForeColor = Color.White;
            refreshBtn.FlatStyle = FlatStyle.Flat;
            refreshBtn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            refreshBtn.Click += new EventHandler(RefreshAllGraphs);
            topPanel.Controls.Add(refreshBtn);

            var clearAllBtn = new Button();
            clearAllBtn.Text = "🗑 Очистить все";
            clearAllBtn.Location = new Point(120, 8);
            clearAllBtn.Size = new Size(100, 30);
            clearAllBtn.BackColor = Color.FromArgb(108, 117, 125);
            clearAllBtn.ForeColor = Color.White;
            clearAllBtn.FlatStyle = FlatStyle.Flat;
            clearAllBtn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            clearAllBtn.Click += new EventHandler(ClearAllGraphs);
            topPanel.Controls.Add(clearAllBtn);

            var infoLabel = new Label();
            infoLabel.Text = "Графики строятся по глобальным параметрам";
            infoLabel.Location = new Point(230, 12);
            infoLabel.Size = new Size(400, 25);
            infoLabel.ForeColor = Color.LightGray;
            infoLabel.Font = new Font("Segoe UI", 9);
            topPanel.Controls.Add(infoLabel);

            this.Controls.Add(containerPanel);
            this.Controls.Add(topPanel);

            CreateGraphGrid();

            updateTimer = new Timer();
            updateTimer.Interval = 50;
            updateTimer.Tick += new EventHandler(UpdateAllGraphs);
            updateTimer.Start();
        }

        private void ClearAllGraphs(object sender, EventArgs e)
        {
            foreach (var chart in charts)
            {
                if (graphData.ContainsKey(chart.Id))
                {
                    graphData[chart.Id].Clear();
                }
            }
            RefreshAllGraphs(null, null);
        }

        private void CreateGraphGrid()
        {
            graphPanels.Clear();
            containerPanel.Controls.Clear();

            if (charts.Count == 0)
            {
                var emptyLabel = new Label();
                emptyLabel.Text = "Нет блоков 'График' на схеме.\n\nДобавьте блоки из библиотеки и подключите их к источникам сигнала.";
                emptyLabel.ForeColor = Color.LightGray;
                emptyLabel.BackColor = Color.FromArgb(30, 30, 35);
                emptyLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);
                emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
                emptyLabel.Dock = DockStyle.Fill;
                containerPanel.Controls.Add(emptyLabel);
                return;
            }

            int columns = 2;
            if (charts.Count <= 2) columns = 1;
            else if (charts.Count <= 4) columns = 2;
            else if (charts.Count <= 9) columns = 3;
            else columns = 4;

            int panelWidth = (this.ClientSize.Width - 40) / columns - 10;
            if (panelWidth < 250) panelWidth = 250;
            int panelHeight = 300;

            int x = 10;
            int y = 10;
            int row = 0;
            int col = 0;

            for (int i = 0; i < charts.Count; i++)
            {
                var chart = charts[i];
                int num = i + 1;

                var graphPanel = new Panel();
                graphPanel.Bounds = new Rectangle(x, y, panelWidth, panelHeight);
                graphPanel.BackColor = Color.FromArgb(40, 40, 45);
                graphPanel.BorderStyle = BorderStyle.FixedSingle;
                graphPanel.Tag = chart;

                var titleLabel = new Label();
                titleLabel.Text = $"{chart.Name} #{num}";
                titleLabel.Location = new Point(5, 5);
                titleLabel.Size = new Size(panelWidth - 10, 25);
                titleLabel.ForeColor = Color.White;
                titleLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                titleLabel.TextAlign = ContentAlignment.MiddleCenter;
                graphPanel.Controls.Add(titleLabel);

                var pictureBox = new PictureBox();
                pictureBox.Location = new Point(5, 35);
                pictureBox.Size = new Size(panelWidth - 10, panelHeight - 70);
                pictureBox.BackColor = Color.Black;
                pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox.Tag = chart.Id;
                graphPanel.Controls.Add(pictureBox);

                var openBtn = new Button();
                openBtn.Text = "Открыть";
                openBtn.Location = new Point(5, panelHeight - 30);
                openBtn.Size = new Size(70, 25);
                openBtn.BackColor = Color.FromArgb(108, 117, 125);
                openBtn.ForeColor = Color.White;
                openBtn.FlatStyle = FlatStyle.Flat;
                openBtn.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                openBtn.Tag = chart;
                openBtn.Click += new EventHandler(OpenGraphWindow);
                graphPanel.Controls.Add(openBtn);

                var clearBtn = new Button();
                clearBtn.Text = "Очистить";
                clearBtn.Location = new Point(80, panelHeight - 30);
                clearBtn.Size = new Size(70, 25);
                clearBtn.BackColor = Color.FromArgb(108, 117, 125);
                clearBtn.ForeColor = Color.White;
                clearBtn.FlatStyle = FlatStyle.Flat;
                clearBtn.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                clearBtn.Tag = chart;
                clearBtn.Click += new EventHandler(ClearGraph);
                graphPanel.Controls.Add(clearBtn);

                containerPanel.Controls.Add(graphPanel);
                graphPanels.Add(graphPanel);

                DrawGraphOnPictureBox(pictureBox, chart.Id);

                col++;
                if (col >= columns)
                {
                    col = 0;
                    row++;
                    x = 10;
                    y = 10 + row * (panelHeight + 10);
                }
                else
                {
                    x += panelWidth + 10;
                }
            }
        }

        public void AddDataPoint(Guid chartId, double value)
        {
            if (graphData.ContainsKey(chartId))
            {
                graphData[chartId].Add(value);
            }
        }

        private void OpenGraphWindow(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag is MathTool chart)
            {
                if (existingGraphs.ContainsKey(chart.Id) && !existingGraphs[chart.Id].IsDisposed)
                {
                    existingGraphs[chart.Id].WindowState = FormWindowState.Normal;
                    existingGraphs[chart.Id].BringToFront();
                }
                else
                {
                    var graph = new GraphForm(chart.Name);
                    graph.Show();
                    existingGraphs[chart.Id] = graph;
                    if (graphData.ContainsKey(chart.Id))
                    {
                        foreach (var val in graphData[chart.Id])
                        {
                            graph.AddValue(val);
                        }
                    }
                }
            }
        }

        private void ClearGraph(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag is MathTool chart)
            {
                if (graphData.ContainsKey(chart.Id))
                {
                    graphData[chart.Id].Clear();
                }
                RefreshAllGraphs(null, null);
            }
        }

        private void RefreshAllGraphs(object sender, EventArgs e)
        {
            for (int i = 0; i < charts.Count && i < graphPanels.Count; i++)
            {
                var chart = charts[i];
                var panel = graphPanels[i];
                foreach (Control ctrl in panel.Controls)
                {
                    if (ctrl is PictureBox pictureBox)
                    {
                        DrawGraphOnPictureBox(pictureBox, chart.Id);
                        break;
                    }
                }
            }
        }

        private void UpdateAllGraphs(object sender, EventArgs e)
        {
            foreach (var chart in charts)
            {
                if (graphData.ContainsKey(chart.Id))
                {
                    graphData[chart.Id].Add(chart.CurrentValue);
                }
            }

            for (int i = 0; i < charts.Count && i < graphPanels.Count; i++)
            {
                var chart = charts[i];
                var panel = graphPanels[i];
                foreach (Control ctrl in panel.Controls)
                {
                    if (ctrl is PictureBox pictureBox)
                    {
                        DrawGraphOnPictureBox(pictureBox, chart.Id);
                        break;
                    }
                }
            }
        }

        private void DrawGraphOnPictureBox(PictureBox box, Guid chartId)
        {
            if (box == null || box.Width <= 0 || box.Height <= 0) return;

            Bitmap bmp = new Bitmap(box.Width, box.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                List<double> values = null;
                if (graphData.ContainsKey(chartId))
                {
                    values = graphData[chartId];
                }

                if (values == null || values.Count < 2)
                {
                    using (Font font = new Font("Segoe UI", 10, FontStyle.Bold))
                    {
                        string msg = "Ожидание данных...";
                        SizeF msgSize = g.MeasureString(msg, font);
                        g.DrawString(msg, font, Brushes.Gray,
                            (box.Width - msgSize.Width) / 2, (box.Height - msgSize.Height) / 2);
                    }
                    box.Image = bmp;
                    return;
                }

                int left = 40;
                int right = box.Width - 10;
                int top = 20;
                int bottom = box.Height - 30;

                if (right <= left || bottom <= top)
                {
                    box.Image = bmp;
                    return;
                }

                using (Pen gridPen = new Pen(Color.FromArgb(50, 50, 55), 1))
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        int x = left + (right - left) * i / 5;
                        g.DrawLine(gridPen, x, top, x, bottom);
                    }
                    for (int i = 1; i <= 4; i++)
                    {
                        int y = top + (bottom - top) * i / 4;
                        g.DrawLine(gridPen, left, y, right, y);
                    }
                }

                using (Pen axisPen = new Pen(Color.FromArgb(180, 180, 180), 1.5f))
                {
                    g.DrawLine(axisPen, left, bottom, right, bottom);
                    g.DrawLine(axisPen, left, top, left, bottom);
                }

                double min = values.Min();
                double max = values.Max();
                double range = max - min;
                if (range < 0.001) range = 1;

                Point? prev = null;
                using (Pen linePen = new Pen(Color.Cyan, 1.5f))
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        float x = left + (float)(i * (right - left) / (values.Count - 1));
                        float y = bottom - (float)((values[i] - min) / range * (bottom - top));
                        y = Math.Max(top, Math.Min(bottom, y));

                        if (prev.HasValue)
                        {
                            g.DrawLine(linePen, prev.Value.X, prev.Value.Y, x, y);
                        }
                        prev = new Point((int)x, (int)y);
                    }
                }

                using (Font statFont = new Font("Consolas", 7, FontStyle.Regular))
                {
                    g.DrawString($"Мин: {min:F3}", statFont, Brushes.LightGreen, left, bottom + 5);
                    g.DrawString($"Макс: {max:F3}", statFont, Brushes.LightGreen, left + 70, bottom + 5);
                    g.DrawString($"Точек: {values.Count}", statFont, Brushes.LightGreen, left + 140, bottom + 5);
                    if (values.Count > 0)
                    {
                        g.DrawString($"Тек: {values[values.Count - 1]:F3}", statFont, Brushes.Yellow, left + 210, bottom + 5);
                    }
                }

                using (Font axisFont = new Font("Segoe UI", 7, FontStyle.Bold))
                {
                    g.DrawString("Отсчёты", axisFont, Brushes.Gray, right - 40, bottom + 5);
                    g.DrawString("Амплитуда (В)", axisFont, Brushes.Gray, 5, top + 5);
                }
            }

            if (box.Image != null)
            {
                box.Image.Dispose();
            }
            box.Image = bmp;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (containerPanel != null)
            {
                CreateGraphGrid();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (updateTimer != null)
            {
                updateTimer.Stop();
                updateTimer.Dispose();
            }
            base.OnFormClosed(e);
        }
    }
}
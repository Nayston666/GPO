using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using MathApp.Models;
using MathApp.Helpers;

namespace MathApp.Rendering
{
    public class BlockRenderer
    {
        private Font _smallFont = new Font("Segoe UI", 7);
        private Font _nameFont = new Font("Segoe UI", 8, FontStyle.Bold);

        private void DrawShadow(Graphics g, Rectangle rect)
        {
            using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
            {
                g.FillRectangle(shadowBrush, rect.X + 2, rect.Y + 2, rect.Width, rect.Height);
            }
        }

        // ============ ОПЕРАЦИИ (серые) ============
        public void DrawAdditionTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(245, 245, 245)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(200, 200, 200), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            // Имя и ID
            g.DrawString($"{tool.Name}\nID:{tool.Id.ToString().Substring(0, 8)}", _nameFont, Brushes.DimGray, rect.X + 8, rect.Y + 5);

            using (var plusPen = new Pen(Color.DimGray, 3))
            {
                int cx = rect.X + rect.Width / 2;
                int cy = rect.Y + rect.Height / 2;
                g.DrawLine(plusPen, cx - 12, cy, cx + 12, cy);
                g.DrawLine(plusPen, cx, cy - 12, cx, cy + 12);
            }
        }

        public void DrawSubtractionTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(245, 245, 245)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(200, 200, 200), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            g.DrawString($"{tool.Name}\nID:{tool.Id.ToString().Substring(0, 8)}", _nameFont, Brushes.DimGray, rect.X + 8, rect.Y + 5);

            using (var minusPen = new Pen(Color.DimGray, 3))
            {
                int cx = rect.X + rect.Width / 2;
                int cy = rect.Y + rect.Height / 2;
                g.DrawLine(minusPen, cx - 12, cy, cx + 12, cy);
            }
        }

        public void DrawMultiplicationTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(245, 245, 245)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(200, 200, 200), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            g.DrawString($"{tool.Name}\nID:{tool.Id.ToString().Substring(0, 8)}", _nameFont, Brushes.DimGray, rect.X + 8, rect.Y + 5);

            using (var multPen = new Pen(Color.DimGray, 3))
            {
                int cx = rect.X + rect.Width / 2;
                int cy = rect.Y + rect.Height / 2;
                g.DrawLine(multPen, cx - 10, cy - 10, cx + 10, cy + 10);
                g.DrawLine(multPen, cx - 10, cy + 10, cx + 10, cy - 10);
            }
        }

        public void DrawDivisionTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(245, 245, 245)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(200, 200, 200), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            g.DrawString($"{tool.Name}\nID:{tool.Id.ToString().Substring(0, 8)}", _nameFont, Brushes.DimGray, rect.X + 8, rect.Y + 5);

            int cx = rect.X + rect.Width / 2;
            int cy = rect.Y + rect.Height / 2;
            using (var divPen = new Pen(Color.DimGray, 2))
                g.DrawLine(divPen, cx - 8, cy, cx + 8, cy);
            g.FillEllipse(Brushes.DimGray, cx - 3, cy - 10, 6, 6);
            g.FillEllipse(Brushes.DimGray, cx - 3, cy + 4, 6, 6);
        }

        // ============ ГЕНЕРАТОР (светлый с зелёной рамкой) ============
        public void DrawGeneratorTool(Graphics g, MathTool tool, MathTool selectedTool, double time)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(250, 255, 250)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(40, 167, 69), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            g.DrawString($"{tool.Name}\nID:{tool.Id.ToString().Substring(0, 8)}", _nameFont, Brushes.DimGray, rect.X + 8, rect.Y + 5);
            g.DrawString($"{tool.Frequency} Гц, {tool.Amplitude} В", _smallFont, Brushes.Gray, rect.X + 8, rect.Y + 45);

            // Маленький график внутри
            int padding = 8;
            int graphWidth = rect.Width - 2 * padding;
            int graphHeight = 20;
            int graphY = rect.Y + rect.Height - graphHeight - 8;
            using (var wavePen = new Pen(Color.FromArgb(40, 167, 69), 1.5f))
            {
                for (int x = 0; x < graphWidth; x++)
                {
                    float t = (float)x / graphWidth * 4 * (float)Math.PI;
                    float y = graphY + graphHeight / 2 + (float)(Math.Sin(t + time) * graphHeight / 2.5);
                    if (x > 0)
                    {
                        float prevT = (float)(x - 1) / graphWidth * 4 * (float)Math.PI;
                        float prevY = graphY + graphHeight / 2 + (float)(Math.Sin(prevT + time) * graphHeight / 2.5);
                        g.DrawLine(wavePen, rect.X + padding + x - 1, prevY, rect.X + padding + x, y);
                    }
                }
            }
        }

        // ============ УСИЛИТЕЛЬ (треугольник, светлый с синей рамкой) ============
        public void DrawAmplifierTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            Point[] triangle = new Point[]
            {
                new Point(rect.X + 15, rect.Y + 10),
                new Point(rect.X + 15, rect.Y + rect.Height - 10),
                new Point(rect.X + rect.Width - 15, rect.Y + rect.Height / 2)
            };

            using (var brush = new SolidBrush(Color.FromArgb(250, 250, 255)))
                g.FillPolygon(brush, triangle);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(0, 123, 255), selectedTool == tool ? 2 : 1.5f))
                g.DrawPolygon(pen, triangle);

            g.DrawString($"{tool.Name}\nID:{tool.Id.ToString().Substring(0, 8)}", _nameFont, Brushes.DimGray, rect.X + 8, rect.Y + 5);
            g.DrawString($"K={tool.Gain:F1}", _smallFont, Brushes.Gray, rect.X + 8, rect.Y + 45);
        }

        // ============ АНТЕННА (светлая с серой рамкой, с рисунком) ============
        public void DrawAntennaTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(250, 250, 250)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(150, 150, 150), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            g.DrawString($"{tool.Name}\nID:{tool.Id.ToString().Substring(0, 8)}", _nameFont, Brushes.DimGray, rect.X + 8, rect.Y + 5);

            // Рисунок антенны
            int cx = rect.X + rect.Width / 2;
            int topY = rect.Y + 25;
            int bottomY = rect.Y + rect.Height - 15;

            using (var antennaPen = new Pen(Color.DimGray, 1.5f))
            {
                g.DrawLine(antennaPen, cx, bottomY, cx, topY);
                g.DrawLine(antennaPen, cx, topY + 3, cx - 10, topY - 5);
                g.DrawLine(antennaPen, cx, topY + 10, cx - 8, topY + 2);
                g.DrawLine(antennaPen, cx, topY + 3, cx + 10, topY - 5);
                g.DrawLine(antennaPen, cx, topY + 10, cx + 8, topY + 2);
            }
        }

        // ============ КАНАЛ (светлый с голубой рамкой) ============
        public void DrawChannelTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(250, 255, 255)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(23, 162, 184), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            g.DrawString($"{tool.Name}\nID:{tool.Id.ToString().Substring(0, 8)}", _nameFont, Brushes.DimGray, rect.X + 8, rect.Y + 5);
            g.DrawString($"затух:{tool.Attenuation:F2}", _smallFont, Brushes.Gray, rect.X + 8, rect.Y + 45);

            // Зигзаг
            Point[] zigzag = new Point[]
            {
                new Point(rect.X + 15, rect.Y + rect.Height / 2),
                new Point(rect.X + 35, rect.Y + 20),
                new Point(rect.X + 55, rect.Y + rect.Height - 20),
                new Point(rect.X + 75, rect.Y + 20),
                new Point(rect.X + rect.Width - 15, rect.Y + rect.Height / 2)
            };
            using (var zigzagPen = new Pen(Color.FromArgb(23, 162, 184), 1.5f))
                for (int i = 0; i < zigzag.Length - 1; i++)
                    g.DrawLine(zigzagPen, zigzag[i], zigzag[i + 1]);
        }

        // ============ ОБЪЕКТ (светлый с фиолетовой рамкой) ============
        public void DrawObjectTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(255, 250, 255)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(102, 16, 242), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            g.DrawString($"{tool.Name}\nID:{tool.Id.ToString().Substring(0, 8)}", _nameFont, Brushes.DimGray, rect.X + 8, rect.Y + 5);
            g.DrawString($"τ={tool.TimeConstant:F2} c", _smallFont, Brushes.Gray, rect.X + 8, rect.Y + 45);

            using (var objPen = new Pen(Color.FromArgb(102, 16, 242), 1))
            {
                Rectangle innerRect = new Rectangle(rect.X + 25, rect.Y + 25, rect.Width - 50, rect.Height - 50);
                g.DrawRectangle(objPen, innerRect);
            }
        }

        // ============ АЦП (светлый с жёлтой рамкой) ============
        public void DrawADCTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(255, 255, 250)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(255, 193, 7), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            g.DrawString($"{tool.Name}\nID:{tool.Id.ToString().Substring(0, 8)}", _nameFont, Brushes.DimGray, rect.X + 8, rect.Y + 5);
            g.DrawString($"{tool.BitResolution} бит", _smallFont, Brushes.Gray, rect.X + 8, rect.Y + 45);

            // Ступенчатый сигнал
            using (var stepPen = new Pen(Color.FromArgb(255, 193, 7), 1.5f))
            {
                int stepHeight = 10;
                int yStart = rect.Y + 30;
                for (int i = 0; i < 4; i++)
                {
                    int x = rect.X + 15 + i * 22;
                    g.DrawLine(stepPen, x, yStart + i * stepHeight, x + 18, yStart + i * stepHeight);
                    if (i < 3)
                        g.DrawLine(stepPen, x + 18, yStart + i * stepHeight, x + 18, yStart + (i + 1) * stepHeight);
                }
            }
        }

        // ============ ГРАФИК (светлый с красной рамкой) ============
        public void DrawChartTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.White))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(220, 53, 69), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            g.DrawString($"{tool.Name}\nID:{tool.Id.ToString().Substring(0, 8)}", _nameFont, Brushes.DimGray, rect.X + 8, rect.Y + 5);

            using (var axisPen = new Pen(Color.LightGray, 1))
            {
                g.DrawLine(axisPen, rect.X + 15, rect.Y + rect.Height - 15, rect.X + rect.Width - 10, rect.Y + rect.Height - 15);
                g.DrawLine(axisPen, rect.X + 15, rect.Y + 20, rect.X + 15, rect.Y + rect.Height - 15);
            }

            if (tool.ValueHistory != null && tool.ValueHistory.Count > 1)
            {
                int startIdx = Math.Max(0, tool.ValueHistory.Count - 30);
                int pointCount = tool.ValueHistory.Count - startIdx;
                if (pointCount > 1)
                {
                    double min = tool.ValueHistory.Min();
                    double max = tool.ValueHistory.Max();
                    double range = max - min;
                    if (range < 0.001) range = 1;
                    int graphLeft = rect.X + 20;
                    int graphRight = rect.X + rect.Width - 15;
                    int graphTop = rect.Y + 25;
                    int graphBottom = rect.Y + rect.Height - 20;
                    using (var linePen = new Pen(Color.FromArgb(220, 53, 69), 1.5f))
                    {
                        Point? prev = null;
                        for (int i = startIdx; i < tool.ValueHistory.Count; i++)
                        {
                            float x = graphLeft + (float)((i - startIdx) * (graphRight - graphLeft) / (pointCount - 1));
                            float y = graphBottom - (float)((tool.ValueHistory[i] - min) / range * (graphBottom - graphTop - 10));
                            y = Math.Max(graphTop, Math.Min(graphBottom - 5, y));
                            if (prev.HasValue)
                                g.DrawLine(linePen, prev.Value.X, prev.Value.Y, x, y);
                            prev = new Point((int)x, (int)y);
                        }
                    }
                }
            }
        }

        // ============ ТОЧКИ ПОДКЛЮЧЕНИЯ ============
        public void DrawConnectionPoints(Graphics g, MathTool tool, IEnumerable<Connection> connections)
        {
            // Выход (справа) - оранжевый
            if (tool.Type != ToolType.Chart)
            {
                Point output = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                bool hasConn = connections.Any(c => c.SourceToolId == tool.Id);
                DrawPoint(g, output, "out", hasConn ? Color.Gold : Color.FromArgb(255, 140, 0), hasConn);
            }

            // Вход (слева) - зелёный
            Point input = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
            bool hasInput = connections.Any(c => c.TargetToolId == tool.Id);
            DrawPoint(g, input, "in", hasInput ? Color.Gold : Color.FromArgb(40, 167, 69), hasInput);
        }

        private void DrawPoint(Graphics g, Point point, string label, Color color, bool hasConnection)
        {
            int size = 8;
            Color finalColor = hasConnection ? Color.Gold : color;
            using (var brush = new SolidBrush(finalColor))
                g.FillEllipse(brush, point.X - size / 2, point.Y - size / 2, size, size);
            using (var pen = new Pen(Color.White, 1))
                g.DrawEllipse(pen, point.X - size / 2, point.Y - size / 2, size, size);
            g.DrawString(label, _smallFont, Brushes.Gray, point.X - 6, point.Y - 10);
        }
    }
}
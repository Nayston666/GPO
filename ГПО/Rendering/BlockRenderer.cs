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
        private readonly Font _iconFont = new Font("Segoe UI", 16, FontStyle.Bold);
        private readonly Font _valueFont = new Font("Segoe UI", 8, FontStyle.Bold);
        private readonly Font _paramFont = new Font("Segoe UI", 7);
        private readonly Font _smallFont = new Font("Segoe UI", 6, FontStyle.Bold);

        private void DrawShadow(Graphics g, Rectangle rect)
        {
            Rectangle shadowRect = new Rectangle(rect.X + 5, rect.Y + 5, rect.Width, rect.Height);
            using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
            {
                GraphicsExtensions.FillRoundedRectangle(g, shadowBrush, shadowRect, 10);
            }
        }

        public void DrawMathTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            Color startColor, endColor;
            switch (tool.Operation)
            {
                case MathOperation.Integrator:
                    startColor = Color.FromArgb(200, 120, 70);
                    endColor = Color.FromArgb(120, 70, 40);
                    break;
                case MathOperation.Differentiator:
                    startColor = Color.FromArgb(70, 120, 200);
                    endColor = Color.FromArgb(40, 70, 120);
                    break;
                case MathOperation.Interpolator:
                    startColor = Color.FromArgb(120, 70, 200);
                    endColor = Color.FromArgb(70, 40, 120);
                    break;
                default:
                    startColor = Color.FromArgb(70, 130, 220);
                    endColor = Color.FromArgb(40, 70, 140);
                    break;
            }

            using (var brush = new LinearGradientBrush(rect, startColor, endColor, 45))
            {
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 10);
            }

            using (var pen = new Pen(selectedTool == tool ? Color.Yellow : Color.FromArgb(100, 150, 255), selectedTool == tool ? 3 : 2))
            {
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 10);
            }

            // Рисуем символ операции в зависимости от типа
            string icon = GetOperationIcon(tool.Operation);

            if (tool.Operation == MathOperation.Integrator)
            {
                DrawCenteredText(g, "∫", _iconFont, Brushes.White, rect);
                string stepText = $"h={tool.StepSize:F3}";
                g.DrawString(stepText, _paramFont, Brushes.LightYellow, rect.X + 10, rect.Y + rect.Height - 20);
            }
            else if (tool.Operation == MathOperation.Differentiator)
            {
                DrawCenteredText(g, "d/dt", new Font("Segoe UI", 10, FontStyle.Bold), Brushes.White, rect);
            }
            else if (tool.Operation == MathOperation.Interpolator)
            {
                DrawCenteredText(g, "📈", _iconFont, Brushes.White, rect);
                if (tool.InterpolationPoints != null)
                {
                    string pointsText = $"{tool.InterpolationPoints.Count} точек";
                    g.DrawString(pointsText, _paramFont, Brushes.LightYellow, rect.X + 10, rect.Y + rect.Height - 20);
                }
            }
            else
            {
                DrawCenteredText(g, icon, _iconFont, Brushes.White, rect);
            }

            if (tool.LastResult.HasValue)
            {
                string valueText = $"= {tool.LastResult.Value:F2}";
                var valueRect = new Rectangle(rect.X, rect.Y + rect.Height - 20, rect.Width, 15);
                DrawCenteredText(g, valueText, _valueFont, Brushes.LightGreen, valueRect);
            }
        }

        public void DrawSineTool(Graphics g, MathTool tool, MathTool selectedTool, double time)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new LinearGradientBrush(rect, Color.FromArgb(100, 150, 255), Color.FromArgb(50, 80, 200), 45))
            {
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 10);
            }

            using (var pen = new Pen(selectedTool == tool ? Color.Yellow : Color.Cyan, 2))
            {
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 10);
            }

            // Рисуем символ синусоиды (используем тильду)
            using (var font = new Font("Segoe UI", 18, FontStyle.Bold))
            {
                DrawCenteredText(g, "~", font, Brushes.White, rect);
            }

            // Параметры внизу блока
            string paramsText = $"f={tool.Frequency:F1} A={tool.Amplitude:F1}";
            g.DrawString(paramsText, _paramFont, Brushes.LightYellow, rect.X + 10, rect.Y + rect.Height - 20);

            // Текущее значение
            if (tool.LastResult.HasValue)
            {
                string valueText = $"{tool.LastResult.Value:F2}";
                var valueRect = new Rectangle(rect.X, rect.Y + rect.Height - 35, rect.Width, 15);
                DrawCenteredText(g, valueText, _valueFont, Brushes.LightGreen, valueRect);
            }
        }

        public void DrawChartTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(250, 250, 255)))
            {
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 10);
            }

            using (var pen = new Pen(selectedTool == tool ? Color.Yellow : Color.FromArgb(150, 100, 200), 2))
            {
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 10);
            }

            // Рисуем символ графика (ASCII символы для совместимости)
            using (var font = new Font("Segoe UI", 20, FontStyle.Bold))
            {
                DrawCenteredText(g, "[ ]", font, Brushes.Purple, rect);
            }

            // Дополнительная подпись
            g.DrawString("Graph", _paramFont, Brushes.DarkGray, rect.X + 10, rect.Y + 10);

            if (tool.ValueHistory != null && tool.ValueHistory.Count > 0)
            {
                DrawMiniChart(g, rect, tool.ValueHistory);
            }
        }

        public void DrawConnectionPoints(Graphics g, MathTool tool, IEnumerable<Connection> connections)
        {
            // Выходная точка (у всех кроме графика)
            if (tool.Type != ToolType.Chart)
            {
                Point output = GetOutputPoint(tool);
                bool hasConnections = connections.Any(c => c.SourceToolId == tool.Id);
                DrawConnectionPoint(g, output, "out", Color.Orange, hasConnections);
            }

            // Входные точки - У СИНУСОИДЫ НЕТ ВХОДОВ!
            if (tool.Type == ToolType.Operation)
            {
                Point inputA = GetInputPoint(tool, InputType.A);
                Point inputB = GetInputPoint(tool, InputType.B);

                bool hasA = connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.A);
                bool hasB = connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.B);

                DrawConnectionPoint(g, inputA, "A", Color.LightGreen, hasA);
                DrawConnectionPoint(g, inputB, "B", Color.LightGreen, hasB);
            }
            else if (tool.Type == ToolType.Chart)  // Только у графика есть вход
            {
                Point input = GetInputPoint(tool, InputType.A);
                bool hasConn = connections.Any(c => c.TargetToolId == tool.Id);
                DrawConnectionPoint(g, input, "in", Color.LightGreen, hasConn);
            }
            // Синусоида - НЕТ ВХОДНЫХ ТОЧЕК!
        }

        private void DrawConnectionPoint(Graphics g, Point point, string label, Color color, bool hasConnection)
        {
            int size = 12;
            Color finalColor = hasConnection ? Color.Yellow : color;

            using (var glowPen = new Pen(Color.FromArgb(100, finalColor), 3))
            {
                g.DrawEllipse(glowPen, point.X - size / 2 - 1, point.Y - size / 2 - 1, size + 2, size + 2);
            }

            using (var brush = new SolidBrush(finalColor))
            {
                g.FillEllipse(brush, point.X - size / 2, point.Y - size / 2, size, size);
            }

            using (var pen = new Pen(Color.White, 1))
            {
                g.DrawEllipse(pen, point.X - size / 2, point.Y - size / 2, size, size);
            }

            g.DrawString(label, _smallFont, Brushes.Black, point.X - 4, point.Y - 12);
        }

        private void DrawCenteredText(Graphics g, string text, Font font, Brush brush, Rectangle rect)
        {
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(text, font, brush, rect, sf);
            }
        }

        private void DrawSineWave(Graphics g, Rectangle rect, double time)
        {
            int padding = 10;
            int graphWidth = rect.Width - 2 * padding;
            int graphHeight = 30;
            int graphY = rect.Y + rect.Height - graphHeight - padding - 10;

            if (graphWidth <= 0) return;

            using (var pen = new Pen(Color.White, 2))
            {
                for (int x = 0; x < graphWidth; x++)
                {
                    float t = (float)x / graphWidth * 4 * (float)Math.PI;
                    float y = graphY + graphHeight / 2 + (float)(Math.Sin(t + time) * graphHeight / 3);

                    if (x > 0)
                    {
                        float prevT = (float)(x - 1) / graphWidth * 4 * (float)Math.PI;
                        float prevY = graphY + graphHeight / 2 + (float)(Math.Sin(prevT + time) * graphHeight / 3);
                        g.DrawLine(pen, rect.X + padding + x - 1, prevY, rect.X + padding + x, y);
                    }
                }
            }
        }

        private void DrawMiniChart(Graphics g, Rectangle rect, List<double> values)
        {
            if (values.Count < 2) return;

            int padding = 10;
            int chartHeight = 40;
            int chartY = rect.Y + rect.Height - chartHeight - padding - 10;
            int chartWidth = rect.Width - 2 * padding;

            double min = values.Min();
            double max = values.Max();
            double range = max - min;
            if (range < 0.001) range = 1;

            int startIndex = Math.Max(0, values.Count - 20);
            int pointCount = values.Count - startIndex;
            if (pointCount < 2) return;

            using (var pen = new Pen(Color.Blue, 1.5f))
            {
                Point? prevPoint = null;
                for (int i = startIndex; i < values.Count; i++)
                {
                    float x = rect.X + padding + (float)((i - startIndex) * chartWidth / (pointCount - 1));
                    float y = chartY + chartHeight - (float)((values[i] - min) / range * chartHeight);
                    if (prevPoint.HasValue)
                        g.DrawLine(pen, prevPoint.Value.X, prevPoint.Value.Y, x, y);
                    prevPoint = new Point((int)x, (int)y);
                }
            }
        }

        private Point GetOutputPoint(MathTool tool) =>
            new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);

        private Point GetInputPoint(MathTool tool, InputType input)
        {
            // У синусоиды НЕТ входных точек
            if (tool.Type == ToolType.SineGenerator)
                return Point.Empty;

            if (tool.Type == ToolType.Chart)
                return new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);

            // Для операций
            return input == InputType.A
                ? new Point(tool.Position.X - 5, tool.Position.Y + 20)
                : new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height - 20);
        }

        private string GetOperationIcon(MathOperation op)
        {
            switch (op)
            {
                case MathOperation.Addition: return "+";
                case MathOperation.Subtraction: return "−";
                case MathOperation.Multiplication: return "×";
                case MathOperation.Division: return "÷";
                case MathOperation.Integrator: return "∫";
                case MathOperation.Differentiator: return "d/dt";
                case MathOperation.Interpolator: return "f(x)";
                default: return "?";
            }
        }
    }
}
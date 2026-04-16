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
                GraphicsExtensions.FillRoundedRectangle(g, shadowBrush, shadowRect, 10);
        }

        public void DrawMathTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            Color startColor, endColor;
            switch (tool.Operation)
            {
                case MathOperation.Integrator: startColor = Color.FromArgb(200, 120, 70); endColor = Color.FromArgb(120, 70, 40); break;
                case MathOperation.Differentiator: startColor = Color.FromArgb(70, 120, 200); endColor = Color.FromArgb(40, 70, 120); break;
                case MathOperation.Interpolator: startColor = Color.FromArgb(120, 70, 200); endColor = Color.FromArgb(70, 40, 120); break;
                case MathOperation.FileIO: startColor = Color.FromArgb(70, 200, 120); endColor = Color.FromArgb(40, 120, 70); break;
                default: startColor = Color.FromArgb(70, 130, 220); endColor = Color.FromArgb(40, 70, 140); break;
            }

            using (var brush = new LinearGradientBrush(rect, startColor, endColor, 45))
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 10);

            using (var pen = new Pen(selectedTool == tool ? Color.Yellow : Color.FromArgb(100, 150, 255), selectedTool == tool ? 3 : 2))
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 10);

            if (tool.Operation == MathOperation.FileIO)
            {
                DrawCenteredText(g, tool.IsReading ? "📂 ЧТЕНИЕ" : "💾 ЗАПИСЬ", new Font("Segoe UI", 9, FontStyle.Bold), Brushes.White, rect);
                string fileInfo = tool.IsReading ? System.IO.Path.GetFileName(tool.InputFilePath) : System.IO.Path.GetFileName(tool.OutputFilePath);
                if (string.IsNullOrEmpty(fileInfo)) fileInfo = "не выбран";
                g.DrawString(fileInfo, _paramFont, Brushes.LightYellow, rect.X + 10, rect.Y + rect.Height - 20);
                if (tool.FileData.Count > 0)
                    g.DrawString($"данных: {tool.FileData.Count}", _paramFont, Brushes.LightGreen, rect.X + 10, rect.Y + 10);
            }
            else if (tool.Operation == MathOperation.Integrator)
            {
                DrawCenteredText(g, "∫", _iconFont, Brushes.White, rect);
                g.DrawString($"h={tool.StepSize:F3}", _paramFont, Brushes.LightYellow, rect.X + 10, rect.Y + rect.Height - 20);
            }
            else if (tool.Operation == MathOperation.Differentiator)
            {
                DrawCenteredText(g, "d/dt", new Font("Segoe UI", 10, FontStyle.Bold), Brushes.White, rect);
            }
            else if (tool.Operation == MathOperation.Interpolator)
            {
                DrawCenteredText(g, "f(x)", _iconFont, Brushes.White, rect);
                g.DrawString($"{tool.InterpolationPoints?.Count ?? 0} точек", _paramFont, Brushes.LightYellow, rect.X + 10, rect.Y + rect.Height - 20);
            }
            else
            {
                DrawCenteredText(g, GetOperationIcon(tool.Operation), _iconFont, Brushes.White, rect);
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
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 10);

            using (var pen = new Pen(selectedTool == tool ? Color.Yellow : Color.Cyan, 2))
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 10);

            using (var font = new Font("Segoe UI", 18, FontStyle.Bold))
                DrawCenteredText(g, "~", font, Brushes.White, rect);

            g.DrawString($"f={tool.Frequency:F1} A={tool.Amplitude:F1}", _paramFont, Brushes.LightYellow, rect.X + 10, rect.Y + rect.Height - 20);
            if (tool.LastResult.HasValue)
                DrawCenteredText(g, tool.LastResult.Value.ToString("F2"), _valueFont, Brushes.LightGreen, new Rectangle(rect.X, rect.Y + rect.Height - 35, rect.Width, 15));
        }

        public void DrawChartTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(250, 250, 255)))
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 10);

            using (var pen = new Pen(selectedTool == tool ? Color.Yellow : Color.FromArgb(150, 100, 200), 2))
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 10);

            using (var font = new Font("Segoe UI", 20, FontStyle.Bold))
                DrawCenteredText(g, "[ ]", font, Brushes.Purple, rect);

            g.DrawString("Graph", _paramFont, Brushes.DarkGray, rect.X + 10, rect.Y + 10);
            if (tool.ValueHistory != null && tool.ValueHistory.Count > 0)
                DrawMiniChart(g, rect, tool.ValueHistory);
        }

        public void DrawConnectionPoints(Graphics g, MathTool tool, IEnumerable<Connection> connections)
        {
            if (tool.Type != ToolType.Chart)
            {
                Point output = GetOutputPoint(tool);
                DrawConnectionPoint(g, output, "out", Color.Orange, connections.Any(c => c.SourceToolId == tool.Id));
            }

            if (tool.Type == ToolType.Operation)
            {
                DrawConnectionPoint(g, GetInputPoint(tool, InputType.A), "A", Color.LightGreen, connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.A));
                DrawConnectionPoint(g, GetInputPoint(tool, InputType.B), "B", Color.LightGreen, connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.B));
            }
            else if (tool.Type == ToolType.Chart)
            {
                DrawConnectionPoint(g, GetInputPoint(tool, InputType.A), "in", Color.LightGreen, connections.Any(c => c.TargetToolId == tool.Id));
            }
        }

        private void DrawConnectionPoint(Graphics g, Point point, string label, Color color, bool hasConnection)
        {
            if (point == Point.Empty) return;
            int size = 12;
            Color finalColor = hasConnection ? Color.Yellow : color;
            using (var glowPen = new Pen(Color.FromArgb(100, finalColor), 3))
                g.DrawEllipse(glowPen, point.X - size / 2 - 1, point.Y - size / 2 - 1, size + 2, size + 2);
            using (var brush = new SolidBrush(finalColor))
                g.FillEllipse(brush, point.X - size / 2, point.Y - size / 2, size, size);
            using (var pen = new Pen(Color.White, 1))
                g.DrawEllipse(pen, point.X - size / 2, point.Y - size / 2, size, size);
            g.DrawString(label, _smallFont, Brushes.Black, point.X - 4, point.Y - 12);
        }

        private void DrawCenteredText(Graphics g, string text, Font font, Brush brush, Rectangle rect)
        {
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(text, font, brush, rect, sf);
        }

        private void DrawMiniChart(Graphics g, Rectangle rect, List<double> values)
        {
            if (values.Count < 2) return;
            int padding = 10, chartHeight = 40, chartY = rect.Y + rect.Height - chartHeight - padding - 10, chartWidth = rect.Width - 2 * padding;
            double min = values.Min(), max = values.Max(), range = max - min;
            if (range < 0.001) range = 1;
            int startIndex = Math.Max(0, values.Count - 20), pointCount = values.Count - startIndex;
            if (pointCount < 2) return;
            using (var pen = new Pen(Color.Blue, 1.5f))
            {
                Point? prevPoint = null;
                for (int i = startIndex; i < values.Count; i++)
                {
                    float x = rect.X + padding + (float)((i - startIndex) * chartWidth / (pointCount - 1));
                    float y = chartY + chartHeight - (float)((values[i] - min) / range * chartHeight);
                    if (prevPoint.HasValue) g.DrawLine(pen, prevPoint.Value.X, prevPoint.Value.Y, x, y);
                    prevPoint = new Point((int)x, (int)y);
                }
            }
        }

        private Point GetOutputPoint(MathTool tool) => new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);

        private Point GetInputPoint(MathTool tool, InputType input)
        {
            if (tool.Type == ToolType.SineGenerator) return Point.Empty;
            if (tool.Type == ToolType.Chart) return new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
            return input == InputType.A ? new Point(tool.Position.X - 5, tool.Position.Y + 20) : new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height - 20);
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
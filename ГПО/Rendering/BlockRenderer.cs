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
        private readonly Font _nameFont = new Font("Segoe UI", 8, FontStyle.Bold);

        private void DrawShadow(Graphics g, Rectangle rect)
        {
            Rectangle shadowRect = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width, rect.Height);
            using (var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                GraphicsExtensions.FillRoundedRectangle(g, shadowBrush, shadowRect, 8);
        }

        // ============ ЕДИНЫЕ МЕТОДЫ ДЛЯ ПОЛУЧЕНИЯ КООРДИНАТ ТОЧЕК ============
        public Point GetInputPoint(MathTool tool, InputType input, int portIndex = 0)
        {
            if (tool.Type == ToolType.SubSystem && tool.SubSystemData != null)
            {
                int yOffset = 35 + portIndex * 20;
                if (tool.Flipped)
                    return new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + yOffset);
                else
                    return new Point(tool.Position.X - 5, tool.Position.Y + yOffset);
            }

            if (tool.Type == ToolType.Operation)
            {
                int yOffset = (input == InputType.A) ? 20 : tool.Size.Height - 20;
                if (tool.Flipped)
                    return new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + yOffset);
                else
                    return new Point(tool.Position.X - 5, tool.Position.Y + yOffset);
            }
            else if (tool.Type == ToolType.Chart)
            {
                if (tool.Flipped)
                    return new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                else
                    return new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
            }
            else // все остальные блоки (усилитель, антенна, канал, объект, АЦП, генератор)
            {
                // У генератора нет входа, но для единообразия вернём что-то
                if (tool.Flipped)
                    return new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                else
                    return new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
            }
        }

        public Point GetOutputPoint(MathTool tool, int portIndex = 0)
        {
            if (tool.Type == ToolType.SubSystem && tool.SubSystemData != null)
            {
                int yOffset = 35 + portIndex * 20;
                if (tool.Flipped)
                    return new Point(tool.Position.X - 5, tool.Position.Y + yOffset);
                else
                    return new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + yOffset);
            }
            else if (tool.Type == ToolType.Chart)
            {
                return Point.Empty; // у графика нет выхода
            }
            else
            {
                if (tool.Flipped)
                    return new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                else
                    return new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
            }
        }

        // ============ МАТЕМАТИЧЕСКИЕ ОПЕРАЦИИ ============
        public void DrawMathTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(250, 250, 255)))
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 10);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 180, 180), selectedTool == tool ? 2 : 1))
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 10);

            if (tool.Operation == MathOperation.FileIO)
            {
                DrawCenteredText(g, tool.IsReading ? "📂 ЧТЕНИЕ" : "💾 ЗАПИСЬ", new Font("Segoe UI", 9, FontStyle.Bold), Brushes.DarkBlue, rect);
                string fileInfo = tool.IsReading ? System.IO.Path.GetFileName(tool.InputFilePath) : System.IO.Path.GetFileName(tool.OutputFilePath);
                if (string.IsNullOrEmpty(fileInfo)) fileInfo = "не выбран";
                g.DrawString(fileInfo, _paramFont, Brushes.DarkGray, rect.X + 10, rect.Y + rect.Height - 20);
                if (tool.FileData.Count > 0)
                    g.DrawString($"данных: {tool.FileData.Count}", _paramFont, Brushes.DarkGreen, rect.X + 10, rect.Y + 10);
            }
            else if (tool.Operation == MathOperation.Integrator)
            {
                DrawCenteredText(g, "∫", _iconFont, Brushes.DarkBlue, rect);
                g.DrawString($"h={tool.StepSize:F3}", _paramFont, Brushes.DarkGray, rect.X + 10, rect.Y + rect.Height - 20);
            }
            else if (tool.Operation == MathOperation.Differentiator)
                DrawCenteredText(g, "d/dt", new Font("Segoe UI", 12, FontStyle.Bold), Brushes.DarkBlue, rect);
            else if (tool.Operation == MathOperation.Interpolator)
            {
                DrawCenteredText(g, "f(x)", _iconFont, Brushes.DarkBlue, rect);
                g.DrawString($"{tool.InterpolationPoints?.Count ?? 0} точек", _paramFont, Brushes.DarkGray, rect.X + 10, rect.Y + rect.Height - 20);
            }
            else
                DrawCenteredText(g, GetOperationIcon(tool.Operation), _iconFont, Brushes.DarkBlue, rect);

            if (tool.LastResult.HasValue)
            {
                string valueText = $"= {tool.LastResult.Value:F2}";
                var valueRect = new Rectangle(rect.X, rect.Y + rect.Height - 20, rect.Width, 15);
                DrawCenteredText(g, valueText, _valueFont, Brushes.DarkGreen, valueRect);
            }
        }

        public void DrawSineTool(Graphics g, MathTool tool, MathTool selectedTool, double time)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(240, 248, 255)))
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 10);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 180, 180), selectedTool == tool ? 2 : 1))
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 10);
            DrawSineWave(g, rect, time);
            string paramsText = $"f={tool.Frequency:F1} A={tool.Amplitude:F1}";
            g.DrawString(paramsText, _paramFont, Brushes.DarkGray, rect.X + 10, rect.Y + 10);
        }

        public void DrawChartTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 10);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 180, 180), selectedTool == tool ? 2 : 1))
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 10);
            g.DrawString("📊", new Font("Segoe UI", 30), Brushes.Purple, rect.X + rect.Width / 2 - 25, rect.Y + 10);
            if (tool.ValueHistory != null && tool.ValueHistory.Count > 0)
                DrawMiniChart(g, rect, tool.ValueHistory);
        }

        public void DrawSubSystemTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            if (tool.SubSystemData != null)
                tool.Size = CalculateSubSystemSize(tool.SubSystemData);
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(230, 240, 255)))
                GraphicsExtensions.FillRoundedRectangle(g, brush, rect, 10);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(150, 150, 200), selectedTool == tool ? 2 : 1))
                GraphicsExtensions.DrawRoundedRectangle(g, pen, rect, 10);
            g.DrawString("🧩", new Font("Segoe UI", 24), Brushes.DarkBlue, rect.X + rect.Width / 2 - 20, rect.Y + 15);
            g.DrawString(tool.Name, _nameFont, Brushes.DarkGray, rect.X + 10, rect.Y + rect.Height - 20);
            if (tool.SubSystemData != null)
            {
                int inputCount = tool.SubSystemData.InputPorts?.Count ?? 0;
                int outputCount = tool.SubSystemData.OutputPorts?.Count ?? 0;
                string portsInfo = $"Вх:{inputCount} Вых:{outputCount}";
                g.DrawString(portsInfo, _nameFont, Brushes.DarkCyan, rect.X + rect.Width - 70, rect.Y + 10);
                // мини-порты для визуализации (можно оставить или убрать – не влияет на функционал)
                for (int i = 0; i < Math.Min(inputCount, 8); i++)
                {
                    int y = 35 + i * 20;
                    Point pt = tool.Flipped ? new Point(rect.X + rect.Width + 3, rect.Y + y) : new Point(rect.X - 3, rect.Y + y);
                    using (var brush = new SolidBrush(Color.LightGreen))
                        g.FillEllipse(brush, pt.X - 4, pt.Y - 4, 8, 8);
                }
                for (int i = 0; i < Math.Min(outputCount, 8); i++)
                {
                    int y = 35 + i * 20;
                    Point pt = tool.Flipped ? new Point(rect.X - 3, rect.Y + y) : new Point(rect.X + rect.Width + 3, rect.Y + y);
                    using (var brush = new SolidBrush(Color.Orange))
                        g.FillEllipse(brush, pt.X - 4, pt.Y - 4, 8, 8);
                }
            }
        }

        // ============ СПЕЦИАЛЬНЫЕ БЛОКИ ============
        public void DrawAmplifierTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            Point[] triangle;
            if (!tool.Flipped)
                triangle = new Point[] { new Point(rect.X, rect.Y), new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height / 2) };
            else
                triangle = new Point[] { new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height), new Point(rect.X, rect.Y + rect.Height / 2) };

            using (var brush = new SolidBrush(Color.FromArgb(250, 250, 255)))
                g.FillPolygon(brush, triangle);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 180, 180), selectedTool == tool ? 2 : 1))
                g.DrawPolygon(pen, triangle);

            // Буква "G" в центре
            using (var fontG = new Font("Segoe UI", 20, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString("G", fontG);
                float x = rect.X + (rect.Width - textSize.Width) / 2;
                float y = rect.Y + (rect.Height - textSize.Height) / 2;
                g.DrawString("G", fontG, Brushes.Black, x, y);
            }

            if (tool.LastResult.HasValue)
            {
                string val = tool.LastResult.Value.ToString("F2");
                g.DrawString(val, _valueFont, Brushes.DarkGreen, rect.X + rect.Width - 30, rect.Y + rect.Height - 15);
            }
        }

        public void DrawAntennaTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(250, 250, 255)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 180, 180), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);
            int cx = rect.X + rect.Width / 2;
            int startY = rect.Y + rect.Height - 15;
            int topY = rect.Y + 20;
            using (var pen2 = new Pen(Color.Black, 2))
            {
                g.DrawLine(pen2, cx, startY, cx, topY + 8);
                int len = 16;
                g.DrawLine(pen2, cx, topY + 8, cx - len, topY);
                g.DrawLine(pen2, cx, topY + 8, cx + len, topY);
                g.DrawLine(pen2, cx, topY + 8, cx, topY - 5);
            }
            g.DrawString(tool.Name, _nameFont, Brushes.Black, rect.X + 5, rect.Y + 5);
        }

        public void DrawChannelTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(250, 250, 255)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 180, 180), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);
            g.DrawString("КАНАЛ", _nameFont, Brushes.Black, rect.X + rect.Width / 2 - 20, rect.Y + rect.Height / 2 - 8);
        }

        public void DrawObjectTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(250, 250, 255)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 180, 180), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);
            g.DrawString("ОБЪЕКТ", _nameFont, Brushes.Black, rect.X + rect.Width / 2 - 25, rect.Y + rect.Height / 2 - 8);
        }

        public void DrawADCTool(Graphics g, MathTool tool, MathTool selectedTool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);
            using (var brush = new SolidBrush(Color.FromArgb(250, 250, 255)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 180, 180), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);
            g.DrawString("АЦП", _nameFont, Brushes.Black, rect.X + rect.Width / 2 - 15, rect.Y + rect.Height / 2 - 8);
        }

        // ============ ТОЧКИ ПОДКЛЮЧЕНИЯ (используют единые методы GetInputPoint/GetOutputPoint) ============
        public void DrawConnectionPoints(Graphics g, MathTool tool, IEnumerable<Connection> connections)
        {
            // Подсистема
            if (tool.Type == ToolType.SubSystem && tool.SubSystemData != null)
            {
                int inCnt = tool.SubSystemData.InputPorts?.Count ?? 0;
                int outCnt = tool.SubSystemData.OutputPorts?.Count ?? 0;
                for (int i = 0; i < inCnt; i++)
                {
                    Point pt = GetInputPoint(tool, InputType.A, i);
                    bool has = connections.Any(c => c.TargetToolId == tool.Id && c.TargetPortIndex == i);
                    DrawConnectionPoint(g, pt, $"IN{i + 1}", Color.LightGreen, has);
                }
                for (int i = 0; i < outCnt; i++)
                {
                    Point pt = GetOutputPoint(tool, i);
                    bool has = connections.Any(c => c.SourceToolId == tool.Id && c.SourcePortIndex == i);
                    DrawConnectionPoint(g, pt, $"OUT{i + 1}", Color.Orange, has);
                }
                return;
            }

            // Выход (кроме графика)
            if (tool.Type != ToolType.Chart)
            {
                Point outPt = GetOutputPoint(tool);
                bool hasOut = connections.Any(c => c.SourceToolId == tool.Id);
                DrawConnectionPoint(g, outPt, "out", Color.Orange, hasOut);
            }

            // Входы
            if (tool.Type == ToolType.Operation)
            {
                Point inA = GetInputPoint(tool, InputType.A);
                Point inB = GetInputPoint(tool, InputType.B);
                bool hasA = connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.A);
                bool hasB = connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.B);
                DrawConnectionPoint(g, inA, "A", Color.LightGreen, hasA);
                DrawConnectionPoint(g, inB, "B", Color.LightGreen, hasB);
            }
            else if (tool.Type == ToolType.Chart)
            {
                Point inPt = GetInputPoint(tool, InputType.A);
                bool hasIn = connections.Any(c => c.TargetToolId == tool.Id);
                DrawConnectionPoint(g, inPt, "in", Color.LightGreen, hasIn);
            }
            else if (tool.Type != ToolType.Generator) // у генератора нет входа
            {
                Point inPt = GetInputPoint(tool, InputType.A);
                bool hasIn = connections.Any(c => c.TargetToolId == tool.Id);
                DrawConnectionPoint(g, inPt, "in", Color.LightGreen, hasIn);
            }
        }

        private void DrawConnectionPoint(Graphics g, Point point, string label, Color color, bool hasConnection)
        {
            int size = 10;
            Color final = hasConnection ? Color.Gold : color;
            using (var brush = new SolidBrush(final))
                g.FillEllipse(brush, point.X - size / 2, point.Y - size / 2, size, size);
            using (var pen = new Pen(Color.White, 1))
                g.DrawEllipse(pen, point.X - size / 2, point.Y - size / 2, size, size);
            using (var font = new Font("Segoe UI", 6, FontStyle.Bold))
                g.DrawString(label, font, Brushes.Black, point.X - 8, point.Y - 10);
        }

        private void DrawCenteredText(Graphics g, string text, Font font, Brush brush, Rectangle rect)
        {
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(text, font, brush, rect, sf);
        }

        private void DrawSineWave(Graphics g, Rectangle rect, double time)
        {
            int pad = 10, w = rect.Width - 2 * pad, h = 30;
            int y0 = rect.Y + rect.Height - h - pad - 10;
            if (w <= 0) return;
            using (var pen = new Pen(Color.DarkBlue, 1.5f))
                for (int x = 0; x < w; x++)
                {
                    float t = (float)x / w * 4 * (float)Math.PI;
                    float y = y0 + h / 2 + (float)(Math.Sin(t + time) * h / 3);
                    if (x > 0)
                    {
                        float prevT = (float)(x - 1) / w * 4 * (float)Math.PI;
                        float prevY = y0 + h / 2 + (float)(Math.Sin(prevT + time) * h / 3);
                        g.DrawLine(pen, rect.X + pad + x - 1, prevY, rect.X + pad + x, y);
                    }
                }
        }

        private void DrawMiniChart(Graphics g, Rectangle rect, List<double> values)
        {
            if (values.Count < 2) return;
            int pad = 10, h = 40;
            int y0 = rect.Y + rect.Height - h - pad - 10;
            int w = rect.Width - 2 * pad;
            double min = values.Min(), max = values.Max(), range = max - min;
            if (range < 0.001) range = 1;
            int start = Math.Max(0, values.Count - 20);
            int cnt = values.Count - start;
            if (cnt < 2) return;
            using (var pen = new Pen(Color.Blue, 1.5f))
            {
                Point? prev = null;
                for (int i = start; i < values.Count; i++)
                {
                    float x = rect.X + pad + (float)((i - start) * w / (cnt - 1));
                    float y = y0 + h - (float)((values[i] - min) / range * h);
                    if (prev.HasValue) g.DrawLine(pen, prev.Value.X, prev.Value.Y, x, y);
                    prev = new Point((int)x, (int)y);
                }
            }
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
                case MathOperation.FileIO: return "📁";
                default: return "?";
            }
        }

        public Size CalculateSubSystemSize(SubSystemData data)
        {
            if (data == null) return new Size(180, 120);
            int maxPorts = Math.Max(data.InputPorts?.Count ?? 0, data.OutputPorts?.Count ?? 0);
            int height = 55 + maxPorts * 20;
            return new Size(200, Math.Max(120, height));
        }
    }
}
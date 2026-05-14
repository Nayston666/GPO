using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using MathApp.Models;

namespace MathApp.Rendering
{
    public class BlockRenderer
    {
        private Font _idFont = new Font("Segoe UI", 9, FontStyle.Regular);
        private Font _opFont = new Font("Segoe UI", 16, FontStyle.Bold);
        private Font _generatorFont = new Font("Segoe UI", 24, FontStyle.Regular);
        private Font _blockNameFont = new Font("Segoe UI", 9, FontStyle.Bold);

        private void DrawShadow(Graphics g, Rectangle rect)
        {
            using (var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0)))
            {
                g.FillRectangle(shadowBrush, rect.X + 2, rect.Y + 2, rect.Width, rect.Height);
            }
        }

        private void DrawShadowPolygon(Graphics g, Point[] points)
        {
            Point[] shadowPoints = new Point[points.Length];
            for (int i = 0; i < points.Length; i++)
                shadowPoints[i] = new Point(points[i].X + 2, points[i].Y + 2);
            using (var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0)))
                g.FillPolygon(shadowBrush, shadowPoints);
        }

        // ============ ОПЕРАЦИИ (ДВА ВХОДА СЛЕВА, ВЫХОД СПРАВА) ============
        public void DrawAdditionTool(Graphics g, MathTool tool, MathTool selectedTool, int idNumber)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(120, 120, 120), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            using (var brush = new SolidBrush(Color.Black))
                g.DrawString($"ID{idNumber}", _idFont, brush, rect.X + 5, rect.Y + 3);

            using (var brush = new SolidBrush(Color.Black))
            {
                SizeF textSize = g.MeasureString("+", _opFont);
                float x = rect.X + (rect.Width - textSize.Width) / 2;
                float y = rect.Y + (rect.Height - textSize.Height) / 2;
                g.DrawString("+", _opFont, brush, x, y);
            }
        }

        public void DrawSubtractionTool(Graphics g, MathTool tool, MathTool selectedTool, int idNumber)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(120, 120, 120), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            using (var brush = new SolidBrush(Color.Black))
                g.DrawString($"ID{idNumber}", _idFont, brush, rect.X + 5, rect.Y + 3);

            using (var brush = new SolidBrush(Color.Black))
            {
                SizeF textSize = g.MeasureString("-", _opFont);
                float x = rect.X + (rect.Width - textSize.Width) / 2;
                float y = rect.Y + (rect.Height - textSize.Height) / 2;
                g.DrawString("-", _opFont, brush, x, y);
            }
        }

        public void DrawMultiplicationTool(Graphics g, MathTool tool, MathTool selectedTool, int idNumber)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(120, 120, 120), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            using (var brush = new SolidBrush(Color.Black))
                g.DrawString($"ID{idNumber}", _idFont, brush, rect.X + 5, rect.Y + 3);

            using (var brush = new SolidBrush(Color.Black))
            {
                SizeF textSize = g.MeasureString("×", _opFont);
                float x = rect.X + (rect.Width - textSize.Width) / 2;
                float y = rect.Y + (rect.Height - textSize.Height) / 2;
                g.DrawString("×", _opFont, brush, x, y);
            }
        }

        public void DrawDivisionTool(Graphics g, MathTool tool, MathTool selectedTool, int idNumber)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(120, 120, 120), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            using (var brush = new SolidBrush(Color.Black))
                g.DrawString($"ID{idNumber}", _idFont, brush, rect.X + 5, rect.Y + 3);

            using (var brush = new SolidBrush(Color.Black))
            {
                SizeF textSize = g.MeasureString("÷", _opFont);
                float x = rect.X + (rect.Width - textSize.Width) / 2;
                float y = rect.Y + (rect.Height - textSize.Height) / 2;
                g.DrawString("÷", _opFont, brush, x, y);
            }
        }

        // ============ ГЕНЕРАТОР (ТОЛЬКО ВЫХОД, НЕТ ВХОДА) ============
        public void DrawGeneratorTool(Graphics g, MathTool tool, MathTool selectedTool, int idNumber)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(120, 120, 120), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            using (var brush = new SolidBrush(Color.Black))
                g.DrawString($"ID{idNumber}", _idFont, brush, rect.X + 5, rect.Y + 3);

            using (var brush = new SolidBrush(Color.Black))
            {
                SizeF textSize = g.MeasureString("G", _generatorFont);
                float x = rect.X + (rect.Width - textSize.Width) / 2;
                float y = rect.Y + (rect.Height - textSize.Height) / 2;
                g.DrawString("G", _generatorFont, brush, x, y);
            }
        }

        // ============ УСИЛИТЕЛЬ (ТРЕУГОЛЬНИК) ============
        public void DrawAmplifierTool(Graphics g, MathTool tool, MathTool selectedTool, int idNumber)
        {
            Point[] triangle = GetTrianglePoints(tool);
            Rectangle bounds = GetTriangleBounds(triangle);

            DrawShadowPolygon(g, triangle);

            using (var brush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillPolygon(brush, triangle);

            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(120, 120, 120), selectedTool == tool ? 2 : 1))
                g.DrawPolygon(pen, triangle);

            using (var brush = new SolidBrush(Color.Black))
            {
                string idText = $"ID{idNumber}";
                SizeF textSize = g.MeasureString(idText, _idFont);
                float x = bounds.X + (bounds.Width - textSize.Width) / 2;
                float y = bounds.Y + (bounds.Height - textSize.Height) / 2;
                g.DrawString(idText, _idFont, brush, x, y);
            }
        }

        // ============ АНТЕННА ============
        public void DrawAntennaTool(Graphics g, MathTool tool, MathTool selectedTool, int idNumber)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(120, 120, 120), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            using (var brush = new SolidBrush(Color.Black))
                g.DrawString($"ID{idNumber}", _idFont, brush, rect.X + 5, rect.Y + 3);

            int centerX = rect.X + rect.Width / 2;
            int startY = rect.Y + rect.Height - 15;
            int topY = rect.Y + 20;

            using (var antennaPen = new Pen(Color.Black, 2))
            {
                g.DrawLine(antennaPen, centerX, startY, centerX, topY + 8);
                int rayLength = 16;
                g.DrawLine(antennaPen, centerX, topY + 8, centerX - rayLength, topY);
                g.DrawLine(antennaPen, centerX, topY + 8, centerX + rayLength, topY);
                g.DrawLine(antennaPen, centerX, topY + 8, centerX, topY - 5);
            }
        }

        // ============ КАНАЛ ============
        public void DrawChannelTool(Graphics g, MathTool tool, MathTool selectedTool, int idNumber)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(120, 120, 120), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            using (var brush = new SolidBrush(Color.Black))
                g.DrawString($"ID{idNumber}", _idFont, brush, rect.X + 5, rect.Y + 3);

            using (var brush = new SolidBrush(Color.Black))
            {
                SizeF textSize = g.MeasureString("КАНАЛ", _blockNameFont);
                float x = rect.X + (rect.Width - textSize.Width) / 2;
                float y = rect.Y + (rect.Height - textSize.Height) / 2;
                g.DrawString("КАНАЛ", _blockNameFont, brush, x, y);
            }
        }

        // ============ ОБЪЕКТ ============
        public void DrawObjectTool(Graphics g, MathTool tool, MathTool selectedTool, int idNumber)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(120, 120, 120), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            using (var brush = new SolidBrush(Color.Black))
                g.DrawString($"ID{idNumber}", _idFont, brush, rect.X + 5, rect.Y + 3);

            using (var brush = new SolidBrush(Color.Black))
            {
                SizeF textSize = g.MeasureString("ОБЪЕКТ", _blockNameFont);
                float x = rect.X + (rect.Width - textSize.Width) / 2;
                float y = rect.Y + (rect.Height - textSize.Height) / 2;
                g.DrawString("ОБЪЕКТ", _blockNameFont, brush, x, y);
            }
        }

        // ============ АЦП ============
        public void DrawADCTool(Graphics g, MathTool tool, MathTool selectedTool, int idNumber)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(120, 120, 120), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            using (var brush = new SolidBrush(Color.Black))
                g.DrawString($"ID{idNumber}", _idFont, brush, rect.X + 5, rect.Y + 3);

            using (var brush = new SolidBrush(Color.Black))
            {
                SizeF textSize = g.MeasureString("АЦП", _blockNameFont);
                float x = rect.X + (rect.Width - textSize.Width) / 2;
                float y = rect.Y + (rect.Height - textSize.Height) / 2;
                g.DrawString("АЦП", _blockNameFont, brush, x, y);
            }
        }

        // ============ ГРАФИК ============
        public void DrawChartTool(Graphics g, MathTool tool, MathTool selectedTool, int idNumber)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);
            DrawShadow(g, rect);

            using (var brush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(selectedTool == tool ? Color.FromArgb(0, 120, 215) : Color.FromArgb(120, 120, 120), selectedTool == tool ? 2 : 1))
                g.DrawRectangle(pen, rect);

            using (var brush = new SolidBrush(Color.Black))
                g.DrawString($"ID{idNumber}", _idFont, brush, rect.X + 5, rect.Y + 3);

            using (var axisPen = new Pen(Color.Black, 1.5f))
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

                    using (var linePen = new Pen(Color.Black, 2))
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

        // ============ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ============

        public Point[] GetTrianglePoints(MathTool tool)
        {
            Rectangle rect = new Rectangle(tool.Position, tool.Size);

            if (!tool.Rotated)
            {
                return new Point[]
                {
                    new Point(rect.X, rect.Y),
                    new Point(rect.X, rect.Y + rect.Height),
                    new Point(rect.X + rect.Width, rect.Y + rect.Height / 2)
                };
            }
            else
            {
                return new Point[]
                {
                    new Point(rect.X + rect.Width, rect.Y),
                    new Point(rect.X + rect.Width, rect.Y + rect.Height),
                    new Point(rect.X, rect.Y + rect.Height / 2)
                };
            }
        }

        private Rectangle GetTriangleBounds(Point[] triangle)
        {
            int minX = Math.Min(Math.Min(triangle[0].X, triangle[1].X), triangle[2].X);
            int minY = Math.Min(Math.Min(triangle[0].Y, triangle[1].Y), triangle[2].Y);
            int maxX = Math.Max(Math.Max(triangle[0].X, triangle[1].X), triangle[2].X);
            int maxY = Math.Max(Math.Max(triangle[0].Y, triangle[1].Y), triangle[2].Y);
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        // ============ ТОЧКИ ПОДКЛЮЧЕНИЯ ============
        public void DrawConnectionPoints(Graphics g, MathTool tool, IEnumerable<Connection> connections, List<MathTool> allTools)
        {
            // ДЛЯ ОПЕРАЦИЙ - ДВА ВХОДА СЛЕВА, ВЫХОД СПРАВА
            if (tool.Type == ToolType.Operation)
            {
                if (!tool.Rotated)
                {
                    // Вход A (верхняя часть слева)
                    Point inputAPoint = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 3);
                    bool hasInputA = connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.A);
                    DrawPoint(g, inputAPoint, hasInputA ? Color.Gold : Color.FromArgb(40, 167, 69));

                    // Вход B (нижняя часть слева)
                    Point inputBPoint = new Point(tool.Position.X - 5, tool.Position.Y + 2 * tool.Size.Height / 3);
                    bool hasInputB = connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.B);
                    DrawPoint(g, inputBPoint, hasInputB ? Color.Gold : Color.FromArgb(40, 167, 69));

                    // Выход (справа посередине)
                    Point outputPoint = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                    bool hasOutput = connections.Any(c => c.SourceToolId == tool.Id);
                    DrawPoint(g, outputPoint, hasOutput ? Color.Gold : Color.FromArgb(255, 140, 0));
                }
                else
                {
                    // При повороте: входы справа, выход слева
                    // Вход A (верхняя часть справа)
                    Point inputAPoint = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 3);
                    bool hasInputA = connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.A);
                    DrawPoint(g, inputAPoint, hasInputA ? Color.Gold : Color.FromArgb(40, 167, 69));

                    // Вход B (нижняя часть справа)
                    Point inputBPoint = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + 2 * tool.Size.Height / 3);
                    bool hasInputB = connections.Any(c => c.TargetToolId == tool.Id && c.TargetInput == InputType.B);
                    DrawPoint(g, inputBPoint, hasInputB ? Color.Gold : Color.FromArgb(40, 167, 69));

                    // Выход (слева посередине)
                    Point outputPoint = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                    bool hasOutput = connections.Any(c => c.SourceToolId == tool.Id);
                    DrawPoint(g, outputPoint, hasOutput ? Color.Gold : Color.FromArgb(255, 140, 0));
                }
                return;
            }

            // Для усилителя
            if (tool.Type == ToolType.Amplifier)
            {
                Point[] triangle = GetTrianglePoints(tool);

                if (!tool.Rotated)
                {
                    Point inputPoint = new Point(triangle[0].X - 5, triangle[0].Y + (triangle[1].Y - triangle[0].Y) / 2);
                    bool hasInput = connections.Any(c => c.TargetToolId == tool.Id);
                    DrawPoint(g, inputPoint, hasInput ? Color.Gold : Color.FromArgb(40, 167, 69));

                    Point outputPoint = new Point(triangle[2].X + 5, triangle[2].Y);
                    bool hasOutput = connections.Any(c => c.SourceToolId == tool.Id);
                    DrawPoint(g, outputPoint, hasOutput ? Color.Gold : Color.FromArgb(255, 140, 0));
                }
                else
                {
                    Point inputPoint = new Point(triangle[0].X + 5, triangle[0].Y + (triangle[1].Y - triangle[0].Y) / 2);
                    bool hasInput = connections.Any(c => c.TargetToolId == tool.Id);
                    DrawPoint(g, inputPoint, hasInput ? Color.Gold : Color.FromArgb(40, 167, 69));

                    Point outputPoint = new Point(triangle[2].X - 5, triangle[2].Y);
                    bool hasOutput = connections.Any(c => c.SourceToolId == tool.Id);
                    DrawPoint(g, outputPoint, hasOutput ? Color.Gold : Color.FromArgb(255, 140, 0));
                }
                return;
            }

            // Для графика - только вход
            if (tool.Type == ToolType.Chart)
            {
                if (!tool.Rotated)
                {
                    Point inputPoint = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                    bool hasInput = connections.Any(c => c.TargetToolId == tool.Id);
                    DrawPoint(g, inputPoint, hasInput ? Color.Gold : Color.FromArgb(40, 167, 69));
                }
                else
                {
                    Point inputPoint = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                    bool hasInput = connections.Any(c => c.TargetToolId == tool.Id);
                    DrawPoint(g, inputPoint, hasInput ? Color.Gold : Color.FromArgb(40, 167, 69));
                }
                return;
            }

            // ДЛЯ ГЕНЕРАТОРА - ТОЛЬКО ВЫХОД (БЕЗ ВХОДА)
            if (tool.Type == ToolType.Generator)
            {
                if (!tool.Rotated)
                {
                    // Выход справа (оранжевый)
                    Point output = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                    bool hasOutput = connections.Any(c => c.SourceToolId == tool.Id);
                    DrawPoint(g, output, hasOutput ? Color.Gold : Color.FromArgb(255, 140, 0));

                    // ВХОДА НЕТ - НЕ РИСУЕМ
                }
                else
                {
                    // Выход слева (оранжевый)
                    Point output = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                    bool hasOutput = connections.Any(c => c.SourceToolId == tool.Id);
                    DrawPoint(g, output, hasOutput ? Color.Gold : Color.FromArgb(255, 140, 0));

                    // ВХОДА НЕТ - НЕ РИСУЕМ
                }
                return;
            }

            // Для всех остальных блоков (антенна, канал, объект, АЦП) - вход и выход
            {
                if (!tool.Rotated)
                {
                    Point output = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                    bool hasOutput = connections.Any(c => c.SourceToolId == tool.Id);
                    DrawPoint(g, output, hasOutput ? Color.Gold : Color.FromArgb(255, 140, 0));

                    Point input = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                    bool hasInput = connections.Any(c => c.TargetToolId == tool.Id);
                    DrawPoint(g, input, hasInput ? Color.Gold : Color.FromArgb(40, 167, 69));
                }
                else
                {
                    Point output = new Point(tool.Position.X - 5, tool.Position.Y + tool.Size.Height / 2);
                    bool hasOutput = connections.Any(c => c.SourceToolId == tool.Id);
                    DrawPoint(g, output, hasOutput ? Color.Gold : Color.FromArgb(255, 140, 0));

                    Point input = new Point(tool.Position.X + tool.Size.Width + 5, tool.Position.Y + tool.Size.Height / 2);
                    bool hasInput = connections.Any(c => c.TargetToolId == tool.Id);
                    DrawPoint(g, input, hasInput ? Color.Gold : Color.FromArgb(40, 167, 69));
                }
            }
        }

        private void DrawPoint(Graphics g, Point point, Color color)
        {
            int size = 8;
            using (var brush = new SolidBrush(color))
                g.FillEllipse(brush, point.X - size / 2, point.Y - size / 2, size, size);
            using (var pen = new Pen(Color.White, 1))
                g.DrawEllipse(pen, point.X - size / 2, point.Y - size / 2, size, size);
        }
    }
}
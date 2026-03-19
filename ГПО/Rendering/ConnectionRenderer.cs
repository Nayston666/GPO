using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using MathApp.Models;

namespace MathApp.Rendering
{
    public static class ConnectionRenderer
    {
        private static readonly Font _valueFont = new Font("Segoe UI", 9, FontStyle.Bold);
        private static readonly Pen _shadowPen = new Pen(Color.FromArgb(30, 0, 0, 0), 4);
        private static readonly Pen _connectionPen = new Pen(Color.Cyan, 3);
        private static readonly Pen _tempConnectionPen = new Pen(Color.White, 2);

        /// <summary>
        /// Рисует соединение между двумя блоками
        /// </summary>
        public static void Draw(Graphics g, Connection conn, MathTool source, MathTool target)
        {
            if (source == null || target == null) return;

            Point start = GetOutputPoint(source);
            Point end = GetInputPoint(target, conn.TargetInput);

            // Тень
            Point shadowStart = new Point(start.X + 2, start.Y + 2);
            Point shadowEnd = new Point(end.X + 2, end.Y + 2);

            _shadowPen.StartCap = LineCap.Round;
            _shadowPen.EndCap = LineCap.Round;
            DrawCurve(g, _shadowPen, shadowStart, shadowEnd);

            // Основная линия
            _connectionPen.StartCap = LineCap.Round;
            _connectionPen.EndCap = LineCap.ArrowAnchor;
            DrawCurve(g, _connectionPen, start, end);

            // Отображение значения
            if (conn.CurrentValue.HasValue)
            {
                DrawValue(g, conn.CurrentValue.Value, start, end);
            }
        }

        /// <summary>
        /// Рисует временное соединение при создании нового
        /// </summary>
        public static void DrawTemp(Graphics g, MathTool source, Point endPoint)
        {
            if (source == null) return;

            Point start = GetOutputPoint(source);

            _tempConnectionPen.DashStyle = DashStyle.Dash;
            _tempConnectionPen.StartCap = LineCap.Round;
            _tempConnectionPen.EndCap = LineCap.Round;

            DrawCurve(g, _tempConnectionPen, start, endPoint);
        }

        /// <summary>
        /// Рисует все соединения для списка блоков
        /// </summary>
        public static void DrawAll(Graphics g, IEnumerable<Connection> connections,
                                   IEnumerable<MathTool> tools)
        {
            foreach (var conn in connections)
            {
                var source = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                var target = tools.FirstOrDefault(t => t.Id == conn.TargetToolId);

                if (source != null && target != null)
                {
                    Draw(g, conn, source, target);
                }
            }
        }

        private static void DrawCurve(Graphics g, Pen pen, Point start, Point end)
        {
            Point mid1 = new Point(start.X + 50, start.Y);
            Point mid2 = new Point(end.X - 50, end.Y);
            g.DrawBezier(pen, start, mid1, mid2, end);
        }

        private static void DrawValue(Graphics g, double value, Point start, Point end)
        {
            Point mid = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2 - 20);

            string valueText = value.ToString("F2");
            SizeF textSize = g.MeasureString(valueText, _valueFont);

            RectangleF textBg = new RectangleF(
                mid.X - textSize.Width / 2 - 3,
                mid.Y - 2,
                textSize.Width + 6,
                textSize.Height + 4
            );

            using (var bgBrush = new SolidBrush(Color.FromArgb(200, 30, 30, 35)))
            {
                g.FillRectangle(bgBrush, textBg);
            }

            g.DrawString(valueText, _valueFont, Brushes.Yellow,
                        mid.X - textSize.Width / 2, mid.Y);
        }

        private static Point GetOutputPoint(MathTool tool)
        {
            return new Point(tool.Position.X + tool.Size.Width + 5,
                            tool.Position.Y + tool.Size.Height / 2);
        }

        private static Point GetInputPoint(MathTool tool, InputType input)
        {
            if (tool.Type == ToolType.Chart || tool.Type == ToolType.SineGenerator)
            {
                return new Point(tool.Position.X - 5,
                                tool.Position.Y + tool.Size.Height / 2);
            }

            return new Point(tool.Position.X - 5,
                input == InputType.A ? tool.Position.Y + 20 :
                tool.Position.Y + tool.Size.Height - 20);
        }
    }
}
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
        private static Font _valueFont = new Font("Segoe UI", 8, FontStyle.Bold);
        private static Pen _connectionPen = new Pen(Color.Cyan, 2.5f);
        private static Pen _tempPen = new Pen(Color.White, 2) { DashStyle = DashStyle.Dash };

        public static void DrawAll(Graphics g, IEnumerable<Connection> connections, IEnumerable<MathTool> tools)
        {
            foreach (var conn in connections)
            {
                var source = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                var target = tools.FirstOrDefault(t => t.Id == conn.TargetToolId);
                if (source != null && target != null)
                    DrawConnection(g, source, target, conn.TargetInput, conn.CurrentValue);
            }
        }

        public static void DrawTemp(Graphics g, MathTool source, Point endPoint)
        {
            Point start = new Point(source.Position.X + source.Size.Width + 5, source.Position.Y + source.Size.Height / 2);
            DrawCurve(g, _tempPen, start, endPoint);
        }

        private static void DrawConnection(Graphics g, MathTool source, MathTool target, InputType input, double? value)
        {
            Point start = new Point(source.Position.X + source.Size.Width + 5, source.Position.Y + source.Size.Height / 2);
            Point end;

            if (target.Type == ToolType.Operation)
            {
                end = (input == InputType.A) ?
                    new Point(target.Position.X - 5, target.Position.Y + 20) :
                    new Point(target.Position.X - 5, target.Position.Y + target.Size.Height - 20);
            }
            else
            {
                end = new Point(target.Position.X - 5, target.Position.Y + target.Size.Height / 2);
            }

            DrawCurve(g, _connectionPen, start, end);

            if (value.HasValue)
            {
                Point mid = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2 - 15);
                string text = value.Value.ToString("F2");
                SizeF size = g.MeasureString(text, _valueFont);
                using (var bg = new SolidBrush(Color.FromArgb(180, 30, 30, 35)))
                    g.FillRectangle(bg, mid.X - size.Width / 2 - 2, mid.Y - 8, size.Width + 4, size.Height + 4);
                g.DrawString(text, _valueFont, Brushes.Yellow, mid.X - size.Width / 2, mid.Y - 6);
            }
        }

        private static void DrawCurve(Graphics g, Pen pen, Point start, Point end)
        {
            int offset = Math.Min(50, Math.Abs(end.X - start.X) / 2);
            Point mid1 = new Point(start.X + offset, start.Y);
            Point mid2 = new Point(end.X - offset, end.Y);
            g.DrawBezier(pen, start, mid1, mid2, end);
        }
    }
}
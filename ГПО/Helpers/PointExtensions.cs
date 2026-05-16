using System.Drawing;

namespace MathApp.Helpers
{
    public static class PointExtensions
    {
        public static double DistanceTo(this Point p1, Point p2)
        {
            return System.Math.Sqrt(System.Math.Pow(p1.X - p2.X, 2) + System.Math.Pow(p1.Y - p2.Y, 2));
        }

        public static Point Add(this Point p1, Point p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Point Subtract(this Point p1, Point p2)
        {
            return new Point(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Point Multiply(this Point p, float factor)
        {
            return new Point((int)(p.X * factor), (int)(p.Y * factor));
        }

        public static Point Divide(this Point p, float factor)
        {
            if (factor == 0) return p;
            return new Point((int)(p.X / factor), (int)(p.Y / factor));
        }

        public static bool IsInside(this Point p, Rectangle rect)
        {
            return p.X >= rect.X && p.X <= rect.Right &&
                   p.Y >= rect.Y && p.Y <= rect.Bottom;
        }

        public static Point Clamp(this Point p, Rectangle bounds)
        {
            int x = p.X;
            int y = p.Y;

            if (x < bounds.Left) x = bounds.Left;
            if (x > bounds.Right) x = bounds.Right;
            if (y < bounds.Top) y = bounds.Top;
            if (y > bounds.Bottom) y = bounds.Bottom;

            return new Point(x, y);
        }

        public static Point FromSize(Size size)
        {
            return new Point(size.Width, size.Height);
        }

        public static Size ToSize(this Point p)
        {
            return new Size(p.X, p.Y);
        }

        public static Point Round(this PointF p)
        {
            return new Point((int)System.Math.Round(p.X), (int)System.Math.Round(p.Y));
        }
    }
}
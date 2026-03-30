using System.Drawing;

namespace MathApp.Helpers
{
    public static class PointExtensions
    {
        public static double DistanceTo(this Point p1, Point p2)
        {
            return System.Math.Sqrt(System.Math.Pow(p1.X - p2.X, 2) + System.Math.Pow(p1.Y - p2.Y, 2));
        }

        public static Point Clamp(this Point p, Rectangle bounds)
        {
            int x = p.X, y = p.Y;
            if (x < bounds.Left) x = bounds.Left;
            if (x > bounds.Right) x = bounds.Right;
            if (y < bounds.Top) y = bounds.Top;
            if (y > bounds.Bottom) y = bounds.Bottom;
            return new Point(x, y);
        }
    }
}
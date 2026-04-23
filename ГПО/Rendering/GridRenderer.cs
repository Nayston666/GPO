using System.Drawing;
using System.Windows.Forms;

namespace MathApp.Rendering
{
    public static class GridRenderer
    {
        private static Pen _thinPen = new Pen(Color.FromArgb(35, 35, 40), 1);
        private static Pen _boldPen = new Pen(Color.FromArgb(50, 50, 55), 1.5f);
        private const int GridSize = 30;
        private const int BoldGridSize = 150;

        public static void Draw(Graphics g, Panel panel)
        {
            Point offset = panel.AutoScrollPosition;

            for (int x = (offset.X % GridSize + GridSize) % GridSize; x < panel.Width; x += GridSize)
                g.DrawLine(_thinPen, x, 0, x, panel.Height);
            for (int y = (offset.Y % GridSize + GridSize) % GridSize; y < panel.Height; y += GridSize)
                g.DrawLine(_thinPen, 0, y, panel.Width, y);

            for (int x = (offset.X % BoldGridSize + BoldGridSize) % BoldGridSize; x < panel.Width; x += BoldGridSize)
                g.DrawLine(_boldPen, x, 0, x, panel.Height);
            for (int y = (offset.Y % BoldGridSize + BoldGridSize) % BoldGridSize; y < panel.Height; y += BoldGridSize)
                g.DrawLine(_boldPen, 0, y, panel.Width, y);
        }
    }
}
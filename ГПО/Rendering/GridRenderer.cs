using System.Drawing;
using System.Windows.Forms;

namespace MathApp.Rendering
{
    public static class GridRenderer
    {
        // Светло-серая сетка для маленьких квадратов
        private static readonly Pen _thinPen = new Pen(Color.FromArgb(220, 220, 220), 1);
        // Более тёмная для больших квадратов
        private static readonly Pen _boldPen = new Pen(Color.FromArgb(180, 180, 180), 1.5f);

        private const int GridSize = 30;
        private const int BoldGridSize = 150;

        public static void Draw(Graphics g, Panel panel)
        {
            // Очищаем фон белым
            g.Clear(Color.White);

            Point scrollOffset = panel.AutoScrollPosition;

            DrawThinGrid(g, panel, scrollOffset);
            DrawBoldGrid(g, panel, scrollOffset);
        }

        private static void DrawThinGrid(Graphics g, Panel panel, Point scrollOffset)
        {
            int offsetX = scrollOffset.X % GridSize;
            int offsetY = scrollOffset.Y % GridSize;

            if (offsetX < 0) offsetX += GridSize;
            if (offsetY < 0) offsetY += GridSize;

            for (int x = offsetX; x < panel.Width; x += GridSize)
            {
                g.DrawLine(_thinPen, x, 0, x, panel.Height);
            }

            for (int y = offsetY; y < panel.Height; y += GridSize)
            {
                g.DrawLine(_thinPen, 0, y, panel.Width, y);
            }
        }

        private static void DrawBoldGrid(Graphics g, Panel panel, Point scrollOffset)
        {
            int boldOffsetX = scrollOffset.X % BoldGridSize;
            int boldOffsetY = scrollOffset.Y % BoldGridSize;

            if (boldOffsetX < 0) boldOffsetX += BoldGridSize;
            if (boldOffsetY < 0) boldOffsetY += BoldGridSize;

            for (int x = boldOffsetX; x < panel.Width; x += BoldGridSize)
            {
                g.DrawLine(_boldPen, x, 0, x, panel.Height);
            }

            for (int y = boldOffsetY; y < panel.Height; y += BoldGridSize)
            {
                g.DrawLine(_boldPen, 0, y, panel.Width, y);
            }
        }
    }
}
using System.Drawing;
using System.Windows.Forms;
using MathApp.Helpers;
using G = System.Drawing.Graphics;

namespace MathApp.Rendering
{
    public static class GridRenderer
    {
        private static readonly Pen _thinPen = new Pen(Color.FromArgb(30, 30, 35), 1);
        private static readonly Pen _boldPen = new Pen(Color.FromArgb(50, 50, 55), 1.5f);
        private const int GridSize = 30;
        private const int BoldGridSize = 150;

        public static void Draw(G g, Panel panel)
        {
            Point scrollOffset = panel.AutoScrollPosition;
            DrawThinGrid(g, panel, scrollOffset);
            DrawBoldGrid(g, panel, scrollOffset);
        }

        private static void DrawThinGrid(G g, Panel panel, Point scrollOffset)
        {
            int offsetX = scrollOffset.X % GridSize;
            int offsetY = scrollOffset.Y % GridSize;
            if (offsetX < 0) offsetX += GridSize;
            if (offsetY < 0) offsetY += GridSize;

            for (int x = offsetX; x < panel.Width; x += GridSize)
                g.DrawLine(_thinPen, x, 0, x, panel.Height);
            for (int y = offsetY; y < panel.Height; y += GridSize)
                g.DrawLine(_thinPen, 0, y, panel.Width, y);
        }

        private static void DrawBoldGrid(G g, Panel panel, Point scrollOffset)
        {
            int boldOffsetX = scrollOffset.X % BoldGridSize;
            int boldOffsetY = scrollOffset.Y % BoldGridSize;
            if (boldOffsetX < 0) boldOffsetX += BoldGridSize;
            if (boldOffsetY < 0) boldOffsetY += BoldGridSize;

            for (int x = boldOffsetX; x < panel.Width; x += BoldGridSize)
                g.DrawLine(_boldPen, x, 0, x, panel.Height);
            for (int y = boldOffsetY; y < panel.Height; y += BoldGridSize)
                g.DrawLine(_boldPen, 0, y, panel.Width, y);
        }
    }
}
using System.Drawing;
using System.Windows.Forms;

namespace MathApp.UI
{
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            AllowDrop = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoScroll = true;
            AutoScrollMinSize = new Size(3000, 3000);
            BackColor = Color.FromArgb(30, 30, 35);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        public Point GetRealMouseLocation(Point mouseLocation)
        {
            return new Point(mouseLocation.X - AutoScrollPosition.X, mouseLocation.Y - AutoScrollPosition.Y);
        }
    }
}
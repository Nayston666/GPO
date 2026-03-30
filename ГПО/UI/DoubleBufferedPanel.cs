using System.Drawing;
using System.Windows.Forms;

namespace MathApp.UI
{
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);

            this.AutoScroll = true;
            this.AutoScrollMinSize = new Size(2000, 2000);
            this.BackColor = Color.FromArgb(30, 30, 35);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        public Point GetRealMouseLocation(Point mouseLocation)
        {
            return new Point(
                mouseLocation.X - this.AutoScrollPosition.X,
                mouseLocation.Y - this.AutoScrollPosition.Y
            );
        }
    }
}
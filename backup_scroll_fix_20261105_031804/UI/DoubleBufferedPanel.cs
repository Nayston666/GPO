using System.Drawing;
using System.Windows.Forms;

namespace MathApp.UI
{
    /// <summary>
    /// Панель с двойной буферизацией для плавной отрисовки
    /// </summary>
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            // Включаем двойную буферизацию
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);

            // Настройки прокрутки
            this.AutoScroll = true;
            this.AutoScrollMinSize = new Size(2000, 2000);
            this.BackColor = Color.White;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Рисуем фон явно белым
            using (var brush = new SolidBrush(Color.White))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }

        /// <summary>
        /// Преобразует экранные координаты в реальные с учетом прокрутки
        /// </summary>
        public Point GetRealMouseLocation(Point mouseLocation)
        {
            return new Point(
                mouseLocation.X - this.AutoScrollPosition.X,
                mouseLocation.Y - this.AutoScrollPosition.Y
            );
        }
    }
}
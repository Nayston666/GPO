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
            this.BackColor = Color.FromArgb(30, 30, 35);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Отключаем фон для предотвращения мерцания
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
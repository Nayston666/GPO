using System;
using System.Windows.Forms;
using MathApp.Helpers;

namespace MathApp.UI
{
    /// <summary>
    /// NumericUpDown с поддержкой инженерного ввода:
    /// 5e-14, 5n, 100p, 2.5u, 0.1m, 1k, 2M
    /// </summary>
    public class EngineeringNumericUpDown : NumericUpDown
    {
        private string _unit = "";
        private bool _isUpdating = false;

        public string Unit
        {
            get => _unit;
            set
            {
                _unit = value;
                if (!_isUpdating)
                    UpdateText();
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            char c = char.ToLower(e.KeyChar);

            // Разрешаем цифры, точку, минус, e, и буквы-суффиксы
            if (char.IsDigit(c) || c == '.' || c == '-' || c == 'e' ||
                c == 'p' || c == 'n' || c == 'u' || c == 'm' || c == 'k' || c == 'g')
            {
                return;
            }

            e.Handled = true;
            base.OnKeyPress(e);
        }

        protected override void OnValidating(System.ComponentModel.CancelEventArgs e)
        {
            _isUpdating = true;

            if (EngineeringParser.TryParse(this.Text, out double result))
            {
                if (result >= (double)Minimum && result <= (double)Maximum)
                {
                    if ((decimal)result != Value)
                        Value = (decimal)result;
                    UpdateText();
                }
                else
                {
                    UpdateText();
                }
            }
            else
            {
                UpdateText();
            }

            _isUpdating = false;
            base.OnValidating(e);
        }

        protected override void OnValueChanged(EventArgs e)
        {
            if (!_isUpdating)
                UpdateText();
            base.OnValueChanged(e);
        }

        private void UpdateText()
        {
            this.Text = EngineeringParser.ToEngineeringString((double)Value, _unit);
        }
    }
}
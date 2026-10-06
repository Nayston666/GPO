using System;
using System.Globalization;

namespace MathApp.Helpers
{
    public static class EngineeringParser
    {
        /// <summary>
        /// Преобразует строку вида "5e-14", "0.05n", "100u", "1.2m" в double.
        /// Поддерживает: p (пико 1e-12), n (нано 1e-9), u (микро 1e-6),
        /// m (милли 1e-3), k (кило 1e3), M (мега 1e6), G (гига 1e9)
        /// </summary>
        public static bool TryParse(string input, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim().Replace(',', '.');

            // Буквенные суффиксы
            var suffixes = new System.Collections.Generic.Dictionary<char, double>
            {
                {'p', 1e-12},  // пико
                {'n', 1e-9},   // нано
                {'u', 1e-6},   // микро (u вместо μ для удобства)
                {'m', 1e-3},   // милли
                {'k', 1e3},    // кило
                {'M', 1e6},    // мега
                {'G', 1e9}     // гига
            };

            char lastChar = input[input.Length - 1];
            double multiplier = 1.0;
            string numberPart = input;

            // Проверяем суффикс
            if (char.IsLetter(lastChar) && suffixes.ContainsKey(lastChar))
            {
                multiplier = suffixes[lastChar];
                numberPart = input.Substring(0, input.Length - 1);
            }
            // Специальная обработка для "u" (микро)
            else if (input.Contains("u") && !input.Contains("e") && !input.Contains("E"))
            {
                int uIndex = input.IndexOf('u');
                if (uIndex > 0 && char.IsDigit(input[uIndex - 1]))
                {
                    multiplier = 1e-6;
                    numberPart = input.Substring(0, uIndex);
                }
            }

            if (!double.TryParse(numberPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                return false;

            value = num * multiplier;
            return true;
        }

        /// <summary>
        /// Преобразует число в читаемый инженерный формат
        /// </summary>
        public static string ToEngineeringString(double value, string unit = "")
        {
            if (value == 0) return $"0{unit}";

            double abs = Math.Abs(value);

            if (abs >= 1e9) return $"{value / 1e9:F3} G{unit}";
            if (abs >= 1e6) return $"{value / 1e6:F3} M{unit}";
            if (abs >= 1e3) return $"{value / 1e3:F3} k{unit}";
            if (abs >= 1) return $"{value:F6} {unit}".TrimEnd();
            if (abs >= 1e-3) return $"{value * 1e3:F3} m{unit}";
            if (abs >= 1e-6) return $"{value * 1e6:F3} u{unit}";
            if (abs >= 1e-9) return $"{value * 1e9:F3} n{unit}";
            if (abs >= 1e-12) return $"{value * 1e12:F3} p{unit}";

            return value.ToString("E3", CultureInfo.InvariantCulture) + unit;
        }
    }
}
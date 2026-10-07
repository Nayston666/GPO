using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MathApp.Helpers
{
    public static class EngineeringParser
    {
        /// <summary>
        /// Поддерживает:
        ///
        /// Обычные числа:
        /// 2
        /// 0.5
        /// 1e-3
        ///
        /// Инженерные суффиксы:
        /// 500m
        /// 100u
        /// 20n
        /// 2k
        ///
        /// Число + единица:
        /// 2 Hz
        /// 5 V
        /// 0.5 s
        ///
        /// Префикс + единица:
        /// 500 ms
        /// 500ms
        /// 100 us
        /// 100 µs
        /// 20 ns
        /// 2 kHz
        /// 5 mV
        ///
        /// Внутри значение возвращается в базовых единицах SI.
        /// </summary>
        public static bool TryParse(string input, out double value)
        {
            value = 0.0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            string text = input
                .Trim()
                .Replace(',', '.')
                .Replace('μ', 'u')
                .Replace('µ', 'u');

            // Формат:
            // число + необязательный инженерный префикс +
            // необязательная единица измерения.
            //
            // Примеры:
            // "500 ms" -> number=500, prefix=m, unit=s
            // "2 kHz"  -> number=2,   prefix=k, unit=Hz
            // "5 V"    -> number=5,   prefix="", unit=V
            // "500m"   -> number=500, prefix=m, unit=""
            var match = Regex.Match(
                text,
                @"^\s*" +
                @"(?<number>[+-]?(?:(?:\d+(?:\.\d*)?)|(?:\.\d+))(?:[eE][+-]?\d+)?)" +
                @"\s*" +
                @"(?<prefix>[pnumkMG]?)" +
                @"\s*" +
                @"(?<unit>[A-Za-z°²/Ω]*)" +
                @"\s*$",
                RegexOptions.CultureInvariant);

            if (!match.Success)
                return false;

            if (!double.TryParse(
                match.Groups["number"].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double number))
            {
                return false;
            }

            string prefix = match.Groups["prefix"].Value;

            double multiplier;

            switch (prefix)
            {
                case "p":
                    multiplier = 1e-12;
                    break;

                case "n":
                    multiplier = 1e-9;
                    break;

                case "u":
                    multiplier = 1e-6;
                    break;

                case "m":
                    multiplier = 1e-3;
                    break;

                case "k":
                    multiplier = 1e3;
                    break;

                case "M":
                    multiplier = 1e6;
                    break;

                case "G":
                    multiplier = 1e9;
                    break;

                default:
                    multiplier = 1.0;
                    break;
            }

            value = number * multiplier;

            return !double.IsNaN(value) &&
                   !double.IsInfinity(value);
        }

        /// <summary>
        /// Форматирует значение в инженерном виде.
        /// Для секунд выводит s / ms / us / ns / ps / fs / as.
        /// </summary>
        public static string ToEngineeringString(
            double value,
            string unit = "")
        {
            if (value == 0.0)
            {
                if (string.IsNullOrEmpty(unit))
                    return "0";

                return "0 " + unit;
            }

            double abs = Math.Abs(value);

            // Для времени делаем привычный вывод.
            if (string.Equals(
                unit,
                "s",
                StringComparison.OrdinalIgnoreCase))
            {
                if (abs >= 1.0)
                    return $"{value:F6} s";

                if (abs >= 1e-3)
                    return $"{value * 1e3:F3} ms";

                if (abs >= 1e-6)
                    return $"{value * 1e6:F3} us";

                if (abs >= 1e-9)
                    return $"{value * 1e9:F3} ns";

                if (abs >= 1e-12)
                    return $"{value * 1e12:F3} ps";

                if (abs >= 1e-15)
                    return $"{value * 1e15:F3} fs";

                return $"{value * 1e18:F3} as";
            }

            // Остальные величины.
            if (abs >= 1e9)
                return $"{value / 1e9:F3} G{unit}";

            if (abs >= 1e6)
                return $"{value / 1e6:F3} M{unit}";

            if (abs >= 1e3)
                return $"{value / 1e3:F3} k{unit}";

            if (abs >= 1.0)
            {
                if (string.IsNullOrEmpty(unit))
                    return $"{value:F6}";

                return $"{value:F6} {unit}";
            }

            if (abs >= 1e-3)
                return $"{value * 1e3:F3} m{unit}";

            if (abs >= 1e-6)
                return $"{value * 1e6:F3} u{unit}";

            if (abs >= 1e-9)
                return $"{value * 1e9:F3} n{unit}";

            if (abs >= 1e-12)
                return $"{value * 1e12:F3} p{unit}";

            if (string.IsNullOrEmpty(unit))
            {
                return value.ToString(
                    "E3",
                    CultureInfo.InvariantCulture);
            }

            return value.ToString(
                       "E3",
                       CultureInfo.InvariantCulture) +
                   " " +
                   unit;
        }
    }
}

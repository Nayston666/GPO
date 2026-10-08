using System;
using MathApp.Models;

namespace MathApp.Core
{
    public static class SignalGenerator
    {
        public static double Calculate(MathTool tool, double time)
        {
            if (tool == null)
                return 0.0;

            // Одиночный прямоугольный импульс:
            // 0 -> фронт -> площадка -> спад -> 0.
            if (tool.Waveform == GeneratorWaveform.Step)
                return CalculateStep(tool, time);

            if (tool.Frequency <= 0 ||
                double.IsNaN(tool.Frequency) ||
                double.IsInfinity(tool.Frequency))
            {
                return 0.0;
            }

            double period = 1.0 / tool.Frequency;

            if (tool.Waveform == GeneratorWaveform.Sine)
            {
                double radians =
                    time * tool.Frequency * 2.0 * Math.PI +
                    tool.Phase * Math.PI / 180.0;

                return tool.Amplitude * Math.Sin(radians);
            }

            double localTime = time % period;
            if (localTime < 0)
                localTime += period;

            switch (tool.Waveform)
            {
                case GeneratorWaveform.Trapezoid:
                    return CalculateTrapezoid(tool, localTime, period);

                case GeneratorWaveform.Square:
                    return CalculateSquare(tool, localTime, period);

                // Внутреннее имя Sawtooth оставлено для совместимости
                // со старыми сохранениями проекта.
                // Фактически это треугольный импульс.
                case GeneratorWaveform.Sawtooth:
                    return CalculateTriangle(tool, localTime, period);

                default:
                    return 0.0;
            }
        }

        private static double CalculateStep(MathTool tool, double time)
        {
            double duration = Math.Max(0.0, tool.StepDuration);
            double rise = Math.Max(0.0, tool.RiseTime);
            double fall = Math.Max(0.0, tool.FallTime);

            if (duration <= 0.0 || time < 0.0 || time > duration)
                return 0.0;

            // Если фронт + спад не помещаются в импульс,
            // сохраняем их отношение и пропорционально уменьшаем.
            if (rise + fall > duration && rise + fall > 0.0)
            {
                double scale = duration / (rise + fall);
                rise *= scale;
                fall *= scale;
            }

            if (rise > 0.0 && time < rise)
                return tool.Amplitude * time / rise;

            double fallStart = duration - fall;

            if (time <= fallStart)
                return tool.Amplitude;

            if (fall > 0.0)
                return tool.Amplitude * (duration - time) / fall;

            return 0.0;
        }

        private static double CalculateSquare(
            MathTool tool,
            double time,
            double period)
        {
            double half = period * 0.5;

            double rise =
                Math.Min(
                    Math.Max(0.0, tool.RiseTime),
                    half);

            double fall =
                Math.Min(
                    Math.Max(0.0, tool.FallTime),
                    half);

            // Первая половина периода — высокий уровень.
            if (rise > 0.0 && time < rise)
                return tool.Amplitude * time / rise;

            if (time < half)
                return tool.Amplitude;

            // Во второй половине начинается спад.
            double fallingTime = time - half;

            if (fall > 0.0 && fallingTime < fall)
            {
                return tool.Amplitude *
                       (1.0 - fallingTime / fall);
            }

            return 0.0;
        }

        private static double CalculateTriangle(
            MathTool tool,
            double time,
            double period)
        {
            double rise = Math.Max(0.0, tool.RiseTime);
            double fall = Math.Max(0.0, tool.FallTime);

            // Треугольный импульс должен уместиться в один период.
            // Если введено больше периода, сохраняем наклон
            // (отношение rise/fall) и масштабируем оба времени.
            if (rise + fall > period && rise + fall > 0.0)
            {
                double scale = period / (rise + fall);
                rise *= scale;
                fall *= scale;
            }

            if (rise <= 0.0 && fall <= 0.0)
                return 0.0;

            // Мгновенный подъём + конечный спад.
            if (rise <= 0.0)
            {
                if (fall > 0.0 && time < fall)
                    return tool.Amplitude * (1.0 - time / fall);

                return 0.0;
            }

            // Фронт от 0 до A.
            if (time < rise)
                return tool.Amplitude * time / rise;

            // Спад от A до 0.
            double fallingTime = time - rise;

            if (fall > 0.0 && fallingTime < fall)
            {
                return tool.Amplitude *
                       (1.0 - fallingTime / fall);
            }

            // Если rise + fall меньше периода,
            // остаток периода остаётся на нуле.
            return 0.0;
        }

        private static double CalculateTrapezoid(
            MathTool tool,
            double time,
            double period)
        {
            double rise = Math.Max(0.0, tool.RiseTime);
            double fall = Math.Max(0.0, tool.FallTime);

            if (rise + fall > period && rise + fall > 0.0)
            {
                double scale = period / (rise + fall);
                rise *= scale;
                fall *= scale;
            }

            double plateauEnd = period - fall;

            if (rise > 0.0 && time < rise)
                return tool.Amplitude * time / rise;

            if (time <= plateauEnd)
                return tool.Amplitude;

            if (fall > 0.0)
                return tool.Amplitude * (period - time) / fall;

            return 0.0;
        }

        public static double GetPeriod(MathTool tool)
        {
            if (tool == null || tool.Frequency <= 0.0)
                return double.PositiveInfinity;

            return 1.0 / tool.Frequency;
        }
    }
}

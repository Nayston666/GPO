using System;
using MathApp.Models;

namespace MathApp.Core
{
    public static class SignalGenerator
    {
        public static double Calculate(
            MathTool tool,
            double time,
            double simulationDuration)
        {
            if (tool == null)
                return 0.0;

            double delay = Math.Max(0.0, tool.Delay);

            if (time < delay)
                return 0.0;

            double localTime = time - delay;

            double availableDuration =
                Math.Max(
                    0.0,
                    simulationDuration - delay);

            if (availableDuration <= 0.0)
                return 0.0;

            switch (tool.Waveform)
            {
                case GeneratorWaveform.Sine:
                    return CalculateSine(tool, localTime);

                case GeneratorWaveform.Step:
                    return CalculateStep(
                        tool,
                        localTime,
                        availableDuration);

                case GeneratorWaveform.Trapezoid:
                    return CalculateTrapezoid(
                        tool,
                        localTime,
                        availableDuration);

                case GeneratorWaveform.Square:
                    return CalculateSquare(
                        tool,
                        localTime,
                        availableDuration);

                case GeneratorWaveform.Sawtooth:
                    return CalculateTriangle(
                        tool,
                        localTime,
                        availableDuration);

                default:
                    return 0.0;
            }
        }

        // Совместимость со старым кодом.
        public static double Calculate(
            MathTool tool,
            double time)
        {
            if (tool == null)
                return 0.0;

            if (tool.Waveform == GeneratorWaveform.Sine)
            {
                double delay = Math.Max(0.0, tool.Delay);

                if (time < delay)
                    return 0.0;

                return CalculateSine(
                    tool,
                    time - delay);
            }

            return Calculate(
                tool,
                time,
                double.MaxValue);
        }

        private static double CalculateSine(
            MathTool tool,
            double localTime)
        {
            if (tool.Frequency <= 0.0 ||
                double.IsNaN(tool.Frequency) ||
                double.IsInfinity(tool.Frequency))
            {
                return 0.0;
            }

            double radians =
                localTime *
                tool.Frequency *
                2.0 *
                Math.PI +
                tool.Phase *
                Math.PI /
                180.0;

            return tool.Amplitude * Math.Sin(radians);
        }

        private static double CalculateStep(
            MathTool tool,
            double localTime,
            double availableDuration)
        {
            if (localTime < 0.0 ||
                localTime >= availableDuration)
            {
                return 0.0;
            }

            return tool.Amplitude;
        }

        private static double CalculateTrapezoid(
            MathTool tool,
            double localTime,
            double availableDuration)
        {
            if (localTime < 0.0 ||
                localTime >= availableDuration)
            {
                return 0.0;
            }

            double rise = Math.Max(0.0, tool.RiseTime);
            double fall = Math.Max(0.0, tool.FallTime);

            if (rise + fall > availableDuration &&
                rise + fall > 0.0)
            {
                double scale =
                    availableDuration /
                    (rise + fall);

                rise *= scale;
                fall *= scale;
            }

            if (rise > 0.0 &&
                localTime < rise)
            {
                return
                    tool.Amplitude *
                    localTime /
                    rise;
            }

            double fallStart =
                availableDuration - fall;

            if (localTime < fallStart)
                return tool.Amplitude;

            if (fall > 0.0)
            {
                return
                    tool.Amplitude *
                    (availableDuration - localTime) /
                    fall;
            }

            return tool.Amplitude;
        }

        private static double CalculateSquare(
            MathTool tool,
            double localTime,
            double availableDuration)
        {
            if (localTime < 0.0 ||
                localTime >= availableDuration)
            {
                return 0.0;
            }

            double half =
                availableDuration * 0.5;

            double rise =
                Math.Min(
                    Math.Max(0.0, tool.RiseTime),
                    half);

            double fall =
                Math.Min(
                    Math.Max(0.0, tool.FallTime),
                    half);

            if (rise > 0.0 &&
                localTime < rise)
            {
                return
                    tool.Amplitude *
                    localTime /
                    rise;
            }

            if (localTime < half)
                return tool.Amplitude;

            double fallingTime =
                localTime - half;

            if (fall > 0.0 &&
                fallingTime < fall)
            {
                return
                    tool.Amplitude *
                    (1.0 - fallingTime / fall);
            }

            return 0.0;
        }

        private static double CalculateTriangle(
            MathTool tool,
            double localTime,
            double availableDuration)
        {
            if (localTime < 0.0 ||
                localTime >= availableDuration)
            {
                return 0.0;
            }

            double rise =
                Math.Max(0.0, tool.RiseTime);

            double fall =
                Math.Max(0.0, tool.FallTime);

            if (rise + fall > availableDuration &&
                rise + fall > 0.0)
            {
                double scale =
                    availableDuration /
                    (rise + fall);

                rise *= scale;
                fall *= scale;
            }

            if (rise <= 0.0 &&
                fall <= 0.0)
            {
                return 0.0;
            }

            if (rise <= 0.0)
            {
                if (fall > 0.0 &&
                    localTime < fall)
                {
                    return
                        tool.Amplitude *
                        (1.0 - localTime / fall);
                }

                return 0.0;
            }

            if (localTime < rise)
            {
                return
                    tool.Amplitude *
                    localTime /
                    rise;
            }

            double fallingTime =
                localTime - rise;

            if (fall > 0.0 &&
                fallingTime < fall)
            {
                return
                    tool.Amplitude *
                    (1.0 - fallingTime / fall);
            }

            return 0.0;
        }

        public static double GetPeriod(MathTool tool)
        {
            if (tool == null ||
                tool.Waveform != GeneratorWaveform.Sine ||
                tool.Frequency <= 0.0)
            {
                return double.PositiveInfinity;
            }

            return 1.0 / tool.Frequency;
        }
    }
}

using MathApp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
//12123123123123123
namespace MathApp.Core
{
    public class CalculationEngine
    {
        private double _currentTime = 0;

        public double GetInputValue(MathTool tool, InputType input,
                                    List<MathTool> tools, List<Connection> connections,
                                    Dictionary<Guid, double> calculatedValues)
        {
            var conn = connections.FirstOrDefault(c =>
                c.TargetToolId == tool.Id && c.TargetInput == input);

            if (conn != null)
            {
                var source = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                if (source != null && (source.Type == ToolType.SineGenerator || source.Type == ToolType.StepGenerator) && source.LastResult.HasValue)
                    return source.LastResult.Value;
                if (calculatedValues.ContainsKey(conn.SourceToolId))
                    return calculatedValues[conn.SourceToolId];
                return double.NaN;
            }

            return input == InputType.A ? tool.CustomValueA : tool.CustomValueB;
        }

        public double Calculate(MathOperation op, double a, double b, MathTool tool)
        {
            try
            {
                switch (op)
                {
                    case MathOperation.Addition:
                        return SafeAdd(a, b);

                    case MathOperation.Subtraction:
                        return SafeSubtract(a, b);

                    case MathOperation.Multiplication:
                        return SafeMultiply(a, b);

                    case MathOperation.Division:
                        return SafeDivide(a, b);

                    case MathOperation.Integrator:
                        return Integrate(a, tool);

                    case MathOperation.Differentiator:
                        return Differentiate(a, tool);

                    case MathOperation.Interpolator:
                        return Interpolate(a, tool.InterpolationPoints);

                    case MathOperation.FileIO:
                        return ProcessFileIO(a, tool);

                    case MathOperation.StepGenerator:
                        // Ступенчатый генератор не должен обрабатываться здесь
                        // Он обрабатывается в BatchProcessor
                        return a;

                    default:
                        return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private double SafeAdd(double a, double b)
        {
            if (a > 0 && b > double.MaxValue - a)
                return double.MaxValue;
            if (a < 0 && b < -double.MaxValue - a)
                return -double.MaxValue;
            return a + b;
        }

        private double SafeSubtract(double a, double b)
        {
            if (a > 0 && -b > double.MaxValue - a)
                return double.MaxValue;
            if (a < 0 && -b < -double.MaxValue - a)
                return -double.MaxValue;
            return a - b;
        }

        private double SafeMultiply(double a, double b)
        {
            if (a == 0 || b == 0) return 0;

            if (Math.Abs(a) > double.MaxValue / Math.Abs(b))
                return (a > 0 ^ b > 0) ? -double.MaxValue : double.MaxValue;
            return a * b;
        }

        private double SafeDivide(double a, double b)
        {
            if (Math.Abs(b) < 1e-300)
            {
                if (Math.Abs(a) < 1e-300)
                    return 0;
                return (a > 0) ? double.MaxValue : -double.MaxValue;
            }
            return a / b;
        }

        private double Integrate(double input, MathTool tool)
        {
            double step = tool.StepSize;

            if (step <= 0 || double.IsNaN(step) || double.IsInfinity(step))
            {
                step = 1e-6;
                tool.StepSize = step;
            }

            if (step < 1e-15)
            {
                step = 1e-15;
                tool.StepSize = step;
            }

            double safeInput = input;
            if (Math.Abs(safeInput) > 1e100)
            {
                safeInput = Math.Sign(safeInput) * 1e100;
            }

            double newIntegral;

            if (step < 1e-9)
            {
                newIntegral = tool.IntegralValue + safeInput * step;
            }
            else
            {
                double prevInput = tool.PreviousInput;
                if (Math.Abs(prevInput) > 1e100) prevInput = Math.Sign(prevInput) * 1e100;
                newIntegral = tool.IntegralValue + (prevInput + safeInput) / 2 * step;
            }

            if (double.IsInfinity(newIntegral) || double.IsNaN(newIntegral) || Math.Abs(newIntegral) > 1e100)
            {
                newIntegral = Math.Sign(newIntegral) * 1e100;
                tool.IntegralValue = newIntegral;
                tool.PreviousInput = safeInput;
                return newIntegral;
            }

            tool.IntegralValue = newIntegral;
            tool.PreviousInput = safeInput;

            return newIntegral;
        }

        private double Differentiate(double input, MathTool tool)
        {
            double dt = tool.StepSize;
            if (dt <= 0 || double.IsNaN(dt) || double.IsInfinity(dt))
            {
                dt = 1e-6;
                tool.StepSize = dt;
            }

            if (dt < 1e-15)
            {
                dt = 1e-15;
                tool.StepSize = dt;
            }

            double derivative = (input - tool.PreviousOutput) / dt;

            if (Math.Abs(derivative) > 1e300)
                derivative = (derivative > 0) ? 1e300 : -1e300;

            tool.PreviousOutput = input;
            return derivative;
        }

        private double Interpolate(double x, List<PointF> points)
        {
            if (points == null || points.Count == 0) return x;
            if (points.Count == 1) return points[0].Y;

            var sorted = points.OrderBy(p => p.X).ToList();
            if (x <= sorted[0].X) return sorted[0].Y;
            if (x >= sorted[sorted.Count - 1].X) return sorted[sorted.Count - 1].Y;

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                if (x >= sorted[i].X && x <= sorted[i + 1].X)
                {
                    double t = (x - sorted[i].X) / (sorted[i + 1].X - sorted[i].X);
                    return sorted[i].Y + t * (sorted[i + 1].Y - sorted[i].Y);
                }
            }
            return sorted.Last().Y;
        }

        private double ProcessFileIO(double input, MathTool tool)
        {
            if (tool.IsReading)
            {
                if (tool.FileData.Count > 0 && tool.CurrentFileIndex < tool.FileData.Count)
                {
                    double value = tool.FileData[tool.CurrentFileIndex];
                    tool.CurrentFileIndex++;
                    return value;
                }
                return 0;
            }
            else
            {
                tool.FileData.Add(input);
                return input;
            }
        }

        public Dictionary<Guid, double> CalculateAll(List<MathTool> tools, List<Connection> connections)
        {
            _currentTime += 0.01;
            var results = new Dictionary<Guid, double>();
            bool changed;

            do
            {
                changed = false;
                foreach (var tool in tools.Where(t => t.Type == ToolType.Operation).OrderBy(t => t.Position.X))
                {
                    if (results.ContainsKey(tool.Id)) continue;

                    double a = GetInputValue(tool, InputType.A, tools, connections, results);
                    double b = GetInputValue(tool, InputType.B, tools, connections, results);

                    if (double.IsNaN(a) || double.IsNaN(b)) continue;

                    double result = Calculate(tool.Operation, a, b, tool);

                    if (double.IsInfinity(result))
                        result = double.MaxValue;

                    results[tool.Id] = result;
                    tool.LastResult = result;

                    foreach (var conn in connections.Where(c => c.SourceToolId == tool.Id))
                        conn.CurrentValue = result;

                    changed = true;
                }
            } while (changed);

            return results;
        }

        public void ResetState(List<MathTool> tools)
        {
            _currentTime = 0;
            foreach (var tool in tools)
            {
                if (tool.Operation == MathOperation.Integrator)
                {
                    tool.IntegralValue = 0;
                    tool.PreviousInput = 0;
                }
                else if (tool.Operation == MathOperation.Differentiator)
                {
                    tool.PreviousTime = 0;
                    tool.PreviousOutput = 0;
                }
                else if (tool.Operation == MathOperation.FileIO && tool.IsReading)
                {
                    tool.CurrentFileIndex = 0;
                }
            }
        }
    }
}
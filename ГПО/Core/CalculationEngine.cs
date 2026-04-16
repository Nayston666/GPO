using MathApp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

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
                if (source != null && source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
                    return source.LastResult.Value;
                if (calculatedValues.ContainsKey(conn.SourceToolId))
                    return calculatedValues[conn.SourceToolId];
                return double.NaN;
            }

            return input == InputType.A ? tool.CustomValueA : tool.CustomValueB;
        }

        public double Calculate(MathOperation op, double a, double b, MathTool tool)
        {
            switch (op)
            {
                case MathOperation.Addition: return a + b;
                case MathOperation.Subtraction: return a - b;
                case MathOperation.Multiplication: return a * b;
                case MathOperation.Division: return b != 0 ? a / b : 0;
                case MathOperation.Integrator:
                    double step = tool.StepSize;
                    double newIntegral = tool.IntegralValue + (tool.PreviousInput + a) / 2 * step;
                    tool.IntegralValue = newIntegral;
                    tool.PreviousInput = a;
                    return newIntegral;
                case MathOperation.Differentiator:
                    double dt = _currentTime - tool.PreviousTime;
                    if (dt < 0.0001) dt = 0.001;
                    double derivative = (a - tool.PreviousOutput) / dt;
                    tool.PreviousOutput = a;
                    tool.PreviousTime = _currentTime;
                    return derivative;
                case MathOperation.Interpolator:
                    return Interpolate(a, tool.InterpolationPoints);
                case MathOperation.FileIO:
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
                        tool.FileData.Add(a);
                        return a;
                    }
                default: return 0;
            }
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
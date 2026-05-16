using System;
using System.Collections.Generic;
using System.Linq;
using MathApp.Models;

namespace MathApp.Core
{
    public class CalculationEngine
    {
        private double _time = 0;
        private Random _random = new Random();

        public void UpdateGenerators(List<MathTool> tools)
        {
            _time += 0.05;

            foreach (var gen in tools.Where(t => t.Type == ToolType.Generator))
            {
                double radians = (_time * gen.Frequency * 2 * Math.PI) + gen.PhaseRad;
                gen.LastResult = gen.Amplitude * Math.Sin(radians);
            }
        }

        public double GetInputValue(MathTool tool, InputType input,
                                    List<MathTool> tools, List<Connection> connections,
                                    Dictionary<Guid, double> calculatedValues)
        {
            var conn = connections.FirstOrDefault(c =>
                c.TargetToolId == tool.Id && c.TargetInput == input);

            if (conn != null)
            {
                var source = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                if (source != null && source.Type == ToolType.Generator && source.LastResult.HasValue)
                    return source.LastResult.Value;
                if (calculatedValues.ContainsKey(conn.SourceToolId))
                    return calculatedValues[conn.SourceToolId];
                return double.NaN;
            }

            return input == InputType.A ? tool.CustomValueA : tool.CustomValueB;
        }

        public double Calculate(MathOperation op, double a, double b)
        {
            switch (op)
            {
                case MathOperation.Addition: return a + b;
                case MathOperation.Subtraction: return a - b;
                case MathOperation.Multiplication: return a * b;
                case MathOperation.Division: return b != 0 ? a / b : 0;
                default: return 0;
            }
        }

        public double ProcessSpecialTool(MathTool tool, double input)
        {
            switch (tool.Type)
            {
                case ToolType.Amplifier:
                    return input * tool.Gain;
                case ToolType.Channel:
                    double noise = (_random.NextDouble() - 0.5) * 0.05;
                    return input * (1.0 / (1 + tool.Distance / 1000)) + noise;
                case ToolType.Object:
                    if (!tool.LastResult.HasValue) return input;
                    return tool.LastResult.Value + (input - tool.LastResult.Value) * 0.1;
                case ToolType.Antenna:
                    return input * tool.EffectiveArea;
                case ToolType.ADC:
                    string[] parts = tool.TableData.Split(';');
                    if (parts.Length > 0 && double.TryParse(parts[0], out double result))
                    {
                        int index = (int)(Math.Abs(input) % parts.Length);
                        if (double.TryParse(parts[index], out double tableValue))
                            return tableValue;
                    }
                    return input;
                default:
                    return input;
            }
        }

        public Dictionary<Guid, double> CalculateAll(List<MathTool> tools, List<Connection> connections)
        {
            UpdateGenerators(tools);
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
                    double result = Calculate(tool.Operation, a, b);
                    results[tool.Id] = result;
                    tool.LastResult = result;
                    foreach (var conn in connections.Where(c => c.SourceToolId == tool.Id))
                        conn.CurrentValue = result;
                    changed = true;
                }

                var specialTools = tools.Where(t => t.Type == ToolType.Amplifier || t.Type == ToolType.Channel ||
                                                     t.Type == ToolType.Object || t.Type == ToolType.Antenna || t.Type == ToolType.ADC);
                foreach (var tool in specialTools)
                {
                    if (results.ContainsKey(tool.Id)) continue;
                    double input = GetInputValue(tool, InputType.A, tools, connections, results);
                    if (double.IsNaN(input)) continue;
                    double result = ProcessSpecialTool(tool, input);
                    results[tool.Id] = result;
                    tool.LastResult = result;
                    foreach (var conn in connections.Where(c => c.SourceToolId == tool.Id))
                        conn.CurrentValue = result;
                    changed = true;
                }
            } while (changed);

            return results;
        }

        public double GetSourceValue(Guid id, List<MathTool> tools, Dictionary<Guid, double> values)
        {
            var source = tools.FirstOrDefault(t => t.Id == id);
            if (source != null && source.Type == ToolType.Generator && source.LastResult.HasValue)
                return source.LastResult.Value;
            return values.ContainsKey(id) ? values[id] : 0;
        }
    }
}
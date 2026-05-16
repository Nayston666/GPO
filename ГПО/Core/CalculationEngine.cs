using MathApp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MathApp.Core
{
    public class CalculationEngine
    {
        private double _time = 0;
        private double _currentTime = 0;
        private Random _random = new Random();

        public void UpdateGenerators(List<MathTool> tools)
        {
            _time += 0.05;
            foreach (var gen in tools.Where(t => t.Type == ToolType.Generator))
            {
                double radians = (_time * gen.Frequency * 2 * Math.PI) + (gen.Phase * Math.PI / 180.0);
                gen.LastResult = gen.Amplitude * Math.Sin(radians);
            }
        }

        public double GetInputValue(MathTool tool, InputType input,
                                    List<MathTool> tools, List<Connection> connections,
                                    Dictionary<Guid, double> calculatedValues)
        {
            var conn = connections.FirstOrDefault(c => c.TargetToolId == tool.Id && c.TargetInput == input);
            if (conn != null)
            {
                var source = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                if (source != null)
                {
                    // Сначала проверяем результат подсистемы
                    if (source.Type == ToolType.SubSystem && source.LastResult.HasValue)
                        return source.LastResult.Value;

                    // Проверяем выходные порты подсистемы
                    if (source.Type == ToolType.SubSystem && source.OutputPortResults.ContainsKey(conn.SourcePortIndex))
                        return source.OutputPortResults[conn.SourcePortIndex];

                    // Генератор
                    if (source.Type == ToolType.Generator && source.LastResult.HasValue)
                        return source.LastResult.Value;

                    // Уже вычисленные значения
                    if (calculatedValues.ContainsKey(conn.SourceToolId))
                        return calculatedValues[conn.SourceToolId];
                }
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
                            return tool.FileData[tool.CurrentFileIndex++];
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
            if (x >= sorted.Last().X) return sorted.Last().Y;
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

        public double ProcessSpecialTool(MathTool tool, double input)
        {
            switch (tool.Type)
            {
                case ToolType.Amplifier:
                    return input * tool.Gain;
                case ToolType.Antenna:
                    return input * tool.EffectiveArea * 10;
                case ToolType.Channel:
                    double noise = (_random.NextDouble() - 0.5) * 0.05;
                    return input * Math.Exp(-tool.Attenuation * tool.Distance / 1000.0) + noise;
                case ToolType.Object:
                    if (!tool.LastResult.HasValue) return input;
                    return tool.LastResult.Value + (input - tool.LastResult.Value) * 0.1 / tool.TimeConstant;
                case ToolType.ADC:
                    int levels = (int)Math.Pow(2, tool.BitResolution);
                    double quantized = Math.Round(input / tool.ReferenceVoltage * (levels - 1)) / (levels - 1) * tool.ReferenceVoltage;
                    return quantized;
                default:
                    return input;
            }
        }

        public Dictionary<Guid, double> CalculateAll(List<MathTool> tools, List<Connection> connections)
        {
            UpdateGenerators(tools);
            var results = new Dictionary<Guid, double>();
            bool changed;
            int maxIterations = 100; // Защита от бесконечного цикла
            int iteration = 0;

            do
            {
                changed = false;
                iteration++;

                // Математические операции
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

                // Специальные блоки
                var specials = tools.Where(t => t.Type == ToolType.Amplifier || t.Type == ToolType.Antenna ||
                                                t.Type == ToolType.Channel || t.Type == ToolType.Object || t.Type == ToolType.ADC);
                foreach (var tool in specials)
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

                // Подсистемы
                foreach (var sub in tools.Where(t => t.Type == ToolType.SubSystem))
                {
                    if (results.ContainsKey(sub.Id)) continue;

                    // Собираем все входные значения для подсистемы
                    var inputValues = new Dictionary<int, double>();
                    bool allInputsReady = true;

                    for (int portIdx = 0; portIdx < (sub.SubSystemData?.InputPorts?.Count ?? 0); portIdx++)
                    {
                        var conn = connections.FirstOrDefault(c => c.TargetToolId == sub.Id && c.TargetPortIndex == portIdx);
                        if (conn != null)
                        {
                            // Проверяем, готов ли источник
                            if (results.ContainsKey(conn.SourceToolId))
                            {
                                inputValues[portIdx] = results[conn.SourceToolId];
                            }
                            else
                            {
                                // Проверяем генератор
                                var sourceTool = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                                if (sourceTool != null && sourceTool.Type == ToolType.Generator && sourceTool.LastResult.HasValue)
                                {
                                    inputValues[portIdx] = sourceTool.LastResult.Value;
                                }
                                else
                                {
                                    allInputsReady = false;
                                }
                            }
                        }
                        else
                        {
                            inputValues[portIdx] = 0; // Нет соединения - значение по умолчанию
                        }
                    }

                    if (allInputsReady || sub.SubSystemData?.InputPorts?.Count == 0)
                    {
                        double result = CalculateSubSystemInternal(sub, inputValues);
                        results[sub.Id] = result;
                        sub.LastResult = result;

                        // Обновляем значения на выходных соединениях
                        foreach (var conn in connections.Where(c => c.SourceToolId == sub.Id))
                        {
                            if (sub.OutputPortResults.ContainsKey(conn.SourcePortIndex))
                                conn.CurrentValue = sub.OutputPortResults[conn.SourcePortIndex];
                            else
                                conn.CurrentValue = result;
                        }
                        changed = true;
                    }
                }

                if (iteration > maxIterations) break;

            } while (changed);

            return results;
        }

        private double CalculateSubSystemInternal(MathTool subSystem, Dictionary<int, double> inputValues)
        {
            if (subSystem.SubSystemData == null) return 0;

            var internalTools = subSystem.SubSystemData.InternalTools;
            var internalConns = subSystem.SubSystemData.InternalConnections;
            var inputPorts = subSystem.SubSystemData.InputPorts;
            var outputPorts = subSystem.SubSystemData.OutputPorts;

            // Создаем словарь значений для портов
            var portValues = new Dictionary<Guid, double>();
            for (int i = 0; i < (inputPorts?.Count ?? 0); i++)
            {
                if (inputPorts[i] != null && inputValues.ContainsKey(i))
                    portValues[inputPorts[i].Id] = inputValues[i];
            }

            if (internalTools.Count == 0)
            {
                subSystem.OutputPortResults.Clear();
                for (int i = 0; i < (outputPorts?.Count ?? 0); i++)
                {
                    subSystem.OutputPortResults[i] = inputValues.ContainsKey(i) ? inputValues[i] : 0;
                }
                subSystem.LastResult = subSystem.OutputPortResults.Count > 0 ? subSystem.OutputPortResults[0] : 0;
                return subSystem.LastResult.Value;
            }

            // Обновляем генераторы внутри подсистемы
            foreach (var gen in internalTools.Where(t => t.Type == ToolType.Generator))
            {
                double radians = (_time * gen.Frequency * 2 * Math.PI) + (gen.Phase * Math.PI / 180.0);
                gen.LastResult = gen.Amplitude * Math.Sin(radians);
            }

            var internalResults = new Dictionary<Guid, double>();
            bool changed;
            int maxIterations = 100;
            int iteration = 0;

            do
            {
                changed = false;
                iteration++;

                // Операции внутри подсистемы
                foreach (var tool in internalTools.Where(t => t.Type == ToolType.Operation).OrderBy(t => t.Position.X))
                {
                    if (internalResults.ContainsKey(tool.Id)) continue;

                    double a = GetInternalInputValue(tool, InputType.A, internalTools, internalConns, internalResults, portValues);
                    double b = GetInternalInputValue(tool, InputType.B, internalTools, internalConns, internalResults, portValues);

                    if (double.IsNaN(a) || double.IsNaN(b)) continue;

                    double res = Calculate(tool.Operation, a, b, tool);
                    internalResults[tool.Id] = res;
                    tool.LastResult = res;
                    changed = true;
                }

                // Специальные блоки внутри подсистемы
                var specials = internalTools.Where(t => t.Type == ToolType.Amplifier || t.Type == ToolType.Antenna ||
                                                         t.Type == ToolType.Channel || t.Type == ToolType.Object || t.Type == ToolType.ADC);
                foreach (var tool in specials)
                {
                    if (internalResults.ContainsKey(tool.Id)) continue;

                    double input = GetInternalInputValue(tool, InputType.A, internalTools, internalConns, internalResults, portValues);
                    if (double.IsNaN(input)) continue;

                    double res = ProcessSpecialTool(tool, input);
                    internalResults[tool.Id] = res;
                    tool.LastResult = res;
                    changed = true;
                }

                if (iteration > maxIterations) break;

            } while (changed);

            // Формируем выходные значения подсистемы
            subSystem.OutputPortResults.Clear();
            for (int portIdx = 0; portIdx < (outputPorts?.Count ?? 0); portIdx++)
            {
                var outPort = outputPorts[portIdx];
                double val = 0;

                // Ищем соединение, которое ведет к этому выходному порту
                var conn = internalConns.FirstOrDefault(c => c.TargetToolId == outPort.Id);
                if (conn != null)
                {
                    if (internalResults.ContainsKey(conn.SourceToolId))
                        val = internalResults[conn.SourceToolId];
                    else if (portValues.ContainsKey(conn.SourceToolId))
                        val = portValues[conn.SourceToolId];
                }
                else if (portValues.ContainsKey(outPort.Id))
                {
                    val = portValues[outPort.Id];
                }

                subSystem.OutputPortResults[portIdx] = val;
            }

            subSystem.LastResult = subSystem.OutputPortResults.Count > 0 ? subSystem.OutputPortResults[0] :
                                   (internalResults.Count > 0 ? internalResults.Values.Last() : 0);

            return subSystem.LastResult.Value;
        }

        private double GetInternalInputValue(MathTool tool, InputType input, List<MathTool> internalTools,
                                             List<Connection> internalConns, Dictionary<Guid, double> internalResults,
                                             Dictionary<Guid, double> portValues)
        {
            var conn = internalConns.FirstOrDefault(c => c.TargetToolId == tool.Id && c.TargetInput == input);
            if (conn != null)
            {
                // Сначала проверяем порты подсистемы
                if (portValues.ContainsKey(conn.SourceToolId))
                    return portValues[conn.SourceToolId];

                var source = internalTools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                if (source != null)
                {
                    if (source.Type == ToolType.Generator && source.LastResult.HasValue)
                        return source.LastResult.Value;

                    if (source.Type == ToolType.SubSystem && source.LastResult.HasValue)
                        return source.LastResult.Value;
                }

                if (internalResults.ContainsKey(conn.SourceToolId))
                    return internalResults[conn.SourceToolId];

                return double.NaN;
            }
            return input == InputType.A ? tool.CustomValueA : tool.CustomValueB;
        }

        public double GetSourceValue(Guid id, List<MathTool> tools, Dictionary<Guid, double> values)
        {
            var src = tools.FirstOrDefault(t => t.Id == id);
            if (src != null && src.Type == ToolType.Generator && src.LastResult.HasValue) return src.LastResult.Value;
            if (src != null && src.Type == ToolType.SubSystem && src.LastResult.HasValue) return src.LastResult.Value;
            return values.ContainsKey(id) ? values[id] : 0;
        }
    }
}
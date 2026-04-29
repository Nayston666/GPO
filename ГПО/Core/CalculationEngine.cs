using MathApp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MathApp.Core
{
    /// <summary>
    /// Отвечает за все математические вычисления в проекте
    /// </summary>
    public class CalculationEngine
    {
        private double _time = 0;
        private double _currentTime = 0;

        /// <summary>
        /// Обновляет значения генераторов синусоиды
        /// </summary>
        public void UpdateGenerators(List<MathTool> tools)
        {
            _time += 0.05;

            foreach (var gen in tools.Where(t => t.Type == ToolType.SineGenerator))
            {
                double radians = (_time * gen.Frequency * 2 * Math.PI) +
                                 (gen.Phase * Math.PI / 180.0);
                gen.LastResult = gen.Amplitude * Math.Sin(radians);
            }
        }

        /// <summary>
        /// Вычисляет значение для входа блока
        /// </summary>
        public double GetInputValue(MathTool tool, InputType input,
                    List<MathTool> tools, List<Connection> connections,
                    Dictionary<Guid, double> calculatedValues)
        {
            var conn = connections.FirstOrDefault(c =>
                c.TargetToolId == tool.Id && c.TargetInput == input);

            if (conn != null)
            {
                var source = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);

                System.Diagnostics.Debug.WriteLine($"[CALC] Tool={tool.Name}, Input={input}, Has connection from={source?.Name ?? "null"}");

                // Для подсистемы возвращаем значение выходного порта
                if (source != null && source.Type == ToolType.SubSystem)
                {
                    int sourcePortIndex = conn.SourcePortIndex;
                    if (source.OutputPortResults.ContainsKey(sourcePortIndex))
                    {
                        return source.OutputPortResults[sourcePortIndex];
                    }
                    if (source.LastResult.HasValue)
                    {
                        return source.LastResult.Value;
                    }
                    return double.NaN;
                }

                if (source != null && source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
                    return source.LastResult.Value;

                if (calculatedValues.ContainsKey(conn.SourceToolId))
                    return calculatedValues[conn.SourceToolId];

                return double.NaN;
            }

            // Если нет соединения, используем пользовательские значения
            double defaultValue = input == InputType.A ? tool.CustomValueA : tool.CustomValueB;
            System.Diagnostics.Debug.WriteLine($"[CALC] Tool={tool.Name}, Input={input}, NO connection, using default={defaultValue}");

            return defaultValue;
        }

        /// <summary>
        /// Выполняет математическую операцию
        /// </summary>
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

        /// <summary>
        /// Выполняет полный цикл вычислений для всех блоков
        /// </summary>
        public Dictionary<Guid, double> CalculateAll(List<MathTool> tools,
                                                      List<Connection> connections)
        {
            UpdateGenerators(tools);

            var results = new Dictionary<Guid, double>();
            bool changed;

            do
            {
                changed = false;

                // Вычисляем математические операции
                foreach (var tool in tools.Where(t => t.Type == ToolType.Operation)
                                          .OrderBy(t => t.Position.X))
                {
                    if (results.ContainsKey(tool.Id)) continue;

                    double a = GetInputValue(tool, InputType.A, tools, connections, results);
                    double b = GetInputValue(tool, InputType.B, tools, connections, results);

                    if (double.IsNaN(a) || double.IsNaN(b)) continue;

                    double result = Calculate(tool.Operation, a, b, tool);
                    results[tool.Id] = result;
                    tool.LastResult = result;

                    foreach (var conn in connections.Where(c => c.SourceToolId == tool.Id))
                    {
                        conn.CurrentValue = result;
                    }

                    changed = true;
                }

                // Вычисляем подсистемы
                foreach (var subSystem in tools.Where(t => t.Type == ToolType.SubSystem))
                {
                    if (results.ContainsKey(subSystem.Id)) continue;

                    // Проверяем, все ли входные значения готовы
                    var inputConns = connections.Where(c => c.TargetToolId == subSystem.Id).ToList();
                    bool inputsReady = true;

                    foreach (var inputConn in inputConns)
                    {
                        if (!results.ContainsKey(inputConn.SourceToolId))
                        {
                            var source = tools.FirstOrDefault(t => t.Id == inputConn.SourceToolId);
                            if (source == null || (source.Type != ToolType.SineGenerator || !source.LastResult.HasValue))
                            {
                                inputsReady = false;
                                break;
                            }
                        }
                    }

                    if (inputsReady)
                    {
                        double result = CalculateSubSystemInternal(subSystem, tools, connections, results);
                        results[subSystem.Id] = result;

                        // Обновляем CurrentValue для всех соединений, выходящих из подсистемы
                        foreach (var conn in connections.Where(c => c.SourceToolId == subSystem.Id))
                        {
                            int portIndex = conn.SourcePortIndex;
                            if (subSystem.OutputPortResults.ContainsKey(portIndex))
                            {
                                conn.CurrentValue = subSystem.OutputPortResults[portIndex];
                            }
                            else
                            {
                                conn.CurrentValue = result;
                            }
                        }

                        changed = true;
                    }
                }

            } while (changed);

            return results;
        }
                
        /// <summary>
        /// Вычисляет значение подсистемы на основе входных сигналов
        /// </summary>
        private double CalculateSubSystemInternal(MathTool subSystem,
                                          List<MathTool> allTools,
                                          List<Connection> allConnections,
                                          Dictionary<Guid, double> externalResults)
        {
            if (subSystem.SubSystemData == null) return 0;

            var internalTools = subSystem.SubSystemData.InternalTools;
            var internalConnections = subSystem.SubSystemData.InternalConnections;
            var inputPorts = subSystem.SubSystemData.InputPorts;
            var outputPorts = subSystem.SubSystemData.OutputPorts;

            // Собираем значения со всех входных портов
            var inputValues = new Dictionary<int, double>();
            for (int i = 0; i < (inputPorts?.Count ?? 0); i++)
            {
                var inputConn = allConnections.FirstOrDefault(c =>
                    c.TargetToolId == subSystem.Id && c.TargetPortIndex == i);

                if (inputConn != null)
                {
                    if (externalResults.ContainsKey(inputConn.SourceToolId))
                        inputValues[i] = externalResults[inputConn.SourceToolId];
                    else
                    {
                        var source = allTools.FirstOrDefault(t => t.Id == inputConn.SourceToolId);
                        if (source != null && source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
                            inputValues[i] = source.LastResult.Value;
                        else if (source != null && source.Type == ToolType.SubSystem && source.LastResult.HasValue)
                            inputValues[i] = source.LastResult.Value;
                        else
                            inputValues[i] = 0;
                    }
                }
                else
                {
                    inputValues[i] = 0;
                }
            }

            // Создаём карту: ID порта -> значение
            var portValues = new Dictionary<Guid, double>();
            for (int i = 0; i < (inputPorts?.Count ?? 0); i++)
            {
                var port = inputPorts[i];
                if (port != null)
                    portValues[port.Id] = inputValues.ContainsKey(i) ? inputValues[i] : 0;
            }

            // Если нет внутренних блоков - просто передаём входы на выходы
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

            var internalResults = new Dictionary<Guid, double>();
            bool changed;

            do
            {
                changed = false;

                // Вычисляем внутренние генераторы
                foreach (var gen in internalTools.Where(t => t.Type == ToolType.SineGenerator))
                {
                    if (!internalResults.ContainsKey(gen.Id))
                    {
                        double radians = (_time * gen.Frequency * 2 * Math.PI) +
                                         (gen.Phase * Math.PI / 180.0);
                        double result = gen.Amplitude * Math.Sin(radians);
                        internalResults[gen.Id] = result;
                        gen.LastResult = result;
                        changed = true;
                    }
                }

                // Вычисляем внутренние математические операции
                foreach (var tool in internalTools.Where(t => t.Type == ToolType.Operation)
                                                  .OrderBy(t => t.Position.X))
                {
                    if (internalResults.ContainsKey(tool.Id)) continue;

                    double a = GetInternalInputValue(tool, InputType.A, internalTools,
                                                      internalConnections, internalResults, portValues);
                    double b = GetInternalInputValue(tool, InputType.B, internalTools,
                                                      internalConnections, internalResults, portValues);

                    if (double.IsNaN(a) || double.IsNaN(b)) continue;

                    double result = Calculate(tool.Operation, a, b, tool);
                    internalResults[tool.Id] = result;
                    tool.LastResult = result;
                    changed = true;
                }

            } while (changed);

            // Вычисляем значения для выходных портов 
            subSystem.OutputPortResults.Clear();

            if (outputPorts != null && outputPorts.Count > 0)
            {
                for (int portIdx = 0; portIdx < outputPorts.Count; portIdx++)
                {
                    var outputPort = outputPorts[portIdx];
                    double portResult = 0;
                    bool found = false;

                    // Ищем соединение от внутреннего блока к этому выходному порту
                    var connToOutput = internalConnections.FirstOrDefault(c => c.TargetToolId == outputPort.Id);
                    if (connToOutput != null)
                    {
                        // Проверяем, есть ли результат у источника
                        if (internalResults.ContainsKey(connToOutput.SourceToolId))
                        {
                            portResult = internalResults[connToOutput.SourceToolId];
                            found = true;
                        }
                        // Проверяем, не порт ли это (прямая связь вход->выход)
                        else if (portValues.ContainsKey(connToOutput.SourceToolId))
                        {
                            portResult = portValues[connToOutput.SourceToolId];
                            found = true;
                        }
                    }

                    // Если не нашли соединение, пробуем взять значение напрямую из порта
                    if (!found && portValues.ContainsKey(outputPort.Id))
                    {
                        portResult = portValues[outputPort.Id];
                        found = true;
                    }

                    subSystem.OutputPortResults[portIdx] = portResult;
                    System.Diagnostics.Debug.WriteLine($"Подсистема [{subSystem.Name}] Выход {portIdx} = {portResult}");
                }
            }

            // Для совместимости сохраняем первый результат в LastResult
            subSystem.LastResult = subSystem.OutputPortResults.Count > 0
                ? subSystem.OutputPortResults[0]
                : (internalResults.Count > 0 ? internalResults.Values.Last() : 0);

            return subSystem.LastResult.Value;
        }

        /// <summary>
        /// Получает входное значение для внутреннего блока подсистемы
        /// </summary>
        private double GetInternalInputValue(MathTool tool, InputType input,
                                     List<MathTool> internalTools,
                                     List<Connection> internalConnections,
                                     Dictionary<Guid, double> internalResults,
                                     Dictionary<Guid, double> portValues)
        {
            var conn = internalConnections.FirstOrDefault(c =>
                c.TargetToolId == tool.Id && c.TargetInput == input);

            if (conn != null)
            {
                // Проверяем, не является ли источник портом подсистемы
                if (portValues.ContainsKey(conn.SourceToolId))
                {
                    return portValues[conn.SourceToolId];
                }

                // Если источник — внутренний генератор или уже вычисленный блок
                var source = internalTools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                if (source != null)
                {
                    if (source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
                        return source.LastResult.Value;

                    if (internalResults.ContainsKey(conn.SourceToolId))
                        return internalResults[conn.SourceToolId];
                }

                return double.NaN;
            }

            return input == InputType.A ? tool.CustomValueA : tool.CustomValueB;
        }

        /// <summary>
        /// Получает символ операции для отладки
        /// </summary>
        private string GetOperationSymbol(MathOperation op)
        {
            switch (op)
            {
                case MathOperation.Addition: return "+";
                case MathOperation.Subtraction: return "-";
                case MathOperation.Multiplication: return "×";
                case MathOperation.Division: return "÷";
                default: return "?";
            }
        }

        /// <summary>
        /// Получает значение от источника (для графиков)
        /// </summary>
        public double GetSourceValue(Guid id, List<MathTool> tools, Dictionary<Guid, double> values)
        {
            var source = tools.FirstOrDefault(t => t.Id == id);
            if (source != null && source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
            {
                return source.LastResult.Value;
            }

            if (source != null && source.Type == ToolType.SubSystem && source.LastResult.HasValue)
            {
                return source.LastResult.Value;
            }

            if (values.ContainsKey(id))
            {
                return values[id];
            }

            return 0;
        }
    }
}
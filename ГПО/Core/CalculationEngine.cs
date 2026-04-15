using System;
using System.Collections.Generic;
using System.Linq;
using MathApp.Models;

namespace MathApp.Core
{
    /// <summary>
    /// Отвечает за все математические вычисления в проекте
    /// </summary>
    public class CalculationEngine
    {
        private double _time = 0;
        private Dictionary<Guid, double> _subSystemResults = new Dictionary<Guid, double>();

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

                if (source != null)
                {
                    // Если источник - генератор
                    if (source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
                        return source.LastResult.Value;

                    // Если источник - подсистема
                    if (source.Type == ToolType.SubSystem && _subSystemResults.ContainsKey(source.Id))
                        return _subSystemResults[source.Id];

                    // Если источник уже вычислен
                    if (calculatedValues.ContainsKey(conn.SourceToolId))
                        return calculatedValues[conn.SourceToolId];
                }

                return double.NaN;
            }

            if (input == InputType.A)
                return tool.CustomValueA;
            else
                return tool.CustomValueB;
        }

        /// <summary>
        /// Выполняет математическую операцию
        /// </summary>
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

        /// <summary>
        /// Вычисляет значение подсистемы
        /// </summary>
        private double CalculateSubSystem(MathTool subSystem,
                                          List<MathTool> allTools,
                                          List<Connection> allConnections,
                                          Dictionary<Guid, double> externalInputs)
        {
            if (subSystem.SubSystemData == null) return 0;

            var internalTools = subSystem.SubSystemData.InternalTools;
            var internalConnections = subSystem.SubSystemData.InternalConnections;
            var inputPorts = subSystem.SubSystemData.InputPorts;
            var outputPorts = subSystem.SubSystemData.OutputPorts;

            if (inputPorts == null || outputPorts == null) return 0;
            if (inputPorts.Count == 0 || outputPorts.Count == 0) return 0;

            // Находим входные значения для подсистемы из внешних соединений
            var inputValues = new Dictionary<Guid, double>();

            for (int i = 0; i < inputPorts.Count; i++)
            {
                var port = inputPorts[i];
                // Ищем внешнее соединение, которое подходит к этому порту подсистемы
                var externalConn = allConnections.FirstOrDefault(c =>
                    c.TargetToolId == subSystem.Id && c.TargetInput == InputType.A);

                if (externalConn != null && externalInputs.ContainsKey(externalConn.SourceToolId))
                {
                    inputValues[port.Id] = externalInputs[externalConn.SourceToolId];
                }
                else
                {
                    inputValues[port.Id] = 0;
                }
            }

            // Создаём карту соответствия: ID порта -> значение
            var portValues = new Dictionary<Guid, double>();
            foreach (var port in inputPorts)
            {
                portValues[port.Id] = inputValues.ContainsKey(port.Id) ? inputValues[port.Id] : 0;
            }

            // Вычисляем внутренние блоки подсистемы
            var internalResults = new Dictionary<Guid, double>();
            bool changed;

            do
            {
                changed = false;

                foreach (var tool in internalTools.Where(t => t.Type == ToolType.Operation)
                                                  .OrderBy(t => t.Position.X))
                {
                    if (internalResults.ContainsKey(tool.Id)) continue;

                    // Получаем входные значения для внутреннего блока
                    double a = GetInternalInputValue(tool, InputType.A, internalTools,
                                                      internalConnections, internalResults, portValues);
                    double b = GetInternalInputValue(tool, InputType.B, internalTools,
                                                      internalConnections, internalResults, portValues);

                    if (double.IsNaN(a) || double.IsNaN(b)) continue;

                    double result = Calculate(tool.Operation, a, b);
                    internalResults[tool.Id] = result;
                    tool.LastResult = result;
                    changed = true;
                }

                // Также обрабатываем генераторы внутри подсистемы
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

            } while (changed);

            // Находим значение на выходном порту
            if (outputPorts.Count > 0)
            {
                var outputPort = outputPorts[0];
                // Ищем соединение от внутреннего блока к выходному порту
                var connToOutput = internalConnections.FirstOrDefault(c => c.TargetToolId == outputPort.Id);
                if (connToOutput != null && internalResults.ContainsKey(connToOutput.SourceToolId))
                {
                    return internalResults[connToOutput.SourceToolId];
                }
            }

            // Если не нашли, возвращаем последний результат или 0
            var lastResult = internalResults.Values.LastOrDefault();
            return lastResult;
        }

        /// <summary>
        /// Получает входное значение для внутреннего блока подсистемы
        /// </summary>
        private double GetInternalInputValue(MathTool tool, InputType input,
                                             List<MathTool> tools,
                                             List<Connection> connections,
                                             Dictionary<Guid, double> calculatedValues,
                                             Dictionary<Guid, double> portValues)
        {
            var conn = connections.FirstOrDefault(c =>
                c.TargetToolId == tool.Id && c.TargetInput == input);

            if (conn != null)
            {
                // Проверяем, не является ли источник портом подсистемы
                var sourcePort = tools.OfType<PortTool>().FirstOrDefault(p => p.Id == conn.SourceToolId);
                if (sourcePort != null && portValues.ContainsKey(sourcePort.Id))
                {
                    return portValues[sourcePort.Id];
                }

                var source = tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                if (source != null)
                {
                    if (source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
                        return source.LastResult.Value;

                    if (calculatedValues.ContainsKey(conn.SourceToolId))
                        return calculatedValues[conn.SourceToolId];
                }

                return double.NaN;
            }

            return input == InputType.A ? tool.CustomValueA : tool.CustomValueB;
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

                    double result = Calculate(tool.Operation, a, b);
                    results[tool.Id] = result;
                    tool.LastResult = result;

                    foreach (var conn in connections.Where(c => c.SourceToolId == tool.Id))
                    {
                        conn.CurrentValue = result;
                    }

                    changed = true;
                }

                // Вычисления в подсистеме
                foreach (var subSystem in tools.Where(t => t.Type == ToolType.SubSystem))
                {
                    if (results.ContainsKey(subSystem.Id)) continue;

                    // Получаем входное значение для подсистемы
                    var inputConn = connections.FirstOrDefault(c => c.TargetToolId == subSystem.Id);
                    double inputValue = 0;
                    bool hasInput = false;

                    if (inputConn != null)
                    {
                        if (results.ContainsKey(inputConn.SourceToolId))
                        {
                            inputValue = results[inputConn.SourceToolId];
                            hasInput = true;
                        }
                        else
                        {
                            var source = tools.FirstOrDefault(t => t.Id == inputConn.SourceToolId);
                            if (source != null && source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
                            {
                                inputValue = source.LastResult.Value;
                                hasInput = true;
                            }
                        }
                    }

                    // Если есть входной сигнал или подсистема имеет внутренние генераторы
                    if (hasInput || (subSystem.SubSystemData?.InternalTools.Any(t => t.Type == ToolType.SineGenerator) == true))
                    {
                        double result = CalculateSubSystemInternal(subSystem, inputValue, tools, connections, results);
                        results[subSystem.Id] = result;
                        subSystem.LastResult = result;

                        foreach (var conn in connections.Where(c => c.SourceToolId == subSystem.Id))
                        {
                            conn.CurrentValue = result;
                        }

                        changed = true;
                    }
                }
            } while (changed);

            return results;
        }

        /// <summary>
        /// Получает значение от источника (для графиков)
        /// </summary>
        public double GetSourceValue(Guid id, List<MathTool> tools, Dictionary<Guid, double> values)
        {
            var source = tools.FirstOrDefault(t => t.Id == id);
            if (source != null)
            {
                if (source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
                    return source.LastResult.Value;

                if (source.Type == ToolType.SubSystem && _subSystemResults.ContainsKey(source.Id))
                    return _subSystemResults[source.Id];
            }

            if (values.ContainsKey(id))
                return values[id];

            return 0;
        }

        /// <summary>
        /// Вычисляет значение подсистемы на основе входного сигнала
        /// </summary>
        private double CalculateSubSystemInternal(MathTool subSystem, double inputValue,
                                                  List<MathTool> allTools,
                                                  List<Connection> allConnections,
                                                  Dictionary<Guid, double> externalResults)
        {
            if (subSystem.SubSystemData == null) return inputValue;

            var internalTools = subSystem.SubSystemData.InternalTools;
            var internalConnections = subSystem.SubSystemData.InternalConnections;
            var inputPorts = subSystem.SubSystemData.InputPorts;
            var outputPorts = subSystem.SubSystemData.OutputPorts;

            // Если нет внутренних блоков, просто передаём входной сигнал на выход
            if (internalTools.Count == 0) return inputValue;

            // Находим входной порт подсистемы и связываем его со значением
            var inputPortId = inputPorts?.FirstOrDefault()?.Id ?? Guid.Empty;

            // Вычисляем внутренние блоки
            var internalResults = new Dictionary<Guid, double>();
            bool changed;

            // Добавляем значение входного порта в результаты
            if (inputPortId != Guid.Empty)
            {
                internalResults[inputPortId] = inputValue;
            }

            do
            {
                changed = false;

                // Вычисляем внутренние генераторы
                foreach (var gen in internalTools.Where(t => t.Type == ToolType.SineGenerator))
                {
                    if (!internalResults.ContainsKey(gen.Id))
                    {
                        // Используем текущее время для генератора
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
                                                      internalConnections, internalResults);
                    double b = GetInternalInputValue(tool, InputType.B, internalTools,
                                                      internalConnections, internalResults);

                    if (double.IsNaN(a) || double.IsNaN(b)) continue;

                    double result = Calculate(tool.Operation, a, b);
                    internalResults[tool.Id] = result;
                    tool.LastResult = result;
                    changed = true;
                }

            } while (changed);

            // Находим выходной порт и возвращаем его значение
            var outputPortId = outputPorts?.FirstOrDefault()?.Id ?? Guid.Empty;

            if (outputPortId != Guid.Empty && internalResults.ContainsKey(outputPortId))
            {
                return internalResults[outputPortId];
            }

            // Если есть соединение к выходному порту
            var connToOutput = internalConnections.FirstOrDefault(c =>
                outputPortId != Guid.Empty && c.TargetToolId == outputPortId);

            if (connToOutput != null && internalResults.ContainsKey(connToOutput.SourceToolId))
            {
                return internalResults[connToOutput.SourceToolId];
            }

            // Возвращаем последний вычисленный результат
            if (internalResults.Count > 0)
            {
                return internalResults.Values.Last();
            }

            return inputValue;
        }

        /// <summary>
        /// Получает входное значение для внутреннего блока подсистемы
        /// </summary>
        private double GetInternalInputValue(MathTool tool, InputType input,
                                             List<MathTool> internalTools,
                                             List<Connection> internalConnections,
                                             Dictionary<Guid, double> internalResults)
        {
            var conn = internalConnections.FirstOrDefault(c =>
                c.TargetToolId == tool.Id && c.TargetInput == input);

            if (conn != null)
            {
                // Проверяем, не является ли источник портом подсистемы
                var sourcePort = internalTools.OfType<PortTool>().FirstOrDefault(p => p.Id == conn.SourceToolId);
                if (sourcePort != null && internalResults.ContainsKey(sourcePort.Id))
                {
                    return internalResults[sourcePort.Id];
                }

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
    }
}
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

                if (source != null && source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
                    return source.LastResult.Value;

                if (calculatedValues.ContainsKey(conn.SourceToolId))
                    return calculatedValues[conn.SourceToolId];

                return double.NaN; // Сигнал, что значение еще не готово
            }

            if (input == InputType.A)
            {
                return tool.CustomValueA;
            }
            else
            {
                return tool.CustomValueB;
            }
        }

        /// <summary>
        /// Выполняет математическую операцию
        /// </summary>
        public double Calculate(MathOperation op, double a, double b)
        {
            // Заменяем switch expression на обычный switch
            switch (op)
            {
                case MathOperation.Addition:
                    return a + b;
                case MathOperation.Subtraction:
                    return a - b;
                case MathOperation.Multiplication:
                    return a * b;
                case MathOperation.Division:
                    if (b != 0)
                        return a / b;
                    else
                        return 0;
                default:
                    return 0;
            }
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

                    // Обновляем значения в соединениях
                    foreach (var conn in connections.Where(c => c.SourceToolId == tool.Id))
                    {
                        conn.CurrentValue = result;
                    }

                    changed = true;
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
            if (source != null && source.Type == ToolType.SineGenerator && source.LastResult.HasValue)
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
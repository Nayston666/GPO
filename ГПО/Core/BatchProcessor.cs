using System;
using System.Collections.Generic;
using System.Linq;
using MathApp.Models;

namespace MathApp.Core
{
    public class BatchProcessor
    {
        private CalculationEngine _engine;
        private List<MathTool> _tools;
        private List<Connection> _connections;
        private List<double> _results;

        public BatchProcessor(CalculationEngine engine, List<MathTool> tools, List<Connection> connections)
        {
            _engine = engine;
            _tools = tools;
            _connections = connections;
            _results = new List<double>();
        }

        public List<double> GetResults()
        {
            return _results;
        }

        public void ProcessBatch()
        {
            var fileSources = _tools.Where(t =>
                t.Type == ToolType.Operation && t.Operation == MathOperation.FileIO && t.IsReading && t.FileData.Count > 0)
                .ToList();

            var stepGenerator = _tools.FirstOrDefault(t => t.Type == ToolType.StepGenerator);

            if (fileSources.Count == 0 && stepGenerator == null)
                return;

            int dataCount = 0;
            double timeStep = 0.01;

            if (fileSources.Count > 0)
            {
                dataCount = fileSources.First().FileData.Count;
                foreach (var fs in fileSources)
                {
                    if (fs.FileData.Count != dataCount)
                    {
                        throw new Exception($"Файлы имеют разное количество данных!\n{fs.Name}: {fs.FileData.Count} значений, ожидалось: {dataCount}");
                    }
                }
            }
            else if (stepGenerator != null)
            {
                dataCount = stepGenerator.StepPoints;
                if (dataCount > 0)
                    timeStep = stepGenerator.StepTimeEnd / dataCount;
            }

            SaveBlockStates();
            ResetBlockStates();
            _results.Clear();

            var orderedTools = _tools.Where(t => t.Type == ToolType.Operation || t.Type == ToolType.StepGenerator)
                                      .OrderBy(t => t.Position.X)
                                      .ToList();

            for (int idx = 0; idx < dataCount; idx++)
            {
                try
                {
                    double currentTime = idx * timeStep;

                    foreach (var fs in fileSources)
                    {
                        fs.LastResult = (idx < fs.FileData.Count) ? fs.FileData[idx] : 0;
                    }

                    double currentValue = 0;

                    foreach (var tool in orderedTools)
                    {
                        if (tool.Operation == MathOperation.FileIO)
                        {
                            currentValue = tool.LastResult ?? 0;
                            continue;
                        }

                        if (tool.Type == ToolType.StepGenerator)
                        {
                            double stepValue = CalculateStepGenerator(tool, currentTime);
                            tool.LastResult = stepValue;
                            currentValue = stepValue;
                            continue;
                        }

                        double valA = GetInputValue(tool, InputType.A, currentValue);
                        double valB = GetInputValue(tool, InputType.B, currentValue);

                        double calcResult = _engine.Calculate(tool.Operation, valA, valB, tool);

                        if (double.IsInfinity(calcResult) || double.IsNaN(calcResult))
                            calcResult = 0;

                        tool.LastResult = calcResult;
                        currentValue = calcResult;

                        foreach (var conn in _connections.Where(c => c.SourceToolId == tool.Id))
                            conn.CurrentValue = calcResult;
                    }

                    var lastTool = orderedTools.LastOrDefault(t => t.Operation != MathOperation.FileIO);
                    if (lastTool != null && lastTool.LastResult.HasValue)
                    {
                        _results.Add(lastTool.LastResult.Value);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка на шаге {idx}: {ex.Message}");
                    _results.Add(0);
                }
            }

            RestoreBlockStates();
        }

        private double GetInputValue(MathTool tool, InputType input, double currentValue)
        {
            var conn = _connections.FirstOrDefault(c =>
                c.TargetToolId == tool.Id && c.TargetInput == input);

            if (conn != null)
            {
                var sourceTool = _tools.FirstOrDefault(t => t.Id == conn.SourceToolId);
                if (sourceTool != null && sourceTool.LastResult.HasValue)
                    return sourceTool.LastResult.Value;
                return currentValue;
            }

            return (input == InputType.A) ? tool.CustomValueA : tool.CustomValueB;
        }

        private double CalculateStepGenerator(MathTool tool, double time)
        {
            double offset = tool.StepOffset;
            double amplitude = tool.StepAmplitude;
            double delay = tool.StepDelay;
            double riseTime = tool.StepRiseTime;
            double duration = tool.StepDuration;

            if (time < delay)
                return offset;

            if (riseTime > 0 && time < delay + riseTime)
            {
                double ratio = (time - delay) / riseTime;
                return offset + amplitude * ratio;
            }

            if (duration <= 0 || time < delay + riseTime + duration)
                return offset + amplitude;

            return offset;
        }

        private Dictionary<Guid, object> _savedStates = new Dictionary<Guid, object>();

        private void SaveBlockStates()
        {
            _savedStates.Clear();
            foreach (var tool in _tools)
            {
                if (tool.Operation == MathOperation.Integrator)
                {
                    _savedStates[tool.Id] = new { tool.IntegralValue, tool.PreviousInput };
                }
                else if (tool.Operation == MathOperation.Differentiator)
                {
                    _savedStates[tool.Id] = new { tool.PreviousOutput };
                }
            }
        }

        private void ResetBlockStates()
        {
            foreach (var tool in _tools)
            {
                if (tool.Operation == MathOperation.Integrator)
                {
                    tool.IntegralValue = 0;
                    tool.PreviousInput = 0;
                }
                else if (tool.Operation == MathOperation.Differentiator)
                {
                    tool.PreviousOutput = 0;
                }
                else if (tool.Operation == MathOperation.FileIO && tool.IsReading)
                {
                    tool.CurrentFileIndex = 0;
                }
            }
        }

        private void RestoreBlockStates()
        {
            foreach (var tool in _tools)
            {
                if (tool.Operation == MathOperation.Integrator && _savedStates.ContainsKey(tool.Id))
                {
                    dynamic state = _savedStates[tool.Id];
                    tool.IntegralValue = state.IntegralValue;
                    tool.PreviousInput = state.PreviousInput;
                }
                else if (tool.Operation == MathOperation.Differentiator && _savedStates.ContainsKey(tool.Id))
                {
                    dynamic state = _savedStates[tool.Id];
                    tool.PreviousOutput = state.PreviousOutput;
                }
            }
        }
    }
}
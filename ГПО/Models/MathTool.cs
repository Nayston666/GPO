using System;
using System.Collections.Generic;
using System.Drawing;

namespace MathApp.Models
{
    public class MathTool
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }
        public MathOperation Operation { get; set; }
        public ToolType Type { get; set; }

        public double CustomValueA { get; set; }
        public double CustomValueB { get; set; }

        public List<double> ValueHistory { get; set; } = new List<double>();
        public int MaxHistorySize { get; set; } = 200;

        // Для генератора синусоиды
        public double Frequency { get; set; } = 1.0;
        public double Amplitude { get; set; } = 1.0;
        public int Phase { get; set; }

        // Для интегратора
        public double IntegralValue { get; set; } = 0;
        public double PreviousInput { get; set; } = 0;
        public double StepSize { get; set; } = 0.01;

        // Для дифференциатора
        public double PreviousTime { get; set; } = 0;
        public double PreviousOutput { get; set; } = 0;

        // Для интерполятора
        public List<PointF> InterpolationPoints { get; set; } = new List<PointF>();

        public double? LastResult { get; set; }

        public void AddToHistory(double value)
        {
            ValueHistory.Add(value);
            if (ValueHistory.Count > MaxHistorySize * 2)
                ValueHistory.RemoveRange(0, ValueHistory.Count - MaxHistorySize);
        }
    }
}
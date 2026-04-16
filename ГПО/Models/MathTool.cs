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
        public int MaxHistorySize { get; set; } = 5000; // Увеличено до 5000

        public double Frequency { get; set; } = 1.0;
        public double Amplitude { get; set; } = 1.0;
        public int Phase { get; set; }

        public double IntegralValue { get; set; } = 0;
        public double PreviousInput { get; set; } = 0;
        public double StepSize { get; set; } = 0.01;

        public double PreviousTime { get; set; } = 0;
        public double PreviousOutput { get; set; } = 0;

        public List<PointF> InterpolationPoints { get; set; } = new List<PointF>();

        public string InputFilePath { get; set; } = "";
        public string OutputFilePath { get; set; } = "";
        public List<double> FileData { get; set; } = new List<double>();
        public int CurrentFileIndex { get; set; } = 0;
        public bool IsReading { get; set; } = true;

        public double? LastResult { get; set; }

        public void AddToHistory(double value)
        {
            ValueHistory.Add(value);
            if (ValueHistory.Count > MaxHistorySize * 2)
                ValueHistory.RemoveRange(0, ValueHistory.Count - MaxHistorySize);
        }
    }
}
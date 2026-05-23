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
        public int MaxHistorySize { get; set; } = 5000;

        // Для генератора синусоиды
        public double Frequency { get; set; } = 1.0;
        public double Amplitude { get; set; } = 1.0;
        public int Phase { get; set; }

        public double StepAmplitude { get; set; } = 0.1;      // Амплитуда 0.1
        public double StepDelay { get; set; } = 0.0;          // Задержка 0
        public double StepRiseTime { get; set; } = 0.0;       // Время нарастания
        public double StepDuration { get; set; } = 500e-9;    // 500 наносекунд = 5e-7 секунд
        public double StepOffset { get; set; } = 0.0;         // Смещение
        public int StepPoints { get; set; } = 1000;           // Количество точек
        public double StepTimeEnd { get; set; } = 1e-6;       // 1 микросекунда = 1e-6 секунд

        // Для интегратора
        public double IntegralValue { get; set; } = 0;
        public double PreviousInput { get; set; } = 0;
        public double StepSize { get; set; } = 0.01;

        // Для дифференциатора
        public double PreviousTime { get; set; } = 0;
        public double PreviousOutput { get; set; } = 0;

        // Для интерполятора
        public List<PointF> InterpolationPoints { get; set; } = new List<PointF>();

        // Для файлового блока
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
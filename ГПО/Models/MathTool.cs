using System;
using System.Collections.Generic;
using System.Drawing;

namespace MathApp.Models
{
    /// <summary>
    /// Блок в схеме (операция, генератор, усилитель, антенна, канал, объект, АЦП, график, подсистема)
    /// </summary>
    public class MathTool
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }

        // Тип блока и операция (для математических)
        public ToolType Type { get; set; }
        public MathOperation Operation { get; set; }

        // Пользовательские значения для входов A и B (для операций)
        public double CustomValueA { get; set; }
        public double CustomValueB { get; set; }

        // История значений (для графика)
        public List<double> ValueHistory { get; set; } = new List<double>();
        public int MaxHistorySize { get; set; } = 200;

        // Общее поле результата
        public double? LastResult { get; set; }

        // Отзеркаливание блока (true – входы справа, выход слева)
        public bool Flipped { get; set; } = false;   // вместо Rotated

        // ============ ГЕНЕРАТОР ============
        public double Frequency { get; set; } = 1.0;      // Гц
        public double Amplitude { get; set; } = 1.0;      // В
        public double Phase { get; set; } = 0.0;       // Фаза

        // ============ УСИЛИТЕЛЬ ============
        public double Gain { get; set; } = 10.0;

        // ============ АНТЕННА ============
        public double EffectiveArea { get; set; } = 0.1;   // м²

        // ============ КАНАЛ ============
        public double Distance { get; set; } = 1000.0;     // м
        public double Attenuation { get; set; } = 0.01;    // дБ/м

        // ============ ОБЪЕКТ ============
        public double RadarCrossSection { get; set; } = 1.0; // м²
        public double TimeConstant { get; set; } = 0.1;      // с

        // ============ АЦП ============
        public int BitResolution { get; set; } = 12;
        public double SamplingRate { get; set; } = 10000.0;   // Гц
        public double QuantizationStep { get; set; } = 0.001; // В
        public double DynamicRange { get; set; } = 120.0;     // дБ
        public double ReferenceVoltage { get; set; } = 5.0;   // В

        // ============ ИНТЕГРАТОР / ДИФФЕРЕНЦИАТОР / ИНТЕРПОЛЯТОР / ФАЙЛ ============
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

        // ============ ПОДСИСТЕМА ============
        public SubSystemData SubSystemData { get; set; }
        public Dictionary<int, double> OutputPortResults { get; set; } = new Dictionary<int, double>();

        // ============ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ============
        public void AddToHistory(double value)
        {
            ValueHistory.Add(value);
            if (ValueHistory.Count > MaxHistorySize * 2)
                ValueHistory.RemoveRange(0, ValueHistory.Count - MaxHistorySize);
        }

        public void ClearHistory() => ValueHistory.Clear();

        public List<double> GetRecentHistory(int count)
        {
            if (ValueHistory.Count <= count) return new List<double>(ValueHistory);
            return ValueHistory.GetRange(ValueHistory.Count - count, count);
        }

        public void ResetState()
        {
            LastResult = null;
            IntegralValue = 0;
            PreviousInput = 0;
            PreviousOutput = 0;
            PreviousTime = 0;
            CurrentFileIndex = 0;
        }
    }
}
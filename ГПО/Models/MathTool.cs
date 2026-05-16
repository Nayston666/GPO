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

        // ГЕНЕРАТОР
        public double Frequency { get; set; } = 0;
        public double Amplitude { get; set; } = 0;
        public double PhaseRad { get; set; } = 0;

        // УСИЛИТЕЛЬ
        public double Gain { get; set; } = 0;

        // АНТЕННА
        public double EffectiveArea { get; set; } = 0;

        // КАНАЛ
        public double Distance { get; set; } = 0;

        // ОБЪЕКТ
        public double RadarCrossSection { get; set; } = 0;

        // АЦП
        public string TableData { get; set; } = "";

        // ОБЩИЕ
        public double? LastResult { get; set; }
        public bool Rotated { get; set; } = false;
        public double CurrentValue { get; set; } = 0;

        // Для хранения данных графика
        public List<double> Values { get; set; } = new List<double>();
    }
}
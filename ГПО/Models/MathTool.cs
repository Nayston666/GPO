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

        // Пользовательские значения для операций
        public double CustomValueA { get; set; }
        public double CustomValueB { get; set; }

        // История значений для графика
        public List<double> ValueHistory { get; set; } = new List<double>();
        public int MaxHistorySize { get; set; } = 200;

        // ============ ГЕНЕРАТОР ============
        public double Frequency { get; set; } = 1000;     // Частота (Гц)
        public double Amplitude { get; set; } = 10;       // Амплитуда (В)
        public double PhaseRad { get; set; } = 0;         // Фаза (рад)

        // ============ УСИЛИТЕЛЬ ============
        public double Gain { get; set; } = 10;            // Коэффициент усиления

        // ============ АНТЕННА ============
        public double EffectiveArea { get; set; } = 0.1;  // Эффективная площадь (м²)

        // ============ КАНАЛ ============
        public double Distance { get; set; } = 1000;      // Расстояние (м)
        public double Attenuation { get; set; } = 0.01;   // Затухание (дБ/м)

        // ============ ОБЪЕКТ ============
        public double TimeConstant { get; set; } = 1.0;   // Постоянная времени (с)
        public double RadarCrossSection { get; set; } = 1; // ЭПР (м²)

        // ============ АЦП ============
        public int BitResolution { get; set; } = 12;           // Разрядность (бит)
        public double ReferenceVoltage { get; set; } = 5.0;    // Опорное напряжение (В)
        public double SamplingRate { get; set; } = 10000;      // Частота дискретизации (Гц)
        public double QuantizationStep { get; set; } = 0.001;  // Шаг квантования (В)
        public double DynamicRange { get; set; } = 120;        // Динамический диапазон (дБ)

        // ============ ОБЩИЕ ПОЛЯ ============
        public double? LastResult { get; set; }
        public double DutyCycle { get; set; } = 50.0;
        public double PulseWidth { get; set; } = 0.5;
        public double Resistance { get; set; } = 1000.0;
        public double Capacitance { get; set; } = 1e-6;
        public double LastOutput { get; set; } = 0.0;
        public double LastInput { get; set; } = 0.0;
        public double NonlinearA { get; set; } = 0.001;
        public double NonlinearB { get; set; } = 0.0001;
        public bool Rotated { get; set; } = false;

        public void AddToHistory(double value)
        {
            ValueHistory.Add(value);
            if (ValueHistory.Count > MaxHistorySize * 2)
                ValueHistory.RemoveRange(0, ValueHistory.Count - MaxHistorySize);
        }

        public void ClearHistory()
        {
            ValueHistory.Clear();
        }

        public List<double> GetRecentHistory(int count)
        {
            if (ValueHistory.Count <= count)
                return new List<double>(ValueHistory);
            return ValueHistory.GetRange(ValueHistory.Count - count, count);
        }

        public void ResetState()
        {
            LastOutput = 0;
            LastInput = 0;
            LastResult = null;
        }

        public double NonlinearCurrent(double voltage)
        {
            return NonlinearA * voltage + NonlinearB * Math.Pow(voltage, 3);
        }

        public double VoltageFromCharge(double charge)
        {
            return charge / Capacitance;
        }

        public double NonlinearRecursiveFilter(double input, double dt)
        {
            double iR = NonlinearCurrent(LastOutput);
            double iC = input - iR;
            double qC = LastInput * Capacitance + dt * (iC + LastInput) / 2.0;
            double output = VoltageFromCharge(qC);
            LastInput = input;
            LastOutput = output;
            return output;
        }
    }
}
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

        // Параметры генератора
        public double Frequency { get; set; } = 1.0;
        public double Amplitude { get; set; } = 1.0;
        public int Phase { get; set; }

        // Параметры усилителя
        public double Gain { get; set; } = 2.0;

        // Параметры канала распространения
        public double Attenuation { get; set; } = 0.8;

        // Параметры объекта (нелинейно-инерционная модель)
        public double TimeConstant { get; set; } = 1.0;

        // Параметры АЦП
        public int BitResolution { get; set; } = 10;
        public double ReferenceVoltage { get; set; } = 5.0;

        // Последнее вычисленное значение
        public double? LastResult { get; set; }

        // Для меандра (скважность)
        public double DutyCycle { get; set; } = 50.0;
        public double PulseWidth { get; set; } = 0.5;

        // Для нелинейного фильтра (усилитель с инерционностью)
        public double Resistance { get; set; } = 1000.0;
        public double Capacitance { get; set; } = 1e-6;
        public double LastOutput { get; set; } = 0.0;
        public double LastInput { get; set; } = 0.0;
        public double NonlinearA { get; set; } = 0.001;
        public double NonlinearB { get; set; } = 0.0001;

        // Поворот блока (для двойного клика)
        public bool Rotated { get; set; } = false;

        /// <summary>
        /// Добавление значения в историю (для графика)
        /// </summary>
        public void AddToHistory(double value)
        {
            ValueHistory.Add(value);
            if (ValueHistory.Count > MaxHistorySize * 2)
                ValueHistory.RemoveRange(0, ValueHistory.Count - MaxHistorySize);
        }

        /// <summary>
        /// Очистка истории
        /// </summary>
        public void ClearHistory()
        {
            ValueHistory.Clear();
        }

        /// <summary>
        /// Получение последних N значений для графика
        /// </summary>
        public List<double> GetRecentHistory(int count)
        {
            if (ValueHistory.Count <= count)
                return new List<double>(ValueHistory);
            return ValueHistory.GetRange(ValueHistory.Count - count, count);
        }

        /// <summary>
        /// Сброс состояния (для рекурсивных моделей)
        /// </summary>
        public void ResetState()
        {
            LastOutput = 0;
            LastInput = 0;
            LastResult = null;
        }

        /// <summary>
        /// Нелинейная функция тока от напряжения (ВАХ)
        /// I = a*U + b*U^3
        /// </summary>
        public double NonlinearCurrent(double voltage)
        {
            return NonlinearA * voltage + NonlinearB * Math.Pow(voltage, 3);
        }

        /// <summary>
        /// Обратная функция: напряжение от заряда (КВХ)
        /// Для линейного конденсатора: U = q/C
        /// </summary>
        public double VoltageFromCharge(double charge)
        {
            return charge / Capacitance;
        }

        /// <summary>
        /// Дискретная модель нелинейного рекурсивного фильтра первого порядка
        /// Аналог нелинейной RC-цепи
        /// </summary>
        /// <param name="input">Входной сигнал (ток)</param>
        /// <param name="dt">Шаг дискретизации (с)</param>
        /// <returns>Выходной сигнал (напряжение)</returns>
        public double NonlinearRecursiveFilter(double input, double dt)
        {
            // Ток через нелинейный резистор (используем предыдущее выходное напряжение)
            double iR = NonlinearCurrent(LastOutput);

            // Ток через конденсатор: iC = iвх - iR
            double iC = input - iR;

            // Интегрирование для получения заряда (метод трапеций)
            double qC = LastInput * Capacitance + dt * (iC + LastInput) / 2.0;

            // Напряжение на конденсаторе (выход)
            double output = VoltageFromCharge(qC);

            // Сохраняем состояние для следующего шага
            LastInput = input;
            LastOutput = output;

            return output;
        }
    }
}
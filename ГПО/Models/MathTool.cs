using System;
using System.Collections.Generic;
using System.Drawing;

namespace MathApp.Models
{
    /// <summary>
    /// Представляет блок в схеме (операция, генератор или график)
    /// </summary>
    public class MathTool
    {
        /// <summary>Уникальный идентификатор блока</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Отображаемое имя</summary>
        public string Name { get; set; }

        /// <summary>Позиция на рабочей области</summary>
        public Point Position { get; set; }

        /// <summary>Размер блока</summary>
        public Size Size { get; set; }

        /// <summary>Тип операции (для математических блоков)</summary>
        public MathOperation Operation { get; set; }

        /// <summary>Тип блока</summary>
        public ToolType Type { get; set; }

        /// <summary>Собственное значение для входа A</summary>
        public double CustomValueA { get; set; }

        /// <summary>Собственное значение для входа B</summary>
        public double CustomValueB { get; set; }

        /// <summary>История значений (для графика)</summary>
        public List<double> ValueHistory { get; set; } = new List<double>();

        /// <summary>Максимальный размер истории</summary>
        public int MaxHistorySize { get; set; } = 200;

        /// <summary>Частота (для генератора синусоиды)</summary>
        public double Frequency { get; set; } = 1.0;

        /// <summary>Амплитуда (для генератора синусоиды)</summary>
        public double Amplitude { get; set; } = 1.0;

        /// <summary>Фаза (для генератора синусоиды)</summary>
        public int Phase { get; set; }

        /// <summary>Последний вычисленный результат</summary>
        public double? LastResult { get; set; }

        /// <summary>Результаты для каждого выходного порта подсистемы</summary>
        public Dictionary<int, double> OutputPortResults { get; set; } = new Dictionary<int, double>();

        // Свойство подсистемы
        public SubSystemData SubSystemData { get; set; }

        /// <summary>
        /// Добавляет значение в историю (для графика)
        /// </summary>
        public void AddToHistory(double value)
        {
            ValueHistory.Add(value);
            if (ValueHistory.Count > MaxHistorySize * 2)
                ValueHistory.RemoveRange(0, ValueHistory.Count - MaxHistorySize);
        }
    }
}
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
        public int MaxHistorySize { get; set; } = 5000;

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

        /// <summary>Отзеркален ли блок (входы справа, выход слева)</summary>
        public bool Flipped { get; set; } = false;

        // ========== НОВАЯ МАТЕМАТИКА ==========

        /// <summary>Текущее значение интеграла (для интегратора)</summary>
        public double IntegralValue { get; set; } = 0;

        /// <summary>Предыдущее входное значение (для интегратора)</summary>
        public double PreviousInput { get; set; } = 0;

        /// <summary>Шаг интегрирования (для интегратора)</summary>
        public double StepSize { get; set; } = 0.01;

        /// <summary>Предыдущее время (для дифференциатора)</summary>
        public double PreviousTime { get; set; } = 0;

        /// <summary>Предыдущее выходное значение (для дифференциатора)</summary>
        public double PreviousOutput { get; set; } = 0;

        /// <summary>Точки интерполяции (для интерполятора)</summary>
        public List<PointF> InterpolationPoints { get; set; } = new List<PointF>();

        /// <summary>Путь к файлу для чтения (для файлового блока)</summary>
        public string InputFilePath { get; set; } = "";

        /// <summary>Путь к файлу для записи (для файлового блока)</summary>
        public string OutputFilePath { get; set; } = "";

        /// <summary>Данные из файла или для записи в файл</summary>
        public List<double> FileData { get; set; } = new List<double>();

        /// <summary>Текущий индекс при чтении из файла</summary>
        public int CurrentFileIndex { get; set; } = 0;

        /// <summary>Режим: true = чтение, false = запись</summary>
        public bool IsReading { get; set; } = true;

        // ========== ПОДСИСТЕМА ==========

        /// <summary>Данные подсистемы (если блок является подсистемой)</summary>
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
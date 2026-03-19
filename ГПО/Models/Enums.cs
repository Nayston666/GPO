using System;

namespace MathApp.Models
{
    /// <summary>
    /// Математические операции
    /// </summary>
    public enum MathOperation
    {
        Addition,
        Subtraction,
        Multiplication,
        Division
    }

    /// <summary>
    /// Типы блоков
    /// </summary>
    public enum ToolType
    {
        Operation,      // Математическая операция
        Chart,          // График
        SineGenerator   // Генератор синусоиды
    }

    /// <summary>
    /// Типы входов блока
    /// </summary>
    public enum InputType
    {
        A,
        B
    }

    /// <summary>
    /// Типы точек соединения
    /// </summary>
    public enum ConnectionPointType
    {
        Input,
        Output
    }

    /// <summary>
    /// Структура для хранения информации о точке соединения
    /// </summary>
    public struct ConnectionPoint
    {
        public Guid ToolId { get; set; }
        public ConnectionPointType Type { get; set; }
        public InputType? InputType { get; set; }
    }
}
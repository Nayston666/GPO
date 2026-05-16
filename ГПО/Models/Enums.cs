using System;

namespace MathApp.Models
{
    public enum MathOperation
    {
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Integrator,
        Differentiator,
        Interpolator,
        FileIO
    }

    public enum ToolType
    {
        Operation,      // Математическая операция 
        Chart,          // График
        Generator,      // Генератор синусоиды 
        Amplifier,      // Усилитель
        Antenna,        // Антенна
        Channel,        // Канал связи
        Object,         // Объект (РЛС)
        ADC,            // Аналого-цифровой преобразователь
        SubSystem,      // Подсистема
        Port            // Порт 
    }

    public enum InputType
    {
        A,
        B
    }

    public enum ConnectionPointType
    {
        Input,
        Output
    }

    /// <summary>
    /// Структура для хранения информации о точке соединения (пине)
    /// </summary>
    public struct ConnectionPoint
    {
        public Guid ToolId { get; set; }
        public ConnectionPointType Type { get; set; }
        public InputType? InputType { get; set; }
        public int PortIndex { get; set; }

        // Конструктор для двух аргументов (используется в коде)
        public ConnectionPoint(Guid toolId, ConnectionPointType type)
        {
            ToolId = toolId;
            Type = type;
            InputType = null;
            PortIndex = 0;
        }

        // Конструктор для трёх аргументов
        public ConnectionPoint(Guid toolId, ConnectionPointType type, InputType inputType)
        {
            ToolId = toolId;
            Type = type;
            InputType = inputType;
            PortIndex = 0;
        }

        // Полный конструктор (со всеми полями)
        public ConnectionPoint(Guid toolId, ConnectionPointType type, InputType? inputType, int portIndex)
        {
            ToolId = toolId;
            Type = type;
            InputType = inputType;
            PortIndex = portIndex;
        }
    }
}
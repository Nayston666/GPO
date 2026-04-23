using System;

namespace MathApp.Models
{
    public enum MathOperation
    {
        Addition,
        Subtraction,
        Multiplication,
        Division
    }

    public enum ToolType
    {
        Operation,
        Chart,
        SineGenerator,
        Generator,
        Amplifier,
        Antenna,        // ← ДОЛЖЕН БЫТЬ!
        Channel,
        Object,
        ADC
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

    public struct ConnectionPoint
    {
        public Guid ToolId;
        public ConnectionPointType Type;
        public InputType? InputType;

        public ConnectionPoint(Guid toolId, ConnectionPointType type, InputType? inputType = null)
        {
            ToolId = toolId;
            Type = type;
            InputType = inputType;
        }
    }
}
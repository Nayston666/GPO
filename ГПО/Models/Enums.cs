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
        FileIO,
        StepGenerator  // Новый тип: генератор ступенчатого сигнала
    }

    public enum ToolType
    {
        Operation,
        Chart,
        SineGenerator,
        FileIO,
        StepGenerator  // Новый тип блока
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
        public Guid ToolId { get; set; }
        public ConnectionPointType Type { get; set; }
        public InputType? InputType { get; set; }
    }
}
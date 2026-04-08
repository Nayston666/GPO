using System;
using System.Drawing;

namespace MathApp.Models
{
    /// <summary>
    /// Специальный блок-порт для подсистемы (вход или выход)
    /// </summary>
    public class PortTool : MathTool
    {
        public PortType PortType { get; set; }
        public int PortIndex { get; set; }
        public string PortName { get; set; }
    }

    public enum PortType
    {
        Input,
        Output
    }
}
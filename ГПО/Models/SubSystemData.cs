using System;
using System.Collections.Generic;
using System.Drawing;

namespace MathApp.Models
{
    /// <summary>
    /// Хранилище данных подсхемы
    /// </summary>
    [Serializable]
    public class SubSystemData
    {
        public string Name { get; set; }
        public List<MathTool> InternalTools { get; set; } = new List<MathTool>();
        public List<Connection> InternalConnections { get; set; } = new List<Connection>();

        // Входные и выходные порты подсхемы
        public List<SubSystemPort> InputPorts { get; set; } = new List<SubSystemPort>();
        public List<SubSystemPort> OutputPorts { get; set; } = new List<SubSystemPort>();
    }

    public class SubSystemPort
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public int Index { get; set; }
        public Point Position { get; set; }
    }
}
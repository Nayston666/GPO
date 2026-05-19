using System;

namespace MathApp.Models
{
    public class Connection
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SourceToolId { get; set; }
        public Guid TargetToolId { get; set; }
        public InputType TargetInput { get; set; }
        public double? CurrentValue { get; set; }

        public bool IsValid() => SourceToolId != Guid.Empty && TargetToolId != Guid.Empty;
    }
}
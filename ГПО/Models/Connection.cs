using System;

namespace MathApp.Models
{
    /// <summary>
    /// Представляет соединение (провод) между блоками
    /// </summary>
    public class Connection
    {
        /// <summary>Уникальный идентификатор соединения</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>ID блока-источника (откуда идет сигнал)</summary>
        public Guid SourceToolId { get; set; }

        /// <summary>ID блока-приемника (куда идет сигнал)</summary>
        public Guid TargetToolId { get; set; }

        /// <summary>Какой вход приемника используется (A или B)</summary>
        public InputType TargetInput { get; set; }

        /// <summary>Текущее значение, передаваемое по соединению</summary>
        public double? CurrentValue { get; set; }

        /// <summary>
        /// Проверяет, является ли соединение валидным
        /// </summary>
        public bool IsValid() => SourceToolId != Guid.Empty && TargetToolId != Guid.Empty;
    }
}
using System.Collections.Generic;

namespace EFCore.Logging
{
    /// <summary>
    /// Данные об изменении сущности
    /// </summary>
    public class LogEntityData
    {
        /// <summary>
        /// Тип сущности
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Тип события
        /// </summary>
        public LogEntityEvent Event { get; set; }

        /// <summary>
        /// Ид сущности
        /// </summary>
        public object Id { get; set; }

        /// <summary>
        /// Значения до события
        /// </summary>
        public IDictionary<string, object> BeforeChanges { get; set; }

        /// <summary>
        /// Значения после события
        /// </summary>
        public IDictionary<string, object> AfterChanges { get; set; }
    }
}

using System.Collections.Generic;

namespace EFCore.Logging
{
    /// <summary>
    /// Слушатель изменений
    /// </summary>
    public interface ILogEntityChangesHandler
    {
        /// <summary>
        /// Обработчик изменений
        /// </summary>
        /// <param name="changes">Изменения</param>
        void Handle(IEnumerable<LogEntityData> changes);
    }
}

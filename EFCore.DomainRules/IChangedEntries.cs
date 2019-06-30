using System.Collections.Generic;

namespace EFCore.DomainRules
{
    /// <summary>
    /// Интерфейс для доступа к информации об измененных сущностях
    /// </summary>
    public interface IChangedEntries
    {
        /// <summary>
        /// Возвращает изменения сущностей указанного типа
        /// </summary>
        /// <typeparam name="TEntity">Тип сущности</typeparam>
        /// <returns>Коллекция сущностей</returns>
        IEnumerable<ChangedEntity<TEntity>> OfType<TEntity>() where TEntity : class;
    }
}

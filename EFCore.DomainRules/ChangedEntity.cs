namespace EFCore.DomainRules
{
    /// <summary>
    /// Измененная сущность
    /// </summary>
    /// <typeparam name="TEntity">Тип сущности</typeparam>
    public class ChangedEntity<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Сущность
        /// </summary>
        public TEntity Entity { get; set; }

        /// <summary>
        /// Событие изменения
        /// </summary>
        public ChangeEntityEvent Event { get; set; }
    }
}

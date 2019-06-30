namespace EFCore.Logging
{
    /// <summary>
    /// Событие сущности
    /// </summary>
    public enum LogEntityEvent
    {
        /// <summary>
        /// Создана
        /// </summary>
        Created = 0,

        /// <summary>
        /// Изменена
        /// </summary>
        Edited = 1,

        /// <summary>
        /// Удалена
        /// </summary>
        Deleted = 2
    }
}

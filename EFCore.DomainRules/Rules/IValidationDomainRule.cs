namespace EFCore.DomainRules.Rules
{
    /// <summary>
    /// Правило  валидации модели предметной области
    /// </summary>
    public interface IValidationDomainRule : IDomainRule
    {
        /// <summary>
        /// Валидация данных модели перед сохранением модели
        /// </summary>
        /// <param name="changedEntries">Список измененных сущностей</param>
        void Validate(IChangedEntries changedEntries);
    }
}

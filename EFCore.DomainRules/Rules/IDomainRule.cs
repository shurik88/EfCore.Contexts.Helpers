namespace EFCore.DomainRules.Rules
{
    /// <summary>
    /// Правило предметной области
    /// </summary>
    public interface IDomainRule
    {
        /// <summary>
        /// Порядок исполнения правил
        /// Приоритет
        /// </summary>
        int Order { get; }
    }
}

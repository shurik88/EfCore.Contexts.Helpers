namespace EFCore.DomainRules.Rules
{
    /// <summary>
    /// Правило пост-обработки, следующего после принятия изменения модели данных
    /// </summary>
    public interface IPostProcessingDomainRule : IDomainRule
    {
        /// <summary>
        /// Выполнить правило
        /// </summary>
        void Execute(IChangedEntries changedEntries);
    }
}

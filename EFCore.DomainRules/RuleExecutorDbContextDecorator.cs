using EFCore.DomainRules.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EFCore.DomainRules
{
    /// <summary>
    /// Декоратор контекста БД, позволяющий выполнять валидацию модели данных и последующую обработку после принятия изменений
    /// </summary>
    public abstract class RuleExecutorDbContextDecorator : DbContext
    {
        private readonly IServiceProvider _serviceProvider;

        public RuleExecutorDbContextDecorator(DbContextOptions options, IServiceProvider serviceProvider) : base(options)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc/>
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var tracker = new ChangedEntriesTracker(ChangeTracker);
            PreSaveChanges(tracker, _serviceProvider.GetServices<IValidationDomainRule>());
            var res = base.SaveChanges(acceptAllChangesOnSuccess);
            PostSaveChanges(tracker, _serviceProvider.GetServices<IPostProcessingDomainRule>());
            return res;
        }

        /// <inheritdoc/>
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tracker = new ChangedEntriesTracker(ChangeTracker);
            PreSaveChanges(tracker, _serviceProvider.GetServices<IValidationDomainRule>());
            var res = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            PostSaveChanges(tracker, _serviceProvider.GetServices<IPostProcessingDomainRule>());
            return res;
        }



        private void PostSaveChanges(ChangedEntriesTracker tracker, IEnumerable<IPostProcessingDomainRule> postProcessingRules)
        {
            foreach (var postProcessingRule in postProcessingRules.OrderBy(x => x.Order))
            {
                postProcessingRule.Execute(tracker);
            }
        }

        private void PreSaveChanges(ChangedEntriesTracker tracker, IEnumerable<IValidationDomainRule> validationRules)
        {
            foreach (var validationRule in validationRules.OrderBy(x => x.Order))
            {
                validationRule.Validate(tracker);
            }
        }

        private class ChangedEntriesTracker : IChangedEntries
        {

            private readonly IEnumerable<TempChangedEntity> _changes;
            private readonly IEnumerable<EntityState> _supportedStates = new List<EntityState> { EntityState.Added, EntityState.Deleted, EntityState.Modified };

            public ChangedEntriesTracker(ChangeTracker tracker)
            {
                _changes = tracker.Entries().ToList().Select(x => new TempChangedEntity { Entity = x.Entity, State = x.State }).ToList();
            }

            public IEnumerable<ChangedEntity<TEntity>> OfType<TEntity>() where TEntity : class
            {
                return _changes.Where(x => x.Entity.GetType() == typeof(TEntity) && _supportedStates.Contains(x.State))
                    .Select(x => new ChangedEntity<TEntity> { Entity = (TEntity)x.Entity, Event = (ChangeEntityEvent)x.State });
            }

            private class TempChangedEntity
            {
                public EntityState State { get; set; }

                public object Entity { get; set; }
            }
        }
    }
}

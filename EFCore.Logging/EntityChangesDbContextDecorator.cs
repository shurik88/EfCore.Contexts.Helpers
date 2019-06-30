using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EFCore.Logging
{
    /// <summary>
    /// Декаратор <seealso cref="DbContext"/> для выполнения логирования изменения сущности
    /// </summary>
    /// <remarks>
    /// Логируемые сущности должны иметь атрибут <seealso cref="LogEntityAttribute"/>
    /// </remarks>
    public abstract class EntityChangesDbContextDecorator : DbContext
    {
        private readonly ILogEntityChangesHandler _changesHandler;

        /// <summary>
        /// Создание экземпляра класса <see cref="EntityChangesDbContextDecorator"/>
        /// </summary>
        /// <param name="changesHandler">Обработичик изменений</param>
        public EntityChangesDbContextDecorator(ILogEntityChangesHandler changesHandler) : base()
        {
            _changesHandler = changesHandler ?? throw new ArgumentNullException(nameof(changesHandler));
        }

        /// <summary>
        /// Создание экземпляра класса <see cref="EntityChangesDbContextDecorator"/>
        /// </summary>
        /// <param name="options">Настройки</param>
        /// <param name="changesHandler">Обработичик изменений</param>
        public EntityChangesDbContextDecorator(DbContextOptions options, ILogEntityChangesHandler changesHandler) : base(options)
        {
            _changesHandler = changesHandler ?? throw new ArgumentNullException(nameof(changesHandler));
        }

        private IEnumerable<TempChangedEntity> ChangedLogEntities =>
            ChangeTracker.Entries()
            .Where(x => x.State != EntityState.Unchanged && x.State != EntityState.Detached && x.Entity.GetType().IsDefined(typeof(LogEntityAttribute), false) && x.Entity.GetType().GetProperty("Id") != null)
            .Select(x => new TempChangedEntity
            {
                State = x.State,
                Entity = x.Entity,
                BeforeObject = x.OriginalValues.ToObject(),
                Type = (x.Entity.GetType().GetCustomAttributes(typeof(LogEntityAttribute), true).FirstOrDefault() as LogEntityAttribute)?.Name
            });

        /// <inheritdoc/>
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var changedEntities = ChangedLogEntities.ToList();
            var res = base.SaveChanges(acceptAllChangesOnSuccess);
            LogChanges(changedEntities);
            return res;
        }

        /// <inheritdoc/>
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            var changedEntities = ChangedLogEntities.ToList();
            var res = base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            LogChanges(changedEntities);
            return res;
        }

        static bool IsSimple(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
              || type.IsEnum
              || type == typeof(string)
              || type == typeof(decimal);
        }

        private string[] GetKeyProperties(Type entityType) => this.Model.FindEntityType(entityType).FindPrimaryKey().Properties
                .Select(x => x.Name).ToArray();

        private object GetKeyValue(object entity)
        {
            var entityType = entity.GetType();
            var keyProperties = GetKeyProperties(entityType);
            return keyProperties.Length == 1 ? entityType.GetProperty(keyProperties[0]).GetValue(entity) : keyProperties.ToDictionary(x => x, x => entityType.GetProperty(x).GetValue(entity));
        }


        private void LogChanges(IEnumerable<TempChangedEntity> changedEntities)
        {
            var loggedChanges = new List<LogEntityData>();
            foreach (var changedEntity in changedEntities)
            {
                switch (changedEntity.State)
                {
                    case EntityState.Added:
                        var values = GetPropertyValues(changedEntity.Entity);
                        loggedChanges.Add(new LogEntityData { Event = LogEntityEvent.Created, EntityType = changedEntity.Type, AfterChanges = values, Id = GetKeyValue(changedEntity.Entity) });
                        break;
                    case EntityState.Deleted:
                        var deletedObjValues = GetPropertyValues(changedEntity.Entity);
                        loggedChanges.Add(new LogEntityData { Event = LogEntityEvent.Deleted, EntityType = changedEntity.Type, BeforeChanges = deletedObjValues, Id = GetKeyValue(changedEntity.Entity) });
                        break;
                    case EntityState.Modified:
                        //TODO: workaround https://github.com/aspnet/EntityFrameworkCore/issues/10093#issuecomment-337671369
                        var isModified = ChangeTracker.Entries().Any(x => x.Entity == changedEntity.Entity);
                        if (!isModified)
                        {
                            var deletedObjValues1 = GetPropertyValues(changedEntity.BeforeObject);
                            loggedChanges.Add(new LogEntityData { Event = LogEntityEvent.Deleted, EntityType = changedEntity.Type, BeforeChanges = deletedObjValues1, Id = GetKeyValue(changedEntity.Entity) });
                            break;
                        }
                        // use entry.OriginalValues
                        var beforeObject = changedEntity.BeforeObject;
                        var afterObject = changedEntity.Entity;
                        if (beforeObject.GetType() == afterObject.GetType())
                        {
                            var type = beforeObject.GetType();
                            var beforeValues = GetPropertyValues(beforeObject);
                            var afterValues = GetPropertyValues(afterObject);
                            loggedChanges.Add(new LogEntityData { Event = LogEntityEvent.Edited, EntityType = changedEntity.Type, BeforeChanges = beforeValues, AfterChanges = afterValues, Id = GetKeyValue(changedEntity.Entity) });
                        }
                        break;
                    case EntityState.Detached:
                    case EntityState.Unchanged:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(changedEntity.State), changedEntity.State, "unhandled state");
                }
            }
            if (loggedChanges.Any())
                _changesHandler.Handle(loggedChanges);
        }

        private IDictionary<string, object> GetPropertyValues(object obj)
        {
            Type type = obj.GetType();
            var values = new Dictionary<string, object>();
            foreach (var property in type.GetProperties().Where(x => Attribute.IsDefined(x, typeof(LogEntityPropertyAttribute))))
            {
                var attr = property.GetCustomAttributes(typeof(LogEntityPropertyAttribute), false).First() as LogEntityPropertyAttribute;
                var propertyValue = property.GetValue(obj);
                if (propertyValue != null && !IsSimple(propertyValue.GetType()))
                    propertyValue = GetKeyValue(propertyValue);
                values.Add(string.IsNullOrEmpty(attr.Name) ? property.Name : attr.Name, propertyValue);
            }
            return values;
        }

        private class TempChangedEntity
        {
            public EntityState State { get; set; }

            public object Entity { get; set; }

            public string Type { get; set; }

            public object BeforeObject { get; set; }
        }
    }
}

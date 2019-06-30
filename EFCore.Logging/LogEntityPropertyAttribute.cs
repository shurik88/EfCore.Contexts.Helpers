using System;
using System.Runtime.CompilerServices;

namespace EFCore.Logging
{
    /// <summary>
    /// Своства сущностей, которые необходимо логировать
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class LogEntityPropertyAttribute : Attribute
    {
        public LogEntityPropertyAttribute([CallerMemberName] string propertyName = null)
        {
            Name = propertyName;
        }

        /// <summary>
        /// Название свойства
        /// </summary>
        public string Name { get; }
    }
}

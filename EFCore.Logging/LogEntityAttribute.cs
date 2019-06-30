using System;

namespace EFCore.Logging
{
    /// <summary>
    /// Сущности, которые необх-одимо логировать
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class LogEntityAttribute : Attribute
    {
        public LogEntityAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Name = name;
        }

        /// <summary>
        /// Название типа сущности
        /// </summary>
        public string Name { get; }

    }
}

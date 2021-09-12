using System;
using System.Linq;
using System.Reflection;

namespace ConstructorIndex
{
    /// <summary>
    /// Extension routine to get used ctor index property.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns index of called constructor when object was created.
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="InvalidOperationException">Throws if object does not keep information about used ctor</exception>
        /// <returns>Constructor index</returns>
        public static int GetConstructorIndex(this object obj)
        {
            var type = obj.GetType();

            string indexFieldName;

            var indexFieldAttribute = type.CustomAttributes
                .FirstOrDefault(at => string.Equals("ConstructorIndexAttribute", at.AttributeType.Name));

            if (indexFieldAttribute != null)
            {
                indexFieldName = indexFieldAttribute.ConstructorArguments.FirstOrDefault()
                    .Value.ToString();

                if (!string.IsNullOrEmpty(indexFieldName))
                {
                    var indexKeepingField = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(f => f.FieldType == typeof(int))
                        .FirstOrDefault(f => string.Equals(f.Name, indexFieldName));
                    if (indexKeepingField != null)
                    {
                        return (int)indexKeepingField.GetValue(obj);
                    }
                }
            }

            throw new InvalidOperationException($"Object {obj} does not contain information about used constructor!");
        }
    }
}

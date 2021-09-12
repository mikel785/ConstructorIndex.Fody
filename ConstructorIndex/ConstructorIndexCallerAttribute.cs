using System;
using JetBrains.Annotations;

namespace ConstructorIndex
{
    /// <summary>
    /// Mark method of target class with this attribute to retrieve constructor index.
    /// <code>public int method_name()</code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [PublicAPI]
    public sealed class ConstructorIndexCallerAttribute : Attribute
    {
    }
}

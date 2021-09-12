using System;

namespace ConstructorIndex.Fody
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ConstructorIndexAttribute : Attribute
    {
        public ConstructorIndexAttribute()
        {
        }
        public ConstructorIndexAttribute(string index)
        {
            IndexField = index;
        }
        public string IndexField { get; set; }
    }
}

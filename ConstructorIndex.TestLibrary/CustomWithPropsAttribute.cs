using System;
using System.Collections.Generic;
using System.Text;

#if FOR_NUGET
namespace ConstructorIndex.TestLibrary.ForNuget
#else
namespace ConstructorIndex.TestLibrary
#endif
{
    /// <summary>
    /// Test attribute without fields
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomWithPropsAttribute : Attribute
    {
        public CustomWithPropsAttribute(string prop)
        {
            Property = prop;
        }

        // ReSharper disable once UnusedMember.Global
        public string Property { get; }
    }
}

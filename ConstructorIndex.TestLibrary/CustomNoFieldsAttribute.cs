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
    public class CustomNoFieldsAttribute : Attribute
    {
    }
}

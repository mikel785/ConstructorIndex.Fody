using System;
using System.Collections.Generic;
using System.Text;

#if FOR_NUGET
namespace ConstructorIndex.TestLibrary.ForNuget
#else
namespace ConstructorIndex.TestLibrary
#endif
{
    [CustomNoFields]
    [CustomWithProps("PROP")]
    public class ClassWithCustomAttribute : ClassWithTwoConstructors
    {
        public ClassWithCustomAttribute()
        {
        }

        public ClassWithCustomAttribute(int dummyIntArgument)
            : base(dummyIntArgument)
        {
        }
    }
}

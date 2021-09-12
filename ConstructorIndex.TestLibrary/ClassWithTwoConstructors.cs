using System;
#if FOR_NUGET
namespace ConstructorIndex.TestLibrary.ForNuget
#else
namespace ConstructorIndex.TestLibrary
#endif
{
    public class ClassWithTwoConstructors : PublicAbstractClass
    {
        public ClassWithTwoConstructors()
        : base("ctor1")
        {
        }

        public ClassWithTwoConstructors(int dummyIntArgument)
            : base("ctor2")
        {
            DummyIntArgument = dummyIntArgument;
        }

        public int DummyIntArgument { get; }
    }
}

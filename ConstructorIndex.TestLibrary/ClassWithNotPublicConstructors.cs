using System;

#if FOR_NUGET
namespace ConstructorIndex.TestLibrary.ForNuget
#else
namespace ConstructorIndex.TestLibrary
#endif
{
    public class ClassWithNotPublicConstructors : PublicAbstractClass
    {
        protected ClassWithNotPublicConstructors()
            : base("defaultCtor")
        {
        }

        public ClassWithNotPublicConstructors(int dummyIntArgument)
            : this()
        {
            DummyIntArgument = dummyIntArgument;
        }

        public ClassWithNotPublicConstructors(double dummyDoubleArgument)
            : base("ctor2")
        {
            DummyDoubleArgument = dummyDoubleArgument;
        }

        public int DummyIntArgument { get; }

        public double DummyDoubleArgument { get; }
    }
}
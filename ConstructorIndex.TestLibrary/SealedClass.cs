#if FOR_NUGET
namespace ConstructorIndex.TestLibrary.ForNuget
#else
namespace ConstructorIndex.TestLibrary
#endif
{
    public sealed class SealedClass
    {
        public SealedClass()
        {
        }

        public SealedClass(int intArg)
        {
            IntArg = intArg;
        }

        public SealedClass(double doubleArg)
        {
            DoubleArg = doubleArg;
        }

        public double DoubleArg { get; }

        public int IntArg { get; }
    }
}

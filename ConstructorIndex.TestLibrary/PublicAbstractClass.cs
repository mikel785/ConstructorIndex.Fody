#if FOR_NUGET
namespace ConstructorIndex.TestLibrary.ForNuget
#else
namespace ConstructorIndex.TestLibrary
#endif
{
    public abstract class PublicAbstractClass
    {
        public PublicAbstractClass(string description)
        {
            Description = description;
        }
        public string Description { get; }
    }
}

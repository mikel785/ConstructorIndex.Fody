using System.Text;
using System.Xml.Linq;
using ConstructorIndex.Fody;
using NUnit.Framework;

namespace ConstructorIndex.Tests.Weavers
{
    [TestFixture]
    public class ConfigTests
    {
        [Test]
        public void ConfigureMultipleClasses()
        {
            var classes = new[] {"ClassLibrary.ClassWithTwoConstructors", "ClassLibrary.SealedClass"};

            var xml = new StringBuilder();
            xml.AppendLine(@$"<ConstructorIndex {nameof(ModuleWeaver.AllowNonPublic)}=""true"">");
            foreach (var @class in classes)
            {
                xml.AppendLine($"<{@class}/>");
            }
            xml.AppendLine(@"</ConstructorIndex>");

            var xElement = XElement.Parse(xml.ToString());
            var moduleWeaver = new ModuleWeaver { Config = xElement };
            moduleWeaver.ReadConfig();

            foreach (var @class in classes)
            {
                Assert.IsTrue(moduleWeaver.ClassNames.Contains(@class));
            }

            Assert.IsTrue(moduleWeaver.AllowNonPublic);
        }
    }
}

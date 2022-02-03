using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ConstructorIndex.Fody;
using Fody;
using NUnit.Framework;

namespace ConstructorIndex.Tests.Weavers
{
    [TestFixture("ConstructorIndex.TestLibrary.PublicAbstractClass", false)]
    [TestFixture("ConstructorIndex.TestLibrary.PublicAbstractClass", true)]
    [TestFixture("ConstructorIndex.TestLibrary.SealedClass", false)]
    [TestFixture("ConstructorIndex.TestLibrary.SealedClass", true)]
    public class InjectionTests
    {
        private static Assembly processedAssembly;
        private static TestResult testResult;

        private const string TestLibraryName = "ConstructorIndex.TestLibrary.dll";

        public InjectionTests(string baseClassName, bool processNonPublicToo)
        {
            BaseClassName = baseClassName;
            ProcessNonPublicToo = processNonPublicToo;
        }

        /// <summary>
        /// Target class FULL name to process.
        /// </summary>
        private string BaseClassName { get; }

        /// <summary>
        /// Process both non public and public constructors.
        /// </summary>
        private bool ProcessNonPublicToo { get; }

        [OneTimeSetUp]
        public void Setup()
        {
            var weavingTask = new ModuleWeaver
            {
                ClassNames = new List<string> {BaseClassName},
                AllowNonPublic = ProcessNonPublicToo
            };

            var asm = Assembly.GetAssembly(typeof(InjectionTests));
            var subjectPath = Path.Combine(Path.GetDirectoryName(asm.Location), TestLibraryName);

            // run pe verify separately to prevent 'Unable to resolve token.(Error: 0x80131869)' errors with weavingTask
            var valid = PeVerifier.Verify(subjectPath,
                subjectPath,
                PEVerifyIgnoreCodes,
                out var beforeOutput,
                out var afterOutput);

            Assert.IsTrue(valid);

            testResult = weavingTask.ExecuteTestRun(subjectPath, runPeVerify: false);
            processedAssembly = testResult.Assembly;

            TestContext.Out.WriteLine($"Run tests for {BaseClassName}, {nameof(ProcessNonPublicToo)}={ProcessNonPublicToo}");
        }

        private IEnumerable<string> PEVerifyIgnoreCodes { get; } = Enumerable.Empty<string>();

        private Type FindBaseClass(Assembly assembly)
        {
            return assembly.GetTypes().FirstOrDefault(t => string.Equals(BaseClassName, t.FullName));
        }

        private IDictionary<ConstructorInfo, object[]> GenerateConstructorArgumentSet(Type type)
        {
            var result = new Dictionary<ConstructorInfo, object[]>();

            if (!type.IsAbstract)
            {

                var flags = BindingFlags.Instance | BindingFlags.Public;
                if (ProcessNonPublicToo)
                    flags |= BindingFlags.NonPublic;

                foreach (var constructor in type.GetConstructors(flags))
                {
                    result.Add(constructor, GenerateConstructorArguments(constructor).ToArray());
                }
            }

            return result;
        }

        public static IEnumerable<object> GenerateConstructorArguments(ConstructorInfo constructorInfo)
        {
            foreach (var parameter in constructorInfo.GetParameters())
            {
                if (parameter.HasDefaultValue)
                {
                    yield return parameter.DefaultValue;
                    continue;
                }

                var valueType = parameter.ParameterType;
                if (valueType.IsValueType)
                {
                    yield return Activator.CreateInstance(valueType);
                }
                else
                {
                    yield return null;
                }
            }

            yield break;
        }

        /// <summary>
        /// Just a lookup of class.
        /// </summary>
        [Test]
        public void CheckBaseClass()
        {
            var baseClass = FindBaseClass(processedAssembly);
            Assert.IsNotNull(baseClass);
            Assert.IsTrue(string.Equals(BaseClassName, baseClass.FullName));
        }

        /// <summary>
        /// Check ctor index detector injection in all available constructors.
        /// </summary>
        [Test]
        public void CheckInjection()
        {
            var baseClass = FindBaseClass(processedAssembly);

            // collect derived classes and self
            var derivedClasses = processedAssembly.GetTypes()
                .Where(t => baseClass.IsAssignableFrom(t))
                .ToList();

            foreach (var derivedClass in derivedClasses)
            {
                var argSetCollection = GenerateConstructorArgumentSet(derivedClass);
                foreach (var index in Enumerable.Range(0, argSetCollection.Count))
                {
                    var dataSet = argSetCollection.ElementAt(index);
                    var constructorFlags = BindingFlags.Instance
                                           | BindingFlags.CreateInstance
                                           | BindingFlags.Public;

                    if (ProcessNonPublicToo && !dataSet.Key.IsPublic)
                    {
                        constructorFlags &= ~BindingFlags.Public;
                        constructorFlags |= BindingFlags.NonPublic;
                    }

                    var instance = Activator.CreateInstance(derivedClass, constructorFlags, null, dataSet.Value, default);

                    var calledConstructorIndex = instance.GetConstructorIndex();
                    Assert.AreEqual(index, calledConstructorIndex);
                }
            }
        }
    }
}
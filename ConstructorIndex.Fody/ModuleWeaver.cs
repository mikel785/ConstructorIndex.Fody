using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Fody;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace ConstructorIndex.Fody
{
    public class ModuleWeaver : BaseModuleWeaver
    {
        /// <summary>
        /// Class to find derived targets.
        /// </summary>
        public ICollection<string> ClassNames { get; set; } = new HashSet<string>();

        /// <summary>
        /// Default name of injected field / property keeping ctor index.
        /// </summary>
        [PublicAPI]
        public const string DefaultPropertyName = "__constructorIndex";

        /// <summary>
        /// Process private, protected, internal constructors too.
        /// Optional parameter.
        /// False by-default.
        /// </summary>
        public bool AllowNonPublic { get; set; }

        /// <summary>
        /// <inheritdoc cref="BaseModuleWeaver.ShouldCleanReference"/>
        /// </summary>
        [PublicAPI]
        public override bool ShouldCleanReference { get; } = false;

        /// <summary>
        /// <inheritdoc cref="BaseModuleWeaver.Execute"/>
        /// </summary>
        public override void Execute()
        {
            ReadConfig();

            var queue = new Queue<TypeDefinition>(FindTypesToProcess());
            var processed = new HashSet<TypeDefinition>();

            // if we have something to process - inject special attribute into module types collection
            if (queue.Any())
            {
                var attributeType = ILGenerator.GenerateIndexKeepingAttributeClass(ModuleDefinition);
                ModuleDefinition.Types.Add(attributeType);
            }

            while (queue.Count != 0)
            {
                var type = queue.Dequeue();
                var fieldDefinition =
                    ILGenerator.MakeConstructorIndexFieldDefinition(ModuleDefinition, type, DefaultPropertyName);
                type.Fields.Add(fieldDefinition);

                // resolve field reference instead of creation it!
                var fieldRef = type.Fields
                    .FirstOrDefault(f => string.Equals(f.Name, fieldDefinition.Name));

                // mark class with attribute contains name of constructor index property
                MarkClassWithAttribute(type, fieldDefinition);

                var constructors = GetConstructorsToInject(type).ToList();
                foreach (var constructor in constructors)
                {
                    var constructorIndex = constructors.IndexOf(constructor);

                    // find parent or self constructor call and inject index saving as constant just after that
                    var initializerCall = constructor.Body
                        .Instructions
                        .FirstOrDefault(ins => IsInitializerInstruction(ins, type));
                    var initializerIndex = constructor.Body.Instructions.IndexOf(initializerCall);

                    var instructions = ILGenerator.GenerateIntFieldSetter(fieldRef, constructorIndex);

                    foreach (var instruction in instructions.Reverse())
                    {
                        constructor.Body.Instructions.Insert(initializerIndex + 1, instruction);
                    }

                    processed.Add(type);
                }
            }

            Console.WriteLine("Weaved classes:");
            processed.ToList().ForEach(t=>Console.WriteLine(t.FullName));
        }

        [PublicAPI]
        public void MarkClassWithAttribute(TypeDefinition type, FieldDefinition fieldDefinition)
        {
            //return;
            // resolve attribute type
            var myType = ModuleDefinition.Types.First(t =>
                t.Name == "ConstructorIndexAttribute" || t.Name == "ConstructorIndex");

            var attDefaultCtorRef =
                ModuleDefinition.ImportReference(myType.GetConstructors().First());

            var param = attDefaultCtorRef.Parameters.FirstOrDefault();

            var att = new CustomAttribute(attDefaultCtorRef);

            var attPropArg = new CustomAttributeArgument(ModuleDefinition.ImportReference(typeof(string)),
                fieldDefinition.Name);
            att.ConstructorArguments.Add(attPropArg);
            type.CustomAttributes.Add(att);
        }

        [PublicAPI]
        public IEnumerable<MethodDefinition> FindRetrieveIndexMethods(TypeDefinition type)
        {
            return type.Methods.Where(m => m.CustomAttributes.Any(a =>
                string.Equals(a.AttributeType.FullName, "ConstructorIndex.ConstructorIndexCallerAttribute")));
        }

        [PublicAPI]
        public IEnumerable<TypeDefinition> FindTypesToProcess()
        {
            foreach (var type in ModuleDefinition.GetTypes())
            {
                if (!type.IsClass)
                {
                    continue;
                }

                if (type.IsAbstract)
                {
                    continue;
                }

                var dependencyChain = GetParentsChain(type);
                // dot not forget to add self type if it has no derived types
                dependencyChain.Add(type);

                var baseType = dependencyChain.FirstOrDefault(t => ClassNames.Contains(t.FullName));

                if (baseType == null)
                {
                    continue;
                }

                // process instance-only constructors
                var constructors = GetConstructorsToInject(type);

                // skip class with single constructor
                if (!constructors.Any())
                {
                    continue;
                }

                yield return type;
            }
        }

        private IEnumerable<MethodDefinition> GetConstructorsToInject(TypeDefinition type)
        {
            var targetFlags = MethodAttributes.Public & ~MethodAttributes.Static;

            if (AllowNonPublic)
                targetFlags &= ~MethodAttributes.Public;

            return type.GetConstructors()
                .Where(c => c.HasBody)
                .Where(c => c.Attributes.HasFlag(targetFlags));
        }

        /// <summary>
        /// Returns list of parent types BUT NOT SELF TYPE!
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [PublicAPI]
        public static IList<TypeDefinition> GetParentsChain(TypeDefinition type)
        {
            var parentChain = new LinkedList<TypeDefinition>();
            var parent = type;
            while (parent != null)
            {
                parent = parent.BaseType?.Resolve();
                if (parent == null)
                    break;
                parentChain.AddFirst(parent);
            }

            return parentChain.ToList();
        }

        [PublicAPI]
        public static bool IsInitializerInstruction(Instruction instruction, TypeDefinition type)
        {
            return IsBaseConstructorCallInstruction(instruction, type)
                   || IsSelfConstructorCallInstruction(instruction, type);
        }

        [PublicAPI]
        public static bool IsBaseConstructorCallInstruction(Instruction instruction, TypeDefinition type)
        {
            return instruction.OpCode == OpCodes.Call
                   && instruction.Operand is MethodReference methodReference
                   && methodReference.DeclaringType.FullName == type.BaseType.FullName
                   && methodReference.Name == ".ctor";
        }

        [PublicAPI]
        public static bool IsSelfConstructorCallInstruction(Instruction instruction, TypeDefinition type)
        {
            return instruction.OpCode == OpCodes.Call
                   && instruction.Operand is MethodReference methodReference
                   && methodReference.DeclaringType.FullName == type.FullName
                   && methodReference.Name == ".ctor";
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            return Enumerable.Empty<string>();
        }

        public void ReadConfig()
        {
            foreach (var xNode in Config.Nodes().OfType<XElement>())
            {
                ClassNames.Add(xNode.Name?.LocalName);
            }

            if (!ClassNames.Any())
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("You must to specify at least one inner class name node to use injection!");
                messageBuilder.AppendLine("Example:");
                messageBuilder.AppendLine("<ConstructorIndex ...>");
                messageBuilder.AppendLine("\t<ClassFullNameA/>");
                messageBuilder.AppendLine("\t<ClassFullNameB/>");
                messageBuilder.AppendLine("\t...");
                messageBuilder.AppendLine("</ConstructorIndex>");
                throw new WeavingException(messageBuilder.ToString());
            }

            // process non public constructors parameter
            var processNonPublicAttribute = Config.Attribute(nameof(AllowNonPublic));
            if (processNonPublicAttribute != null)
            {
                if (!bool.TryParse(processNonPublicAttribute.Value, out var processNonPublic))
                {
                    var message = $"Bad attribute {nameof(AllowNonPublic)} BOOL value: {processNonPublicAttribute.Value}";
                    throw new WeavingException(message);
                }

                AllowNonPublic = processNonPublic;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace ConstructorIndex.Fody
{
    /// <summary>
    /// Helper for IL code generation.
    /// </summary>
    internal static class ILGenerator
    {
        /// <summary>
        /// Generate int field set instructions.
        /// </summary>
        /// <param name="fieldRef">Int32 field to set.</param>
        /// <param name="value">Value</param>
        /// <returns></returns>
        public static IEnumerable<Instruction> GenerateIntFieldSetter(FieldReference fieldRef, int value)
        {
            return new[]
            {
                //IL_0014: ldarg.0      // this
                Instruction.Create(OpCodes.Ldarg_0),
                //IL_0015: ldc.i4.1
                Instruction.Create(OpCodes.Ldc_I4, value),
                //IL_0016: stfld int32
                Instruction.Create(OpCodes.Stfld, fieldRef),
            };
        }

        public static IEnumerable<Instruction> GenerateBackingFieldGetter(FieldReference backingField)
        {
            return new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldfld, backingField),
                Instruction.Create(OpCodes.Ret)
            };
        }

        public static IEnumerable<Instruction> GenerateConstructor(ModuleDefinition module, FieldReference backingField)
        {
            return new[]
            {
                // call default constructor of object
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, module.ImportReference(typeof(object).GetConstructor(Type.EmptyTypes))),
     
                // do not forget instruction alignment!
                Instruction.Create(OpCodes.Nop),
                Instruction.Create(OpCodes.Nop),

                // set backing field
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Stfld, backingField),
                Instruction.Create(OpCodes.Ret),
            };
        }

        /// <summary>
        /// Generate type for ConstructorIndexAttribute which marks processed classes with constructor index
        /// field name.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public static TypeDefinition GenerateIndexKeepingAttributeClass(ModuleDefinition module)
        {
            Console.WriteLine($"Injecting index keeping attribute class");

            var attributeType = new TypeDefinition(
                "",
                "ConstructorIndexAttribute",
                TypeAttributes.Class,
                module.ImportReference(typeof(Attribute)))
            {
                IsSealed = true,
            };

            var fieldName = "IndexFieldName";

            var backingField = new FieldDefinition(
                fieldName,
                FieldAttributes.Public | FieldAttributes.InitOnly,
                module.ImportReference(typeof(string)));

            attributeType.Fields.Add(backingField);

            // generate ctor with parameter
            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            var constructor = new MethodDefinition(".ctor", methodAttributes, module.TypeSystem.Void);
            var parameter = new ParameterDefinition("indexFieldName",
                ParameterAttributes.None, 
                module.ImportReference(typeof(string)));
            
            constructor.Parameters.Add(parameter);
            parameter.Resolve();

            constructor.Body.MaxStackSize = 8;

            foreach (var instruction in GenerateConstructor(module, backingField))
            {
                var offset = constructor.Body.Instructions.Sum(inst => inst.GetSize());
                instruction.Offset = offset;
                constructor.Body.Instructions.Insert(constructor.Body.Instructions.Count, instruction);
            }

            attributeType.Methods.Add(constructor);

           return attributeType.Resolve();
        }

        public static FieldDefinition MakeConstructorIndexFieldDefinition(ModuleDefinition module, TypeDefinition targetType, string defaultFieldName)
        {
            var fieldName = defaultFieldName;
            while (targetType.Fields.FirstOrDefault(f => string.Equals(f.Name, fieldName)) != null)
            {
                fieldName = fieldName.Insert(0, "_");
            }
            // inject private field into class definition - and mark it with attribute
            var fieldAttributes = FieldAttributes.Private;
            var typeReference = module.ImportReference(typeof(int));
            var fieldDefinition = new FieldDefinition(fieldName, fieldAttributes, typeReference);

            return fieldDefinition;
        }
    }
}

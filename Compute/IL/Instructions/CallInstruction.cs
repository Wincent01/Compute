using System;
using System.Text;
using Compute.IL.Compiler;
using Compute.IL.Utility;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    [Instruction(Code.Call)]
    public class CallInstruction : InstructionBase
    {
        public override string Compile()
        {
            var reference = (MethodReference) Instruction.Operand;

            var method = reference.Resolve();

            var name = method.Name;

            var alias = false;
            
            foreach (var attribute in method.CustomAttributes)
            {
                var attributeType = attribute.AttributeType;

                if (attributeType.FullName == typeof(AliasAttribute).FullName)
                {
                    name = (string) attribute.ConstructorArguments[0].Value;

                    alias = true;
                }
            }

            if (!alias)
            {
                Source.Link(method.FindInfo());
            }
            
            var builder = new StringBuilder();

            builder.Append($"{name}(");

            foreach (var parameter in reference.Parameters)
            {
                var value = Stack.Pop();

                var type = TypeHelper.Find(parameter.ParameterType.FullName);

                builder.Append($"({type.CLString(Source)}) {value}, ");
            }

            if (reference.Parameters.Count != 0)
            {
                builder.Length -= 2;
            }

            builder.Append(")");
            
            Stack.Push(builder.ToString());

            return "";
        }
    }
}
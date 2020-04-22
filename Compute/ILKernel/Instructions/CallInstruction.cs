using System;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.ILKernel.Instructions
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
                Context.RequestHelper(IlKernelCompiler.FindInfo(method));
            }
            
            var builder = new StringBuilder();

            builder.Append($"{name}(");

            foreach (var parameter in reference.Parameters)
            {
                var value = Stack.Pop();

                var type = Type.GetType(parameter.ParameterType.FullName);

                builder.Append($"({IlKernelCompiler.GenerateType(type)}) {value}, ");
            }

            if (reference.Parameters.Count != 0)
            {
                builder.Length -= 2;
            }

            builder.Append(")");
            
            Stack.Push(builder.ToString());
            
            /*
            var returnType = Type.GetType(method.ReturnType.FullName);

            var temp = IlKernelCompiler.SetTemporaryVariable(returnType, builder.ToString(), out var variable);

            Stack.Push(variable);

            return temp;
            */

            return "";
        }
    }
}
using System.Collections.Generic;
using Compute.IL.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.Instructions
{
    public abstract class InstructionBase
    {
        public Instruction Instruction { get; set; }
        
        public MethodDefinition Definition { get; set; }
        
        public MethodBody Body { get; set; }
        
        public Stack<object> Stack { get; set; }
        
        public Dictionary<int, object> Variables { get; set; }

        public ILSource Source { get; set; }
        
        public List<string> Prefix { get; set; }
        
        public abstract string Compile();

        public string GetArgument(int index)
        {
            return Body.Method.Parameters[index].Name;
        }

        public string SetVariable(int index, object variable)
        {
            var type = TypeHelper.Find(Body.Variables[index].VariableType.FullName);

            return $"local{index} = ({type.CLString(Source)}) ({variable})";
        }

        public static string GetVariable(int index)
        {
            return $"local{index}";
        }
    }
}
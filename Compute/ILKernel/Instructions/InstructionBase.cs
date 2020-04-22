using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.ILKernel.Instructions
{
    public abstract class InstructionBase
    {
        public Instruction Instruction { get; set; }
        
        public MethodDefinition Definition { get; set; }
        
        public MethodBody Body { get; set; }
        
        public Stack<object> Stack { get; set; }
        
        public Dictionary<int, object> Variables { get; set; }

        public ILProgramContext Context { get; set; }
        
        public abstract string Compile();
    }
}
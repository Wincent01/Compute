using Mono.Cecil.Cil;

namespace Compute.ILKernel.Instructions
{
    [Instruction(Code.Clt)]
    public class CltInstruction : InstructionBase
    {
        public override string Compile()
        {
            var value1 = Stack.Pop();
            var value2 = Stack.Pop();

            var type = IlKernelCompiler.GenerateType(typeof(int));
            
            Stack.Push($"((({type}) {value1}) > (({type}) {value2}))");

            return "";
        }
    }
}
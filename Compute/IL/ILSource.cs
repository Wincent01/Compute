using System.Reflection;
using Compute.IL.Compiler;

namespace Compute.IL
{
    public class ILSource : ILCode
    {
        public MethodInfo Info { get; protected set; }

        public override string Signature => CLGenerator.GenerateSignature(Info, this);
        
        public bool IsKernel { get; private set; }

        protected override void Compile()
        {
            Source = ILCompiler.Compile(Info, this);
        }

        internal static T Compile<T>(MethodInfo info, ILProgram program) where T : ILSource, new()
        {
            var source = new T
            {
                Info = info,
                Program = program
            };
            
            source.Compile();

            var kernel = info.GetCustomAttribute<KernelAttribute>();

            if (kernel != null)
            {
                source.IsKernel = true;
            }

            return source;
        }

        public override string ToString()
        {
            return Info.ToString();
        }
    }
}
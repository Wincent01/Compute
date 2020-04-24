using System;
using System.Linq;
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

        public void Link(MethodInfo info)
        {
            if (Linked.OfType<ILSource>().Any(l => l.Info.Equals(info))) return;

            Linked.Add(Program.Compile(info));
        }

        public void LinkStruct(Type type)
        {
            if (Linked.OfType<ILStruct>().Any(l => l.Type == type)) return;
            
            Linked.Add(Program.Register(type));
        }
    }
}
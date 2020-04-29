using System;
using Compute.IL.Compiler;

namespace Compute.IL
{
    public class ILStruct : ILCode
    {
        public Type Type { get; private set; }

        public override string Signature => CLStructGenerator.GenerateStruct(Type, this);
        
        protected override void Compile()
        {
            CLStructGenerator.GenerateStruct(Type, this);
        }

        internal static T Compile<T>(Type type, ILProgram program) where T : ILStruct, new()
        {
            var source = new T
            {
                Type = type,
                Program = program
            };
            
            source.Compile();

            return source;
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}
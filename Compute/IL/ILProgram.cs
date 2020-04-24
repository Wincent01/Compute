using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Compute.IL
{
    public class ILProgram
    {
        public Assembly Assembly { get; }
        
        public List<ILCode> Code { get; }

        public IEnumerable<ILSource> Kernels => Code.OfType<ILSource>().Where(s => s.IsKernel);
        
        public ILProgram(Assembly assembly)
        {
            Assembly = assembly;
            
            Code = new List<ILCode>();
        }

        public ILSource Compile(MethodInfo info) => Compile<ILSource>(info);
        
        public T Compile<T>(MethodInfo info) where T : ILSource, new()
        {
            var source = Code.OfType<T>().FirstOrDefault(
                s => s.Info.Equals(info)
            );

            source ??= ILSource.Compile<T>(info, this);

            if (!Code.Contains(source))
            {
                Code.Add(source);
            }

            return source;
        }

        public ILStruct Register(Type type) => Register<ILStruct>(type);

        public T Register<T>(Type type) where T : ILStruct, new()
        {
            var source = Code.OfType<T>().FirstOrDefault(
                s => s.Type == type
            );

            source ??= ILStruct.Compile<T>(type, this);
            
            if (!Code.Contains(source))
            {
                Code.Add(source);
            }

            return source;
        }

        public string CompleteSource(ILSource source)
        {
            var linked = new List<ILCode>();

            source.Complete(linked);
            
            var builder = new StringBuilder();

            foreach (var signature in linked.OfType<ILStruct>().Select(l => l.Signature))
            {
                builder.AppendLine($"{signature};");
            }
            
            foreach (var signature in linked.OfType<ILSource>().Select(l => l.Signature))
            {
                builder.AppendLine($"{signature};");
            }

            foreach (var code in linked.Select(l => l.Source))
            {
                builder.AppendLine($"{code}");
            }

            return builder.ToString();
        }
    }
}
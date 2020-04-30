using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Compute.IL
{
    public class ILProgram : IDisposable
    {
        public List<ILCode> Code { get; }

        public Context Context { get; }
        
        public Dictionary<MethodInfo, KernelDelegate> Programs { get; }

        public IEnumerable<ILSource> Kernels => Code.OfType<ILSource>().Where(s => s.IsKernel);

        public ILProgram(Context context)
        {
            Programs = new Dictionary<MethodInfo, KernelDelegate>();
            
            Code = new List<ILCode>();

            Context = context;
        }

        public ILProgram(Accelerator accelerator)
        {
            Programs = new Dictionary<MethodInfo, KernelDelegate>();

            Code = new List<ILCode>();

            Context = accelerator.CreateContext();
        }

        public KernelDelegate Compile<T>(T @delegate) where T : Delegate
        {
            return CompileDelegate(@delegate.Method, out _);
        }

        public KernelDelegate Compile<T>(T @delegate, out string code) where T : Delegate
        {
            return CompileDelegate(@delegate.Method, out code);
        }
        
        public KernelDelegate CompileDelegate(MethodInfo method, out string code)
        {
            code = "";
            
            if (Programs.TryGetValue(method, out var value))
            {
                return value;
            }

            if (!method.IsStatic)
            {
                throw new InvalidOperationException("Kernels need to be static methods!");
            }

            var attribute = method.GetCustomAttribute<KernelAttribute>();

            if (attribute == null)
            {
                throw new InvalidOperationException("Kernels must be marked with the Kernel attribute!");
            }

            var source = Compile(method);

            code = CompleteSource(source);

            try
            {
                var program = DeviceProgram.FromSource(Context, code);

                program.Build();

                var kernel = program.BuildKernel(method.Name);

                value = (workers, parameters) => KernelInvoker(source, parameters, kernel, workers);

                Programs[method] = value;
            
                return value;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return null;
            }
        }

        private void KernelInvoker(ILSource source, UIntPtr[] parameters, Kernel kernel, uint workers)
        {
            var values = new List<KernelArgument>();

            var method = source.Info;

            var types = method.GetParameters();

            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                var info = types[index].ParameterType;

                if (info.IsArray)
                {
                    values.Add(new KernelArgument
                    {
                        Size = 8,
                        Value = parameter
                    });

                    continue;
                }

                values.Add(new KernelArgument
                {
                    Size = (uint) Marshal.SizeOf(info),
                    Value = parameter
                });
            }

            lock (kernel)
            {
                kernel.Invoke(workers, values.ToArray());
            }
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

            foreach (var code in linked.OfType<ILStruct>().Reverse())
            {
                var signature = code.Signature;
                
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

        public void Dispose()
        {
            Dispose(false);
        }

        public void Dispose(bool dispose)
        {
            if (dispose)
            {
                Context?.Dispose();
            }
        }
    }
}
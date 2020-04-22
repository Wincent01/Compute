using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Compute.ILKernel
{
    public class ILProgramContext
    {
        public List<MethodInfo> Helpers { get; }

        public List<MethodInfo> Compiled { get; }
        
        public List<string> Sources { get; }
        
        public ILProgramContext()
        {
            Helpers = new List<MethodInfo>();
            
            Compiled = new List<MethodInfo>();
            
            Sources = new List<string>();
        }

        public void RequestHelper(MethodInfo info)
        {
            if (Helpers.Any(c => c.Equals(info))) return;

            Helpers.Add(info);
        }

        public void CompileHelpers()
        {
            while (true)
            {
                var any = false;

                foreach (var helper in Helpers.ToArray())
                {
                    if (Compiled.Any(c => c.Equals(helper))) continue;

                    Sources.Add(IlKernelCompiler.Compile(helper, this));

                    Compiled.Add(helper);

                    any = true;
                }

                if (any)
                {
                    continue;
                }

                break;
            }
        }

        public IEnumerable<string> Signatures()
        {
            foreach (var info in Compiled)
            {
                yield return IlKernelCompiler.GenerateSignature(info);
            }
        }

        public string HelperSource()
        {
            var builder = new StringBuilder();

            foreach (var signature in Signatures())
            {
                builder.AppendLine($"{signature};\n");
            }

            foreach (var source in Sources)
            {
                builder.AppendLine($"{source}\n");
            }

            return builder.ToString();
        }
    }
}
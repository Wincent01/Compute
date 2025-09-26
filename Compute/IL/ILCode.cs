using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Compute.IL
{
    public abstract class ILCode
    {
        public ILProgram Program { get; protected set; }

        private ILCode[] Linked { get; set; } = new ILCode[0];
        
        public string Source { get; protected set; }
        
        public abstract string Signature { get; }
        
        protected abstract void Compile();
        
        public void Complete(List<ILCode> sources)
        {
            if (sources.Contains(this)) return;

            sources.Add(this);

            lock (Linked)
            {
                foreach (var source in Linked)
                {
                    source.Complete(sources);
                }
            }
        }

        public IEnumerable<ILCode> LinkedCode => Linked;

        public void Link(ILCode code)
        {
            var ilCodes = Linked;
            
            lock (Linked)
            {
                Array.Resize(ref ilCodes, ilCodes.Length + 1);

                ilCodes[^1] = code;

                Linked = ilCodes;
            }
        }

        public void Link(MethodBase info)
        {
            if (LinkedCode.OfType<ILSource>().Any(l => l.Info.Equals(info))) return;

            Link(Program.Compile(info));
        }

        public void Link(Type type)
        {
            if (LinkedCode.OfType<ILStruct>().Any(l => l.Type == type)) return;
            
            Link(Program.Register(type));
        }
    }
}
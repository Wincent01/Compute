using System.Collections.Generic;

namespace Compute.IL
{
    public abstract class ILCode
    {
        public ILProgram Program { get; protected set; }

        public List<ILCode> Linked { get; } = new List<ILCode>();
        
        public string Source { get; protected set; }
        
        public abstract string Signature { get; }
        
        protected abstract void Compile();
        
        public void Complete(List<ILCode> sources)
        {
            if (sources.Contains(this)) return;

            sources.Add(this);
            
            foreach (var source in Linked)
            {
                source.Complete(sources);
            }
        }
    }
}
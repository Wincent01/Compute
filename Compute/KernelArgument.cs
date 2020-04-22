using System;

namespace Compute
{
    public class KernelArgument
    {
        public UIntPtr Value { get; set; }
        
        public uint Size { get; set; }
    }
}
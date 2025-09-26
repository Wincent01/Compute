using System;

namespace Compute
{
    public struct KernelArgument
    {
        public UIntPtr Value { get; set; }
        
        public uint Size { get; set; }
    }
}
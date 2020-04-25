using System;

namespace Compute.IL
{
    public delegate void KernelDelegate(uint workers, params UIntPtr[] parameters);
}
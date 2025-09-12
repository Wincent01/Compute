using System;

namespace Compute.IL
{
    public delegate void KernelDelegate(WorkerDimensions workers, params UIntPtr[] parameters);
}
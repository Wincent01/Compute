using System;

namespace Compute.IL
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field)]
    public class WriteOnlyAttribute : Attribute
    {
        public WriteOnlyAttribute()
        {
        }
    }
}
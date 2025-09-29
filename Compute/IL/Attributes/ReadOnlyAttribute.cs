using System;

namespace Compute.IL
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field)]
    public class ReadOnlyAttribute : Attribute
    {
        public ReadOnlyAttribute()
        {
        }
    }
}
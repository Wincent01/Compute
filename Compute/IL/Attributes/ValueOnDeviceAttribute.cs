using System;

namespace Compute.IL
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ValueOnDeviceAttribute : Attribute
    {
        public ValueOnDeviceAttribute()
        {
        }
    }
}
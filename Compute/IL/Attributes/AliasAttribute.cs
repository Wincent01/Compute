using System;

namespace Compute.IL
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AliasAttribute : Attribute
    {
        public string Alias { get; set; }

        public AliasAttribute(string alias)
        {
            Alias = alias;
        }
    }
}
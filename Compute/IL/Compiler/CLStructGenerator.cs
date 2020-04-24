using System;
using System.Text;

namespace Compute.IL.Compiler
{
    public static class CLStructGenerator
    {
        public static string GenerateStruct(Type type, ILCode code)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"typedef struct {type.Name}");

            builder.AppendLine("\n{\n");

            builder.AppendLine(GenerateMembers(type, code));
            
            builder.AppendLine("\n} " + $" {type.Name}");

            return builder.ToString();
        }

        public static string GenerateMembers(Type type, ILCode code)
        {
            var builder = new StringBuilder();
            
            foreach (var member in type.GetFields())
            {
                builder.AppendLine($"\t{member.FieldType.CLString(code)} {member.Name};");
            }

            return builder.ToString();
        }
    }
}
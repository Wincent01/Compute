using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Compute.IL.Instructions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace Compute.IL.Compiler
{
    public class CLBodyGenerator
    {
        public static string GenerateVariables(MethodBody body, ILSource source)
        {
            var builder = new StringBuilder();

            for (var index = 0; index < body.Variables.Count; index++)
            {
                var variable = body.Variables[index];
                var type = TypeHelper.Find(variable.VariableType.FullName);

                if (type == null)
                {
                    throw new Exception($"Could not find type {variable.VariableType}!");
                }

                var init = body.InitLocals;

                if (!type.IsPrimitive)
                {
                    init = false;
                }

                builder.AppendLine($"\t{type.CLString(source)} local{index}{(init ? " = 0" : "")};");
            }

            return builder.ToString();
        }
        
        public static string GenerateBodyContent(MethodDefinition definition, ILSource source)
        {
            var body = definition.Body;

            var instructions = typeof(ILSource).Assembly.GetTypes().Where(
                i => i.BaseType == typeof(InstructionBase)
            ).ToArray();
            
            var codes = new Dictionary<Code, Type>();

            foreach (var instruction in instructions)
            {
                var attribute = instruction.GetCustomAttribute<InstructionAttribute>();

                if (attribute == null) continue;

                foreach (var code in attribute.Codes)
                {
                    codes[code] = instruction;
                }
            }

            var stack = new Stack<object>();
            
            var variables = new Dictionary<int, object>();

            var prefix = new List<string>();
            
            var builder = new StringBuilder();

            var declare = GenerateVariables(body, source);

            foreach (var instruction in body.Instructions)
            {
                builder.AppendLine($"IL{instruction.Offset}: // {instruction}");
                
                if (!codes.TryGetValue(instruction.OpCode.Code, out var type))
                {
                    throw new NotSupportedException($"The {instruction.OpCode.Code} OpCode is not supported!");
                }

                var instance = (InstructionBase) Activator.CreateInstance(type);

                instance.Instruction = instruction;
                instance.Definition = definition;
                instance.Body = body;
                instance.Stack = stack;
                instance.Variables = variables;
                instance.Source = source;
                instance.Prefix = prefix;

                try
                {
                    var cl = instance.Compile();

                    if (string.IsNullOrWhiteSpace(cl)) continue;

                    builder.AppendLine($"\t{cl};");
                }
                catch (Exception e)
                {
                    Console.WriteLine(builder.ToString());
                    Console.WriteLine(e);
                    throw;
                }
            }

            var final = new StringBuilder();

            final.AppendLine($"{declare}");

            foreach (var temp in prefix)
            {
                final.AppendLine($"\t{temp};");
            }

            final.AppendLine($"{builder}");

            return final.ToString();
        }
    }
}
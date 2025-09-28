using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compute.IL.AST.CodeGeneration;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Instructions;
using Compute.IL.AST.Statements;
using Compute.IL.Utility;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.AST
{
    /// <summary>
    /// Helper class for working with AST-based compilation
    /// </summary>
    public class AstCompiler
    {
        private readonly Dictionary<Code, Type> _astInstructions;

        public AstCompiler()
        {
            _astInstructions = LoadAstInstructions();
        }

        /// <summary>
        /// Compiles a method body to AST and then generates code
        /// </summary>
        /// <param name="definition">The method definition</param>
        /// <returns>Generated code string</returns>
        public BlockStatement CompileMethodBody(MethodDefinition definition, out HashSet<Type> typeDependencies, out HashSet<MethodDefinition> methodDependencies)
        {
            var body = definition.Body;
            var expressionStack = new Stack<IExpression>();
            var variables = new Dictionary<int, IExpression>();
            var statements = new List<IStatement>();

            var context = new AstInstructionContext
            {
                Definition = definition,
                Body = body,
                ExpressionStack = expressionStack,
                Variables = variables,
                Statements = statements,
                TypeDependencies = [],
                MethodDependencies = []
            };

            // Generate variable declarations
            for (int i = 0; i < body.Variables.Count; i++)
            {
                var variable = body.Variables[i];
                var clrType = TypeHelper.Find(variable.VariableType.FullName) ?? typeof(int);
                var astType = AstType.FromClrType(clrType);
                var name = $"local{i}";

                var declaration = new VariableDeclarationStatement(astType, name, null);
                statements.Add(declaration);

                // Also create the identifier for later reference
                variables[i] = new IdentifierExpression(name, astType);

                context.TypeDependencies.Add(clrType);
            }
            
            for (int i = 0; i < definition.Parameters.Count; i++)
            {
                var param = definition.Parameters[i];
                var clrType = TypeHelper.Find(param.ParameterType.FullName) ?? typeof(int);

                context.TypeDependencies.Add(clrType);
            }

            // Process each IL instruction
            foreach (var instruction in body.Instructions)
            {
                if (!_astInstructions.TryGetValue(instruction.OpCode.Code, out var instructionType))
                {
                    throw new NotSupportedException($"Instruction {instruction} is not supported in AST-based compilation.");
                }

                var astInstruction = Activator.CreateInstance(instructionType) as AstInstructionBase ??
                    throw new InvalidOperationException($"Failed to create instance of AST instruction for {instruction}.");

                astInstruction.Instruction = instruction;
                astInstruction.Context = context;

                try
                {
                    statements.Add(new CommentStatement($"{instruction}"));
                    statements.Add(new LabelStatement(instruction.Offset));

                    var result = astInstruction.CompileToAst();

                    if (result != null)
                    {
                        statements.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to compile instruction {instruction} using AST: {ex.Message}", ex);
                }
            }

            // Create a block statement containing all the method statements
            var block = new BlockStatement(statements);

            typeDependencies = context.TypeDependencies;
            methodDependencies = context.MethodDependencies;

            return block;
        }

        private static Dictionary<Code, Type> LoadAstInstructions()
        {
            var instructions = new Dictionary<Code, Type>();
            var assembly = typeof(AstInstructionBase).Assembly;

            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(AstInstructionBase).IsAssignableFrom(type) || type.IsAbstract)
                    continue;

                var attributes = type.GetCustomAttributes(false);

                InstructionAttribute? attribute = null;

                foreach (var attr in attributes)
                {
                    if (attr is InstructionAttribute instructionAttribute)
                    {
                        attribute = instructionAttribute;
                        break;
                    }
                }

                if (attribute == null) continue;

                foreach (var code in attribute.Codes)
                {
                    instructions[code] = type;
                }
            }

            return instructions;
        }
    }
}
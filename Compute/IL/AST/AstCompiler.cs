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
                MethodDependencies = [],
                InlineArgumentStructs = [],
                Arguments = []
            };

            // Generate variable declarations
            for (int i = 0; i < body.Variables.Count; i++)
            {
                var variable = body.Variables[i];
                var clrType = ResolveVariableType(variable.VariableType);
                var astType = AstType.FromClrType(clrType);
                var name = $"local{i}";

                var declaration = new VariableDeclarationStatement(astType, name, null);
                statements.Add(declaration);

                // Also create the identifier for later reference
                variables[i] = new IdentifierExpression(name, IdentifierType.Variable, i, astType);

                context.TypeDependencies.Add(clrType);
            }

            if (definition.HasThis)
            {
                var thisType = TypeHelper.Find(definition.DeclaringType.FullName) ?? typeof(object);
                var astType = new PointerAstType(AstType.FromClrType(thisType));

                context.Arguments[0] = new IdentifierExpression("this", IdentifierType.Parameter, 0, astType);
            }
            
            for (int i = 0; i < definition.Parameters.Count; i++)
            {
                var param = definition.Parameters[i];
                var clrType = TypeHelper.Find(param.ParameterType.FullName) ?? typeof(int);

                var index = definition.HasThis ? i + 1 : i;

                context.Arguments[index] = new IdentifierExpression(param.Name, IdentifierType.Parameter, index, AstType.FromClrType(clrType));

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

                //try
                {
                    statements.Add(new CommentStatement($"{instruction}"));
                    statements.Add(new LabelStatement(instruction.Offset));

                    var result = astInstruction.CompileToAst();

                    if (result != null)
                    {
                        statements.Add(result);
                    }
                }
                /*catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to compile instruction {instruction} using AST: {ex.Message}", ex);
                }*/
            }

            // Remove labels that are not referenced by any jumps
            var referencedLabels = new HashSet<int>();

            foreach (var stmt in statements.OfType<BranchStatement>())
            {
                referencedLabels.Add(stmt.TargetOffset);
            }

            statements = [.. statements.Where(s => s is not LabelStatement labelStmt || referencedLabels.Contains(labelStmt.Offset))];

            // Create a block statement containing all the method statements
            var block = new BlockStatement(statements);

            typeDependencies = context.TypeDependencies;
            methodDependencies = context.MethodDependencies;

            return block;
        }

        /// <summary>
        /// Resolves a Cecil TypeReference to a CLR Type, handling array types,
        /// pointer types, and generic parameters that TypeHelper.Find can't resolve directly.
        /// </summary>
        private static Type ResolveVariableType(TypeReference typeRef)
        {
            // Direct lookup first
            var direct = TypeHelper.Find(typeRef.FullName);
            if (direct != null)
                return direct;

            // Handle array types (e.g. System.Single[])
            if (typeRef is ArrayType arrayType)
            {
                var elementType = ResolveVariableType(arrayType.ElementType);
                return elementType.MakeArrayType();
            }

            // Handle pointer types
            if (typeRef is PointerType pointerType)
            {
                var elementType = ResolveVariableType(pointerType.ElementType);
                return elementType.MakePointerType();
            }

            // Handle by-reference types
            if (typeRef is ByReferenceType byRefType)
            {
                var elementType = ResolveVariableType(byRefType.ElementType);
                return elementType.MakeByRefType();
            }

            // Fallback
            return typeof(int);
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
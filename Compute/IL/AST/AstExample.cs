using System;
using Compute.IL.AST;
using Compute.IL.AST.CodeGeneration;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;

namespace Compute.IL.AST
{
    /// <summary>
    /// Example demonstrating the new AST-based code generation
    /// </summary>
    public static class AstExample
    {
        /// <summary>
        /// Creates a simple AST and generates OpenCL code from it
        /// </summary>
        /// <returns>Generated OpenCL C code</returns>
        public static string GenerateSimpleExample()
        {
            // Create a simple expression: (a + b) * c
            var a = new IdentifierExpression("a", PrimitiveAstType.Float32);
            var b = new IdentifierExpression("b", PrimitiveAstType.Float32);
            var c = new IdentifierExpression("c", PrimitiveAstType.Float32);
            
            var addExpression = new BinaryExpression(a, BinaryOperatorType.Add, b, PrimitiveAstType.Float32);
            var multiplyExpression = new BinaryExpression(addExpression, BinaryOperatorType.Multiply, c, PrimitiveAstType.Float32);
            
            // Create variable declarations
            var statements = new IStatement[]
            {
                new VariableDeclarationStatement(PrimitiveAstType.Float32, "a", LiteralExpression.Float32(1.0f)),
                new VariableDeclarationStatement(PrimitiveAstType.Float32, "b", LiteralExpression.Float32(2.0f)),
                new VariableDeclarationStatement(PrimitiveAstType.Float32, "c", LiteralExpression.Float32(3.0f)),
                new VariableDeclarationStatement(PrimitiveAstType.Float32, "result", null),
                new AssignmentStatement(
                    new IdentifierExpression("result", PrimitiveAstType.Float32),
                    multiplyExpression
                ),
                new ReturnStatement(new IdentifierExpression("result", PrimitiveAstType.Float32))
            };
            
            var block = new BlockStatement(statements);
            
            // Generate OpenCL code
            var generator = new OpenClCodeGenerator();
            return generator.GenerateBody(block);
        }
    }
}
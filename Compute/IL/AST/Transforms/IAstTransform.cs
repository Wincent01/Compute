using System;
using System.Reflection;
using Compute.IL.AST.CodeGeneration;
using Compute.IL.AST.Statements;

namespace Compute.IL.AST.Transforms
{
    /// <summary>
    /// Context passed to AST transforms, providing information about the
    /// method being compiled and its closure (if any).
    /// </summary>
    public class AstTransformContext
    {
        /// <summary>
        /// The .NET method being compiled
        /// </summary>
        public required MethodBase Method { get; init; }

        /// <summary>
        /// Closure fields, if compiling a lambda/closure. Null for static methods.
        /// </summary>
        public FieldInfo[]? ClosureFields { get; init; }

        /// <summary>
        /// The CLR type of the closure (display class), if any.
        /// </summary>
        public Type? ClosureType { get; init; }

        /// <summary>
        /// The code generator being used (for type lookups, etc.)
        /// </summary>
        public required ICodeGenerator CodeGenerator { get; init; }
    }

    /// <summary>
    /// Interface for AST-to-AST transforms that run as post-processing passes
    /// after initial compilation and before code generation.
    /// </summary>
    public interface IAstTransform
    {
        /// <summary>
        /// Transforms the AST body, returning a new (or same) block statement.
        /// </summary>
        /// <param name="body">The method body to transform</param>
        /// <param name="context">Context about the method being compiled</param>
        /// <returns>The transformed body</returns>
        BlockStatement Transform(BlockStatement body, AstTransformContext context);
    }
}

namespace Compute.IL.AST.CodeGeneration
{
    /// <summary>
    /// Interface for code generators that convert AST to target language
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// Generate code for an AST node
        /// </summary>
        /// <param name="node">The AST node to generate code for</param>
        /// <returns>Generated code string</returns>
        string GenerateBody(IAstNode node);

        /// <summary>
        /// Generate type declaration for the target language
        /// </summary>
        /// <param name="type">The AST type to generate</param>
        /// <returns>Type declaration string</returns>
        string GenerateType(AstType type);

        /// <summary>
        /// Generate function signature for the target language
        /// </summary>
        /// <param name="methodSource">The method source to generate signature for</param>
        /// <returns>Function signature string</returns>
        string GenerateFunctionSignature(AstMethodSource methodSource);

        /// <summary>
        /// Generate type definition for the target language
        /// </summary>
        /// <param name="type">The AST type to generate definition for</param>
        /// <returns>Type definition string</returns>
        string GenerateTypeDefinition(AstType type);
    }
}
namespace Compute.IL.AST
{
    /// <summary>
    /// Interface for expressions that evaluate to a value
    /// </summary>
    public interface IExpression : IAstNode
    {
        /// <summary>
        /// The type information for this expression
        /// </summary>
        AstType Type { get; }
    }
}
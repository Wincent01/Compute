namespace Compute.IL.AST
{
    /// <summary>
    /// Visitor interface for AST traversal with return value
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    public interface IAstVisitor<T>
    {
        T Visit(IAstNode node);
    }

    /// <summary>
    /// Visitor interface for AST traversal without return value
    /// </summary>
    public interface IAstVisitor
    {
        void Visit(IAstNode node);
    }
}
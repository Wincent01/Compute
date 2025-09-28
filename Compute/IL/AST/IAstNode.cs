using System.Collections.Generic;

namespace Compute.IL.AST
{
    /// <summary>
    /// Base interface for all AST nodes
    /// </summary>
    public interface IAstNode
    {
        /// <summary>
        /// Gets the children of this node
        /// </summary>
        IEnumerable<IAstNode> Children { get; }

        /// <summary>
        /// Accepts a visitor for traversal
        /// </summary>
        /// <typeparam name="T">Return type of the visitor</typeparam>
        /// <param name="visitor">The visitor to accept</param>
        /// <returns>Result from the visitor</returns>
        T Accept<T>(IAstVisitor<T> visitor);

        /// <summary>
        /// Accepts a visitor for traversal without return value
        /// </summary>
        /// <param name="visitor">The visitor to accept</param>
        void Accept(IAstVisitor visitor);
    }
}
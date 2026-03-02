using System;

namespace Compute.IL.AST;

/// <summary>
/// Enum representing variable usage types
/// </summary>
[Flags]
public enum AstVariableUsage
{
    /// <summary>
    /// Variable is not used
    /// </summary>
    Unused = 0 << 0,

    /// <summary>
    /// Variable is accessed
    /// </summary>
    Accessed = 1 << 1,

    /// <summary>
    /// Variable is read
    /// </summary>
    Read = 1 << 2,

    /// <summary>
    /// Variable is written
    /// </summary>
    Write = 1 << 3,
}
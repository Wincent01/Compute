# AST-Based Code Generation System

This document describes the new Abstract Syntax Tree (AST) based code generation system for the Compute library. This system replaces direct string manipulation with a structured approach that makes it easier to work with pointers, type safety, and provides extensibility for multiple target languages.

## Architecture Overview

The AST system is organized into several key components:

### Core Interfaces

- **`IAstNode`**: Base interface for all AST nodes
- **`IExpression`**: Interface for nodes that evaluate to a value
- **`IStatement`**: Interface for nodes that represent statements
- **`IAstVisitor<T>`**: Visitor pattern interface for AST traversal

### Type System (`AstType`)

The AST includes a type system that can represent:
- **`PrimitiveAstType`**: Basic types (int, float, etc.)
- **`PointerAstType`**: Pointer types with proper element type tracking
- **`ArrayAstType`**: Array types
- Custom struct types (extensible)

### Expression Nodes

Located in `Compute.IL.AST.Expressions`:
- **`LiteralExpression`**: Constant values
- **`IdentifierExpression`**: Variable references
- **`BinaryExpression`**: Binary operations (add, multiply, etc.)
- **`UnaryExpression`**: Unary operations (negation, dereference, etc.)
- **`FunctionCallExpression`**: Function calls
- **`CastExpression`**: Type casts
- **`ArrayAccessExpression`**: Array indexing
- **`FieldAccessExpression`**: Struct field access

### Statement Nodes

Located in `Compute.IL.AST.Statements`:
- **`VariableDeclarationStatement`**: Variable declarations
- **`AssignmentStatement`**: Assignments
- **`ReturnStatement`**: Return statements
- **`ExpressionStatement`**: Expressions used as statements
- **`BlockStatement`**: Block of statements
- **`NopStatement`**: No-operation placeholder

### Code Generators

Located in `Compute.IL.AST.CodeGeneration`:
- **`ICodeGenerator`**: Interface for target language generators
- **`OpenClCodeGenerator`**: Generates OpenCL C code from AST
- Extensible for other targets (Vulkan GLSL, HLSL, etc.)

## New Instruction System

The AST system introduces a new instruction base class:

### `AstInstructionBase`

Replaces the string-based `InstructionBase` with:
- **`ExpressionStack`**: Stack of `IExpression` objects instead of strings
- **`CompileToAst()`**: Returns `IStatement` instead of string
- **`PrefixStatements`**: List of statements to emit before the main instruction

### Example AST Instructions

Several example instructions demonstrate the new approach:
- **`AstAddInstruction`**: Binary addition
- **`AstAndInstruction`**: Bitwise AND
- **`AstLdcInstruction`**: Load constants
- **`AstLdlocInstruction`**: Load local variables
- **`AstStlocInstruction`**: Store to local variables

## Usage Example

Here's how to create and use the AST system:

```csharp
// Create expressions
var a = new IdentifierExpression("a", PrimitiveAstType.Float32);
var b = new IdentifierExpression("b", PrimitiveAstType.Float32);
var addExpr = new BinaryExpression(a, BinaryOperatorType.Add, b, PrimitiveAstType.Float32);

// Create statements
var assignment = new AssignmentStatement(
    new IdentifierExpression("result", PrimitiveAstType.Float32),
    addExpr
);

// Generate code
var generator = new OpenClCodeGenerator();
string code = generator.Generate(assignment);
// Output: "result = (a + b)"
```

## Benefits of the AST System

### 1. **Type Safety**
- Strong typing throughout the compilation process
- Better error detection and reporting
- Easier to implement type checking and promotion rules

### 2. **Pointer Handling**
- Proper representation of pointer types and operations
- Clear distinction between value and pointer access
- Support for complex pointer arithmetic

### 3. **Extensibility**
- Easy to add new target languages (Vulkan GLSL, HLSL, etc.)
- Modular code generator architecture
- Simple to add new expression and statement types

### 4. **Maintainability**
- Structured representation instead of string concatenation
- Easier debugging and testing
- Clear separation of concerns

### 5. **Optimization Opportunities**
- AST can be transformed and optimized before code generation
- Dead code elimination
- Common subexpression elimination
- Constant folding

## Migration Strategy

The AST system is designed for gradual migration:

1. **Coexistence**: Both string-based and AST-based instructions can coexist
2. **Gradual Conversion**: Instructions can be converted one by one
3. **Fallback**: Unsupported instructions fall back to string-based compilation
4. **Testing**: New instructions can be tested alongside existing ones

## Future Extensions

The AST system provides foundation for:

### Vulkan Compute Shader Support
```csharp
var vulkanGenerator = new VulkanGlslCodeGenerator();
string glslCode = vulkanGenerator.Generate(astNode);
```

### HLSL Support
```csharp
var hlslGenerator = new HlslCodeGenerator();
string hlslCode = hlslGenerator.Generate(astNode);
```

### Advanced Optimizations
- Control flow analysis
- Data flow optimization
- Memory access optimization
- Loop unrolling and vectorization

## Getting Started

To start using the AST system:

1. **For new instructions**: Inherit from `AstInstructionBase`
2. **For code generation**: Use `AstCompiler` with desired `ICodeGenerator`
3. **For custom expressions**: Implement `IExpression`
4. **For custom statements**: Implement `IStatement`

The system is designed to be intuitive and follows common compiler design patterns, making it easy for developers familiar with AST concepts to contribute and extend.
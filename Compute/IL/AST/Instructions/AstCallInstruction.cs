using System;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Compute.IL.Utility;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Compute.IL.AST.Instructions
{
    /// <summary>
    /// AST instruction for method calls
    /// </summary>
    [Instruction(Code.Call, Code.Callvirt)]
    public class AstCallInstruction : AstInstructionBase
    {
        public override IStatement CompileToAst()
        {
            var methodRef = (MethodReference)Instruction.Operand;

            var declearingType = TypeHelper.Find(methodRef.DeclaringType.FullName);
            declearingType ??= TypeHelper.Find(methodRef.DeclaringType.FullName.Replace('/', '+'));
            declearingType ??= methodRef.DeclaringType.Resolve() is { } resolvedDeclaringType
                ? TypeHelper.Find(resolvedDeclaringType)
                : null;
            if (declearingType == null)
            {
                throw new InvalidOperationException($"Unable to resolve type for method {methodRef.FullName}");
            }

            // Collect arguments from stack
            var argumentCount = methodRef.Parameters.Count;
            var arguments = new IExpression[argumentCount];

            for (int i = argumentCount - 1; i >= 0; i--)
            {
                arguments[i] = ExpressionStack.Pop();
            }

            IExpression? instance = null;

            if (methodRef.HasThis)
            {
                instance = ExpressionStack.Pop();
            }

            if (methodRef.HasThis && (methodRef.Name.StartsWith("get_") || methodRef.Name.StartsWith("set_")))
            {
                var obj = instance ?? throw new InvalidOperationException($"Expected instance for property call {methodRef.FullName}");

                var property = declearingType.GetProperty(methodRef.Name.Substring(4), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);

                if (property == null)
                    throw new InvalidOperationException($"Property {methodRef.Name.Substring(4)} not found in type {declearingType.FullName}");

                var aliasAttributes = property.GetCustomAttributes(typeof(AliasAttribute), false);
                if (aliasAttributes.Length > 0)
                {
                    var aliasName = (aliasAttributes[0] as AliasAttribute)!.Alias;

                    if (methodRef.Name.StartsWith("get_"))
                    {
                        // It's a property getter
                        var propertyAccess = new FieldAccessExpression(obj, aliasName, AstType.FromClrType(property.PropertyType));
                        ExpressionStack.Push(propertyAccess);
                        return new NopStatement();
                    }
                    else if (methodRef.Name.StartsWith("set_"))
                    {
                        // It's a property setter
                        var value = arguments.Length > 0 ? arguments[0] : throw new InvalidOperationException("Setter method has no arguments");
                        var propertyAccess = new FieldAccessExpression(obj, aliasName, AstType.FromClrType(property.PropertyType));
                        var assignment = new AssignmentStatement(propertyAccess, value);
                        return assignment;
                    }
                }
            }

            var method = TypeHelper.FindMethod(methodRef.Resolve());

            if (method == null)
            {
                throw new InvalidOperationException($"Method {methodRef.Name} not found in type {declearingType.FullName}");
            }

            var parameters = method.GetParameters();

            if (methodRef.HasThis)
            {
                var withInstance = new IExpression[arguments.Length + 1];
                withInstance[0] = instance ?? throw new InvalidOperationException($"Expected instance for call {methodRef.FullName}");
                Array.Copy(arguments, 0, withInstance, 1, arguments.Length);
                arguments = withInstance;
            }

            var argumentOffset = methodRef.HasThis ? 1 : 0;

            for (int i = 0; i < parameters.Length; i++)
            {
                var attributes = parameters[i].GetCustomAttributes(typeof(ByValueAttribute), false);

                if (attributes.Length == 0)
                {
                    if (arguments[i + argumentOffset].Type.IsStruct && !arguments[i + argumentOffset].Type.IsPrimitive)
                    {
                        // If the parameter is not marked with [ByValue] and is a struct, we need to pass a pointer
                        arguments[i + argumentOffset] = new AddressOfExpression(arguments[i + argumentOffset], new PointerAstType(arguments[i + argumentOffset].Type));
                    }
                }
            }

            var returnType = TypeHelper.Find(methodRef.ReturnType.FullName);

            // If TypeHelper.Find failed (e.g. generic parameter T[]), resolve from Cecil type reference
            if (returnType == null && methodRef.ReturnType.FullName != "System.Void")
            {
                returnType = ResolveReturnType(methodRef);
            }

            // Determine return type
            var returnAstType = methodRef.ReturnType.FullName == "System.Void"
                ? PrimitiveAstType.Void
                : AstType.FromClrType(returnType ?? typeof(int));

            var callExpr = new FunctionCallExpression(methodRef, arguments, returnAstType);

            if (returnAstType != PrimitiveAstType.Void)
            {
                ExpressionStack.Push(callExpr);
            }

            if (returnType != null)
            {
                TypeDependencies.Add(returnType);
            }

            MethodDependencies.Add(methodRef.Resolve());

            if (returnAstType == PrimitiveAstType.Void)
            {
                return new ExpressionStatement(callExpr);
            }
            else
            {
                return new NopStatement(); // Expression is pushed onto stack, no statement needed
            }
        }

        /// <summary>
        /// Resolves the CLR return type from a Cecil MethodReference when TypeHelper.Find fails.
        /// Handles generic method instantiations (e.g. Allocate&lt;float&gt;() returning float[])
        /// by resolving generic parameters against the generic arguments.
        /// </summary>
        private static Type? ResolveReturnType(MethodReference methodRef)
        {
            var returnTypeRef = methodRef.ReturnType;

            // Handle array types: T[] where T is a generic parameter
            if (returnTypeRef is ArrayType arrayTypeRef)
            {
                var elementType = ResolveTypeReference(arrayTypeRef.ElementType, methodRef);
                return elementType?.MakeArrayType();
            }

            // Handle direct generic parameters
            if (returnTypeRef is GenericParameter)
            {
                var resolved = ResolveTypeReference(returnTypeRef, methodRef);
                return resolved;
            }

            return null;
        }

        /// <summary>
        /// Resolves a Cecil TypeReference, substituting generic parameters from a GenericInstanceMethod.
        /// </summary>
        private static Type? ResolveTypeReference(TypeReference typeRef, MethodReference methodRef)
        {
            if (typeRef is GenericInstanceType git)
            {
                var openGeneric = TypeHelper.Find(git.ElementType.FullName) ??
                                  TypeHelper.Find(git.ElementType.FullName.Replace('/', '+'));

                if (openGeneric != null && openGeneric.IsGenericTypeDefinition)
                {
                    var genericArgs = new Type[git.GenericArguments.Count];
                    for (var i = 0; i < git.GenericArguments.Count; i++)
                    {
                        var resolvedArg = ResolveTypeReference(git.GenericArguments[i], methodRef);
                        if (resolvedArg == null)
                            return null;
                        genericArgs[i] = resolvedArg;
                    }

                    return openGeneric.MakeGenericType(genericArgs);
                }

                return openGeneric;
            }

            if (typeRef is GenericParameter gp && methodRef is GenericInstanceMethod gim)
            {
                // Resolve the generic parameter to the concrete type argument
                if (gp.Position < gim.GenericArguments.Count)
                {
                    var concreteTypeRef = gim.GenericArguments[gp.Position];
                    return TypeHelper.Find(concreteTypeRef.FullName);
                }
            }

            // Try direct lookup
            return TypeHelper.Find(typeRef.FullName) ?? TypeHelper.Find(typeRef.FullName.Replace('/', '+'));
        }
    }
}
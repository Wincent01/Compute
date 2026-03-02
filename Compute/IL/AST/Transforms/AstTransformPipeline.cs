using System.Collections.Generic;
using Compute.IL.AST.Statements;

namespace Compute.IL.AST.Transforms
{
    /// <summary>
    /// Runs a sequence of <see cref="IAstTransform"/> passes over an AST body.
    /// Transforms execute in registration order, each receiving the output of the previous.
    /// </summary>
    public class AstTransformPipeline
    {
        private readonly List<IAstTransform> _transforms = [];

        /// <summary>
        /// Adds a transform to the end of the pipeline.
        /// </summary>
        public AstTransformPipeline Add(IAstTransform transform)
        {
            _transforms.Add(transform);
            return this;
        }

        /// <summary>
        /// Runs all registered transforms in order, threading the body through each one.
        /// </summary>
        public BlockStatement Run(BlockStatement body, AstTransformContext context)
        {
            foreach (var transform in _transforms)
            {
                body = transform.Transform(body, context);
            }

            return body;
        }

        /// <summary>
        /// Creates a pipeline pre-configured with the standard closure compilation transforms.
        /// </summary>
        public static AstTransformPipeline CreateDefault()
        {
            var pipeline = new AstTransformPipeline();
            pipeline.Add(new ClosureInliningTransform());
            pipeline.Add(new LocalMemoryTransform());
            return pipeline;
        }
    }
}

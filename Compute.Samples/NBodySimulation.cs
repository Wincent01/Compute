using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Compute.IL;
using Compute.IL.AST;
using Compute.IL.AST.CodeGeneration;
using Compute.Memory;

namespace Compute.Samples
{
    public static class Constants
    {
        public const float G = 6.67430e-11f; // Gravitational constant
        public const float SOFTENING = 1e-9f; // Softening parameter to avoid singularities
    }


    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public struct Body
    {
        [FieldOffset(0)]
        public float Mass;

        [FieldOffset(16)]
        public Float3 Position;

        [FieldOffset(32)]
        public Float3 Velocity;
    }

    public static class NBodySimulation
    {
        [Kernel]
        public static void Simulate([Global] Body[] inBodies, [Global] Body[] outBodies, [Const] uint bodyCount, [Const] float deltaTime)
        {
            var id = BuiltIn.GetGlobalId(0);
            if (id >= bodyCount) return;

            var body = inBodies[id];

            /*
            BuiltIn.Print("Id %i", id);
            BuiltIn.Print("Len %i", BuiltIn.SizeOf(body));
            BuiltIn.Print("Mass %f", body.Mass);
            BuiltIn.Print("Px %f", body.Position.X);
            BuiltIn.Print("Py %f", body.Position.Y);
            BuiltIn.Print("Pz %f", body.Position.Z);
            BuiltIn.Print("Vx %f", body.Velocity.X);
            BuiltIn.Print("Vy %f", body.Velocity.Y);
            BuiltIn.Print("Vz %f", body.Velocity.Z);
            */

            // Initialize force to zero
            Float3 force;
            force.X = 0.0f;
            force.Y = 0.0f;
            force.Z = 0.0f;

            // Calculate gravitational forces from all other bodies
            var count = (int)bodyCount;
            for (var j = 0; j < count; j++)
            {
                if (j == id) continue; // Skip self

                var other = inBodies[j];

                // Calculate distance vector
                Float3 r = other.Position - body.Position;

                // Calculate distance squared with softening
                var distanceSq = r.X * r.X + r.Y * r.Y + r.Z * r.Z + Constants.SOFTENING;
                var distance = BuiltIn.Sqrt(distanceSq);

                // Calculate force magnitude
                var forceMagnitude = Constants.G * body.Mass * other.Mass / distanceSq;

                // Add force components (unit vector * force magnitude)
                var invDistance = 1.0f / distance;
                force = force + (r * invDistance * forceMagnitude);
            }

            // Update velocity using F = ma -> a = F/m
            var invMass = 1.0f / body.Mass;
            body.Velocity = body.Velocity + (force * invMass * deltaTime);

            // Update position using v = dx/dt -> dx = v * dt
            body.Position = body.Position + (body.Velocity * deltaTime);

            outBodies[id] = body;
        }

        /// <summary>
        /// CPU version of the N-body simulation for verification
        /// </summary>
        public static void SimulateCPU(Body[] inBodies, Body[] outBodies, float deltaTime)
        {
            var bodyCount = inBodies.Length;

            for (var i = 0; i < bodyCount; i++)
            {
                var body = inBodies[i];
                
                // Initialize force to zero
                var forceX = 0.0f;
                var forceY = 0.0f;
                var forceZ = 0.0f;

                // Calculate gravitational forces from all other bodies
                for (var j = 0; j < bodyCount; j++)
                {
                    if (j == i) continue; // Skip self

                    var other = inBodies[j];
                    
                    // Calculate distance vector
                    var rX = other.Position.X - body.Position.X;
                    var rY = other.Position.Y - body.Position.Y;
                    var rZ = other.Position.Z - body.Position.Z;
                    
                    // Calculate distance squared with softening
                    var distanceSq = rX * rX + rY * rY + rZ * rZ + Constants.SOFTENING;
                    var distance = MathF.Sqrt(distanceSq);
                    
                    // Calculate force magnitude
                    var forceMagnitude = Constants.G * body.Mass * other.Mass / distanceSq;
                    
                    // Add force components (unit vector * force magnitude)
                    var invDistance = 1.0f / distance;
                    forceX = forceX + (rX * invDistance * forceMagnitude);
                    forceY = forceY + (rY * invDistance * forceMagnitude);
                    forceZ = forceZ + (rZ * invDistance * forceMagnitude);
                }

                // Update velocity using F = ma -> a = F/m
                var invMass = 1.0f / body.Mass;
                body.Velocity.X = body.Velocity.X + (forceX * invMass * deltaTime);
                body.Velocity.Y = body.Velocity.Y + (forceY * invMass * deltaTime);
                body.Velocity.Z = body.Velocity.Z + (forceZ * invMass * deltaTime);

                // Update position using v = dx/dt -> dx = v * dt
                body.Position.X = body.Position.X + (body.Velocity.X * deltaTime);
                body.Position.Y = body.Position.Y + (body.Velocity.Y * deltaTime);
                body.Position.Z = body.Position.Z + (body.Velocity.Z * deltaTime);

                outBodies[i] = body;
            }
        }

        /// <summary>
        /// Run N-body simulation example with both GPU and CPU implementations
        /// </summary>
        public static void RunNBodyExample(Accelerator accelerator)
        {
            Console.WriteLine($"\n=== N-Body Simulation on {accelerator.Name} ===");

            using var context = accelerator.CreateContext();
            var ilProgram = new AstProgram(context, new OpenClCodeGenerator());

            // Compile the kernel
            var watch = Stopwatch.StartNew();
            var kernel = ilProgram.Compile(Simulate, out var source);

            // Write kernel source to file for debugging
            File.WriteAllText("nbody_kernel.cl", source);

            watch.Stop();
            Console.WriteLine($"Kernel compilation: {watch.ElapsedMilliseconds}ms");

            const int bodyCount = 100;
            const float deltaTime = 1000f;
            const int timeSteps = 1000;

            // Initialize random bodies
            var random = new Random(11); // Use seed for reproducible results
            var bodies = new Body[bodyCount];
            
            for (var i = 0; i < bodyCount; i++)
            {
                var position = new Float3 {
                    X = (random.NextSingle() - 0.5f) * 1e11f, // Random position in range [-5e10, 5e10]
                    Y = (random.NextSingle() - 0.5f) * 1e11f,
                    Z = (random.NextSingle() - 0.5f) * 1e11f
                };

                var velocity = new Float3 {
                    X = (random.NextSingle() - 0.5f) * 1e4f, // Random velocity in range [-5e3, 5e3]
                    Y = (random.NextSingle() - 0.5f) * 1e4f,
                    Z = (random.NextSingle() - 0.5f) * 1e4f
                };

                bodies[i].Mass = random.NextSingle() * 1e24f + 1e23f; // Random mass between 1e23 and 1.1e24
                bodies[i].Position = position;
                bodies[i].Velocity = velocity;
            }

            var gpuBodies = new Body[bodyCount];
            var cpuBodies = new Body[bodyCount];
            var tempBodies = new Body[bodyCount];

            // Copy initial state
            Array.Copy(bodies, gpuBodies, bodyCount);
            Array.Copy(bodies, cpuBodies, bodyCount);

            Console.WriteLine($"Simulating {bodyCount} bodies for {timeSteps} time steps...");

            // GPU simulation
            using var inputBuffer = new SharedCollection<Body>(context, bodyCount);
            using var outputBuffer = new SharedCollection<Body>(context, bodyCount);

            watch.Restart();

            for (var step = 0; step < timeSteps; step++)
            {
                inputBuffer.CopyToDevice(gpuBodies);
                var deltaTimeAsUInt = BitConverter.SingleToUInt32Bits(deltaTime);
                kernel(bodyCount, inputBuffer, outputBuffer, bodyCount, deltaTimeAsUInt);
                outputBuffer.CopyToHostNonAlloc(gpuBodies);
            }
            
            var gpuTime = watch.ElapsedMilliseconds;

            Console.WriteLine($"GPU simulation completed in {gpuTime}ms");

            // CPU simulation
            watch.Restart();

            for (var step = 0; step < timeSteps; step++)
            {
                SimulateCPU(cpuBodies, tempBodies, deltaTime);
                Array.Copy(tempBodies, cpuBodies, bodyCount);
            }
            
            var cpuTime = watch.ElapsedMilliseconds;
            watch.Stop();

            // Verify results
            var maxError = 0.0f;
            var avgError = 0.0f;
            var validResults = 0;

            for (var i = 0; i < bodyCount; i++)
            {
                var gpuPos = gpuBodies[i].Position;
                var cpuPos = cpuBodies[i].Position;
                
                // Calculate error manually
                var dX = gpuPos.X - cpuPos.X;
                var dY = gpuPos.Y - cpuPos.Y;
                var dZ = gpuPos.Z - cpuPos.Z;
                var error = (float)Math.Sqrt(dX * dX + dY * dY + dZ * dZ);
                
                if (!float.IsNaN(error) && !float.IsInfinity(error))
                {
                    maxError = Math.Max(maxError, error);
                    avgError += error;
                    validResults++;
                }
            }

            if (validResults > 0)
            {
                avgError /= validResults;
            }

            // Display results
            Console.WriteLine($"GPU Time: {gpuTime}ms");
            Console.WriteLine($"CPU Time: {cpuTime}ms");
            Console.WriteLine($"Speedup: {(double)cpuTime / gpuTime:F2}x");
            Console.WriteLine($"Valid Results: {validResults}/{bodyCount}");
            Console.WriteLine($"Average Position Error: {avgError:E2}");
            Console.WriteLine($"Maximum Position Error: {maxError:E2}");

            // Print some sample results
            Console.WriteLine("\nSample final positions (first 3 bodies):");
            for (var i = 0; i < Math.Min(3, bodyCount); i++)
            {
                var gpu = gpuBodies[i].Position;
                var cpu = cpuBodies[i].Position;
                Console.WriteLine($"Body {i}:");
                Console.WriteLine($"  GPU: ({gpu.X:E2}, {gpu.Y:E2}, {gpu.Z:E2})");
                Console.WriteLine($"  CPU: ({cpu.X:E2}, {cpu.Y:E2}, {cpu.Z:E2})");
            }

            // Color-code success/failure
            if (maxError < 1e6f && validResults == bodyCount) // Allow for some numerical differences
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ N-Body simulation results match between GPU and CPU!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ N-Body simulation shows significant differences (expected due to numerical precision)");
            }
            Console.ResetColor();
        }
    }
}
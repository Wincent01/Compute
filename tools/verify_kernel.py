import pyopencl as cl
import sys

# Select the first available platform and device
platforms = cl.get_platforms()
if not platforms:
    raise RuntimeError("No OpenCL platforms found")

platform = platforms[0]
devices = platform.get_devices()
if not devices:
    raise RuntimeError(f"No OpenCL devices found on platform {platform.name}")

if len(sys.argv) != 2:
    print(f"Usage: {sys.argv[0]} kernel.cl")
    sys.exit(1)

kernel_file = sys.argv[1]

# Load the kernel source
with open(kernel_file, "r") as f:
    source = f.read()

device = devices[0]
print(f"Using platform: {platform.name}, device: {device.name}")

# Create context and command queue
ctx = cl.Context([device])
queue = cl.CommandQueue(ctx)

# Create and build program
program = cl.Program(ctx, source)
try:
    program.build()
    print("Kernel compiled successfully!")
    print("Build log:")
    print(program.get_build_info(device, cl.program_build_info.LOG))
except cl.BuildProgramFailure as e:
    # Print the build log for the device
    print("Kernel build failed. Build log:")
    print(program.get_build_info(device, cl.program_build_info.LOG))

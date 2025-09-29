using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Silk.NET.OpenCL;
using Compute.IL;

namespace Compute.Memory;

/// <summary>
/// Represents the type of OpenCL image (1D, 2D, or 3D)
/// </summary>
public enum ImageType
{
    Image1D,
    Image2D, 
    Image3D
}

/// <summary>
/// Represents common OpenCL image channel orders
/// </summary>
public enum ImageChannelOrder : uint
{
    R = 0x10B0,
    A = 0x10B1,
    RG = 0x10B2,
    RA = 0x10B3,
    RGB = 0x10B4,
    RGBA = 0x10B5,
    BGRA = 0x10B6,
    ARGB = 0x10B7,
    Intensity = 0x10B8,
    Luminance = 0x10B9
}

/// <summary>
/// Represents common OpenCL image channel data types
/// </summary>
public enum ImageChannelType : uint
{
    SignedInt8 = 0x10D0,
    SignedInt16 = 0x10D1,
    SignedInt32 = 0x10D2,
    UnsignedInt8 = 0x10D3,
    UnsignedInt16 = 0x10D4,
    UnsignedInt32 = 0x10D5,
    HalfFloat = 0x10D6,
    Float = 0x10D7,
    UNorm101010 = 0x10E0
}

/// <summary>
/// Represents access patterns for SharedImage
/// </summary>
public enum ImageAccess
{
    ReadOnly,
    WriteOnly,
    ReadWrite
}

/// <summary>
/// A comprehensive OpenCL image manager that supports 1D, 2D, and 3D images with various formats
/// </summary>
public class SharedImage : IDisposable
{
    /// <summary>
    /// The compute context this image belongs to
    /// </summary>
    public Context Context { get; }
    
    /// <summary>
    /// The OpenCL memory object handle
    /// </summary>
    public IntPtr Handle { get; }
    
    /// <summary>
    /// The UIntPtr representation of the handle for kernel arguments
    /// </summary>
    public UIntPtr UPtr 
    { 
        get 
        { 
            ThrowIfDisposed(); 
            return (UIntPtr)Handle.ToInt64(); 
        } 
    }
    
    /// <summary>
    /// The type of image (1D, 2D, or 3D)
    /// </summary>
    public ImageType Type { get; }
    
    /// <summary>
    /// Width of the image
    /// </summary>
    public uint Width { get; }
    
    /// <summary>
    /// Height of the image (0 for 1D images)
    /// </summary>
    public uint Height { get; }
    
    /// <summary>
    /// Depth of the image (0 for 1D and 2D images)
    /// </summary>
    public uint Depth { get; }
    
    /// <summary>
    /// Channel order of the image
    /// </summary>
    public ImageChannelOrder ChannelOrder { get; }
    
    /// <summary>
    /// Channel data type of the image
    /// </summary>
    public ImageChannelType ChannelType { get; }
    
    /// <summary>
    /// Access pattern for this image
    /// </summary>
    public ImageAccess Access { get; }

    private bool _disposed = false;

    /// <summary>
    /// Creates a new SharedImage with the specified parameters.
    /// This constructor does not perform OpenCL operations - use static factory methods instead.
    /// </summary>
    /// <param name="context">The compute context</param>
    /// <param name="handle">The OpenCL memory object handle</param>
    /// <param name="type">The image type</param>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height (0 for 1D)</param>
    /// <param name="depth">Image depth (0 for 1D and 2D)</param>
    /// <param name="channelOrder">Channel order</param>
    /// <param name="channelType">Channel data type</param>
    /// <param name="access">Access pattern</param>
    public SharedImage(Context context, IntPtr handle, ImageType type, uint width, uint height, uint depth,
        ImageChannelOrder channelOrder, ImageChannelType channelType, ImageAccess access)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Handle = handle != IntPtr.Zero ? handle : throw new ArgumentException("Handle cannot be zero", nameof(handle));
        Type = type;
        Width = width;
        Height = height;
        Depth = depth;
        ChannelOrder = channelOrder;
        ChannelType = channelType;
        Access = access;
    }

    /// <summary>
    /// Creates a 1D image with RGBA float format (common for compute)
    /// </summary>
    public static unsafe SharedImage Create1D(Context context, uint width, ImageAccess access = ImageAccess.ReadWrite, float* hostData = null)
    {
        var memFlags = access switch
        {
            ImageAccess.ReadOnly => MemFlags.ReadOnly,
            ImageAccess.WriteOnly => MemFlags.WriteOnly,
            ImageAccess.ReadWrite => MemFlags.ReadWrite,
            _ => MemFlags.ReadWrite
        };
        
        if (hostData != null)
        {
            memFlags |= MemFlags.CopyHostPtr;
        }

        var handle = context.CreateImage1D(width, Silk.NET.OpenCL.ChannelOrder.Rgba, Silk.NET.OpenCL.ChannelType.Float, memFlags, hostData);
        
        return new SharedImage(context, handle, ImageType.Image1D, width, 0, 0, 
            ImageChannelOrder.RGBA, ImageChannelType.Float, access);
    }

    /// <summary>
    /// Creates a 2D image with RGBA float format (common for compute)
    /// </summary>
    public static unsafe SharedImage Create2D(Context context, uint width, uint height, ImageAccess access = ImageAccess.ReadWrite, float* hostData = null)
    {
        var memFlags = access switch
        {
            ImageAccess.ReadOnly => MemFlags.ReadOnly,
            ImageAccess.WriteOnly => MemFlags.WriteOnly,
            ImageAccess.ReadWrite => MemFlags.ReadWrite,
            _ => MemFlags.ReadWrite
        };
        
        if (hostData != null)
        {
            memFlags |= MemFlags.CopyHostPtr;
        }

        var handle = context.CreateImage2D(width, height, Silk.NET.OpenCL.ChannelOrder.Rgba, Silk.NET.OpenCL.ChannelType.Float, memFlags, hostData);
        
        return new SharedImage(context, handle, ImageType.Image2D, width, height, 0, 
            ImageChannelOrder.RGBA, ImageChannelType.Float, access);
    }

    /// <summary>
    /// Creates a 3D image with RGBA float format (common for compute)
    /// </summary>
    public static unsafe SharedImage Create3D(Context context, uint width, uint height, uint depth, ImageAccess access = ImageAccess.ReadWrite, float* hostData = null)
    {
        var memFlags = access switch
        {
            ImageAccess.ReadOnly => MemFlags.ReadOnly,
            ImageAccess.WriteOnly => MemFlags.WriteOnly,
            ImageAccess.ReadWrite => MemFlags.ReadWrite,
            _ => MemFlags.ReadWrite
        };
        
        if (hostData != null)
        {
            memFlags |= MemFlags.CopyHostPtr;
        }

        var handle = context.CreateImage3D(width, height, depth, Silk.NET.OpenCL.ChannelOrder.Rgba, Silk.NET.OpenCL.ChannelType.Float, memFlags, hostData);
        
        return new SharedImage(context, handle, ImageType.Image3D, width, height, depth, 
            ImageChannelOrder.RGBA, ImageChannelType.Float, access);
    }

    /// <summary>
    /// Creates a 1D image with custom format
    /// </summary>
    public static unsafe SharedImage Create1DCustom(Context context, uint width, ImageChannelOrder channelOrder, ImageChannelType channelType, ImageAccess access = ImageAccess.ReadWrite, void* hostData = null)
    {
        var memFlags = access switch
        {
            ImageAccess.ReadOnly => MemFlags.ReadOnly,
            ImageAccess.WriteOnly => MemFlags.WriteOnly,
            ImageAccess.ReadWrite => MemFlags.ReadWrite,
            _ => MemFlags.ReadWrite
        };
        
        if (hostData != null)
        {
            memFlags |= MemFlags.CopyHostPtr;
        }

        var handle = context.CreateImage1D(width, (Silk.NET.OpenCL.ChannelOrder)channelOrder, (Silk.NET.OpenCL.ChannelType)channelType, memFlags, hostData);
        
        return new SharedImage(context, handle, ImageType.Image1D, width, 0, 0, channelOrder, channelType, access);
    }

    /// <summary>
    /// Creates a 2D image with custom format
    /// </summary>
    public static unsafe SharedImage Create2DCustom(Context context, uint width, uint height, ImageChannelOrder channelOrder, ImageChannelType channelType, ImageAccess access = ImageAccess.ReadWrite, void* hostData = null)
    {
        var memFlags = access switch
        {
            ImageAccess.ReadOnly => MemFlags.ReadOnly,
            ImageAccess.WriteOnly => MemFlags.WriteOnly,
            ImageAccess.ReadWrite => MemFlags.ReadWrite,
            _ => MemFlags.ReadWrite
        };
        
        if (hostData != null)
        {
            memFlags |= MemFlags.CopyHostPtr;
        }

        var handle = context.CreateImage2D(width, height, (Silk.NET.OpenCL.ChannelOrder)channelOrder, (Silk.NET.OpenCL.ChannelType)channelType, memFlags, hostData);
        
        return new SharedImage(context, handle, ImageType.Image2D, width, height, 0, channelOrder, channelType, access);
    }

    /// <summary>
    /// Creates a 3D image with custom format
    /// </summary>
    public static unsafe SharedImage Create3DCustom(Context context, uint width, uint height, uint depth, ImageChannelOrder channelOrder, ImageChannelType channelType, ImageAccess access = ImageAccess.ReadWrite, void* hostData = null)
    {
        var memFlags = access switch
        {
            ImageAccess.ReadOnly => MemFlags.ReadOnly,
            ImageAccess.WriteOnly => MemFlags.WriteOnly,
            ImageAccess.ReadWrite => MemFlags.ReadWrite,
            _ => MemFlags.ReadWrite
        };
        
        if (hostData != null)
        {
            memFlags |= MemFlags.CopyHostPtr;
        }

        var handle = context.CreateImage3D(width, height, depth, (Silk.NET.OpenCL.ChannelOrder)channelOrder, (Silk.NET.OpenCL.ChannelType)channelType, memFlags, hostData);
        
        return new SharedImage(context, handle, ImageType.Image3D, width, height, depth, channelOrder, channelType, access);
    }

    /// <summary>
    /// Reads image data from device to host memory
    /// </summary>
    public unsafe Span<T> ReadToHost<T>() where T : unmanaged
    {
        ThrowIfDisposed();
        var elementSize = Marshal.SizeOf<T>();
        var totalElements = Type switch
        {
            ImageType.Image1D => (int)Width,
            ImageType.Image2D => (int)(Width * Height),
            ImageType.Image3D => (int)(Width * Height * Depth),
            _ => throw new InvalidOperationException("Unknown image type")
        };

        // Account for channel count (assuming 4 channels for RGBA)
        var channelCount = ChannelOrder switch
        {
            ImageChannelOrder.R => 1,
            ImageChannelOrder.RG => 2,
            ImageChannelOrder.RGB => 3,
            ImageChannelOrder.RGBA => 4,
            ImageChannelOrder.BGRA => 4,
            ImageChannelOrder.ARGB => 4,
            _ => 4 // Default to 4 channels
        };

        var data = new T[totalElements * channelCount];

        fixed (nuint* origin = stackalloc nuint[3] { 0, 0, 0 })
        fixed (nuint* region = Type switch
        {
            ImageType.Image1D => stackalloc nuint[3] { Width, 1, 1 },
            ImageType.Image2D => stackalloc nuint[3] { Width, Height, 1 },
            ImageType.Image3D => stackalloc nuint[3] { Width, Height, Depth },
            _ => throw new InvalidOperationException("Unknown image type")
        })
        {
            Context.ReadImage(Handle, origin, region, data.AsSpan());
        }

        return data;
    }

    /// <summary>
    /// Writes image data from host to device memory
    /// </summary>
    public unsafe void WriteFromHost<T>(Span<T> data) where T : unmanaged
    {
        ThrowIfDisposed();
        fixed (nuint* origin = stackalloc nuint[3] { 0, 0, 0 })
        fixed (nuint* region = Type switch
        {
            ImageType.Image1D => stackalloc nuint[3] { Width, 1, 1 },
            ImageType.Image2D => stackalloc nuint[3] { Width, Height, 1 },
            ImageType.Image3D => stackalloc nuint[3] { Width, Height, Depth },
            _ => throw new InvalidOperationException("Unknown image type")
        })
        {
            Context.WriteImage(Handle, origin, region, data);
        }
    }

    /// <summary>
    /// Reads a portion of the image data from device to host memory
    /// </summary>
    public unsafe Span<T> ReadRegionToHost<T>(uint originX, uint originY, uint originZ, uint regionWidth, uint regionHeight, uint regionDepth) where T : unmanaged
    {
        ThrowIfDisposed();
        var elementSize = Marshal.SizeOf<T>();
        var totalElements = (int)(regionWidth * regionHeight * regionDepth);

        // Account for channel count
        var channelCount = ChannelOrder switch
        {
            ImageChannelOrder.R => 1,
            ImageChannelOrder.RG => 2,
            ImageChannelOrder.RGB => 3,
            ImageChannelOrder.RGBA => 4,
            ImageChannelOrder.BGRA => 4,
            ImageChannelOrder.ARGB => 4,
            _ => 4 // Default to 4 channels
        };

        var data = new T[totalElements * channelCount];

        fixed (nuint* origin = stackalloc nuint[3] { originX, originY, originZ })
        fixed (nuint* region = stackalloc nuint[3] { regionWidth, regionHeight, regionDepth })
        {
            Context.ReadImage(Handle, origin, region, data.AsSpan());
        }

        return data;
    }

    /// <summary>
    /// Writes data to a specific region of the image
    /// </summary>
    public unsafe void WriteRegionFromHost<T>(Span<T> data, uint originX, uint originY, uint originZ, uint regionWidth, uint regionHeight, uint regionDepth) where T : unmanaged
    {
        ThrowIfDisposed();
        fixed (nuint* origin = stackalloc nuint[3] { originX, originY, originZ })
        fixed (nuint* region = stackalloc nuint[3] { regionWidth, regionHeight, regionDepth })
        {
            Context.WriteImage(Handle, origin, region, data);
        }
    }

    /// <summary>
    /// Gets the total number of pixels in the image
    /// </summary>
    public uint GetPixelCount()
    {
        return Type switch
        {
            ImageType.Image1D => Width,
            ImageType.Image2D => Width * Height,
            ImageType.Image3D => Width * Height * Depth,
            _ => throw new InvalidOperationException("Unknown image type")
        };
    }

    /// <summary>
    /// Gets the number of channels based on the channel order
    /// </summary>
    public int GetChannelCount()
    {
        return ChannelOrder switch
        {
            ImageChannelOrder.R => 1,
            ImageChannelOrder.A => 1,
            ImageChannelOrder.Intensity => 1,
            ImageChannelOrder.Luminance => 1,
            ImageChannelOrder.RG => 2,
            ImageChannelOrder.RA => 2,
            ImageChannelOrder.RGB => 3,
            ImageChannelOrder.RGBA => 4,
            ImageChannelOrder.BGRA => 4,
            ImageChannelOrder.ARGB => 4,
            _ => 4
        };
    }

    /// <summary>
    /// Implicit conversion to UIntPtr for use as kernel argument
    /// </summary>
    public static implicit operator UIntPtr(SharedImage image)
    {
        return image.UPtr;
    }

    /// <summary>
    /// Get a read-only view of the image
    /// </summary>
    public T ReadOnlyView<T>() where T : IReadOnlyImage
    {
        return (T)Activator.CreateInstance(typeof(T), UPtr)!;
    }

    /// <summary>
    /// Get a write-only view of the image
    /// </summary>
    public T WriteOnlyView<T>() where T : IWriteOnlyImage
    {
        return (T)Activator.CreateInstance(typeof(T), UPtr)!;
    }

    /// <summary>
    /// Disposes the SharedImage and releases OpenCL resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Protected implementation of dispose pattern
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources if any
                // Currently no managed resources to dispose
            }

            // Release unmanaged OpenCL image object
            if (Handle != IntPtr.Zero)
            {
                try
                {
                    Context.ReleaseBuffer(Handle);
                }
                catch
                {
                    // Suppress exceptions during finalization to prevent app crashes
                    // This can happen if the context is already disposed
                }
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer to ensure OpenCL resources are released
    /// </summary>
    ~SharedImage()
    {
        Dispose(false);
    }
    
    /// <summary>
    /// Throws an exception if the object has been disposed
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SharedImage));
        }
    }
}

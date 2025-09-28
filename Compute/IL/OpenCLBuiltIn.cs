using System;
using System.Runtime.InteropServices;

namespace Compute.IL
{
    public class AliasException : Exception
    {
        public AliasException() : base("This method is an alias for OpenCL, not meant for use in .NET") { }
    }

    public static class BuiltIn
    {
        [Alias("get_global_id")]
        public static int GetGlobalId(int dimension)
        {
            return default;
        }

        [Alias("sqrt")]
        public static float Sqrt(float value)
        {
            return MathF.Sqrt(value);
        }

        [Alias("length")]
        public static float Length([ByValue] Float4 vector)
        {
            throw new AliasException();
        }

        [Alias("normalize")]
        public static Float4 Normalize([ByValue] Float4 vector)
        {
            throw new AliasException();
        }

        [Alias("printf")]
        public static int Print(string format, float arg)
        {
            return default;
        }

        [Alias("printf")]
        public static int Print(string format, int arg)
        {
            return default;
        }

        [Alias("printf")]
        public static int Print(string format, Float3 arg)
        {
            return default;
        }

        [Alias("sizeof")]
        public static int SizeOf([ByValue] object obj)
        {
            return Marshal.SizeOf(obj);
        }
    }

    [Alias("float2")]
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public struct Float2
    {
        /// <summary>
        /// The X and Y components of the vector.
        /// </summary>
        [Alias("x")]
        public float X;
        [Alias("y")]
        public float Y;

        [Alias("xy")]
        public Float2 XY { get => this; set => (X, Y) = (value.X, value.Y); }

        [Alias("yx")]
        public Float2 YX { get => new Float2 { X = Y, Y = X }; set => (Y, X) = (value.X, value.Y); }

        [Alias("operator*")]
        public static Float2 operator *([ByValue] Float2 left, [ByValue] Float2 right) => throw new AliasException();

        [Alias("operator+")]
        public static Float2 operator +([ByValue] Float2 left, [ByValue] Float2 right) => throw new AliasException();

        [Alias("operator-")]
        public static Float2 operator -([ByValue] Float2 left, [ByValue] Float2 right) => throw new AliasException();

        [Alias("operator/")]
        public static Float2 operator /([ByValue] Float2 left, [ByValue] Float2 right) => throw new AliasException();

        [Alias("operator*")]
        public static Float2 operator *([ByValue] Float2 left, [ByValue] float right) => throw new AliasException();

        [Alias("operator*")]
        public static Float2 operator *(float left, [ByValue] Float2 right) => throw new AliasException();
    }

    [Alias("float3")]
    [StructLayout(LayoutKind.Sequential, Size = 12)]
    public struct Float3
    {
        /// <summary>
        /// The X, Y, and Z components of the vector.
        /// </summary>
        [Alias("x")]
        public float X;
        [Alias("y")]
        public float Y;
        [Alias("z")]
        public float Z;

        [Alias("xy")]
        public Float2 XY { get => new Float2 { X = X, Y = Y }; set => (X, Y) = (value.X, value.Y); }

        [Alias("yz")]
        public Float2 YZ { get => new Float2 { X = Y, Y = Z }; set => (Y, Z) = (value.X, value.Y); }

        [Alias("xz")]
        public Float2 XZ { get => new Float2 { X = X, Y = Z }; set => (X, Z) = (value.X, value.Y); }

        [Alias("xyz")]
        public Float3 XYZ { get => this; set => (X, Y, Z) = (value.X, value.Y, value.Z); }

        [Alias("operator*")]
        public static Float3 operator *([ByValue] Float3 left, [ByValue] Float3 right) => throw new AliasException();

        [Alias("operator+")]
        public static Float3 operator +([ByValue] Float3 left, [ByValue] Float3 right) => throw new AliasException();

        [Alias("operator-")]
        public static Float3 operator -([ByValue] Float3 left, [ByValue] Float3 right) => throw new AliasException();

        [Alias("operator/")]
        public static Float3 operator /([ByValue] Float3 left, [ByValue] Float3 right) => throw new AliasException();

        [Alias("operator*")]
        public static Float3 operator *([ByValue] Float3 left, [ByValue] float right) => throw new AliasException();

        [Alias("operator*")]
        public static Float3 operator *(float left, [ByValue] Float3 right) => throw new AliasException();

        public override string ToString() => $"({X}, {Y}, {Z})";
    }

    [Alias("float4")]
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct Float4
    {
        /// <summary>
        /// The X, Y, Z, and W components of the vector.
        /// </summary>
        [Alias("x")]
        public float X;
        [Alias("y")]
        public float Y;
        [Alias("z")]
        public float Z;
        [Alias("w")]
        public float W;

        [Alias("xy")]
        public Float2 XY { get => new Float2 { X = X, Y = Y }; set => (X, Y) = (value.X, value.Y); }

        [Alias("xyz")]
        public Float3 XYZ { get => new Float3 { X = X, Y = Y, Z = Z }; set => (X, Y, Z) = (value.X, value.Y, value.Z); }

        [Alias("zw")]
        public Float2 ZW { get => new Float2 { X = Z, Y = W }; set => (Z, W) = (value.X, value.Y); }

        [Alias("xw")]
        public Float2 XW { get => new Float2 { X = X, Y = W }; set => (X, W) = (value.X, value.Y); }

        [Alias("yw")]
        public Float2 YW { get => new Float2 { X = Y, Y = W }; set => (Y, W) = (value.X, value.Y); }

        [Alias("zw")]
        public Float2 YZ { get => new Float2 { X = Y, Y = Z }; set => (Y, Z) = (value.X, value.Y); }

        [Alias("xzy")]
        public Float3 XZY { get => new Float3 { X = X, Y = Z, Z = Y }; set => (X, Z, Y) = (value.X, value.Y, value.Z); }

        [Alias("wzy")]
        public Float3 WZY { get => new Float3 { X = W, Y = Z, Z = Y }; set => (W, Z, Y) = (value.X, value.Y, value.Z); }

        [Alias("wzyx")]
        public Float4 WZYX
        {
            get => new Float4 { X = W, Y = Z, Z = Y, W = X };
            set => (X, Y, Z, W) = (value.W, value.Z, value.Y, value.X);
        }

        [Alias("xyzw")]
        public Float4 XYZW
        {
            get => this;
            set => (X, Y, Z, W) = (value.X, value.Y, value.Z, value.W);
        }

        [Alias("operator*")]
        public static Float4 operator *([ByValue] Float4 left, [ByValue] Float4 right) => throw new AliasException();

        [Alias("operator+")]
        public static Float4 operator +([ByValue] Float4 left, [ByValue] Float4 right) => throw new AliasException();

        [Alias("operator-")]
        public static Float4 operator -([ByValue] Float4 left, [ByValue] Float4 right) => throw new AliasException();

        [Alias("operator/")]
        public static Float4 operator /([ByValue] Float4 left, [ByValue] Float4 right) => throw new AliasException();

        [Alias("operator*")]
        public static Float4 operator *([ByValue] Float4 left, [ByValue] float right) => throw new AliasException();

        [Alias("operator*")]
        public static Float4 operator *(float left, [ByValue] Float4 right) => throw new AliasException();
    }
}
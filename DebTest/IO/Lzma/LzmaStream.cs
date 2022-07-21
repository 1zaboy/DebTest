using System.Runtime.InteropServices;

namespace Packaging.Targets.IO
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct LzmaStream
    {
        public IntPtr NextIn;
        public uint AvailIn;
        public ulong TotalIn;
        public IntPtr NextOut;
        public uint AvailOut;
        public ulong TotalOut;
        public IntPtr Allocator;
#pragma warning disable SA1214 // Readonly fields must appear before non-readonly fields
        public readonly IntPtr InternalState;
#pragma warning restore SA1214 // Readonly fields must appear before non-readonly fields
        private readonly IntPtr reservedPtr1;
        private readonly IntPtr reservedPtr2;
        private readonly IntPtr reservedPtr3;
        private readonly IntPtr reservedPtr4;
        private readonly ulong reservedInt1;
        private readonly ulong reservedInt2;
        private readonly uint reservedInt3;
        private readonly uint reservedInt4;
        private readonly uint reservedEnum1;
        private readonly uint reservedEnum2;
    }
}
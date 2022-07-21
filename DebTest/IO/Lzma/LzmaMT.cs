namespace Packaging.Targets.IO
{
#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter
#pragma warning disable SA1310 // Field names must not contain underscore
#pragma warning disable CS0169 // The field '' is never used
#pragma warning disable IDE0051 // Remove unused private members
    internal struct LzmaMT
    {
        public uint flags;
        public uint threads;
        public ulong block_size;
        public uint timeout;
        public uint preset;
        public IntPtr filters;
        public LzmaCheck check;
        private readonly int reserved_enum1;
        private readonly int reserved_enum2;
        private readonly int reserved_enum3;
        private readonly int reserved_int1;
        private readonly int reserved_int2;
        private readonly int reserved_int3;
        private readonly int reserved_int4;
        private readonly ulong reserved_int5;
        private readonly ulong reserved_int6;
        private readonly ulong reserved_int7;
        private readonly ulong reserved_int8;
        private readonly IntPtr reserved_ptr1;
        private readonly IntPtr reserved_ptr2;
        private readonly IntPtr reserved_ptr3;
        private readonly IntPtr reserved_ptr4;
    }
#pragma warning restore SA1307 // Accessible fields must begin with upper-case letter
#pragma warning restore SA1310 // Field names must not contain underscore
#pragma warning restore CS0169 // The field '' is never used
#pragma warning restore IDE0051 // Remove unused private members
}

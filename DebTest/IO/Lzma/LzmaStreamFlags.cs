using System.Runtime.InteropServices;

namespace Packaging.Targets.IO
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct LzmaStreamFlags
    {
        public readonly uint Version;
        public ulong BackwardSize;
        public LzmaCheck Check;
        private readonly int reservedEnum1;
        private readonly int reservedEnum2;
        private readonly int reservedEnum3;
        private readonly int reservedEnum4;
        private readonly char reservedBool1;
        private readonly char reservedBool2;
        private readonly char reservedBool3;
        private readonly char reservedBool4;
        private readonly char reservedBool5;
        private readonly char reservedBool6;
        private readonly char reservedBool7;
        private readonly char reservedBool8;
        private readonly uint reservedInt1;
        private readonly uint reservedInt2;
    }
}

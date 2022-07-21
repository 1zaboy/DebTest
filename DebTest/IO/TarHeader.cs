using System.Runtime.InteropServices;
using System.Text;

namespace Packaging.Targets.IO
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct TarHeader : IArchiveHeader
    {
        private static readonly string Empty8 = new string('\0', 8);

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 100)]
        private byte[] name;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
        private byte[] mode;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
        private byte[] uid;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
        private byte[] gid;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 12)]
        private byte[] size;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 12)]
        private byte[] mtime;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
        private byte[] chksum;

        private byte typeflag;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 100)]
        private byte[] linkname;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 6)]
        private byte[] magic;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 2)]
        private byte[] version;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 32)]
        private byte[] uname;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 32)]
        private byte[] gname;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
        private byte[] devmajor;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
        private byte[] devminor;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 155)]
        private byte[] prefix;

        public string FileName
        {
            get => GetString(name, 100);
            set => name = CreateString(value, 100);
        }

        public LinuxFileMode FileMode
        {
            get => (LinuxFileMode)Convert.ToUInt32(GetString(mode, 8), 8);
            set => mode = GetUIntTo8((uint)value);
        }

        public uint UserId
        {
            get => Convert.ToUInt32(GetString(uid, 8), 8);
            set => uid = GetUIntTo8(value);
        }

        public uint GroupId
        {
            get => Convert.ToUInt32(GetString(gid, 8), 8);
            set => gid = GetUIntTo8(value);
        }

        public uint FileSize
        {
            get => Convert.ToUInt32(GetString(size, 12), 8);
            set => size = GetUIntTo8(value, 12);
        }

        public DateTimeOffset LastModified
        {
            get => DateTimeOffset.FromUnixTimeSeconds((long)Convert.ToUInt64(GetString(mtime, 12), 8));
            set => mtime = GetUIntTo8((uint)value.ToUnixTimeSeconds(), 12);
        }

        public uint Checksum
        {
            get => Convert.ToUInt32(GetString(chksum, 8), 8);
            set
            {
                var s = GetUIntTo8(value, 7);
                var buffer = new byte[8];
                Array.Copy(s, buffer, 7);
                buffer[7] = 32;
                chksum = buffer;
            }
        }

        public TarTypeFlag TypeFlag
        {
            get => (TarTypeFlag)typeflag;
            set => typeflag = (byte)value;
        }

        public string LinkName
        {
            get => GetString(linkname, 100);
            set => linkname = CreateString(value, 100);
        }

        public string Magic
        {
            get => GetString(magic, 6)?.Trim();
            set => magic = CreateString(value.PadRight(6), 6);
        }

        public uint? Version
        {
            get
            {
                var v = GetString(version, 2);
                if (uint.TryParse(v, out uint rv))
                {
                    return rv;
                }

                return null;
            }

            set
            {
                if (value == null)
                {
                    version = new byte[] { 32, 0 };
                }
                else
                {
                    version = GetUIntTo8(value, 2);
                }
            }
        }

        public string UserName
        {
            get => GetString(uname, 32);
            set => uname = CreateString(value, 32);
        }

        public string GroupName
        {
            get => GetString(gname, 32);
            set => gname = CreateString(value, 32);
        }

        public uint? DevMajor
        {
            get => devmajor[0] == 0 ? (uint?)null : Convert.ToUInt32(GetString(devmajor, 8), 8);
            set => devmajor = GetUIntTo8(value);
        }

        public uint? DevMinor
        {
            get => devminor[0] == 0 ? (uint?)null : Convert.ToUInt32(GetString(devminor, 8), 8);
            set => devminor = GetUIntTo8(value);
        }

        public string Prefix
        {
            get => GetString(prefix, 155);
            set => prefix = CreateString(value, 155);
        }

        public uint ComputeChecksum()
        {
            var other = this;
            other.chksum = new byte[8];
            for (var c = 0; c < 8; c++)
            {
                other.chksum[c] = 32;
            }

            var data = new byte[Marshal.SizeOf<TarHeader>()];
            fixed (byte* ptr = data)
            {
                Marshal.StructureToPtr(other, new IntPtr(ptr), true);
            }

            uint sum = 0;
            foreach (var b in data)
            {
                sum += b;
            }

            return sum;
        }

        private string GetString(byte[] data, int maxLen)
        {
            int len;
            for (len = 0; len < maxLen; len++)
            {
                if (data[len] == 0)
                {
                    break;
                }
            }

            if (len == 0)
            {
                return null;
            }

            return Encoding.UTF8.GetString(data, 0, len);
        }

        private byte[] GetUIntTo8(uint? data, int len = 8)
        {
            if (data == null)
            {
                return new byte[len];
            }

            return CreateString(Convert.ToString(data.Value, 8).PadLeft(len - 1, '0'), len);
        }

        private byte[] CreateString(string s, int len)
        {
            var target = new byte[len];
            if (s == null)
            {
                return target;
            }

            var buffer = Encoding.UTF8.GetBytes(s);
            if (buffer.Length > len)
            {
                throw new Exception($"String {s} exceeds the limit of {len}");
            }

            for (var c = 0; c < len; c++)
            {
                target[c] = (c < buffer.Length) ? buffer[c] : (byte)0;
            }

            return target;
        }
    }
}
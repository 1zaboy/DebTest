using System.Runtime.InteropServices;
using System.Text;

namespace Packaging.Targets.IO
{
    public struct ArHeader : IArchiveHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
        private byte[] fileName;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 12)]
        private byte[] lastModified;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 6)]
        private byte[] ownerId;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 6)]
        private byte[] groupId;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
        private byte[] fileMode;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 10)]
        private byte[] fileSize;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 2)]
        private byte[] endChar;

        public string FileName
        {
            get => GetString(fileName, 16).Trim();
            set => fileName = CreateString(value, 16);
        }

        public DateTimeOffset LastModified
        {
            get => DateTimeOffset.FromUnixTimeSeconds(int.Parse(GetString(lastModified, 12).Trim()));
            set => lastModified = CreateString(value.ToUnixTimeSeconds().ToString(), 12);
        }

        public uint OwnerId
        {
            get => ReadUInt(ownerId);
            set => ownerId = CreateString(value.ToString(), 6);
        }

        public uint GroupId
        {
            get => ReadUInt(groupId);
            set => groupId = CreateString(value.ToString(), 6);
        }

        public LinuxFileMode FileMode
        {
            get => (LinuxFileMode)Convert.ToUInt32(GetString(fileMode, 8).Trim(), 8);
            set => fileMode = CreateString(Convert.ToString((uint)value, 8), 8);
        }

        public uint FileSize
        {
            get => ReadUInt(fileSize);
            set => fileSize = CreateString(value.ToString(), 10);
        }

        public string EndChar
        {
            get => GetString(endChar, 2);
            set => endChar = CreateString(value, 2);
        }

        public override string ToString()
        {
            return FileName;
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

        private uint ReadUInt(byte[] data) => Convert.ToUInt32(GetString(data, data.Length).Trim());

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
                target[c] = (c < buffer.Length) ? buffer[c] : (byte)0x20;
            }

            return target;
        }
    }
}

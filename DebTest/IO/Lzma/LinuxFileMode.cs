namespace Packaging.Targets.IO
{
    [Flags]
    public enum LinuxFileMode : ushort
    {
        None = 0,
        S_ISUID = 0x0800,
        S_ISGID = 0x0400,
        S_ISVTX = 0x0200,
        S_IRUSR = 0x0100,
        S_IWUSR = 0x0080,
        S_IXUSR = 0x0040,
        S_IRGRP = 0x0020,
        S_IWGRP = 0x0010,
        S_IXGRP = 0x0008,
        S_IROTH = 0x0004,
        S_IWOTH = 0x0002,
        S_IXOTH = 0x0001,
        S_IFIFO = 0x1000, // 010000 in octal
        S_IFCHR = 0x2000, // 0020000 in octal
        S_IFDIR = 0x4000, // 0040000 in octal
        S_IFBLK = 0x6000, // 0060000 in octal
        S_IFREG = 0x8000, // 0100000 in octal
        S_IFLNK = 0xA000, // 0120000 in octal
        S_IFSOCK = 0xC000, // 0140000 in octal
        PermissionsMask = 0x0FFF,
        FileTypeMask = 0xF000,
    }
}

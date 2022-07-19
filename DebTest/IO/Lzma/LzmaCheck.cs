namespace Packaging.Targets.IO
{
    internal enum LzmaCheck
    {
        None = 0,
        Crc32 = 1,
        Crc64 = 4,
        Sha256 = 10
    }
}

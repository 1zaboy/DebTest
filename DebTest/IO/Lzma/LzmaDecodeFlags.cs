namespace Packaging.Targets.IO
{
    internal enum LzmaDecodeFlags : uint
    {
        TellNoCheck = 0x01,
        TellUnsupportedCheck = 0x02,
        TellAnyCheck = 0x04,
        Concatenated = 0x08
    }
}

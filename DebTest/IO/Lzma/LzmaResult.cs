namespace Packaging.Targets.IO
{
    internal enum LzmaResult : uint
    {
        OK = 0,
        StreamEnd = 1,
        NoCheck = 2,
        UnsupportedCheck = 3,
        GetCheck = 4,
        MemError = 5,
        MemlimitError = 6,
        FormatError = 7,
        OptionsError = 8,
        DataError = 9,
        BufferError = 10,
        ProgError = 11
    }
}

using System;

namespace Packaging.Targets.IO
{
    public interface IArchiveHeader
    {
        LinuxFileMode FileMode { get; }
        DateTimeOffset LastModified { get; }
        uint FileSize { get; }
    }
}

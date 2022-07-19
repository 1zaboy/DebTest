using Packaging.Targets.IO;

namespace Packaging.Targets.Deb
{
    public class DebPackageControlFileData
    {
        public LinuxFileMode Mode { get; set; }
        public string Contents { get; set; }
    }
}
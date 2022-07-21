namespace Packaging.Targets.IO
{
    public class ArchiveEntry
    {
        public string TargetPath { get; set; }
        public string Owner { get; set; }
        public string Group { get; set; }
        public LinuxFileMode Mode { get; set; }
        public string SourceFilename { get; set; }
        public uint FileSize { get; set; }
        public DateTimeOffset Modified { get; set; }
        public byte[] Sha256 { get; set; }
        public byte[] Md5Hash { get; set; }
        public ArchiveEntryType Type { get; set; }
        public string LinkTo { get; set; }
        public uint Inode { get; set; }
        public bool IsAscii { get; set; }
        public bool RemoveOnUninstall { get; set; }
        
        public string TargetPathWithFinalSlash
        {
            get
            {
                if (Mode.HasFlag(LinuxFileMode.S_IFDIR) && !TargetPath.EndsWith("/"))
                {
                    return TargetPath + "/";
                }
                else
                {
                    return TargetPath;
                }
            }
        }

        public string TargetPathWithoutFinalSlash
        {
            get
            {
                return TargetPath?.TrimEnd('/');
            }
        }

        public override string ToString()
        {
            return TargetPath;
        }
    }
}

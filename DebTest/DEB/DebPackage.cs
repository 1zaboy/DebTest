namespace Packaging.Targets.Deb
{
    internal class DebPackage
    {
        public Version PackageFormatVersion { get; set; } 
        public Dictionary<string, string> ControlFile { get; set; }
        public Dictionary<string, DebPackageControlFileData> ControlExtras { get; set; }
        public Dictionary<string, string> Md5Sums { get; set; }
        public string PreInstallScript { get; set; }
        public string PostInstallScript { get; set; }
        public string PreRemoveScript { get; set; }
        public string PostRemoveScript { get; set; }
    }
}

using Microsoft.Build.Framework;
using Packaging.Targets.Deb;
using Packaging.Targets.IO;

namespace Packaging.Targets
{
    public class DebTask
    {
        public string PublishDir { get; set; }
        public string DebPath { get; set; }
        public string DebTarPath { get; set; }
        public string DebTarXzPath { get; set; }
        public string Prefix { get; set; }
        public string Version { get; set; }
        public string PackageName { get; set; }
        public ITaskItem[] Content { get; set; }
        public string Maintainer { get; set; }
        public string Description { get; set; }
        public string DebPackageArchitecture { get; set; }
        public string AppHost { get; set; }
        public ITaskItem[] LinuxFolders { get; set; }
        public ITaskItem[] DebDotNetDependencies { get; set; }
        public ITaskItem[] DebDependencies { get; set; }
        public ITaskItem[] DebRecommends { get; set; }

        public bool Execute()
        {
            using (var targetStream = File.Open(DebPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (var tarStream = File.Open(DebTarPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                ArchiveBuilder archiveBuilder = new ArchiveBuilder()
                {
                    //
                };

                var archiveEntries = archiveBuilder.FromDirectory(
                    PublishDir,
                    AppHost,
                    Prefix,
                    Content);

                archiveEntries.AddRange(archiveBuilder.FromLinuxFolders(LinuxFolders));
                EnsureDirectories(archiveEntries);

                archiveEntries = archiveEntries
                    .OrderBy(e => e.TargetPathWithFinalSlash, StringComparer.Ordinal)
                    .ToList();

                TarFileCreator.FromArchiveEntries(archiveEntries, tarStream);
                tarStream.Position = 0;

                List<string> dependencies = new List<string>();

                if (DebDependencies != null)
                {
                    var debDependencies = DebDependencies.Select(d => d.ItemSpec).ToArray();

                    dependencies.AddRange(debDependencies);
                }

                if (DebDotNetDependencies != null)
                {
                    var debDotNetDependencies = DebDotNetDependencies.Select(d => d.ItemSpec).ToArray();

                    dependencies.AddRange(debDotNetDependencies);
                }

                List<string> recommends = new List<string>();

                if (DebRecommends != null)
                {
                    recommends.AddRange(DebRecommends.Select(d => d.ItemSpec));
                }

                using (var tarXzStream = File.Open(DebTarXzPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (var xzStream = new XZOutputStream(tarXzStream))
                {
                    tarStream.CopyTo(xzStream);
                }

                using (var tarXzStream = File.Open(DebTarXzPath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    var pkg = DebPackageCreator.BuildDebPackage(
                        archiveEntries,
                        PackageName,
                        Description,
                        Maintainer,
                        Version,
                        DebPackageArchitecture,
                        dependencies,
                        recommends,
                        null);

                    DebPackageCreator.WriteDebPackage(
                        archiveEntries,
                        tarXzStream,
                        targetStream,
                        pkg);
                }

                return true;
            }
        }

        internal static void EnsureDirectories(List<ArchiveEntry> entries, bool includeRoot = true)
        {
            var dirs = new HashSet<string>(entries.Where(x => x.Mode.HasFlag(LinuxFileMode.S_IFDIR))
                .Select(d => d.TargetPathWithoutFinalSlash));

            var toAdd = new List<ArchiveEntry>();

            string GetDirPath(string path)
            {
                path = path.TrimEnd('/');
                if (path == string.Empty)
                {
                    return "/";
                }

                if (!path.Contains("/"))
                {
                    return string.Empty;
                }

                return path.Substring(0, path.LastIndexOf('/'));
            }

            void EnsureDir(string dirPath)
            {
                if (dirPath == string.Empty || dirPath == ".")
                {
                    return;
                }

                if (!dirs.Contains(dirPath))
                {
                    if (dirPath != "/")
                    {
                        EnsureDir(GetDirPath(dirPath));
                    }

                    dirs.Add(dirPath);
                    toAdd.Add(new ArchiveEntry()
                    {
                        Mode = LinuxFileMode.S_IXOTH | LinuxFileMode.S_IROTH | LinuxFileMode.S_IXGRP |
                               LinuxFileMode.S_IRGRP | LinuxFileMode.S_IXUSR | LinuxFileMode.S_IWUSR |
                               LinuxFileMode.S_IRUSR | LinuxFileMode.S_IFDIR,
                        Modified = DateTime.Now,
                        Group = "root",
                        Owner = "root",
                        TargetPath = dirPath,
                        LinkTo = string.Empty,
                    });
                }
            }

            foreach (var entry in entries)
            {
                EnsureDir(GetDirPath(entry.TargetPathWithFinalSlash));
            }

            if (includeRoot)
            {
                EnsureDir("/");
            }

            entries.AddRange(toAdd);
        }
    }
}
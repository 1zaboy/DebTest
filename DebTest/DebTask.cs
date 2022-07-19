using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Packaging.Targets.Deb;
using Packaging.Targets.IO;

namespace Packaging.Targets
{
    public class DebTask // : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string PublishDir { get; set; }

        [Required]
        public string DebPath { get; set; }

        [Required]
        public string DebTarPath { get; set; }

        [Required]
        public string DebTarXzPath { get; set; }

        [Required]
        public string Prefix { get; set; }

        [Required]
        public string Version { get; set; }

        [Required]
        public string PackageName { get; set; }

        [Required]
        public ITaskItem[] Content { get; set; }

        [Required]
        public string Maintainer { get; set; }

        [Required]
        public string Description { get; set; }

        public string RuntimeIdentifier { get; set; }

        public string DebPackageArchitecture { get; set; }

        public string Section { get; set; }

        public string Homepage { get; set; }

        public string Priority { get; set; }

        public string AppHost { get; set; }

        public ITaskItem[] LinuxFolders { get; set; }

        public ITaskItem[] DebDotNetDependencies { get; set; }

        public ITaskItem[] DebDependencies { get; set; }

        public ITaskItem[] DebRecommends { get; set; }

        public bool CreateUser { get; set; }

        public string UserName { get; set; }

        public bool InstallService { get; set; }
        public string ServiceName { get; set; }
        public string PreInstallScript { get; set; }
        public string PostInstallScript { get; set; }
        public string PreRemoveScript { get; set; }
        public string PostRemoveScript { get; set; }
        public static string GetPackageArchitecture(string runtimeIdentifier)
        {
            RuntimeIdentifiers.ParseRuntimeId(runtimeIdentifier, out _, out _, out Architecture? architecture, out _);

            if (architecture != null)
            {
                switch (architecture.Value)
                {
                    case Architecture.Arm:
                        return "armhf";

                    case Architecture.Arm64:
                        return "arm64";

                    case Architecture.X64:
                        return "amd64";

                    case Architecture.X86:
                        return "i386";
                }
            }

            return "all";
        }

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
                        !string.IsNullOrWhiteSpace(DebPackageArchitecture) ? DebPackageArchitecture : GetPackageArchitecture(RuntimeIdentifier),
                        CreateUser,
                        UserName,
                        InstallService,
                        ServiceName,
                        Prefix,
                        Section,
                        Priority,
                        Homepage,
                        PreInstallScript,
                        PostInstallScript,
                        PreRemoveScript,
                        PostRemoveScript,
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
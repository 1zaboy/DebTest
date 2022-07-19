using Microsoft.Build.Framework;
using Packaging.Targets.IO;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace Packaging.Targets
{
    internal class ArchiveBuilder
    {
        private uint inode = 0;

        public ArchiveBuilder()
        {
            //
        }

        public List<ArchiveEntry> FromDirectory(string directory, string appHost, string prefix, ITaskItem[] metadata)
        {
            List<ArchiveEntry> value = new List<ArchiveEntry>();
            AddDirectory(directory, string.Empty, prefix, value, metadata);

            if (appHost != null)
            {
                value.Add(
                    new ArchiveEntry()
                    {
                        Mode = LinuxFileMode.S_IXOTH | LinuxFileMode.S_IROTH | LinuxFileMode.S_IXGRP | LinuxFileMode.S_IRGRP | LinuxFileMode.S_IXUSR | LinuxFileMode.S_IWUSR | LinuxFileMode.S_IRUSR | LinuxFileMode.S_IFLNK,
                        Modified = DateTimeOffset.UtcNow,
                        Group = "root",
                        Owner = "root",
                        TargetPath = $"/usr/local/bin/{appHost}",
                        LinkTo = $"{prefix}/{appHost}",
                        Inode = inode++,
                        Sha256 = Array.Empty<byte>(),
                    });
            }

            return value;
        }

        protected void AddDirectory(string directory, string relativePath, string prefix, List<ArchiveEntry> value, ITaskItem[] metadata)
        {
            inode++;

            var entries = Directory.GetFileSystemEntries(directory).OrderBy(e => Directory.Exists(e) ? e + "/" : e, StringComparer.Ordinal).ToArray();

            foreach (var entry in entries)
            {
                if (File.Exists(entry))
                {
                    AddFile(entry, relativePath + Path.GetFileName(entry), prefix, value, metadata);
                }
                else
                {
                    AddDirectory(entry, relativePath + Path.GetFileName(entry) + "/", prefix + "/" + Path.GetFileName(entry), value, metadata);
                }
            }
        }
        
        public Collection<ArchiveEntry> FromLinuxFolders(ITaskItem[] metadata)
        {
            Collection<ArchiveEntry> value = new Collection<ArchiveEntry>();

            if (metadata != null)
            {
                foreach (var folder in metadata)
                {
                    var path = folder.ItemSpec.Replace("\\", "/");

                    LinuxFileMode mode = LinuxFileMode.S_IXOTH | LinuxFileMode.S_IROTH | LinuxFileMode.S_IXGRP | LinuxFileMode.S_IRGRP | LinuxFileMode.S_IXUSR | LinuxFileMode.S_IWUSR | LinuxFileMode.S_IRUSR | LinuxFileMode.S_IFDIR;
                    mode = this.GetFileMode(path, folder, mode);

                    ArchiveEntry directoryEntry = new ArchiveEntry()
                    {
                        FileSize = 0x00001000,
                        Sha256 = Array.Empty<byte>(),
                        Mode = mode,
                        Modified = DateTime.Now,
                        Group = folder.GetGroup(),
                        Owner = folder.GetOwner(),
                        Inode = this.inode++,
                        TargetPath = path,
                        LinkTo = string.Empty,
                        RemoveOnUninstall = folder.GetRemoveOnUninstall()
                    };

                    value.Add(directoryEntry);
                }
            }

            return value;
        }

        protected void AddFile(string entry, string relativePath, string prefix, List<ArchiveEntry> value, ITaskItem[] metadata)
        {
            var fileName = Path.GetFileName(entry);

            byte[] fileHeader = null;
            byte[] hash = null;
            byte[] md5hash = null;
            byte[] buffer = new byte[1024];
            bool isAscii = true;

            var fileMetadata = metadata.SingleOrDefault(m => m.IsPublished() && string.Equals(relativePath, m.GetPublishedPath()));

            using (Stream fileStream = File.OpenRead(entry))
            {
                if (fileName.StartsWith("."))
                {
                    return;
                }

                if (fileStream.Length == 0)
                {
                    return;
                }

                using (var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                using (var md5hasher = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
                {
                    int read;

                    while (true)
                    {
                        read = fileStream.Read(buffer, 0, buffer.Length);

                        if (fileHeader == null)
                        {
                            fileHeader = new byte[read];
                            Buffer.BlockCopy(buffer, 0, fileHeader, 0, read);
                        }

                        hasher.AppendData(buffer, 0, read);
                        md5hasher.AppendData(buffer, 0, read);
                        isAscii = isAscii && buffer.All(c => c < 128);

                        if (read < buffer.Length)
                        {
                            break;
                        }
                    }

                    hash = hasher.GetHashAndReset();
                    md5hash = md5hasher.GetHashAndReset();
                }

                ArchiveEntryType entryType = ArchiveEntryType.None;

                var mode = LinuxFileMode.S_IROTH | LinuxFileMode.S_IRGRP | LinuxFileMode.S_IRUSR | LinuxFileMode.S_IFREG;

                if (entryType == ArchiveEntryType.Executable32 || entryType == ArchiveEntryType.Executable64)
                {
                    mode |= LinuxFileMode.S_IXOTH | LinuxFileMode.S_IXGRP | LinuxFileMode.S_IWUSR | LinuxFileMode.S_IXUSR;
                }

                string name = fileMetadata?.GetLinuxPath();

                if (name == null)
                {
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        name = prefix + "/" + fileName;
                    }
                    else
                    {
                        name = fileName;
                    }
                }

                string linkTo = string.Empty;

                if (mode.HasFlag(LinuxFileMode.S_IFLNK))
                {
                    int stringEnd = 0;

                    while (stringEnd < fileHeader.Length - 1 && fileHeader[stringEnd] != 0)
                    {
                        stringEnd++;
                    }

                    linkTo = Encoding.UTF8.GetString(fileHeader, 0, stringEnd + 1);
                    hash = new byte[] { };
                }

                mode = GetFileMode(name, fileMetadata, mode);

                ArchiveEntry archiveEntry = new ArchiveEntry()
                {
                    FileSize = (uint)fileStream.Length,
                    Group = fileMetadata.GetGroup(),
                    Owner = fileMetadata.GetOwner(),
                    Modified = File.GetLastWriteTimeUtc(entry),
                    SourceFilename = entry,
                    TargetPath = name,
                    Sha256 = hash,
                    Md5Hash = md5hash,
                    Type = entryType,
                    LinkTo = linkTo,
                    Inode = inode++,
                    IsAscii = isAscii,
                    Mode = mode
                };

                value.Add(archiveEntry);
            }
        }

        private LinuxFileMode GetFileMode(string name, ITaskItem metadata, LinuxFileMode defaultMode)
        {
            LinuxFileMode mode = defaultMode;
            LinuxFileMode defaultFileTypeMask = defaultMode & LinuxFileMode.FileTypeMask;

            var overridenFileMode = metadata?.GetLinuxFileMode();

            if (overridenFileMode != null)
            {
                try
                {
                    mode = (LinuxFileMode)Convert.ToUInt32(overridenFileMode, 8);

                    LinuxFileMode fileType = mode & LinuxFileMode.FileTypeMask;

                    if (fileType != LinuxFileMode.None && fileType != defaultFileTypeMask)
                    {
                        //
                    }

                    mode = (mode & ~LinuxFileMode.FileTypeMask) | defaultFileTypeMask;
                }
                catch (Exception)
                {
                    throw new Exception($"Could not parse the file mode '{overridenFileMode}' for file '{name}'. Make sure to set the LinuxFileMode attriubute to an octal representation of a Unix file mode.");
                }
            }

            return mode;
        }
    }
}

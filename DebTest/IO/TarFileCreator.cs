using System.Text;

namespace Packaging.Targets.IO
{
    internal static class TarFileCreator
    {
        public static void FromArchiveEntries(List<ArchiveEntry> archiveEntries, Stream targetStream)
        {
            using (TarFile cpioFile = new TarFile(targetStream, leaveOpen: true))
            {
                foreach (var entry in archiveEntries)
                {
                    WriteEntry(targetStream, entry);
                }

                WriteTrailer(targetStream);
            }
        }

        public static void WriteTrailer(Stream stream)
        {
            if (stream.CanSeek)
            {
                Align(stream);
            }

            var trailer = new byte[1024];
            stream.Write(trailer, 0, trailer.Length);
        }

        public static void WriteEntry(Stream stream, TarHeader header, Stream data)
        {
            header.Checksum = header.ComputeChecksum();
            int written = stream.WriteStruct(header);
            Align(stream, written);
            data.CopyTo(stream);
            Align(stream, data.Length);
        }

        public static void WriteEntry(Stream stream, ArchiveEntry entry, Stream data = null)
        {
            var targetPath = entry.TargetPath;
            if (!targetPath.StartsWith("."))
            {
                targetPath = "." + targetPath;
            }

            if (targetPath.Length > 99)
            {
                var nameLength = Encoding.UTF8.GetByteCount(targetPath);
                byte[] entryName = new byte[nameLength + 1];

                Encoding.UTF8.GetBytes(targetPath, 0, targetPath.Length, entryName, 0);

                ArchiveEntry nameEntry = new ArchiveEntry()
                {
                    Mode = entry.Mode,
                    Modified = entry.Modified,
                    TargetPath = "././@LongLink",
                    Owner = entry.Owner,
                    Group = entry.Group
                };

                using (MemoryStream nameStream = new MemoryStream(entryName))
                {
                    WriteEntry(stream, nameEntry, nameStream);
                }

                targetPath = targetPath.Substring(0, 99);
            }

            var isDir = entry.Mode.HasFlag(LinuxFileMode.S_IFDIR);
            var isLink = !isDir && !string.IsNullOrWhiteSpace(entry.LinkTo);
            var isFile = !isDir && !isLink;

            TarTypeFlag type;

            if (entry.TargetPath == "././@LongLink")
            {
                type = TarTypeFlag.LongName;
            }
            else if (isFile)
            {
                type = TarTypeFlag.RegType;
            }
            else if (isDir)
            {
                type = TarTypeFlag.DirType;
            }
            else
            {
                type = TarTypeFlag.SymType;
            }

            bool dispose = false;
            if (data == null)
            {
                if (isFile)
                {
                    dispose = true;
                    data = File.OpenRead(entry.SourceFilename);
                }
                else
                {
                    data = new MemoryStream();
                }
            }

            try
            {
                var hdr = new TarHeader()
                {
                    FileMode = entry.Mode & LinuxFileMode.PermissionsMask,
                    DevMajor = null,
                    DevMinor = null,
                    FileName = targetPath,
                    FileSize = (uint)data.Length,
                    GroupId = 0,
                    UserId = 0,
                    GroupName = entry.Group,
                    LinkName = isLink ? entry.LinkTo : string.Empty,
                    Prefix = string.Empty,
                    TypeFlag = type,
                    UserName = entry.Owner,
                    Version = null,
                    LastModified = entry.Modified,
                    Magic = "ustar"
                };
                WriteEntry(stream, hdr, data);
            }
            finally
            {
                if (dispose)
                {
                    data.Dispose();
                }
            }
        }

        private static void Align(Stream stream)
        {
            Align(stream, stream.Position);
        }

        private static void Align(Stream stream, long position)
        {
            var spos = position % 512;
            if (spos == 0)
            {
                return;
            }

            var align = new byte[512 - spos];
            stream.Write(align, 0, align.Length);
        }
    }
}
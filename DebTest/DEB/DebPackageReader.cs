using Packaging.Targets.IO;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Packaging.Targets.Deb
{
    internal static class DebPackageReader
    {
        internal static DebPackage Read(Stream stream)
        {
            DebPackage package = new DebPackage();
            using (ArFile archive = new ArFile(stream, leaveOpen: true))
            {
                while (archive.Read())
                {
                    if (archive.FileName == "debian-binary")
                    {
                        ReadDebianBinary(archive, package);
                    }
                    else if (archive.FileName == "control.tar.gz")
                    {
                        ReadControlArchive(archive, package);
                    }
                }
            }

            return package;
        }
        
        internal static void unTAR(Stream streamTar, string directoryPath)
        {
            var reader = ReaderFactory.Open(streamTar);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    ExtractionOptions opt = new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    };
                    Directory.CreateDirectory(directoryPath);
                    reader.WriteEntryToDirectory(directoryPath, opt);
                }
            }
        }

        internal static Stream GetPayloadStream(Stream stream)
        {
            using (ArFile archive = new ArFile(stream, leaveOpen: true))
            {
                while (archive.Read())
                {
                    if (archive.FileName.StartsWith("data.tar."))
                    {
                        var ext = Path.GetExtension(archive.FileName);
                        if (ext == ".gz")
                        {
                            return new GZipDecompressor(archive.Open(), false);
                        }

                        if (ext == ".xz")
                        {
                            var payload = new MemoryStream();
                            using (var xz = new XZInputStream(archive.Open()))
                            {
                                xz.CopyTo(payload);
                                payload.Seek(0, SeekOrigin.Begin);
                                return payload;
                            }
                        }

                        throw new InvalidDataException("Don't know how to decompress " + archive.FileName);
                    }
                }

                throw new InvalidDataException("data.tar.?? not found");
            }
        }

        private static void ReadDebianBinary(ArFile archive, DebPackage package)
        {
            using (Stream stream = archive.Open())
            using (StreamReader reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();
                package.PackageFormatVersion = new Version(content);
            }
        }

        private static void ReadControlArchive(ArFile archive, DebPackage package)
        {
            package.ControlExtras = new Dictionary<string, DebPackageControlFileData>();
            package.Md5Sums = new Dictionary<string, string>();
            using (Stream stream = archive.Open())
            using (GZipDecompressor decompressedStream = new GZipDecompressor(stream, leaveOpen: true))
            using (TarFile tarFile = new TarFile(decompressedStream, leaveOpen: true))
            {
                while (tarFile.Read())
                {
                    switch (tarFile.FileName)
                    {
                        case "./control":
                            using (Stream controlFile = tarFile.Open())
                            {
                                package.ControlFile = ControlFileParser.Read(controlFile);
                            }

                            break;
                        case "./md5sums":
                            using (var sums = new StreamReader(tarFile.Open()))
                            {
                                string line;
                                while ((line = sums.ReadLine()) != null)
                                {
                                    var s = line.Split(new[] { "  " }, 2, StringSplitOptions.None);
                                    package.Md5Sums[s[1]] = s[0];
                                }
                            }

                            break;
                        case "./preinst":
                            package.PreInstallScript = tarFile.ReadAsUtf8String();
                            break;
                        case "./postinst":
                            package.PostInstallScript = tarFile.ReadAsUtf8String();
                            break;
                        case "./prerm":
                            package.PreRemoveScript = tarFile.ReadAsUtf8String();
                            break;
                        case "./postrm":
                            package.PostRemoveScript = tarFile.ReadAsUtf8String();
                            break;

                        case "./":
                            tarFile.Skip();
                            break;
                        default:
                            package.ControlExtras[tarFile.FileName] = new DebPackageControlFileData
                            {
                                Mode = tarFile.FileHeader.FileMode,
                                Contents = tarFile.ReadAsUtf8String()
                            };
                            break;
                    }
                }
            }
        }
    }
}

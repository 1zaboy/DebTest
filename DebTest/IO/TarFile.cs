using System;
using System.IO;

namespace Packaging.Targets.IO
{
    public class TarFile : ArchiveFile
    {
        private TarHeader entryHeader;
        public TarFile(Stream stream, bool leaveOpen)
            : base(stream, leaveOpen)
        {
        }

        public string LinkName
        {
            get;
            private set;
        }

        public override bool Read()
        {
            Align(512);
            EntryStream?.Dispose();
            EntryStream = null;

            entryHeader = Stream.ReadStruct<TarHeader>();
            FileHeader = entryHeader;
            FileName = entryHeader.FileName;
            LinkName = entryHeader.LinkName;

            if (string.IsNullOrEmpty(entryHeader.Magic))
            {
                return false;
            }

            if (entryHeader.Magic != "ustar")
            {
                throw new InvalidDataException("The magic for the file entry is invalid");
            }

            Align(512);

            // TODO: Validate Checksum
            EntryStream = new SubStream(Stream, Stream.Position, entryHeader.FileSize, leaveParentOpen: true);

            if (entryHeader.TypeFlag == TarTypeFlag.LongName)
            {
                var longFileName = this.ReadAsUtf8String();
                var read = Read();
                FileName = longFileName;

                return read;
            }
            else if (entryHeader.TypeFlag == TarTypeFlag.LongLink)
            {
                var longLinkName = this.ReadAsUtf8String();
                var read = Read();
                LinkName = longLinkName;
                return read;
            }
            else
            {
                return true;
            }
        }
    }
}

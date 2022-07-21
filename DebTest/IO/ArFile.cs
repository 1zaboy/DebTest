using System.Text;

namespace Packaging.Targets.IO
{
    public class ArFile : ArchiveFile
    {
        private const string Magic = "!<arch>\n";
        private bool magicRead;
        private ArHeader entryHeader;
        public ArFile(Stream stream, bool leaveOpen)
            : base(stream, leaveOpen)
        {
        }

        public override bool Read()
        {
            EnsureMagicRead();

            if (EntryStream != null)
            {
                EntryStream.Dispose();
            }

            Align(2);

            if (Stream.Position == Stream.Length)
            {
                return false;
            }

            entryHeader = Stream.ReadStruct<ArHeader>();
            FileHeader = entryHeader;
            FileName = entryHeader.FileName;

            if (entryHeader.EndChar != "`\n")
            {
                throw new InvalidDataException("The magic for the file entry is invalid");
            }

            EntryStream = new SubStream(Stream, Stream.Position, entryHeader.FileSize, leaveParentOpen: true);

            return true;
        }

        protected void EnsureMagicRead()
        {
            if (!magicRead)
            {
                byte[] buffer = new byte[Magic.Length];
                Stream.Read(buffer, 0, buffer.Length);
                var magic = Encoding.ASCII.GetString(buffer);

                if (!string.Equals(magic, Magic, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("The .ar file did not start with the expected magic");
                }

                magicRead = true;
            }
        }
    }
}

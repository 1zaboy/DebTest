using System;
using System.IO;

namespace Packaging.Targets.IO
{
    public abstract class ArchiveFile : IDisposable
    {
        private readonly Stream stream;
        private readonly bool leaveOpen;
        private bool disposed;

        public ArchiveFile(Stream stream, bool leaveOpen)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            this.stream = stream;
            this.leaveOpen = leaveOpen;
        }

        public SubStream EntryStream
        {
            get;
            protected set;
        }

        public string FileName
        {
            get;
            protected set;
        }

        public IArchiveHeader FileHeader
        {
            get;
            protected set;
        }

        protected Stream Stream
        {
            get
            {
                EnsureNotDisposed();
                return stream;
            }
        }

        public static int PaddingSize(int multiple, int value)
        {
            if (value % multiple == 0)
            {
                return 0;
            }
            else
            {
                return multiple - value % multiple;
            }
        }

        public Stream Open()
        {
            return EntryStream;
        }

        public void Dispose()
        {
            if (!leaveOpen)
            {
                Stream.Dispose();
            }

            disposed = true;
        }

        public abstract bool Read();

        public void Skip()
        {
            byte[] buffer = new byte[60 * 1024];

            while (EntryStream.Read(buffer, 0, buffer.Length) > 0)
            {
                // Keep reading until we're at the end of the stream.
            }
        }

        protected void EnsureNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        protected void Align(int alignmentBase)
        {
            var currentIndex =
                (int)(EntryStream != null ? (EntryStream.Offset + EntryStream.Length) : Stream.Position);

            if (Stream.CanSeek)
            {
                Stream.Seek(currentIndex + PaddingSize(alignmentBase, currentIndex), SeekOrigin.Begin);
            }
            else
            {
                byte[] buffer = new byte[PaddingSize(alignmentBase, currentIndex)];
                Stream.Read(buffer, 0, buffer.Length);
            }
        }
    }
}

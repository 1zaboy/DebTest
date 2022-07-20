using System;
using System.IO;
using System.IO.Compression;

namespace Packaging.Targets.IO
{
    internal class GZipDecompressor : GZipStream
    {
        private long position = 0;

        public GZipDecompressor(Stream stream, bool leaveOpen)
            : base(stream, CompressionMode.Decompress, leaveOpen)
        {
        }

        public override long Position
        {
            get { return this.position; }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] array, int offset, int count)
        {
            var read = base.Read(array, offset, count);
            this.position += read;
            return read;
        }
    }
}

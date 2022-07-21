namespace Packaging.Targets.IO
{
    public class SubStream : Stream
    {
        private Stream stream;
        private long subStreamOffset;
        private long subStreamLength;
        private bool leaveParentOpen;
        private bool readOnly;
        private long position;

        public SubStream(Stream stream, long offset, long length, bool leaveParentOpen = false, bool readOnly = false)
        {
            this.stream = stream;
            subStreamOffset = offset;
            subStreamLength = length;
            this.leaveParentOpen = leaveParentOpen;
            this.readOnly = readOnly;
            position = 0;

            if (this.stream.CanSeek)
            {
                Seek(0, SeekOrigin.Begin);
            }
        }

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => !readOnly && stream.CanWrite;

        public override long Length => subStreamLength;

        public override long Position
        {
            get => position;

            set
            {
                lock (stream)
                {
                    stream.Position = value + Offset;
                    position = value;
                }
            }
        }

        internal Stream Stream => stream;

        internal long Offset => subStreamOffset;

        public override void Flush()
        {
            lock (stream)
            {
                stream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (stream)
            {
                EnsurePosition();

                // Make sure we don't pass the size of the substream
                long bytesRemaining = Length - Position;
                long bytesToRead = Math.Min(count, bytesRemaining);

                if (bytesToRead < 0)
                {
                    bytesToRead = 0;
                }

                var read = stream.Read(buffer, offset, (int)bytesToRead);
                position += read;
                return read;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (readOnly)
            {
                throw new NotSupportedException();
            }

            lock (stream)
            {
                EnsurePosition();

                if (Position + offset + count > Length || Position < 0)
                {
                    throw new InvalidOperationException("This write operation would exceed the current length of the substream.");
                }

                stream.Write(buffer, offset, count);
                position += count;
            }
        }

        public override void WriteByte(byte value)
        {
            if (readOnly)
            {
                throw new NotSupportedException();
            }

            lock (stream)
            {
                EnsurePosition();

                if (Position > Length || Position < 0)
                {
                    throw new InvalidOperationException("This write operation would exceed the current length of the substream.");
                }

                stream.WriteByte(value);
                position++;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (stream)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        offset += subStreamOffset;
                        break;

                    case SeekOrigin.End:
                        long enddelta = subStreamOffset + subStreamLength - stream.Length;
                        offset += enddelta;
                        break;

                    case SeekOrigin.Current:
                        break;
                }

                if (origin == SeekOrigin.Current)
                {
                    EnsurePosition();
                }

                var parentPosition = stream.Seek(offset, origin);
                position = parentPosition - Offset;
                return position;
            }
        }

        public override void SetLength(long value)
        {
            if (readOnly)
            {
                throw new NotSupportedException();
            }

            subStreamLength = value;
        }

        protected override void Dispose(bool disposing)
        {
            if (!leaveParentOpen)
            {
                stream.Dispose();
            }

            base.Dispose(disposing);
        }

        private void EnsurePosition()
        {
            if (stream.Position != position + Offset)
            {
                stream.Position = position + Offset;
            }
        }
    }
}

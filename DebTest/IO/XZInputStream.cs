using System.Runtime.InteropServices;

namespace Packaging.Targets.IO
{
    public class XZInputStream : Stream
    {
        private const int BufSize = 512;

        private readonly List<byte> internalBuffer = new List<byte>();
        private readonly Stream innerStream;
        private readonly IntPtr inbuf;
        private readonly IntPtr outbuf;
        private LzmaStream lzmaStream;
        private long length;
        private long position;
        private bool disposed;

        public XZInputStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            this.innerStream = stream;

            var ret = NativeMethods.lzma_stream_decoder(ref this.lzmaStream, ulong.MaxValue, LzmaDecodeFlags.Concatenated);

            this.inbuf = Marshal.AllocHGlobal(BufSize);
            this.outbuf = Marshal.AllocHGlobal(BufSize);

            this.lzmaStream.AvailIn = 0;
            this.lzmaStream.NextOut = this.outbuf;
            this.lzmaStream.AvailOut = BufSize;

            if (ret == LzmaResult.OK)
            {
                return;
            }

            switch (ret)
            {
                case LzmaResult.MemError:
                    throw new Exception("Memory allocation failed");

                case LzmaResult.OptionsError:
                    throw new Exception("Unsupported decompressor flags");

                default:
                    throw new Exception("Unknown error, possibly a bug");
            }
        }

        public override bool CanRead
        {
            get
            {
                this.EnsureNotDisposed();
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                this.EnsureNotDisposed();
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                this.EnsureNotDisposed();
                return false;
            }
        }

        public override long Length
        {
            get
            {
                this.EnsureNotDisposed();

                const int streamFooterSize = 12;

                if (this.length == 0)
                {
                    var lzmaStreamFlags = default(LzmaStreamFlags);
                    var streamFooter = new byte[streamFooterSize];

                    this.innerStream.Seek(-streamFooterSize, SeekOrigin.End);
                    this.innerStream.Read(streamFooter, 0, streamFooterSize);

                    NativeMethods.lzma_stream_footer_decode(ref lzmaStreamFlags, streamFooter);
                    var indexPointer = new byte[lzmaStreamFlags.BackwardSize];

                    this.innerStream.Seek(-streamFooterSize - (long)lzmaStreamFlags.BackwardSize, SeekOrigin.End);
                    this.innerStream.Read(indexPointer, 0, (int)lzmaStreamFlags.BackwardSize);
                    this.innerStream.Seek(0, SeekOrigin.Begin);

                    var index = IntPtr.Zero;
                    var memLimit = ulong.MaxValue;
                    uint inPos = 0;

                    NativeMethods.lzma_index_buffer_decode(ref index, ref memLimit, IntPtr.Zero, indexPointer, ref inPos, lzmaStreamFlags.BackwardSize);

                    if (inPos != lzmaStreamFlags.BackwardSize)
                    {
                        NativeMethods.lzma_index_end(index, IntPtr.Zero);
                        throw new Exception("Index decoding failed!");
                    }

                    var uSize = NativeMethods.lzma_index_uncompressed_size(index);

                    NativeMethods.lzma_index_end(index, IntPtr.Zero);
                    this.length = (long)uSize;
                    return this.length;
                }
                else
                {
                    return this.length;
                }
            }
        }

        public override long Position
        {
            get
            {
                this.EnsureNotDisposed();
                return this.position;
            }

            set
            {
                this.EnsureNotDisposed();
                throw new NotSupportedException("XZ Stream does not support setting position");
            }
        }

        public override void Flush()
        {
            this.EnsureNotDisposed();

            throw new NotSupportedException("XZ Stream does not support flush");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            this.EnsureNotDisposed();

            throw new NotSupportedException("XZ Stream does not support seek");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("XZ Stream does not support setting length");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            this.EnsureNotDisposed();

            var action = LzmaAction.Run;

            var readBuf = new byte[BufSize];
            var outManagedBuf = new byte[BufSize];

            while (this.internalBuffer.Count < count)
            {
                if (this.lzmaStream.AvailIn == 0)
                {
                    this.lzmaStream.AvailIn = (uint)this.innerStream.Read(readBuf, 0, readBuf.Length);
                    Marshal.Copy(readBuf, 0, this.inbuf, (int)this.lzmaStream.AvailIn);
                    this.lzmaStream.NextIn = this.inbuf;

                    if (this.lzmaStream.AvailIn == 0)
                    {
                        action = LzmaAction.Finish;
                    }
                }

                var ret = NativeMethods.lzma_code(ref this.lzmaStream, action);

                if (this.lzmaStream.AvailOut == 0 || ret == LzmaResult.StreamEnd)
                {
                    var writeSize = BufSize - (int)this.lzmaStream.AvailOut;
                    Marshal.Copy(this.outbuf, outManagedBuf, 0, writeSize);

                    this.internalBuffer.AddRange(outManagedBuf);
                    var tail = outManagedBuf.Length - writeSize;
                    this.internalBuffer.RemoveRange(this.internalBuffer.Count - tail, tail);

                    this.lzmaStream.NextOut = this.outbuf;
                    this.lzmaStream.AvailOut = BufSize;
                }

                if (ret != LzmaResult.OK)
                {
                    if (ret == LzmaResult.StreamEnd)
                    {
                        break;
                    }

                    NativeMethods.lzma_end(ref this.lzmaStream);

                    switch (ret)
                    {
                        case LzmaResult.MemError:
                            throw new Exception("Memory allocation failed");

                        case LzmaResult.FormatError:
                            throw new Exception("The input is not in the .xz format");

                        case LzmaResult.OptionsError:
                            throw new Exception("Unsupported compression options");

                        case LzmaResult.DataError:
                            throw new Exception("Compressed file is corrupt");

                        case LzmaResult.BufferError:
                            throw new Exception("Compressed file is truncated or otherwise corrupt");

                        default:
                            throw new Exception("Unknown error.Possibly a bug");
                    }
                }
            }

            if (this.internalBuffer.Count >= count)
            {
                this.internalBuffer.CopyTo(0, buffer, offset, count);
                this.internalBuffer.RemoveRange(0, count);
                this.position += count;
                return count;
            }
            else
            {
                var intBufLength = this.internalBuffer.Count;
                this.internalBuffer.CopyTo(0, buffer, offset, intBufLength);
                this.internalBuffer.Clear();
                this.position += intBufLength;
                return intBufLength;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("XZ Input stream does not support writing");
        }

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            NativeMethods.lzma_end(ref this.lzmaStream);

            Marshal.FreeHGlobal(this.inbuf);
            Marshal.FreeHGlobal(this.outbuf);

            base.Dispose(disposing);

            this.disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(XZInputStream));
            }
        }
    }
}
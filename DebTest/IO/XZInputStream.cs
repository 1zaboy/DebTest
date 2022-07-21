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

            innerStream = stream;

            var ret = NativeMethods.lzma_stream_decoder(ref lzmaStream, ulong.MaxValue, LzmaDecodeFlags.Concatenated);

            inbuf = Marshal.AllocHGlobal(BufSize);
            outbuf = Marshal.AllocHGlobal(BufSize);

            lzmaStream.AvailIn = 0;
            lzmaStream.NextOut = outbuf;
            lzmaStream.AvailOut = BufSize;

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
                EnsureNotDisposed();
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                EnsureNotDisposed();
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                EnsureNotDisposed();
                return false;
            }
        }

        public override long Length
        {
            get
            {
                EnsureNotDisposed();

                const int streamFooterSize = 12;

                if (length == 0)
                {
                    var lzmaStreamFlags = default(LzmaStreamFlags);
                    var streamFooter = new byte[streamFooterSize];

                    innerStream.Seek(-streamFooterSize, SeekOrigin.End);
                    innerStream.Read(streamFooter, 0, streamFooterSize);

                    NativeMethods.lzma_stream_footer_decode(ref lzmaStreamFlags, streamFooter);
                    var indexPointer = new byte[lzmaStreamFlags.BackwardSize];

                    innerStream.Seek(-streamFooterSize - (long)lzmaStreamFlags.BackwardSize, SeekOrigin.End);
                    innerStream.Read(indexPointer, 0, (int)lzmaStreamFlags.BackwardSize);
                    innerStream.Seek(0, SeekOrigin.Begin);

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
                    length = (long)uSize;
                    return length;
                }
                else
                {
                    return length;
                }
            }
        }

        public override long Position
        {
            get
            {
                EnsureNotDisposed();
                return position;
            }

            set
            {
                EnsureNotDisposed();
                throw new NotSupportedException("XZ Stream does not support setting position");
            }
        }

        public override void Flush()
        {
            EnsureNotDisposed();

            throw new NotSupportedException("XZ Stream does not support flush");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            EnsureNotDisposed();

            throw new NotSupportedException("XZ Stream does not support seek");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("XZ Stream does not support setting length");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            EnsureNotDisposed();

            var action = LzmaAction.Run;

            var readBuf = new byte[BufSize];
            var outManagedBuf = new byte[BufSize];

            while (internalBuffer.Count < count)
            {
                if (lzmaStream.AvailIn == 0)
                {
                    lzmaStream.AvailIn = (uint)innerStream.Read(readBuf, 0, readBuf.Length);
                    Marshal.Copy(readBuf, 0, inbuf, (int)lzmaStream.AvailIn);
                    lzmaStream.NextIn = inbuf;

                    if (lzmaStream.AvailIn == 0)
                    {
                        action = LzmaAction.Finish;
                    }
                }

                var ret = NativeMethods.lzma_code(ref lzmaStream, action);

                if (lzmaStream.AvailOut == 0 || ret == LzmaResult.StreamEnd)
                {
                    var writeSize = BufSize - (int)lzmaStream.AvailOut;
                    Marshal.Copy(outbuf, outManagedBuf, 0, writeSize);

                    internalBuffer.AddRange(outManagedBuf);
                    var tail = outManagedBuf.Length - writeSize;
                    internalBuffer.RemoveRange(internalBuffer.Count - tail, tail);

                    lzmaStream.NextOut = outbuf;
                    lzmaStream.AvailOut = BufSize;
                }

                if (ret != LzmaResult.OK)
                {
                    if (ret == LzmaResult.StreamEnd)
                    {
                        break;
                    }

                    NativeMethods.lzma_end(ref lzmaStream);

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

            if (internalBuffer.Count >= count)
            {
                internalBuffer.CopyTo(0, buffer, offset, count);
                internalBuffer.RemoveRange(0, count);
                position += count;
                return count;
            }
            else
            {
                var intBufLength = internalBuffer.Count;
                internalBuffer.CopyTo(0, buffer, offset, intBufLength);
                internalBuffer.Clear();
                position += intBufLength;
                return intBufLength;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("XZ Input stream does not support writing");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            NativeMethods.lzma_end(ref lzmaStream);

            Marshal.FreeHGlobal(inbuf);
            Marshal.FreeHGlobal(outbuf);

            base.Dispose(disposing);

            disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(XZInputStream));
            }
        }
    }
}
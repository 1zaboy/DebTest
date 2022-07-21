using System.Diagnostics;

namespace Packaging.Targets.IO
{
    public unsafe class XZOutputStream : Stream
    {
        public const uint DefaultPreset = 6;
        public const uint PresetExtremeFlag = (uint)1 << 31;

        private const int BufSize = 4096;

        private readonly Stream innerStream;
        private readonly bool leaveOpen;
        private readonly byte[] outbuf;
        private LzmaStream lzmaStream;
        private bool disposed;

        public XZOutputStream(Stream s)
            : this(s, DefaultThreads)
        {
        }

        public XZOutputStream(Stream s, int threads)
            : this(s, threads, DefaultPreset)
        {
        }

        public XZOutputStream(Stream s, int threads, uint preset)
            : this(s, threads, preset, false)
        {
        }

        public XZOutputStream(Stream s, int threads, uint preset, bool leaveOpen)
        {
            innerStream = s;
            this.leaveOpen = leaveOpen;

            LzmaResult ret;
            if (threads == 1 || !NativeMethods.SupportsMultiThreading)
            {
                ret = NativeMethods.lzma_easy_encoder(ref lzmaStream, preset, LzmaCheck.Crc64);
            }
            else
            {
                if (threads <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(threads));
                }

                if (threads > Environment.ProcessorCount)
                {
                    Trace.TraceWarning("{0} threads required, but only {1} processors available", threads, Environment.ProcessorCount);
                    threads = Environment.ProcessorCount;
                }

                var mt = new LzmaMT()
                {
                    preset = preset,
                    check = LzmaCheck.Crc64,
                    threads = (uint)threads
                };
                ret = NativeMethods.lzma_stream_encoder_mt(ref lzmaStream, ref mt);
            }

            if (ret == LzmaResult.OK)
            {
                outbuf = new byte[BufSize];
                lzmaStream.AvailOut = BufSize;
                return;
            }

            GC.SuppressFinalize(this);
            throw GetError(ret);
        }

        ~XZOutputStream()
        {
            Dispose(false);
        }

        public static int DefaultThreads => Environment.ProcessorCount;

        public static bool SupportsMultiThreading => NativeMethods.SupportsMultiThreading;

        /// <inheritdoc/>
        public override bool CanRead
        {
            get
            {
                EnsureNotDisposed();
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool CanSeek
        {
            get
            {
                EnsureNotDisposed();
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool CanWrite
        {
            get
            {
                EnsureNotDisposed();
                return true;
            }
        }

        /// <inheritdoc/>
        public override long Length
        {
            get
            {
                EnsureNotDisposed();
                throw new NotSupportedException();
            }
        }

        /// <inheritdoc/>
        public override long Position
        {
            get
            {
                EnsureNotDisposed();
                throw new NotSupportedException();
            }

            set
            {
                EnsureNotDisposed();
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Single-call buffer encoding
        /// </summary>
        public static byte[] Encode(byte[] buffer, uint preset = DefaultPreset)
        {
            var res = new byte[(long)NativeMethods.lzma_stream_buffer_bound((UIntPtr)buffer.Length)];

            UIntPtr outPos;
            var ret = NativeMethods.lzma_easy_buffer_encode(preset, LzmaCheck.Crc64, null, buffer, (UIntPtr)buffer.Length, res, &outPos, (UIntPtr)res.Length);
            if (ret != LzmaResult.OK)
            {
                throw GetError(ret);
            }

            if ((long)outPos < res.Length)
            {
                Array.Resize(ref res, (int)(ulong)outPos);
            }

            return res;
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            EnsureNotDisposed();
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            EnsureNotDisposed();
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            EnsureNotDisposed();
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureNotDisposed();

            if (count == 0)
            {
                return;
            }

            var guard = buffer[checked((uint)offset + (uint)count) - 1];

            if (lzmaStream.AvailIn != 0)
            {
                throw new InvalidOperationException();
            }

            lzmaStream.AvailIn = (uint)count;
            do
            {
                LzmaResult ret;
                fixed (byte* inbuf = &buffer[offset])
                {
                    lzmaStream.NextIn = (IntPtr)inbuf;
                    fixed (byte* outbuf = &this.outbuf[BufSize - lzmaStream.AvailOut])
                    {
                        lzmaStream.NextOut = (IntPtr)outbuf;
                        ret = NativeMethods.lzma_code(ref lzmaStream, LzmaAction.Run);
                    }

                    offset += (int)((ulong)lzmaStream.NextIn - (ulong)(IntPtr)inbuf);
                }

                if (ret != LzmaResult.OK)
                {
                    throw ThrowError(ret);
                }

                if (lzmaStream.AvailOut == 0)
                {
                    innerStream.Write(outbuf, 0, BufSize);
                    lzmaStream.AvailOut = BufSize;
                }
            }
            while (lzmaStream.AvailIn != 0);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            // finish encoding only if all input has been successfully processed
            if (lzmaStream.InternalState != IntPtr.Zero && lzmaStream.AvailIn == 0)
            {
                LzmaResult ret;
                do
                {
                    fixed (byte* outbuf = &this.outbuf[BufSize - (int)lzmaStream.AvailOut])
                    {
                        lzmaStream.NextOut = (IntPtr)outbuf;
                        ret = NativeMethods.lzma_code(ref lzmaStream, LzmaAction.Finish);
                    }

                    if (ret > LzmaResult.StreamEnd)
                    {
                        throw ThrowError(ret);
                    }

                    var writeSize = BufSize - (int)lzmaStream.AvailOut;
                    if (writeSize != 0)
                    {
                        innerStream.Write(outbuf, 0, writeSize);
                        lzmaStream.AvailOut = BufSize;
                    }
                }
                while (ret != LzmaResult.StreamEnd);
            }

            NativeMethods.lzma_end(ref lzmaStream);

            if (disposing && !leaveOpen)
            {
                innerStream?.Dispose();
            }

            base.Dispose(disposing);

            disposed = true;
        }

        private static Exception GetError(LzmaResult ret)
        {
            switch (ret)
            {
                case LzmaResult.MemError: return new OutOfMemoryException("Memory allocation failed");
                case LzmaResult.OptionsError: return new ArgumentException("Specified preset is not supported");
                case LzmaResult.UnsupportedCheck: return new Exception("Specified integrity check is not supported");
                case LzmaResult.DataError: return new InvalidDataException("File size limits exceeded");
                default: return new Exception("Unknown error, possibly a bug: " + ret);
            }
        }

        /// <summary>
        /// Throws an exception if this stream is disposed of.
        /// </summary>
        private void EnsureNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(XZOutputStream));
            }
        }

        private Exception ThrowError(LzmaResult ret)
        {
            NativeMethods.lzma_end(ref lzmaStream);
            return GetError(ret);
        }
    }
}
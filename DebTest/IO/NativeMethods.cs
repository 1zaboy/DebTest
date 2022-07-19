using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using FunctionLoader = Packaging.Targets.Native.FunctionLoader;

namespace Packaging.Targets.IO
{
    internal static class NativeMethods
    {
        private const string LibraryName = @"lzma";

        private static lzma_stream_decoder_delegate lzma_stream_decoder_ptr;
        private static lzma_code_delegate lzma_code_ptr;
        private static lzma_stream_footer_decode_delegate lzma_stream_footer_decode_ptr;
        private static lzma_index_uncompressed_size_delegate lzma_index_uncompressed_size_ptr;
        private static lzma_index_buffer_decode_delegate lzma_index_buffer_decode_ptr;
        private static lzma_index_end_delegate lzma_index_end_ptr;
        private static lzma_end_delegate lzma_end_ptr;
        private static lzma_easy_encoder_delegate lzma_easy_encoder_ptr;
        private static lzma_stream_encoder_mt_delegate lzma_stream_encoder_mt_ptr;
        private static lzma_stream_buffer_bound_delegate lzma_stream_buffer_bound_ptr;
        private static lzma_easy_buffer_encode_delegate lzma_easy_buffer_encode_ptr;

        static NativeMethods()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (RuntimeInformation.OSArchitecture != Architecture.X64)
                {
                    throw new InvalidOperationException(".NET packaging only supports 64-bit Windows processes");
                }
            }

            var libraryPath = Path.GetDirectoryName(typeof(NativeMethods).GetTypeInfo().Assembly.Location);
            var lzmaWindowsPath = Path.GetFullPath(Path.Combine(libraryPath, "../../runtimes/win7-x64/native/lzma.dll"));

            IntPtr library = FunctionLoader.LoadNativeLibrary(
                new string[] { lzmaWindowsPath, "lzma.dll" }, // lzma.dll is used when running unit tests.
                new string[] { "liblzma.so.5", "liblzma.so" },
                new string[] { "liblzma.dylib" });

            if (library == IntPtr.Zero)
            {
                throw new FileLoadException("Could not load liblzma. On Linux, make sure you've installed liblzma-dev or an equivalent package.");
            }

            lzma_stream_decoder_ptr = FunctionLoader.LoadFunctionDelegate<lzma_stream_decoder_delegate>(library, nameof(lzma_stream_decoder));
            lzma_code_ptr = FunctionLoader.LoadFunctionDelegate<lzma_code_delegate>(library, nameof(lzma_code));
            lzma_stream_footer_decode_ptr = FunctionLoader.LoadFunctionDelegate<lzma_stream_footer_decode_delegate>(library, nameof(lzma_stream_footer_decode));
            lzma_index_uncompressed_size_ptr = FunctionLoader.LoadFunctionDelegate<lzma_index_uncompressed_size_delegate>(library, nameof(lzma_index_uncompressed_size));
            lzma_index_buffer_decode_ptr = FunctionLoader.LoadFunctionDelegate<lzma_index_buffer_decode_delegate>(library, nameof(lzma_index_buffer_decode));
            lzma_index_end_ptr = FunctionLoader.LoadFunctionDelegate<lzma_index_end_delegate>(library, nameof(lzma_index_end));
            lzma_end_ptr = FunctionLoader.LoadFunctionDelegate<lzma_end_delegate>(library, nameof(lzma_end));
            lzma_easy_encoder_ptr = FunctionLoader.LoadFunctionDelegate<lzma_easy_encoder_delegate>(library, nameof(lzma_easy_encoder));
            lzma_stream_encoder_mt_ptr = FunctionLoader.LoadFunctionDelegate<lzma_stream_encoder_mt_delegate>(library, nameof(lzma_stream_encoder_mt), throwOnError: false);
            lzma_stream_buffer_bound_ptr = FunctionLoader.LoadFunctionDelegate<lzma_stream_buffer_bound_delegate>(library, nameof(lzma_stream_buffer_bound));
            lzma_easy_buffer_encode_ptr = FunctionLoader.LoadFunctionDelegate<lzma_easy_buffer_encode_delegate>(library, nameof(lzma_easy_buffer_encode));
        }

        private delegate LzmaResult lzma_stream_decoder_delegate(ref LzmaStream stream, ulong memLimit, LzmaDecodeFlags flags);

        private unsafe delegate LzmaResult lzma_easy_buffer_encode_delegate(uint preset, LzmaCheck check, void* allocator, byte[] @in, UIntPtr in_size, byte[] @out, UIntPtr* out_pos, UIntPtr out_size);

        private delegate UIntPtr lzma_stream_buffer_bound_delegate(UIntPtr uncompressed_size);

        private delegate LzmaResult lzma_stream_encoder_mt_delegate(ref LzmaStream stream, ref LzmaMT mt);

        private delegate LzmaResult lzma_easy_encoder_delegate(ref LzmaStream stream, uint preset, LzmaCheck check);

        private delegate void lzma_end_delegate(ref LzmaStream stream);

        private delegate void lzma_index_end_delegate(IntPtr i, IntPtr allocator);

        private delegate uint lzma_index_buffer_decode_delegate(ref IntPtr i, ref ulong memLimit, IntPtr allocator, byte[] indexBuffer, ref uint inPosition, ulong inSize);

        private delegate ulong lzma_index_uncompressed_size_delegate(IntPtr i);

        private delegate LzmaResult lzma_stream_footer_decode_delegate(ref LzmaStreamFlags options, byte[] inp);

        private delegate LzmaResult lzma_code_delegate(ref LzmaStream stream, LzmaAction action);

        public static bool SupportsMultiThreading
        {
            get { return lzma_stream_encoder_mt_ptr != null; }
        }

        public static LzmaResult lzma_stream_decoder(ref LzmaStream stream, ulong memLimit, LzmaDecodeFlags flags) => lzma_stream_decoder_ptr(ref stream, memLimit, flags);

        public static LzmaResult lzma_code(ref LzmaStream stream, LzmaAction action) => lzma_code_ptr(ref stream, action);

        public static LzmaResult lzma_stream_footer_decode(ref LzmaStreamFlags options, byte[] inp) => lzma_stream_footer_decode_ptr(ref options, inp);

        public static ulong lzma_index_uncompressed_size(IntPtr i) => lzma_index_uncompressed_size_ptr(i);

        public static uint lzma_index_buffer_decode(ref IntPtr i, ref ulong memLimit, IntPtr allocator, byte[] indexBuffer, ref uint inPosition, ulong inSize)
            => lzma_index_buffer_decode_ptr(ref i, ref memLimit, allocator, indexBuffer, ref inPosition, inSize);

        public static void lzma_index_end(IntPtr i, IntPtr allocator) => lzma_index_end_ptr(i, allocator);

        public static void lzma_end(ref LzmaStream stream) => lzma_end_ptr(ref stream);

        public static LzmaResult lzma_easy_encoder(ref LzmaStream stream, uint preset, LzmaCheck check) => lzma_easy_encoder_ptr(ref stream, preset, check);

        public static LzmaResult lzma_stream_encoder_mt(ref LzmaStream stream, ref LzmaMT mt)
        {
            if (SupportsMultiThreading)
            {
                return lzma_stream_encoder_mt_ptr(ref stream, ref mt);
            }
            else
            {
                throw new PlatformNotSupportedException("lzma_stream_encoder_mt is not supported on this platform. Check SupportsMultiThreading to see whether you can use this functionality.");
            }
        }

        public static UIntPtr lzma_stream_buffer_bound(UIntPtr uncompressed_size) => lzma_stream_buffer_bound_ptr(uncompressed_size);

        public static unsafe LzmaResult lzma_easy_buffer_encode(uint preset, LzmaCheck check, void* allocator, byte[] @in, UIntPtr in_size, byte[] @out, UIntPtr* out_pos, UIntPtr out_size)
            => lzma_easy_buffer_encode_ptr(preset, check, allocator, @in, in_size, @out, out_pos, out_size);
    }
}

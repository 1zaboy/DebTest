using System.Runtime.InteropServices;

namespace Packaging.Targets.Native
{
    internal static class FunctionLoader
    {
        public static IntPtr LoadNativeLibrary(IEnumerable<string> windowsNames, IEnumerable<string> linuxNames, IEnumerable<string> osxNames)
        {
            IntPtr lib = IntPtr.Zero;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                foreach (var name in linuxNames)
                {
                    lib = LinuxNativeMethods.dlopen(name, LinuxNativeMethods.RTLD_NOW);

                    if (lib != IntPtr.Zero)
                    {
                        break;
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                foreach (var name in osxNames)
                {
                    lib = MacNativeMethods.dlopen(name, MacNativeMethods.RTLD_NOW);

                    if (lib != IntPtr.Zero)
                    {
                        break;
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (var name in windowsNames)
                {
                    lib = WindowsNativeMethods.LoadLibrary(name);

                    if (lib != IntPtr.Zero)
                    {
                        break;
                    }
                }
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return lib;
        }

        public static T LoadFunctionDelegate<T>(IntPtr nativeLibraryHandle, string functionName, bool throwOnError = true)
            where T : class
        {
            IntPtr ptr = LoadFunctionPointer(nativeLibraryHandle, functionName);

            if (ptr == IntPtr.Zero)
            {
                if (throwOnError)
                {
#if NETSTANDARD2_0
                    throw new EntryPointNotFoundException($"Could not find the entrypoint for {functionName}");
#else
                    throw new Exception($"Could not find the entrypoint for {functionName}");
#endif
                }
                else
                {
                    return null;
                }
            }

            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }

        private static IntPtr LoadFunctionPointer(IntPtr nativeLibraryHandle, string functionName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return LinuxNativeMethods.dlsym(nativeLibraryHandle, functionName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return MacNativeMethods.dlsym(nativeLibraryHandle, functionName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return WindowsNativeMethods.GetProcAddress(nativeLibraryHandle, functionName);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}

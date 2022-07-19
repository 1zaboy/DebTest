using System;
using System.Runtime.InteropServices;

namespace Packaging.Targets.Native
{
    internal static class WindowsNativeMethods
    {
        private const string Kernel32 = "kernel32";

        [DllImport(Kernel32, CharSet = CharSet.Ansi, BestFitMapping = false)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport(Kernel32, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string dllToLoad);
    }
}

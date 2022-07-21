using System.Text;

namespace Packaging.Targets.IO
{
    internal static class Extensions
    {
        public static string ReadAsUtf8String(this TarFile file)
        {
            using (var stream = file.Open())
            {
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                return Encoding.UTF8.GetString(data);
            }
        }
    }
}
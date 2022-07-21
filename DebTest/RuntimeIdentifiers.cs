using System.Runtime.InteropServices;

namespace Packaging.Targets
{
    public static class RuntimeIdentifiers
    {
        public static void ParseRuntimeId(string runtimeId, out string osName, out string version, out Architecture? architecture, out string qualifiers)
        {
            osName = null;
            version = null;
            architecture = null;
            qualifiers = null;

            if (string.IsNullOrEmpty(runtimeId))
            {
                return;
            }

            int versionSeparator = runtimeId.IndexOf('.');
            if (versionSeparator >= 0)
            {
                osName = runtimeId.Substring(0, versionSeparator);
            }
            else
            {
                osName = null;
            }

            int muslSeparator = runtimeId.IndexOf("-musl", versionSeparator + 1);
            int architectureSeparator = runtimeId.IndexOf('-', muslSeparator + 1);
            if (architectureSeparator >= 0)
            {
                if (versionSeparator >= 0)
                {
                    version = runtimeId.Substring(versionSeparator + 1, architectureSeparator - versionSeparator - 1);
                }
                else
                {
                    osName = runtimeId.Substring(0, architectureSeparator);
                    version = null;
                }

                qualifiers = runtimeId.Substring(architectureSeparator + 1);
            }
            else
            {
                if (versionSeparator >= 0)
                {
                    version = runtimeId.Substring(versionSeparator + 1);
                }
                else
                {
                    osName = runtimeId;
                    version = null;
                }

                qualifiers = null;
            }

            if (osName.StartsWith("win") && osName.Length > 3)
            {
                version = osName.Substring(3);
                osName = "win";
            }

            architecture = null;

            if (!string.IsNullOrEmpty(qualifiers))
            {
                string architectureString = qualifiers;
                qualifiers = null;

                architectureSeparator = architectureString.IndexOf('-');
                if (architectureSeparator > 0)
                {
                    qualifiers = architectureString.Substring(architectureSeparator + 1);
                    architectureString = architectureString.Substring(0, architectureSeparator);
                }

                if (architectureString == "armel")
                {
                    architectureString = "arm";
                }

                if (Enum.TryParse<Architecture>(architectureString, ignoreCase: true, out Architecture parsedArchitecture))
                {
                    architecture = parsedArchitecture;
                }
                else
                {
                    qualifiers = architectureString;
                }
            }
        }
    }
}

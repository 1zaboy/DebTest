using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Packaging.Targets.Deb
{
    internal static class ControlFileParser
    {
        internal static Dictionary<string, string> Read(Stream stream)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, bufferSize: 1024, leaveOpen: true))
            {
                string line;
                string currentKey = null;

                while (reader.Peek() > 0)
                {
                    line = reader.ReadLine();

                    if (line.StartsWith("#"))
                    {
                        continue;
                    }

                    if (line.StartsWith(" ") || line.StartsWith("\t"))
                    {
                        var value = values[currentKey];
                        value += '\n';

                        if (line.Trim() != ".")
                        {
                            value += line.Trim();
                        }

                        values[currentKey] = value;
                    }
                    else
                    {
                        string[] parts = line.Split(new char[] { ':' }, 2);
                        currentKey = parts[0].Trim();
                        string value = parts[1].Trim();

                        values.Add(currentKey, value);
                    }
                }
            }

            return values;
        }
    }
}

using Microsoft.Build.Framework;
using Packaging.Targets;
using System.Diagnostics;

static class Program
{
    private static readonly Dictionary<string, string> m_Properties = new()
    {
        { "gateway_id", "175" }
    };

    public static void Main(string[] args)
    {
        var path = args[0];
        var file = args[1];
        var fileOutput = args[2];

        Console.WriteLine("Path: " + path);
        Console.WriteLine("File: " + file);
        Console.WriteLine("File output: " + fileOutput);

        var process = new ProcessStartInfo
        {
            WorkingDirectory = "/home/vlad/",
            FileName = "dpkg-deb",
            Arguments = $"-x {path}/{file} {path}/{fileOutput}",
        };

        var cmd = Process.Start(process);
        cmd.WaitForExit();

        Console.WriteLine();
        Console.WriteLine("Unpacked");

        // ----------------------------------------------------

        var pathIni = Path.Combine(
            "/home/vlad",
            $"{path}",
            $"{fileOutput}",
            ""
        );
        var lines = File.ReadAllLines(pathIni);
        for (var i = 0; i < lines.Length; i++)
        {
            var v = lines[i].Split(':');
            if (v.Length <= 1) continue;
            var key = v[0].Trim();
            var value = v[1].Trim();
            if (m_Properties.ContainsKey(key))
            {
                lines[i] = $"{key}: {m_Properties[key]}";
                Console.WriteLine(@"Added property: {0}: {1}", key, value);
            }
        }

        File.WriteAllLines(pathIni, lines);

        // ----------------------------------------------------

        Console.WriteLine("Deb building...");

        var debTask = new DebTask()
        {
            DebPath = Path.Combine("", ""),
            DebTarPath = "",
            PublishDir = "C:/Users/Vlad Kolosov/Downloads",
            AppHost = "",
            Prefix = "",
            Content = Array.Empty<ITaskItem>(),
            LinuxFolders = Array.Empty<ITaskItem>(),
            DebTarXzPath = "",
            PackageName = "",
            Description = "",
            Maintainer = "",
            Version = "1.0.0",
            DebPackageArchitecture = "",
            RuntimeIdentifier = "",
        };

        debTask.Execute();

        Console.WriteLine("End");
    }
}

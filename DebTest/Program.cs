using Microsoft.Build.Framework;
using Packaging.Targets;
using Packaging.Targets.Deb;

static class Program
{
    private static readonly Dictionary<string, string> m_Properties = new()
    {
        { "gateway_id", "175" },
        { "client_secret", "" },
        { "controller_url", "" },
    };

    private static string m_HomePath = "/home/vlad";
    private static string m_IniPath = "etc/opt/sdp/app_config.ini";

    public static void Main(string[] args)
    {
        var path = args[0];
        var file = args[1];
        var fileOutput = args[2];

        Console.WriteLine("Path: " + path);
        Console.WriteLine("File: " + file);
        Console.WriteLine("File output: " + fileOutput);


        using (FileStream s = File.Open(Path.Combine(m_HomePath, path, file), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var data = DebPackageReader.GetPayloadStream(s);
            DebPackageReader.unTAR(data, Path.Combine(m_HomePath, path, fileOutput));
        }

        Console.WriteLine();
        Console.WriteLine("Unpacked");

        // ----------------------------------------------------

        EditIni(Path.Combine(m_HomePath, path, fileOutput, m_IniPath));

        // ----------------------------------------------------

        Console.WriteLine("Deb building...");

        var customName = "sdp-gateway";

        var debTask = new DebTask()
        {
            DebPath = Path.Combine(m_HomePath, $"{path}", customName + ".deb"),
            DebTarPath = Path.Combine(m_HomePath, $"{path}", customName + ".tar"),
            PublishDir = Path.Combine(m_HomePath, $"{path}", fileOutput),
            AppHost = null,
            Prefix = "",
            Content = Array.Empty<ITaskItem>(),
            LinuxFolders = Array.Empty<ITaskItem>(),
            DebTarXzPath = Path.Combine(m_HomePath, $"{path}", customName + ".tar.xz"),
            PackageName = $"{customName}.deb",
            Description = "",
            Maintainer = "",
            Version = "1.0.0",
            DebPackageArchitecture = "amd64",
        };

        debTask.Execute();

        Console.WriteLine("End");
    }

    private static void EditIni(string pathIni)
    {
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
    }
}

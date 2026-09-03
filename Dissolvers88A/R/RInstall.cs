using System.IO;
using Microsoft.Win32;

namespace Dissolvers88A.R;

/// <summary>A discovered native R installation on this machine.</summary>
public sealed record RInstall(string Home, string Version, string RTermExe)
{
    public const string DownloadUrl = "https://cran.r-project.org/bin/windows/base/";

    /// <summary>Find the newest usable R, or <c>null</c> if none is installed.</summary>
    public static RInstall? Discover()
    {
        foreach (var home in CandidateHomes())
        {
            if (string.IsNullOrWhiteSpace(home) || !Directory.Exists(home)) continue;
            foreach (var sub in new[] { @"bin\x64", @"bin\i386", "bin" })
            {
                var term = Path.Combine(home, sub, "Rterm.exe");
                if (File.Exists(term))
                {
                    var name = new DirectoryInfo(home.TrimEnd('\\')).Name;
                    var version = name.StartsWith("R-") ? name[2..] : name;
                    return new RInstall(home, version, term);
                }
            }
        }
        return null;
    }

    private static IEnumerable<string> CandidateHomes()
    {
        var env = Environment.GetEnvironmentVariable("R_HOME");
        if (!string.IsNullOrEmpty(env)) yield return env;

        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            string?[] paths;
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var rKey = baseKey.OpenSubKey(@"SOFTWARE\R-core\R");
                if (rKey is null) continue;
                var list = new List<string?> { rKey.GetValue("InstallPath") as string };
                foreach (var subName in rKey.GetSubKeyNames())
                {
                    using var subKey = rKey.OpenSubKey(subName);
                    list.Add(subKey?.GetValue("InstallPath") as string);
                }
                paths = list.ToArray();
            }
            catch { continue; }

            foreach (var p in paths) if (p is not null) yield return p;
        }

        foreach (var special in new[] { Environment.SpecialFolder.ProgramFiles,
                                        Environment.SpecialFolder.ProgramFilesX86 })
        {
            var root = Path.Combine(Environment.GetFolderPath(special), "R");
            if (!Directory.Exists(root)) continue;
            foreach (var dir in Directory.GetDirectories(root, "R-*")
                                         .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase))
                yield return dir;
        }
    }
}

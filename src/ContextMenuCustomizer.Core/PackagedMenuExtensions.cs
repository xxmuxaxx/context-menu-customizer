using System.Xml.Linq;
using Windows.Management.Deployment;

namespace ContextMenuCustomizer.Core;

public sealed record PackagedVerb(string PackageName, string PackageDisplayName, ManifestVerb Verb);

/// <summary>Находит пункты нового меню Windows 11, которые регистрируют установленные пакеты (MSIX / sparse).</summary>
public static class PackagedMenuExtensions
{
    public static IReadOnlyList<PackagedVerb> Enumerate()
    {
        var result = new List<PackagedVerb>();
        IEnumerable<Windows.ApplicationModel.Package> packages;
        try
        {
            packages = new PackageManager().FindPackagesForUser(string.Empty).ToList();
        }
        catch (Exception)
        {
            return result;
        }

        foreach (var package in packages)
        {
            try
            {
                var manifestPath = FindManifest(package);
                if (manifestPath == null) continue;

                var text = File.ReadAllText(manifestPath);
                if (!text.Contains("FileExplorerContextMenus", StringComparison.Ordinal)) continue;

                var verbs = ManifestContextMenuParser.Parse(XDocument.Parse(text));
                if (verbs.Count == 0) continue;

                var name = package.Id.Name;
                var displayName = SafeDisplayName(package) ?? name;
                result.AddRange(verbs.Select(v => new PackagedVerb(name, displayName, v)));
            }
            catch (Exception)
            {
                // Повреждённый или недоступный пакет — пропускаем.
            }
        }
        return result;
    }

    private static string? FindManifest(Windows.ApplicationModel.Package package)
    {
        var candidates = new List<string>();
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            try { candidates.Add(package.InstalledPath); } catch (Exception) { }
        }
        try { candidates.Add(package.InstalledLocation.Path); } catch (Exception) { }
        var programFiles = Environment.GetEnvironmentVariable("ProgramW6432")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        candidates.Add(Path.Combine(programFiles, "WindowsApps", package.Id.FullName));

        return candidates
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => Path.Combine(c, "AppxManifest.xml"))
            .FirstOrDefault(File.Exists);
    }

    private static string? SafeDisplayName(Windows.ApplicationModel.Package package)
    {
        try
        {
            var name = package.DisplayName;
            return string.IsNullOrWhiteSpace(name) || name.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase)
                ? null
                : name;
        }
        catch (Exception)
        {
            return null;
        }
    }
}

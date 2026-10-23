namespace BMCL.Core.Minecraft;

public static class MavenPath
{
    public static string? ToRelativePath(string coordinates, string? classifier = null)
    {
        if (string.IsNullOrWhiteSpace(coordinates))
        {
            return null;
        }

        var extension = "jar";
        var atIndex = coordinates.IndexOf('@');
        if (atIndex >= 0)
        {
            extension = coordinates[(atIndex + 1)..];
            coordinates = coordinates[..atIndex];
        }

        var parts = coordinates.Split(':');
        if (parts.Length < 3)
        {
            return null;
        }

        var group = parts[0];
        var artifact = parts[1];
        var version = parts[2];
        classifier ??= parts.Length > 3 && parts[3].Length > 0 ? parts[3] : null;

        var fileName = classifier is null
            ? $"{artifact}-{version}.{extension}"
            : $"{artifact}-{version}-{classifier}.{extension}";

        return $"{group.Replace('.', '/')}/{artifact}/{version}/{fileName}";
    }
}

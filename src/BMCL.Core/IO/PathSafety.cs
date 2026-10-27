namespace BMCL.Core.IO;

public static class PathSafety
{
    public static bool IsValidFileName(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value is not "." and not ".." &&
        !value.Contains('/', StringComparison.Ordinal) &&
        !value.Contains('\\', StringComparison.Ordinal) &&
        value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    public static void ValidateFileName(
        string? value,
        string parameterName,
        string description)
    {
        if (!IsValidFileName(value))
        {
            throw new ArgumentException(
                $"The {description} is not a valid file name.",
                parameterName);
        }
    }

    public static string CombineUnderRoot(string root, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var fullRoot = Path.GetFullPath(root);
        var combined = Path.GetFullPath(Path.Combine(
            fullRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var relative = Path.GetRelativePath(fullRoot, combined);

        if (Path.IsPathRooted(relative) ||
            relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"The path escapes its root: {relativePath}");
        }

        return combined;
    }
}

namespace BMCL.Launch.Java;

public sealed record JavaRuntime(
    string Path,
    string Home,
    string Version,
    int MajorVersion,
    string Architecture,
    string? Vendor);

public interface IJavaLocator
{
    Task<IReadOnlyList<JavaRuntime>> FindAsync(CancellationToken cancellationToken = default);
}

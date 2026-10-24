namespace BMCL.Install;

public sealed record InstallProgress(
    string Stage,
    int Completed = 0,
    int Total = 0,
    string? FileName = null)
{
    public double Percent => Total <= 0
        ? 0
        : Math.Clamp(Completed * 100d / Total, 0, 100);
}

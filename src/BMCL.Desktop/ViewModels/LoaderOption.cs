using Material.Icons;

namespace BMCL.Desktop.ViewModels;

public sealed record LoaderOption(
    string Name,
    string Description,
    MaterialIconKind Icon,
    bool IsAvailable)
{
    public double Opacity => IsAvailable ? 1 : 0.62;
}

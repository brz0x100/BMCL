using Avalonia.Controls;

namespace BMCL.Desktop.Views.Dialogs;

internal static class DialogSizing
{
    public static void ApplySettings(Window dialog, Window owner)
    {
        var ownerWidth = owner.ClientSize.Width;
        var ownerHeight = owner.ClientSize.Height;

        // Target about 85% of the owner's visible area at the default size.
        var widthRatio = Math.Clamp(0.86 - (ownerWidth - 1200) * 0.00025, 0.68, 0.90);
        var heightRatio = Math.Clamp(0.86 - (ownerHeight - 760) * 0.0004, 0.68, 0.90);

        dialog.Width = Math.Clamp(ownerWidth * widthRatio, 720, 1400);
        dialog.Height = Math.Clamp(ownerHeight * heightRatio, 500, 900);
    }

    public static void ApplyAccount(Window dialog, Window owner)
    {
        var ownerWidth = owner.ClientSize.Width;
        var ownerHeight = owner.ClientSize.Height;

        var widthRatio = Math.Clamp(0.80 - (ownerWidth - 800) * 0.00035, 0.56, 0.80);
        var heightRatio = Math.Clamp(0.82 - (ownerHeight - 500) * 0.0005, 0.58, 0.84);

        dialog.Width = Math.Clamp(ownerWidth * widthRatio, 680, 960);
        dialog.Height = Math.Clamp(ownerHeight * heightRatio, 440, 720);
    }
}

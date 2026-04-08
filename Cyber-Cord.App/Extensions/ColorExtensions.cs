using System.Drawing;

namespace Cyber_Cord.App.Extensions;

public static class ColorExtensions
{
    public static string AsHex(this Color color, string prefix = "")
        => $"{prefix}{color.R:X2}{color.G:X2}{color.B:X2}";
}

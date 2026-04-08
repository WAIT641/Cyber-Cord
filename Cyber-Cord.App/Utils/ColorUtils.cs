using System.Buffers;
using System.Drawing;

namespace Cyber_Cord.App.Utils;

public static class ColorUtils
{
    private enum HueValues
    {
        Case1 = 0,
        Case2 = 1,
        Case3 = 2,
        Case4 = 3,
        Case5 = 4,
    }

    private enum RGBIndicies
    {
        Red = 0, Green = 1, Blue = 2
    }

    private const int _randomColorCoefficient = 360;
    private const double _saturation = 0.7;
    private const double _lightness = 0.5;
    private const int _toArgbConversion = 255;
    private const int _hBarCoefficient = 60;
    private const int _alphaIndex = 3;
    private const int _blueIndex = 2;
    private const int _greenIndex = 1;
    private const int _redIndex = 0;
    private const int _channelMask = 0xFF;
    private const int _nonAlphaHexLength = 6;
    private const int _octetCharLength = 2;

    public static Color RandomPrettyColor()
    {
        //Get HSL
        double hue = Random.Shared.NextDouble() * _randomColorCoefficient;
        double saturation = _saturation;
        double lightness = _lightness;

        double chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        double hBar = hue / _hBarCoefficient;
        double X = chroma * (1 - Math.Abs(hBar % 2 - 1));
        double m = lightness - chroma / 2;

        double[] rgb;

        int i_hBar = (int)hBar;
        rgb = (HueValues)i_hBar switch
        {
            HueValues.Case1 => [chroma, X, 0],
            HueValues.Case2 => [X, chroma, 0],
            HueValues.Case3 => [0, chroma, X],
            HueValues.Case4 => [0, X, chroma],
            HueValues.Case5 => [X, 0, chroma],
            _               => [chroma, 0, X]
        };

        //Convert to final RGB
        byte r = (byte)((rgb[(int)RGBIndicies.Red] + m) * _toArgbConversion);
        byte g = (byte)((rgb[(int)RGBIndicies.Green] + m) * _toArgbConversion);
        byte b = (byte)((rgb[(int)RGBIndicies.Blue] + m) * _toArgbConversion);

        //return color
        return Color.FromArgb(r, g, b);
    }

    public static Color FromBytes(byte[] color, bool hasAlpha = false)
    {
        int subtractOffset = hasAlpha ? _redIndex : 0;

        return Color.FromArgb(
            hasAlpha ? color[_alphaIndex] : _channelMask,
            color[_redIndex   - subtractOffset],
            color[_greenIndex - subtractOffset],
            color[_blueIndex  - subtractOffset]
            );
    }

    public static bool TryGetFromHex(string hex, out Color color, string prefix = "")
    {
        color = default;

        if (prefix.Length >= hex.Length)
        {
            return false;
        }

        bool containsAlpha = hex.Length > _nonAlphaHexLength + prefix.Length;

        var bytes = new byte[_nonAlphaHexLength + (containsAlpha ? _octetCharLength : 0)];
        if (Convert.FromHexString(hex.Substring(prefix.Length), bytes, out _, out _) != OperationStatus.Done)
        {
            return false;
        }

        color = FromBytes(bytes, containsAlpha);

        return true;
    }
}

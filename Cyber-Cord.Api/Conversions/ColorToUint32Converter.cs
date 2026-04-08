using System.Drawing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cyber_Cord.Api.Conversions;

public class ColorToUint32Converter : ValueConverter<Color, int>
{
    public ColorToUint32Converter() : base(c => c.ToArgb(), v => Color.FromArgb(v))
    {
    }
}

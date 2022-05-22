using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ClosedXML.Extensions
{
    internal static class ColorExtensions
    {
        public static string ToArgbHex(this Color color)
        {
            Rgba32 rgba = color;
            return ((uint) (rgba.B | rgba.G << 8 | rgba.R << 16 | rgba.A << 24)).ToString("X8");
        }

        public static string ToRgbHex(this Color color)
        {
            Rgba32 rgba = color;
            return ((uint) (rgba.B | rgba.G << 8 | rgba.R << 16)).ToString("X6");
        }

        public static Color ParseArgbHex(string argbHex)
        {
            var color = Color.ParseHex(argbHex);
            if (argbHex.Length == 8 || argbHex.Length == 9)
            {
                // The input format is aarrggbb but was parsed as rrggbbaa, so the components must be swapped
                Rgba32 rgba = color;
                return new Rgba32(r: rgba.G, g: rgba.B, b: rgba.A, a: rgba.R);
            }
            return color;
        }

        public static Color FromArgb(int argb)
        {
            var a = (byte)((argb & 0xFF000000) >> 24);
            var r = (byte)((argb & 0x00FF0000) >> 16);
            var g = (byte)((argb & 0x0000FF00) >>  8);
            var b = (byte)(argb & 0x000000FF);
            return Color.FromRgba(r, g, b, a);
        }
    }
}

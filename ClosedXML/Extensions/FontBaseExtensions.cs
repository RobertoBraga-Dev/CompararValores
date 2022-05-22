// Keep this file CodeMaid organised and cleaned
using System;
using System.Collections.Generic;
using SixLabors.Fonts;

namespace ClosedXML.Excel
{
    public static class FontConfiguration
    {
        public static IReadOnlyFontCollection FontCollection { get; set; } = SystemFonts.Collection;
    }

    internal static class FontBaseExtensions
    {
        public static void CopyFont(this IXLFontBase font, IXLFontBase sourceFont)
        {
            font.Bold = sourceFont.Bold;
            font.Italic = sourceFont.Italic;
            font.Underline = sourceFont.Underline;
            font.Strikethrough = sourceFont.Strikethrough;
            font.VerticalAlignment = sourceFont.VerticalAlignment;
            font.Shadow = sourceFont.Shadow;
            font.FontSize = sourceFont.FontSize;
            font.FontColor = sourceFont.FontColor;
            font.FontName = sourceFont.FontName;
            font.FontFamilyNumbering = sourceFont.FontFamilyNumbering;
            font.FontCharSet = sourceFont.FontCharSet;
        }

        public static Double GetHeight(this IXLFontBase fontBase, Dictionary<IXLFontBase, TextOptions> textOptionsCache)
        {
            var textOptions = GetCachedTextOptions(fontBase, textOptionsCache);
            var textHeight = TextMeasurer.Measure("X", textOptions).Height;
            return Math.Round(textHeight * 0.85, 2);
        }

        public static Double GetWidth(this IXLFontBase fontBase, String text, Dictionary<IXLFontBase, TextOptions> textOptionsCache)
        {
            if (String.IsNullOrWhiteSpace(text))
                return 0;

            var textOptions = GetCachedTextOptions(fontBase, textOptionsCache);
            var textWidth = TextMeasurer.Measure(text, textOptions).Width;

            double width = (textWidth / 7d * 256 - 128 / 7) / 256;
            width = Math.Round(width + 0.2, 2);

            return width;
        }

        private static TextOptions GetCachedTextOptions(IXLFontBase fontBase, Dictionary<IXLFontBase, TextOptions> textOptionsCache)
        {
            if (!textOptionsCache.TryGetValue(fontBase, out TextOptions textOptions))
            {
                var fontFamily = FontConfiguration.FontCollection.Get(fontBase.FontName);
                var font = new Font(fontFamily, (float)fontBase.FontSize, GetFontStyle(fontBase));
                textOptions = new TextOptions(font) { Dpi = 96 };
                textOptionsCache.Add(fontBase, textOptions);
            }
            return textOptions;
        }

        private static FontStyle GetFontStyle(IXLFontBase font)
        {
            FontStyle fontStyle = FontStyle.Regular;
            if (font.Bold) fontStyle |= FontStyle.Bold;
            if (font.Italic) fontStyle |= FontStyle.Italic;
            // Strikethrough and Underline are not supported by ImageSharp but that probably
            // doesn't matter much for measuring the size of the rendered fonts.
            return fontStyle;
        }
    }
}

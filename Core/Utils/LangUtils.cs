using System;
using Terraria;
using Terraria.Localization;

namespace ZensSky.Core.Utils;

public static partial class Utilities
{
    /// <summary>
    /// Retrieves the text value for a specified localization key — but with glyph support via <see cref="Lang.SupportGlyphs"/>, allowing the use of <c>&lt;left&gt;</c> and <c>&lt;right&gt;</c> —. <br/>
    /// The text returned will be for the currently selected language.
    /// </summary>
    public static string GetTextValueWithGlyphs(string key) =>
        Lang.SupportGlyphs(Language.GetTextValue(key));

    /// <returns><paramref name="value"/> clamped to <paramref name="maxLength"/> with the attached <paramref name="suffix"/>.</returns>
    public static string Truncate(this string value, int maxLength, string suffix = "") =>
        value[..Math.Min(value.Length, maxLength)] + suffix;
}

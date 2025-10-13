using Microsoft.Xna.Framework;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using static ReLogic.Graphics.DynamicSpriteFont;

namespace ZensSky.Core.Utils;

public static partial class Utilities
{
    #region Lang

    /// <summary>
    /// Retrieves the text value for a specified localization key with glyph support via <see cref="Lang.SupportGlyphs"/>; e.g. <c>&lt;left&gt;</c> and <c>&lt;right&gt;</c>. <br/>
    /// The text returned will be for the currently selected language.
    /// </summary>
    public static string GetTextValueWithGlyphs(string key) =>
        Lang.SupportGlyphs(Language.GetTextValue(key));

    #endregion

    #region Strings

    /// <returns><paramref name="value"/> clamped to <paramref name="maxLength"/> with the attached <paramref name="suffix"/>.</returns>
    public static string Truncate(this string value, int maxLength, string suffix = "") =>
        value[..Math.Min(value.Length, maxLength)] + suffix;

    /// <inheritdoc cref="string.Replace(string, string?)"/>
    public static string Replace(this string input, IEnumerable<char> oldValue, string newValue)
    {
        string output = input;

        foreach (char c in oldValue)
            output = output.Replace(c.ToString(), newValue);

        return output;
    }

    public static string Only(this string input, IEnumerable<char> allowedChars) =>
        new([.. input.Where(c => allowedChars.Contains(c))]);

    #endregion

    #region Fonts

    public static Vector2 MeasureChar(this DynamicSpriteFont font, char c, bool firstChar, float lastKerning, out float kerningZ)
    {
        Vector2 output = Vector2.Zero;
        output.Y = font.LineSpacing;

        SpriteCharacterData characterData = font.GetCharacterData(c);
        Vector3 kerning = characterData.Kerning;

        if (firstChar)
            kerning.X = Math.Max(kerning.X, 0f);
        else
            output.X += font.CharacterSpacing + lastKerning;

        output.X += kerning.X + kerning.Y;

        output.Y = Math.Max(output.Y, characterData.Padding.Height);

        output.X += Math.Max(kerning.Z, 0f);

        kerningZ = kerning.Z;

        return output;
    }

    #endregion
}

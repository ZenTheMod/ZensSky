using Daybreak.Common.Features.Hooks;
using Microsoft.Xna.Framework.Input;
using ReLogic.Localization.IME;
using ReLogic.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameInput;
using static ZensSky.Core.Utils.InputCancellationType;

namespace ZensSky.Core.Utils;

    // The C# 14.0 'extension' block seems to still be a little buggy.
#pragma warning disable CA1822 // Member does not access instance data and can be marked as static.

public static class Input
{
    #region Private Fields

    private const int MaxStrokeLength = 20;

    private const int KeyDelay = 45;

    private static readonly char[] InvalidChars = [.. '\x0'.Range('\x20'), '\x7F'];

    #endregion

    #region Public Properties

    public static bool WritingText
    {
        get => PlayerInput.WritingText;
        set => PlayerInput.WritingText = value;
    }

    public static int CursorPositon { get; set; }

    public static string KeyStroke { get; private set; }
        = "";

    public static int BackspaceTimer { get; private set; }
        = KeyDelay;

    public static int LeftArrowTimer { get; private set; }
        = KeyDelay;


    public static int RightArrowTimer { get; private set; }
        = KeyDelay;

    #endregion

    #region Loading

    [OnLoad]
    public static void Load() =>
        Platform.Get<IImeService>().AddKeyListener(OnKeyStroke);

    [OnUnload]
    public static void Unload() =>
        Platform.Get<IImeService>().RemoveKeyListener(OnKeyStroke);

    private static void OnKeyStroke(char key)
    {
        if (WritingText &&
            KeyStroke.Length <= MaxStrokeLength)
            KeyStroke += key;
    }

    #endregion

    #region Input

    public static InputCancellationType GetInput(
        string input,
        out string output,
        bool allowLineBreaks = false,
        IEnumerable<char>? blacklistedChars = null,
        IEnumerable<char>? whitelistedChars = null)
    {
        output = input;

            // Perhaps I may want to use my own KeyboardStates?
        Main.oldInputText = Main.inputText;
	    Main.inputText = Keyboard.GetState();

            // Should only write while focused.
        WritingText &= Main.hasFocus;

        if (!WritingText)
            return None;

        Main.instance.HandleIME();

        blacklistedChars ??= [];
        blacklistedChars = blacklistedChars.Concat(InvalidChars);

        #region Cursor

            // Left
        if (Keys.Left.Held)
            LeftArrowTimer--;
        else
            LeftArrowTimer = KeyDelay;

        if (Keys.Left.JustPressed ||
            (Keys.Left.Held &&
            LeftArrowTimer <= 0))
            CursorPositon--;

            // Right
        if (Keys.Right.Held)
            RightArrowTimer--;
        else
            RightArrowTimer = KeyDelay;

        if (Keys.Right.JustPressed ||
            (Keys.Right.Held &&
            RightArrowTimer <= 0))
            CursorPositon++;

        CursorPositon = Math.Clamp(CursorPositon, 0, output.Length);

        #endregion

        #region Special Actions

        bool controlPressed =
            (Keys.LeftControl.Pressed ||
            Keys.RightControl.Pressed) &&
            !Keys.LeftAlt.Pressed &&
            !Keys.RightAlt.Pressed;

        bool shiftPressed =
            Keys.LeftShift.Pressed ||
            Keys.RightShift.Pressed;

            // Clear
        if (controlPressed && Keys.Z.JustPressed)
        {
            output = string.Empty;
            CursorPositon = 0;
        }
            // Cut
        else if (
            (controlPressed && Keys.X.JustPressed) ||
            (shiftPressed && Keys.Delete.JustPressed))
        {
            Platform.Get<IClipboard>().Value = output;
            output = string.Empty;
            CursorPositon = 0;
        }
            // Copy
        else if (Keys.C.JustPressed)
            Platform.Get<IClipboard>().Value = output;
            // Paste
        else if (
            (controlPressed && Keys.V.JustPressed) ||
            (shiftPressed && Keys.Insert.JustPressed))
        {
            string paste = GetPaste(output, allowLineBreaks);

            paste = paste.Replace(blacklistedChars, string.Empty);

            if (whitelistedChars is not null &&
                whitelistedChars.Any())
                paste = paste.Only(whitelistedChars);

            output = output.Insert(CursorPositon, paste);

            CursorPositon += paste.Length;
        }

        #endregion

        #region Input

        if (KeyStroke.Length >= 1)
        {
            string stroke = KeyStroke.Replace(blacklistedChars, string.Empty);

            if (whitelistedChars is not null &&
                whitelistedChars.Any())
                stroke = stroke.Only(whitelistedChars);

            KeyStroke = string.Empty;

            output = output.Insert(CursorPositon, stroke);

            CursorPositon += stroke.Length;
        }

        #endregion

        #region Backspace

        if (Keys.Back.Held)
            BackspaceTimer--;
        else
            BackspaceTimer = KeyDelay;

        if ((Keys.Back.JustPressed ||
            (Keys.Back.Held &&
            BackspaceTimer <= 0)) &&
            output.Length >= 1 &&
            CursorPositon >= 1)
        {
            output = string.Concat(output.AsSpan(0, CursorPositon - 1),
                output.AsSpan(CursorPositon, output.Length - CursorPositon));

            CursorPositon--;
        }

        #endregion

        #region Escapes

            // Unsure of why vanilla checks if you're on Windows before allowing escape inputs.
                // if (!Platform.IsWindows && Main.inputText.IsKeyDown(Keys.Escape) && !Main.oldInputText.IsKeyDown(Keys.Escape))
        if (Keys.Escape.JustPressed)
        {
            WritingText = false;
            return Escaped;
        }

        if (Keys.Enter.JustPressed)
        {
            WritingText = false;
            return Confirmed;
        }

        #endregion

        return None;
    }

    private static string GetPaste(string input, bool allowLineBreaks) =>
        input.Insert(CursorPositon,
            allowLineBreaks ? Platform.Get<IClipboard>().MultiLineValue : Platform.Get<IClipboard>().Value);

    #endregion

    #region Extension Members

    extension(Keys key)
    {
        public bool Pressed =>
            Main.inputText.IsKeyDown(key);

        public bool Held =>
            Main.inputText.IsKeyDown(key) &&
            Main.oldInputText.IsKeyDown(key);

        public bool JustPressed =>
            Main.inputText.IsKeyDown(key) &&
            !Main.oldInputText.IsKeyDown(key);

        public bool JustReleased =>
            !Main.inputText.IsKeyDown(key) &&
            Main.oldInputText.IsKeyDown(key);
    }

    #endregion
}

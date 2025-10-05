using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Core.Utils;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;
using ZensSky.Common.Systems.Menu.Elements;
using ZensSky.Core.UI;

namespace ZensSky.Common.Systems.Menu.Controllers;

public sealed class ButtonColorController : MenuController
{
    #region Private Fields

    private const float DefaultHeight = 75f;

    private readonly ColorPicker Picker;

    private readonly HoverImageButton ColorDisplay;
    private readonly HoverImageButton HoverColorDisplay;

    private static bool SettingHover;

    private const string ColorDisplayHoverKey = "Mods.ZensSky.MenuController.ColorDisplayHover";
    private const string HoverColorDisplayHoverKey = "Mods.ZensSky.MenuController.HoverColorDisplayHover";
    
    private static readonly Color Outline = new(215, 215, 215);

    private static readonly Color DefaultColor = Color.Gray;
    private static readonly Color DefaultHover = new(255, 215, 0);

    private static Color ButtonColor;
    private static Color ButtonHoverColor;

    #endregion

    #region Private Properties

    private static ref Color Modifying =>
        ref SettingHover ? ref MenuConfig.Instance.MenuButtonHoverColor : ref MenuConfig.Instance.MenuButtonColor;

    private static ref bool ModifyingUse =>
        ref SettingHover ? ref MenuConfig.Instance.UseMenuButtonHoverColor : ref MenuConfig.Instance.UseMenuButtonColor;

    private bool ShowPicker
    {
        get => field;
        set
        {
            field = value;

            if (value && !Elements.Contains(Picker))
            {
                Append(Picker);

                Recalculate();

                MenuControllerSystem.State?.Controllers?.Recalculate();

                float height = Height.Pixels - DefaultHeight;

                MenuControllerSystem.State?.Controllers?.ViewPosition += height;
            }
            else if (!value)
            {
                RemoveChild(Picker);

                float height = Height.Pixels - DefaultHeight;
                MenuControllerSystem.State?.Controllers?.ViewPosition -= height;

                Recalculate();
                MenuControllerSystem.State?.Controllers?.Recalculate();
            }
        }
    }

    #endregion

    #region Public Properties

    public override int Index => 7;

    public override string Name => "Mods.ZensSky.MenuController.ButtonColor";

    #endregion

    #region Constructor

    public ButtonColorController() : base()
    {
        Height.Set(DefaultHeight, 0f);

        Picker = new();

        Picker.Top.Set(60f, 0f);

        ColorDisplay = CreateColorDisplay(false);
        HoverColorDisplay = CreateColorDisplay(true);

        Append(ColorDisplay);
        Append(HoverColorDisplay);
    }

    #endregion

    #region Loading

    public override void OnLoad() => 
        MainThreadSystem.Enqueue(() => IL_Main.DrawMenu += ModifyColors);

    public override void OnUnload() => 
        MainThreadSystem.Enqueue(() => IL_Main.DrawMenu -= ModifyColors);

    private void ModifyColors(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            int colorIndex = -1;

            int rIndex = -1;
            int gIndex = -1;
            int bIndex = -1;
            int aIndex = -1;

            int hoveredIndex = -1;
            int outerIteratorIndex = -1;
            int innerIteratorIndex = -1;

            int interpolatorIndex = -1;

            ILLabel jumpColorCtorTarget = c.DefineLabel();

            for (int i = 0; i < 5; i++)
            {
                    // Grab relevant color indices.
                c.GotoNext(MoveType.After,
                    i => i.MatchLdloca(out colorIndex),
                    i => i.MatchLdloc(out rIndex),
                    i => i.MatchConvU1(),
                    i => i.MatchLdloc(out gIndex),
                    i => i.MatchConvU1(),
                    i => i.MatchLdloc(out bIndex),
                    i => i.MatchConvU1(),
                    i => i.MatchLdloc(out aIndex),
                    i => i.MatchConvU1(),
                    i => i.MatchCall<Color>(".ctor"));

                if (i == 4)
                    break;

                c.EmitLdloca(colorIndex);

                c.EmitLdloc(rIndex);
                c.EmitLdloc(gIndex);
                c.EmitLdloc(bIndex);
                c.EmitLdloc(aIndex);

                c.EmitLdcR4(0f);

                c.EmitDelegate(ModifyColor);

                c.EmitPop();
            }

                // Mark this label so we can skip this ctor later.
            c.MarkLabel(jumpColorCtorTarget);

                // Grab the inner iterator to check if were drawing the colored text and not the shadow.
            c.GotoNext(MoveType.After,
                i => i.MatchLdloc(out innerIteratorIndex),
                i => i.MatchLdcI4(4),
                i => i.MatchBneUn(out _));

                // Insert our stuff before the game handles hover color.
            c.GotoPrev(MoveType.Before,
                i => i.MatchLdloc(out hoveredIndex),
                i => i.MatchLdloc(out outerIteratorIndex),
                i => i.MatchBneUn(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchLdcI4(4),
                i => i.MatchBneUn(out _),
                i => i.MatchLdloc(out interpolatorIndex));

            c.MoveAfterLabels();

            c.EmitLdloca(colorIndex);

            c.EmitLdloc(innerIteratorIndex);

            c.EmitLdloc(rIndex);
            c.EmitLdloc(gIndex);
            c.EmitLdloc(bIndex);
            c.EmitLdloc(aIndex);

            c.EmitLdloc(interpolatorIndex);

            c.EmitLdloc(hoveredIndex);
            c.EmitLdloc(outerIteratorIndex);

            c.EmitDelegate((ref Color color, int i, int r, int g, int b, int a, int interpolator, int hovered, int j) =>
            {
                if (i != 4)
                    return false;

                return ModifyColor(ref color, r, g, b, a, hovered == j ? interpolator / 255f : 0);
            });

            c.EmitBrtrue(jumpColorCtorTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(ModContent.GetInstance<ZensSky>(), il, e);
        }
    }

    private static bool ModifyColor(ref Color color, int r, int g, int b, int a, float interpolator)
    {
        MenuConfig config = MenuConfig.Instance;

            // TODO: More accurate impl that uses r g b a inputs.
        if (!config.UseMenuButtonColor)
            ButtonColor = DefaultColor;
        if (!config.UseMenuButtonHoverColor)
            ButtonHoverColor = DefaultHover;

        if (!config.UseMenuButtonColor && !config.UseMenuButtonHoverColor)
            return false;

        Color normalColor = config.UseMenuButtonColor ? ButtonColor : new(r, g, b, a);
        Color hoverColor = ButtonHoverColor;

        color = Color.Lerp(normalColor, hoverColor, interpolator);

        return true;
    }

    #endregion

    #region Updating

    public override void Refresh()
    {
        MenuConfig config = MenuConfig.Instance;

        if (config.UseMenuButtonColor)
            ButtonColor = config.MenuButtonColor;
        if (config.UseMenuButtonHoverColor)
            ButtonHoverColor = config.MenuButtonHoverColor;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Picker.IsHeld)
        {
            ModifyingUse = true;
            Modifying = Picker.Color;
            Refresh();
        }
        else
            Picker.Color = Modifying;

        ColorDisplay.InnerColor = ButtonColor;
        HoverColorDisplay.InnerColor = ButtonHoverColor;

        if (ShowPicker)
        {
            ColorDisplay.OuterColor = SettingHover ? Outline : Color.White;
            HoverColorDisplay.OuterColor = SettingHover ? Color.White : Outline;
        }
        else
        {
            ColorDisplay.OuterColor = Outline;
            HoverColorDisplay.OuterColor = Outline;
        }
    }

    public override void Recalculate()
    {
        base.Recalculate();

        if (!ShowPicker)
        {
            Height.Set(75f, 0f);
            return;
        }

        float width = GetDimensions().Width;

        Height.Set(width + 90f, 0f);
    }

    #endregion

    #region Private Methods

    private HoverImageButton CreateColorDisplay(bool isHover)
    {
        HoverImageButton display = new(ButtonTextures.ColorInner, Color.White, ButtonTextures.ColorOuter);

        display.Width.Set(28f, 0f);
        display.Height.Set(28f, 0f);

        display.Top.Set(20f, 0f);

        display.Left.Set(-14f, isHover ? .6666f : .3333f);

        display.OnLeftMouseDown += (evt, listeningElement) => ShowColor(isHover);

        display.OnRightMouseDown +=
            (evt, listeningElement) => ResetColor(isHover);

        display.HoverText = Utilities.GetTextValueWithGlyphs(isHover ? HoverColorDisplayHoverKey : ColorDisplayHoverKey);

        return display;
    }

    private void ShowColor(bool isHover)
    {
        if (SettingHover == isHover)
            ShowPicker = !ShowPicker;
        else
            ShowPicker = true;

        SettingHover = isHover;

        SoundEngine.PlaySound(in SoundID.MenuOpen);
    }

    private static void ResetColor(bool isHover)
    {
        SettingHover = isHover;

        ModifyingUse = false;
        Modifying = Color.Red;

        SoundEngine.PlaySound(in SoundID.MenuOpen);
    }

    #endregion
}

using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Registries;
using ZensSky.Common.Systems.MainMenu.Elements;
using ZensSky.Common.Utilities;
using ZensSky.Core.Exceptions;

namespace ZensSky.Common.Systems.MainMenu.Controllers;

public sealed class ButtonColorController : MenuControllerElement
{
    #region Private Fields

    private readonly ColorTriangle Triangle;

    private readonly UISlider Slider;

    private readonly HoverImageButton ColorDisplay;

    private static bool SettingHover;

    private const string DisplayHover = "Mods.ZensSky.MenuController.ButtonColorHover";
    
    private static readonly Color Outline = new(215, 215, 215);
    private static readonly Color Hover = new(255, 215, 0);

    private static Color ButtonColor;
    private static Color ButtonHoverColor;

    #endregion

    #region Properties

    private static ref Vector3 Modifying => ref SettingHover ? ref MenuConfig.Instance.MenuButtonHoverColor : ref MenuConfig.Instance.MenuButtonColor;

    private static ref bool ModifyingUse => ref SettingHover ? ref MenuConfig.Instance.UseMenuButtonHoverColor : ref MenuConfig.Instance.UseMenuButtonColor;

    public override int Index => 7;

    public override string Name => "Mods.ZensSky.MenuController.ButtonColor";

    #endregion

    #region Constructor

    public ButtonColorController() : base()
    {
        Height.Set(75f, 0f);

        Slider = new();

        Slider.Top.Set(-16f, 1f);

        Slider.InnerTexture = Textures.HueGradient;
        Slider.InnerColor = Color.White;

        Append(Slider);

        Triangle = new();

        Triangle.Top.Set(40f, 0f);

        Append(Triangle);

        ColorDisplay = new(Textures.ColorInner, Color.White, Textures.ColorOuter, Outline);

        ColorDisplay.Width.Set(28f, 0f);
        ColorDisplay.Height.Set(28f, 0f);

        ColorDisplay.Top.Set(20f, 0f);

        ColorDisplay.OnLeftMouseDown += (evt, listeningElement) =>
        {
            SettingHover = !SettingHover;

            SoundEngine.PlaySound(in SoundID.MenuOpen);
        };

        ColorDisplay.OnRightMouseDown += (evt, listeningElement) =>
        {
            ModifyingUse = false;
            Modifying = new();

            SoundEngine.PlaySound(in SoundID.MenuOpen);
        };

        Append(ColorDisplay);
    }

    #endregion

    #region Loading

    public override void OnLoad() => Main.QueueMainThreadAction(() => IL_Main.DrawMenu += ModifyColors);

    public override void OnUnload() => Main.QueueMainThreadAction(() => IL_Main.DrawMenu -= ModifyColors);

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

            ILLabel? jumpHoverColorTarget = c.DefineLabel();

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
                i => i.MatchBneUn(out jumpHoverColorTarget),
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

        if (!config.UseMenuButtonColor)
            ButtonColor = new(r, g, b, a);
        if (!config.UseMenuButtonHoverColor)
            ButtonHoverColor = Hover;

        if (!config.UseMenuButtonColor && !config.UseMenuButtonHoverColor)
            return false;

        Color normalColor = ButtonColor;
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
            ButtonColor = GetColor(config.MenuButtonColor);
        if (config.UseMenuButtonHoverColor)
            ButtonHoverColor = GetColor(config.MenuButtonHoverColor);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Triangle.Hue = Slider.Ratio;

        if (Triangle.IsHeld)
        {
            Modifying.X = Triangle.PickerPosition.X;
            Modifying.Y = Triangle.PickerPosition.Y;
        }
        else
            Triangle.PickerPosition = new(Modifying.X, Modifying.Y);

        if (Slider.IsHeld)
            Modifying.Z = Slider.Ratio;
        else
            Slider.Ratio = Modifying.Z;

        if (Triangle.IsHeld || Slider.IsHeld)
        {
            ModifyingUse = true;
            Refresh();
        }

        ColorDisplay.InnerColor = SettingHover ? ButtonHoverColor : ButtonColor;
        ColorDisplay.HoverText = MiscUtils.GetTextValueWithGlyphs(DisplayHover + SettingHover);
    }

    public override void Recalculate()
    {
        base.Recalculate();

        float width = GetDimensions().Width;

        Height.Set(width - 30f, 0f);
        Triangle.Height.Set(width, 0);
    }

    #endregion

    #region Private Methods

    private static Color GetColor(Vector3 pos) =>
        MiscUtils.LerpTriangle(new(pos.X, pos.Y), ColorTriangle.NormalizedPoints, ColorTriangle.GetColors(pos.Z));

    #endregion
}

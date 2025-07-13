using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;
using ZensSky.Common.Registries;
using ZensSky.Common.Utilities;
using ZensSky.Core.Exceptions;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Config.Elements;

    // Pure raw hate.
[Autoload(Side = ModSide.Client)]
public abstract class LockedSliderElement<T> : PrimitiveRangeElement<T>, ILoadable where T : IComparable<T>
{
    #region Private Fields

    private const float TheMagicNumber = 167f;

    private const float LockedBackgroundMultiplier = 0.4f;

    private static readonly Color LockedGradient = new(40, 40, 40);

    private static ILHook? SkipDrawing;

    private static bool Drawing = false;

    #endregion

    #region Properties

    public abstract bool IsLocked { get; }

    #endregion

    #region Loading

    public void Load(Mod mod)
    {
        Main.QueueMainThreadAction(() => {
            MethodInfo? drawSelf = typeof(RangeElement).GetMethod("DrawSelf", NonPublic | Instance);

            if (drawSelf is not null)
                SkipDrawing = new(drawSelf,
                    SkipRangeElementDrawing);
        });
    }

    public void Unload() => Main.QueueMainThreadAction(() => SkipDrawing?.Dispose());

    private void SkipRangeElementDrawing(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel jumpret = c.DefineLabel();

            c.GotoNext(MoveType.After,
                i => i.MatchCall<ConfigElement>("DrawSelf"));

            c.EmitDelegate(() => Drawing);
            c.EmitBrfalse(jumpret);

            c.EmitRet();

            c.MarkLabel(jumpret);
        }
        catch (Exception e)
        {
            throw new ILEditException(ModContent.GetInstance<ZensSky>(), il, e);
        }
    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
            // I genuinely hate that what I'd indeally want is ONLY possible in IL.
        Drawing = true;
        backgroundColor = IsLocked ? UICommon.DefaultUIBlue * LockedBackgroundMultiplier : UICommon.DefaultUIBlue;
        base.DrawSelf(spriteBatch);
        Drawing = false;

        rightHover = null;

        if (!Main.mouseLeft)
            rightLock = null;

        CalculatedStyle dimensions = GetDimensions();

            // Not sure the purpose of this.
        IngameOptions.valuePosition = new(dimensions.X + dimensions.Width - 10f, dimensions.Y + 16f);

        DrawSlider(spriteBatch, Proportion, out float ratio);

            // No need to run logic if the value doesn't do anything.
        if (IsLocked)
            return;

        if (IngameOptions.inBar || rightLock == this)
        {
            rightHover = this;
            if (PlayerInput.Triggers.Current.MouseLeft && rightLock == this)
                Proportion = ratio;
        }

        if (rightHover is not null && rightLock is null && PlayerInput.Triggers.JustPressed.MouseLeft)
            rightLock = rightHover;
    }

    public void DrawSlider(SpriteBatch spriteBatch, float perc, out float ratio)
    {
        perc = MathHelper.Clamp(perc, -0.05f, 1.05f);

        Texture2D colorBar = TextureAssets.ColorBar.Value;
        Texture2D colorBarHighlight = TextureAssets.ColorHighlight.Value;
        Texture2D gradient = Textures.Gradient.Value;
        Texture2D colorSlider = TextureAssets.ColorSlider.Value;
        Texture2D lockIcon = Textures.Lock.Value;

        IngameOptions.valuePosition.X -= colorBar.Width;
        Rectangle rectangle = new((int)IngameOptions.valuePosition.X, (int)IngameOptions.valuePosition.Y - (int)(colorBar.Height * .5f), colorBar.Width, colorBar.Height);
        Rectangle destinationRectangle = rectangle;

        float x = rectangle.X + 5f;
        float y = rectangle.Y + 4f;

        spriteBatch.Draw(colorBar, rectangle, IsLocked ? Color.Gray : Color.White);

        Rectangle inner = new((int)x, (int)y, (int)TheMagicNumber + 2, 8);

            // Draw the gradient
        spriteBatch.Draw(gradient, inner, null, IsLocked ? LockedGradient : SliderColor, 0f, Vector2.Zero, SpriteEffects.None, 0f);

        rectangle.Inflate(-5, 2);

            // Logic.
        bool isHovering = rectangle.Contains(new Point(Main.mouseX, Main.mouseY)) || rightLock == this;

        if ((rightLock != this && rightLock is not null) || IsLocked)
            isHovering = false;

        if (isHovering)
            spriteBatch.Draw(colorBarHighlight, destinationRectangle, Main.OurFavoriteColor);

        Vector2 lockOffset = new(0, -4);

        if (IsLocked)
            spriteBatch.Draw(lockIcon, inner.Center() + lockOffset, null, Color.White, 0f, lockIcon.Size() * .5f, 1f, SpriteEffects.None, 0f);
        else
            spriteBatch.Draw(colorSlider, new Vector2(x + TheMagicNumber * perc, y + 4f), null, Color.White, 0f, colorSlider.Size() * .5f, 1f, SpriteEffects.None, 0f);

        IngameOptions.inBar = isHovering;
        ratio = MiscUtils.Saturate((Main.mouseX - rectangle.X) / (float)rectangle.Width);
    }

    #endregion
}

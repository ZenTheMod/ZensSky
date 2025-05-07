using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.Utilities;
using ZensSky.Common.Registries;
using Terraria.UI;

namespace ZensSky.Common.Systems.ModIcon;

public sealed class ModIconSystem : ModSystem
{
    #region Private Fields

    private static readonly Color DarkBackgroundColor = new(18, 28, 61);
    private static readonly Color DarkHoverBackgroundColor = new(15, 25, 58, 150);

    private static readonly Color StarColor = new(153, 185, 255);

    private const float OuterStarScale = 0.3f;
    private const float InnerStarScale = 0.1f;

    private const int StarCount = 300;

    private const float TimeMultiplier = 0.7f;

    private const float MaxPhase = MathHelper.Pi * 8f;

    private static ILHook? UIModItemDraw;
    private static ILHook? UIModItemInitialize;

    #endregion

    #region Loading

    public override void Load()
    {
        MethodInfo? drawSelf = typeof(UIModItem).GetMethod("DrawSelf", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo? onInitialize = typeof(UIModItem).GetMethod("OnInitialize", BindingFlags.Instance | BindingFlags.Public);

        if (drawSelf is not null)
            UIModItemDraw = new(drawSelf, EditUIModItemVisuals);

        if (onInitialize is not null)
            UIModItemInitialize = new(onInitialize, UseWobblyIcon);
    }

    public override void Unload()
    {
        UIModItemDraw?.Dispose();
        UIModItemInitialize?.Dispose();
    }

    #endregion

    #region Sparkles

    private void EditUIModItemVisuals(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            VariableDefinition isOurMod = new(il.Import(typeof(bool)));
            il.Body.Variables.Add(isOurMod);

            c.EmitLdarg0();
            c.EmitDelegate((UIModItem item) => item._mod.Name == Mod.Name);
            c.EmitStloc(isOurMod);

                // Change the panel color.
            c.EmitLdarg0();
            c.EmitLdloc(isOurMod);
            c.EmitDelegate((UIModItem item, bool ourMod) =>
            {
                if (!ourMod)
                    return;

                item.BackgroundColor = item.IsMouseHovering ? DarkHoverBackgroundColor : DarkBackgroundColor;
                item.BorderColor = item.IsMouseHovering ? Color.White : StarColor;

                item._modName.TextColor = DarkBackgroundColor;
                item._modName.ShadowColor = StarColor;
            });

            c.GotoNext(MoveType.After,
                i => i.MatchLdarg1(),
                i => i.MatchCall<UIPanel>("DrawSelf"));

                // Draw sparkles.
            c.EmitLdarg0();
            c.EmitLdloc(isOurMod);
            c.EmitLdarg1();
            c.EmitDelegate((UIModItem item, bool ourMod, SpriteBatch spriteBatch) =>
            {
                if (!ourMod)
                    return;

                UnifiedRandom rand = new(item._mod.Name.GetHashCode());

                int starCount = StarCount;

                float time = Main.GlobalTimeWrappedHourly * TimeMultiplier;

                Texture2D star = Textures.Star.Value;
                Vector2 starOrigin = star.Size() * 0.5f;

                CalculatedStyle dimensions = item.GetDimensions();

                Rectangle range = new((int)dimensions.X + item._cornerSize, (int)dimensions.Y + item._cornerSize,
                    (int)dimensions.Width - (item._cornerSize * 2), (int)dimensions.Height - (item._cornerSize * 2));

                for (int i = 0; i < starCount; i++)
                {
                    Vector2 starPosition = rand.NextVector2FromRectangle(range);

                    float lifeTime = time + rand.NextFloat(MaxPhase);
                    lifeTime %= MaxPhase;

                    if (lifeTime < MathHelper.TwoPi)
                    {
                        float sinValue = MathF.Sin(lifeTime);

                        float scale = MathF.Pow(2, 10 * (sinValue - 1));

                        Color outerColor = StarColor * sinValue;
                        outerColor.A = 0;
                        spriteBatch.Draw(star, starPosition, null, outerColor, 0, starOrigin, scale * OuterStarScale, SpriteEffects.None, 0f);

                        Color innerColor = Color.White * sinValue;
                        innerColor.A = 0;
                        spriteBatch.Draw(star, starPosition, null, innerColor, 0, starOrigin, scale * InnerStarScale, SpriteEffects.None, 0f);
                    }
                }
            });
        }
        catch (Exception e)
        {
            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    #endregion

    #region Change Icon

    private void UseWobblyIcon(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.Before, 
            i => i.MatchStfld<UIModItem>("_modIcon")))
            throw new ILPatchFailureException(Mod, il, null);

        c.EmitLdarg0();
        c.EmitDelegate((UIImage icon, UIModItem item) =>
        {
            if (item._mod.Name != Mod.Name)
                return icon;

            return new WobblyModIcon
            {
                _dimensions = icon._dimensions
            };
        }
        );
    }

    #endregion
}

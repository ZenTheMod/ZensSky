using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using ZensSky.Common.Systems.Compat;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems.Ambience;

[Autoload(Side = ModSide.Client)]
public sealed class RainSystem : ModSystem
{
    #region Private Fields

    private const int Margin = 600;
    private const float MagicScreenWidth = 1920f;
    private const float WindOffset = 600f;

    #endregion

    #region Loading

    public override void Load()
    {
        MainThreadSystem.Enqueue(() =>
        {
            IL_Main.DoUpdate += SpawnMenuRain;
            IL_Main.DoDraw += DontDegradeRain;
            IL_Main.UpdateAudio += RainWindAmbience;

            On_Main.DrawBackgroundBlackFill += DrawMenuRain;
        });
    }
    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
        {
            IL_Main.DoUpdate -= SpawnMenuRain;
            IL_Main.DoDraw -= DontDegradeRain;
            IL_Main.UpdateAudio -= RainWindAmbience;

            On_Main.DrawBackgroundBlackFill -= DrawMenuRain;
        });
    }

    #endregion

    #region Updating

    private void SpawnMenuRain(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.Before,
                i => i.MatchCall<Main>(nameof(Main.UpdateMenu)),
                i => i.MatchLdsfld<Main>(nameof(Main.netMode)));

            c.EmitDelegate(() =>
            {
                if (Main.cloudAlpha <= 0)
                    return;

                    // Make the water color change with the daynight cycle.
                Main.waterStyle = Main.dayTime ? WaterStyleID.Purity : WaterStyleID.Corrupt;

                float num = Main.screenWidth / MagicScreenWidth;
                num *= 0.25f + 1f * Main.cloudAlpha;

                Vector2 position = Main.screenPosition;

                for (int i = 0; i < num; i++)
                {
                    Vector2 vector = new(Main.rand.Next((int)position.X - Margin, (int)position.X + Main.screenWidth + Margin),
                        position.Y - Main.rand.Next(20, 100));

                    vector.X -= Main.WindForVisuals * WindOffset;

                    Vector2 rainFallVelocity = Rain.GetRainFallVelocity();
                    Rain.NewRain(vector, rainFallVelocity);
                }
            });
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    private void DontDegradeRain(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.gameMenu)),
                i => i.MatchBrfalse(out _),
                i => i.MatchLdloc2(),
                i => i.MatchLdcR4(20));

            c.EmitPop();

            c.EmitLdcR4(0f);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Menu Touchups

    private void RainWindAmbience(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? jumpMenuCheck = c.DefineLabel();
            
                // Rain.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>(nameof(Main.gameMenu)),
                i => i.MatchBrfalse(out jumpMenuCheck),
                i => i.MatchLdcR4(0));

            c.EmitDelegate(UsingDefaultMenu);
            c.EmitBrtrue(jumpMenuCheck);

                // Wind.
            c.GotoNext(MoveType.After,
                i => i.MatchStelemR4(),
                i => i.MatchBr(out _),
                i => i.MatchLdsfld<Main>(nameof(Main.gameMenu)));

            c.EmitPop();
            c.EmitDelegate(UsingDefaultMenu);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    private static bool UsingDefaultMenu()
    {
        ModMenu menu = MenuLoader.currentMenu;

            // Because most mods completely cover up the background - and credits text <3 - with their own visuals;
            // I'm being safe and only playing ambience on the vanilla menus.
        return menu is MenutML ||
            menu is MenuJourneysEnd ||
            menu is MenuOldVanilla;
    }

    private void DrawMenuRain(On_Main.orig_DrawBackgroundBlackFill orig, Main self)
    {
        orig(self);

        if (!Main.gameMenu)
            return;

        DrawRain();
    }

    private static void DrawRain()
    {
        SpriteBatch spriteBatch = Main.spriteBatch;

        Rectangle frame = new(0, 0, 2, 40);

        Color sky = Main.ColorOfTheSkies * .9f;

        Color baseColor = new(Math.Max(sky.R, (byte)105), Math.Max(sky.G, (byte)115), Math.Max(sky.B, (byte)125), sky.A);

        foreach (Rain rain in Main.rain.Where(r => r.active))
        {
            frame.X = rain.type * 4;

            Color color = baseColor;

            if (RealisticSkySystem.IsEnabled)
                color = RealisticSkySystem.GetRainColor(color, rain);

            Texture2D texture = TextureAssets.Rain.Value;

            if (rain.waterStyle >= 15)
                texture = LoaderManager.Get<WaterStylesLoader>().Get(rain.waterStyle).GetRainTexture().Value;

            Vector2 position = rain.position - Main.screenPosition;

            if (SloprainSystem.IsEnabled)
                SloprainSystem.QueueRain(() =>
                    spriteBatch.Draw(texture, position, frame, color, rain.rotation, Vector2.Zero, rain.scale, SpriteEffects.None, 0f));
            else
                spriteBatch.Draw(texture, position, frame, color, rain.rotation, Vector2.Zero, rain.scale, SpriteEffects.None, 0f);

            rain.Update();
        }
    }

    #endregion
}

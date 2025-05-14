using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.ModLoader;
using ZensSky.Common.Systems.Stars;
using ZensSky.Common.Systems.SunAndMoon;

namespace ZensSky;

public class ZensSky : Mod 
{
    #region ModCalls

    public override object Call(params object[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
            throw new ArgumentException("Mod.Call arguments array cannot be of length zero!");

        if (args[0] is not string call)
            throw new ArgumentException("First Mod.Call argument is not of Type 'string'!");

        switch (call)
        {
            case "getStarAlpha":
                return StarSystem.StarAlpha;

            case "setStarAlpha":
                {
                    if (args[1] is float alpha)
                    {
                        StarSystem.TemporaryStarAlpha = alpha;
                        return true;
                    }
                    return false;
                }

            case "regenerateStars":
                {
                    StarSystem.GenerateStars();
                    return true;
                }

            case "drawStars":
                {
                    if (StarSystem.StarAlpha > 0 && 
                        args[1] is SpriteBatch spriteBatch &&
                        args[2] is Vector2 screenCenter &&
                        args[3] is float alpha)
                    {
                        StarTargetContent.DrawStars(spriteBatch, screenCenter, alpha);
                        return true;
                    }
                    return false;
                }

            case "drawSun":
                {
                    if (StarSystem.StarAlpha > 0 &&
                        args[1] is SpriteBatch spriteBatch &&
                        args[2] is GraphicsDevice device)
                    {
                        SunAndMoonTargetContent.DrawSunAndMoon(spriteBatch, device);
                        return true;
                    }
                    return false;
                }
        }

        return false;
    }

    #endregion
}

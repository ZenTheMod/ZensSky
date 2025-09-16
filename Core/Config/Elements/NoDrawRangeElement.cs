using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using ZensSky.Core.Exceptions;
using static System.Reflection.BindingFlags;

namespace ZensSky.Core.Config.Elements;

/// <summary>
/// Acts the exact same as a <see cref="PrimitiveRangeElement&lt;T&gt;"/> however does not draw the slider element, nor run any of its logic;
/// however still counts as a <see cref="PrimitiveRangeElement&lt;T&gt;"/> preventing interaction issues.
/// </summary>
[Autoload(Side = ModSide.Client)]
public abstract class NoDrawRangeElement<T> : PrimitiveRangeElement<T>, ILoadable where T : IComparable<T>
{
    #region Private Fields

    private static ILHook? PatchDrawSelf;

    private static bool IsDrawing;

    #endregion

    #region Loading

    public void Load(Mod mod)
    {
        Main.QueueMainThreadAction(() => {
            MethodInfo? drawSelf = typeof(RangeElement).GetMethod("DrawSelf", NonPublic | Instance);

            if (drawSelf is not null)
                PatchDrawSelf = new(drawSelf,
                    SkipRangeElementDrawing);
        });
    }

    public void Unload() =>
        Main.QueueMainThreadAction(() => PatchDrawSelf?.Dispose());

    private void SkipRangeElementDrawing(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel jumpret = c.DefineLabel();

            c.GotoNext(MoveType.After,
                i => i.MatchCall<ConfigElement>("DrawSelf"));

            c.EmitDelegate(() => IsDrawing);

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
        IsDrawing = true;
        base.DrawSelf(spriteBatch);
        IsDrawing = false;
    }

    #endregion
}

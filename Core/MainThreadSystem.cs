using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Core.Utils;

namespace ZensSky.Core;

    // Only load on clients as servers should not use the main thread queue. 
[Autoload(Side = ModSide.Client)]
public sealed class MainThreadSystem : ModSystem
{
    #region Private Fields

    private static readonly Queue<Action> MainThreadActions = [];

    #endregion

    #region Loading

        // TODO: Make the thread blocking more consistent for actions in inconvenient methods. (Perhaps make enqueuing force a wait???)
        // Use OnModLoad to wait for all actions to dequeue; counter-intuitivly this hook is ran AFTER all ILoadables finish loading.
    public override void OnModLoad()
    {
        Main.QueueMainThreadAction(() =>
            On_Main.DoUpdate += DequeueActions);

            // Block loading thread until all items have been dequeued. (Bad idea.)
        Utilities.WaitUntil(() => MainThreadActions.Count <= 0, 1).GetAwaiter().GetResult();
    }

    public override void OnModUnload() =>
        Main.QueueMainThreadAction(() => On_Main.DoUpdate -= DequeueActions);

    #endregion

    #region Dequeuing

        // Detour DoUpdate over ConsumeAllMainThreadActions to prevent, get this, a race condition!
    private void DequeueActions(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
    {
        orig(self, ref gameTime);

        if (MainThreadActions.Count <= 0)
            return;

        Main.QueueMainThreadAction(() =>
        {
            if (MainThreadActions.TryDequeue(out Action? action))
                action?.Invoke();
        });
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Queues actions to run on the main thread, but will only dequeue one action per frame; useful for preventing freezes during loading.<br/>
    /// Will switch to use <see cref="Main.QueueMainThreadAction"/> during unloading.<br/><br/>
    /// If on a server <paramref name="action"/> will be invoked directly, as <see cref="Main.Update"/> is not ran on servers with 0 connected players.
    /// </summary>
    public static void Enqueue(Action action)
    {
        if (Main.dedServ)
            action();
        else if (ZensSky.Unloading)
            Main.QueueMainThreadAction(() => action());
        else
            MainThreadActions.Enqueue(action);
    }

    public static void ClearQueue() =>
        MainThreadActions.Clear();

    #endregion
}

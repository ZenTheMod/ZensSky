using System;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

namespace ZensSky.Core.Utils;

public static partial class Utilities
{
    #region Public Properties

    public static Rectangle ScreenDimensions => new(0, 0, Main.screenWidth, Main.screenHeight);

    public static Vector2 ScreenSize => new(Main.screenWidth, Main.screenHeight);

    public static Vector2 HalfScreenSize => ScreenSize * .5f;

    public static Vector2 MousePosition => new(PlayerInput.MouseX, PlayerInput.MouseY);

    public static Vector2 UIMousePosition => UserInterface.ActiveInstance.MousePosition;

    #endregion

    /// <summary>
    /// Blocks thread until <paramref name="condition"/> returns <see cref="true"/> or timeout occurs.
    /// </summary>
    /// <param name="frequency">The frequency at which <paramref name="condition"/> will be checked, in milliseconds.</param>
    /// <param name="timeout">The timeout in milliseconds.</param>
    public static async Task WaitUntil(Func<bool> condition, int frequency = 1, int timeout = -1)
    {
        Task? waitTask = Task.Run(async () =>
        {
            while (!condition())
                await Task.Delay(frequency);
        });

        if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
            throw new TimeoutException();
    }
}

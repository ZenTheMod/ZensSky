using System;
using System.Linq;
using System.Reflection;
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

    /// <summary>
    /// Checks if <paramref name="methodInfo"/>'s arguments matches the types of <paramref name="arguments"/>.
    /// </summary>
    public static bool MatchesArguments(this MethodInfo methodInfo, object?[]? arguments)
    {
        ParameterInfo[] parameters = methodInfo.GetParameters();

        if (parameters.Length != (arguments?.Length ?? 0))
            return false;

        if (parameters.Length <= 0)
            return true;

        for (int i = 0; i < parameters.Length; i++)
            if (parameters[i].ParameterType != arguments?[i]?.GetType())
                return false;

        return true;
    }
}

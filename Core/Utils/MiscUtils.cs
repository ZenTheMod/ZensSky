using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;
using ZensSky.Common.DataStructures;

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

    #region Arrays

    /// <param name="accending">
    ///     <see cref="false"/> – The instance should be found based on preceding order.<br/>
    ///     <see cref="true"/> – The instance should be found based on accending order.<br/>
    /// </param>
    public static T CompareFor<T>(this T[] array, Func<T, IComparable> getComparable, bool accending = true)
    {
        int index = 0;

        IComparable lastcomparison = getComparable(array[0]);

        for (int i = 1; i < array.Length; i++)
        {
            T item = array[i];

            IComparable compare = getComparable(item);

            if ((compare.CompareTo(lastcomparison) >= 0) == accending)
            {
                index = i;
                lastcomparison = compare;
            }
        }

        return array[index];
    }

    /// <param name="accending">
    ///     <see cref="false"/> – The instance should be found based on preceding order.<br/>
    ///     <see cref="true"/> – The instance should be found based on accending order.<br/>
    /// </param>
    public static T CompareFor<T>(this List<T> list, Func<T, IComparable> getComparable, bool accending = true)
    {
        int index = 0;

        IComparable lastcomparison = getComparable(list[0]);

        for (int i = 1; i < list.Count; i++)
        {
            T item = list[i];

            IComparable compare = getComparable(item);

            if ((compare.CompareTo(lastcomparison) >= 0) == accending)
            {
                index = i;
                lastcomparison = compare;
            }
        }

        return list[index];
    }

    #endregion
}

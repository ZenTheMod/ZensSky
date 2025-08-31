using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using ZensSky.Core.Systems.ModCall;
using static System.Reflection.BindingFlags;

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

    #region Async

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

    #endregion

    #region Reflection

    /// <summary>
    /// Checks if <paramref name="methodInfo"/>'s arguments matches the types of <paramref name="arguments"/>.
    /// </summary>
    public static bool MatchesParameters(this MethodInfo methodInfo, object?[]? arguments)
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

    /// <returns>All methods in <paramref name="assembly"/> with the attribute <typeparamref name="T"/>.</returns>
    public static MethodInfo[] GetAllDecoratedMethods<T>(this Assembly assembly, BindingFlags flags = Public | NonPublic | Static) where T : Attribute =>
        [.. 
            assembly.GetTypes()
            .SelectMany(t => t.GetMethods(flags))
            .Where(m => m.GetCustomAttribute<T>() is not null &&
                !m.IsGenericMethod)
        ];

    /// <summary>
    /// <inheritdoc cref="ModContent.GetInstance"/><br/>
    /// However uses <paramref name="type"/> over a generic argument.
    /// </summary>
    /// <returns>The instance of the object with type of <paramref name="type"/>.</returns>
    public static object GetInstance(Type type) =>
        ContentInstance.contentByType[type].instance;

    #endregion

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
    public static T CompareFor<T>(this IEnumerable<T> collection, Func<T, IComparable> getComparable, bool accending = true)
    {
        T matching = collection.First();
        IComparable lastcomparison = getComparable(matching);

        foreach (T item in collection)
        {
            IComparable compare = getComparable(item);

            if ((compare.CompareTo(lastcomparison) >= 0) == accending)
            {
                matching = item;
                lastcomparison = compare;
            }
        }

        return matching;
    }

    #endregion
}

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using static System.Reflection.BindingFlags;

namespace ZensSky.Core.Utils;

    // The C# 14.0 'extension' block seems to still be a little buggy.
#pragma warning disable CA1822 // Member does not access instance data and can be marked as static.

public static partial class Utilities
{
    #region Public Properties

    public static Rectangle ScreenDimensions =>
        new(0, 0, Main.screenWidth, Main.screenHeight);

    public static Vector2 ScreenSize =>
        new(Main.screenWidth, Main.screenHeight);

    public static Vector2 HalfScreenSize =>
        ScreenSize * .5f;

    public static Vector2 MousePosition =>
        new(PlayerInput.MouseX, PlayerInput.MouseY);

    public static Vector2 UIMousePosition =>
        UserInterface.ActiveInstance.MousePosition;

    public static float TimeRatio =>
        Terraria.Utils.GetDayTimeAs24FloatStartingFromMidnight() / 24f;

    #endregion

    #region Time

    public static string GetReadableTime() =>
        GetReadableTime(Terraria.Utils.GetDayTimeAs24FloatStartingFromMidnight());

    public static string GetReadableTime(float time)
    {
        int hour = (int)MathF.Floor(time % 24);

        int minute = (int)MathF.Floor(time % 1 * 100 * .6f);

        DateTime date = new(1, 1, 1, hour, minute, 0);

        return date.ToShortTimeString();
    }

    #endregion

    #region Color

    public static Color FromHex3(string hexString)
    {
        Color output = new();

        if (uint.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint hex))
        {
            uint r = (hex >> 16) & 0xFFu;
            uint g = (hex >> 8) & 0xFFu;
            uint b = hex & 0xFFu;

            output = new((int)r, (int)g, (int)b);
        }

        return output;
    }

    #endregion

    #region Tasks

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

    #region Attributes

    /// <returns>All methods in <paramref name="assembly"/> with the attribute <typeparamref name="T"/>.</returns>
    public static IEnumerable<MethodInfo> GetAllDecoratedMethods<T>(this Assembly assembly, BindingFlags flags = Public | NonPublic | Static, bool inherit = true) where T : Attribute =>
        assembly.GetTypes()
            .SelectMany(t => t.GetMethods(flags))
            .Where(m => m.GetCustomAttribute<T>(inherit) is not null &&
                !m.IsGenericMethod);


    /// <returns>All types in <paramref name="assembly"/> with the attribute <typeparamref name="T"/>.</returns>
    public static IEnumerable<Type> GetAllDecoratedTypes<T>(this Assembly assembly, bool inherit = true) where T : Attribute =>
        assembly.GetTypes()
            .Where(m => m.GetCustomAttribute<T>(inherit) is not null);

    #endregion

    #region Singleton Instances

    /// <inheritdoc cref="ModContent.GetInstance"/>
    /// <returns>The singleton instance of <paramref name="type"/>.</returns>
    public static object GetInstance(Type type) =>
        ContentInstance.contentByType[type].instance;

    public static bool TryGetInstance(Type type, [NotNullWhen(true)] out object? obj)
    {
        obj = null;

        if (!ContentInstance.contentByType.TryGetValue(type, out ContentInstance.ContentEntry? entry) ||
            entry is null)
            return false;

        obj = entry.instance;

        return true;
    }

    #endregion

    #region PropertyFieldWrapper

    extension(PropertyFieldWrapper member)
    {
        public bool IsStatic =>
            member.fieldInfo?.IsStatic ??
            member.propertyInfo.GetAccessors(true)[0].IsStatic;
    }

    #endregion

    #endregion

    #region Collections

    /// <param name="accending">
    ///     <list type="bullet">
    ///         <item>
    ///             <term><see cref="false"/></term>
    ///             The instance of <typeparamref name="T"/> should be found based on preceding order.
    ///         </item>
    ///         <item>
    ///             <term><see cref="true"/></term>
    ///             The instance of <typeparamref name="T"/> should be found based on accending order.
    ///         </item>
    ///     </list>
    /// </param>
    public static T CompareFor<T>(
        this IEnumerable<T> collection,
        Func<T, IComparable> getComparable,
        bool accending = true)
    {
        T matching = collection.First();
        IComparable lastComparison = getComparable(matching);

        foreach (T item in collection)
        {
            IComparable compare = getComparable(item);

            if ((compare.CompareTo(lastComparison) >= 0) == accending)
            {
                matching = item;
                lastComparison = compare;
            }
        }

        return matching;
    }

    /// <inheritdoc cref="CompareFor{T}(IEnumerable{T}, Func{T, IComparable}, bool)"/>
    public static T CompareFor<T, TComparable>(
        this IEnumerable<T> collection,
        Func<T, TComparable> getComparable,
        out TComparable lastComparison,
        bool accending = true) where TComparable : IComparable
    {
        T matching = collection.First();
        lastComparison = getComparable(matching);

        foreach (T item in collection)
        {
            TComparable compare = getComparable(item);

            if ((compare.CompareTo(lastComparison) >= 0) == accending)
            {
                matching = item;
                lastComparison = compare;
            }
        }

        return matching;
    }

    public static IEnumerable<char> Range(this char start, char end) =>
        Enumerable.Range(start, end - start + 1).Select(i => (char)i);

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T As<T>(this object @this) where T : class =>
        (T)@this;
}

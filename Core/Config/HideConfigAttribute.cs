using System;
using Terraria.ModLoader.Config;

namespace ZensSky.Core.Config;

/// <summary>
/// Hides the decorated <see cref="ModConfig"/> from the config list.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class HideConfigAttribute : Attribute;

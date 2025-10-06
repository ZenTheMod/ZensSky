using System;
using static System.AttributeTargets;

namespace ZensSky.Core.Config;

/// <summary>
/// Makes the the decorated type, and all its members, not be localized.
/// </summary>
[AttributeUsage(Class | AttributeTargets.Enum, AllowMultiple = false)]
public sealed class NoConfigLocalizationAttribute : Attribute;

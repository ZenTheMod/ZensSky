using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;

namespace ZensSky.Common.Registries;

public static partial class Textures
{
    private static readonly Lazy<Asset<Texture2D>> _lockedToggle = new(() => Request("UI/LockedSettingsToggle"));

    private static readonly Lazy<Asset<Texture2D>> _modDeps = new(() => Request("UI/PanelStyle/ModDeps"));
    private static readonly Lazy<Asset<Texture2D>> _modConfig = new(() => Request("UI/PanelStyle/ModConfig"));
    private static readonly Lazy<Asset<Texture2D>> _modInfo = new(() => Request("UI/PanelStyle/ModInfo"));

    private static readonly Lazy<Asset<Texture2D>> _panelGradient = new(() => Request("UI/PanelStyle/PanelGradient"));

    private static readonly Lazy<Asset<Texture2D>> _slider = new(() => Request("UI/Slider"));
    private static readonly Lazy<Asset<Texture2D>> _sliderHighlight = new(() => Request("UI/SliderHighlight"));

    private static readonly Lazy<Asset<Texture2D>> _buttonHover = new(() => Request("UI/SunMoonOutline"));

    public static Asset<Texture2D> LockedToggle => _lockedToggle.Value;

    public static Asset<Texture2D> ModDeps => _modDeps.Value;
    public static Asset<Texture2D> ModConfig => _modConfig.Value;
    public static Asset<Texture2D> ModInfo => _modInfo.Value;

    public static Asset<Texture2D> PanelGradient => _panelGradient.Value;

    public static Asset<Texture2D> Slider => _slider.Value;
    public static Asset<Texture2D> SliderHighlight => _sliderHighlight.Value;

    public static Asset<Texture2D> ButtonHover => _buttonHover.Value;
}

# Zens Sky
This mod aims to overhaul Terraria's sky with a number of highly customizable visuals changes; some of which mirror the upcoming Terraria 1.4.5 update's visuals.

## Current Planned Features
Checkbox shown next to currently complete features.
- [x] Complete star overhaul including supernovae, and shooting stars.
    - Supernovae are very-much WIP.
- [x] Sun and moon overhaul that completely replaces the vanilla visuals.
    - Including support for all [Moon Styles](https://terraria.wiki.gg/wiki/Moon_phase#Notes).
- [x] Minor ambience changes e.g. wind particles.
- [x] Panel for configuring the titlescreen.
- [x] Configuration to pixelate the sky.
- [x] Dynamic cloud lighting in the style of the Terraria 1.4.5 update.
- [x] Darker night sky.
- [ ] Overhauled shimmer biome visuals.
- [ ] Overhauled background meteor visuals.
- [ ] Lightning visual rework.
- [ ] Aurora borealis while in cold biomes.

## Compatiblity

### Current Compat

This mod features cross compatibility with a large amount of popular mods that may tweak or otherwise interact with the visuals of the sky.

- 'IDG's Better Night Sky' by IDGCaptainRussia and Trivaxy\*[^BetterNightSky]
- 'Calamity Fables' by IbanPlay\*[^CalamityFables]
- 'Calamity Mod' by Ozzatron\*[^CalamityMod]
- 'Dark Surface' by 2bluntz@once
- 'Fancy Lighting' by Rex
- 'High FPS Support' by Stellar
- 'Lights And Shadows' by yiyang233\*[^LightsAndShadows]
- 'Macrocosm [BETA]' by MechanicPluto24\*\*\*[^Macrocosm]
- 'Rain Overhaul' by supchyan
- 'Rain++' by shrek
- 'Realistic Sky' by Lucille Karma
- 'Red Sun and Realistic Sky' by Waffles22
- 'Wrath of the Gods' by Lucille Karma\*[^WrathOfTheGods]
- 'You Boss' by Lucille Karma

If any issues arise with any mods, including the above; please bring up an issue [here](https://github.com/ZenTheMod/ZensSky/issues).

### Adding Compat

This mod allows for other mod creators to easily include their own compat with 'Mod Calls.'

Example:
```cs
if (!ModLoader.TryGetMod("ZensSky", out Mod zensSky))
    return;

zensSky.Call({MethodAlias}, {Arguments});
 ```

Where {MethodAlias} would be the name of the method you wish to call, and {Arguments} would be the full arguments of that method.\
A full list of the available methods and their aliases can be found [here](https://github.com/search?q=repo%3AZenTheMod%2FZensSky%20ModCall&type=code), or by searching this repository for the `[ModCall]` attribute.\
If you feel any feature lacks certain cross compatibility that you'd like, please bring up an issue [here](https://github.com/ZenTheMod/ZensSky/issues).

## Credits

### Developer 
- Zoey (`@z_e_n_.`)

### Help with Development 
- Sprunolia
- Tomat
- Ebonfly
- Roton
- Lion8Cake
- jupiter.ryo
- Azathoth
- Plushie
- Tuna
- Oli

### Textures 
- Sprunolia
- [​Space Engine](https://spaceengine.org/)
- [​Solar System Scope Textures](https://www.solarsystemscope.com/textures/)
- [​USGS](https://www.usgs.gov/)
- ​[Björn Jónsson's Planetary Maps](https://bjj.mmedia.is/)
- Ebonfly

### 3D Modeling 
- Sprunolia
- Zoey
- Yonmaruyon

[^BetterNightSky]: May lack support for some visuals.
[^CalamityFables]: As of writing not all [Fables Moon Styles](https://calamityfables.wiki.gg/wiki/Vanilla_changes#Vanity_moons) have been properly accounted for; currently incomplete moons includes:
    - Throne
    - Iris
    - Crater
    - Sea
    - Shatter* (Although all assets have been created the current implementation could use work.)
    - Artifact
    - Striped
    - Greenhouse
    - Square
    - Aletheia
[^CalamityMod]: Untested.
[^LightsAndShadows]: There are a few issues I would like to fix; (e.g. the effect not showing on the titlescreen.)
    However these issues are of Lights and Shadow's devoloper(s), and I don't intend to rewrite their mod to account for it.

    More specifically Lights and Shadows should move to a proper Filter based system to prevent common issues with detouring FilterManager.EndCapture.
[^Macrocosm]: Working with dev team to make direct changes to mod over hacky implementation.
[^WrathOfTheGods]: Currently no changes made to any stargazing scenes.

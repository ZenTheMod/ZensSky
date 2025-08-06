# Zens Sky

This mod aims to overhaul Terraria's sky with a number of highly customizable visuals changes; some of which mirror the upcoming Terraria 1.4.5 update's visuals.
Included with this mod is a dynamic menu controller panel allowing for customization of the main menu itself.

## Current Planned Features
Checkbox shown next to currently complete features.
- [x] Complete star overhaul[^1] including supernovae, and shooting stars.
- [x] Sun and moon[^2] overhaul that completely replaces the vanilla visuals.
- [x] Minor ambience changes e.g. wind particles.
- [x] Configuration to pixelate the sky.
- [x] Dynamic cloud lighting in the style of the Terraria 1.4.5 update.
- [ ] Overhauled background meteor visuals.
- [ ] Lightning visual rework.
- [ ] Aurora borealis while in cold biomes.

## Cross Compatiblity
This mod features cross compatibility with a large amount of popular mods that may tweak or otherwise interact with the visuals of the sky.
- 'Macrocosm [BETA]' by MechanicPluto24\*\*\*[^3]
- 'Realistic Sky' by Lucille Karma
- 'Wrath of the Gods' by Lucille Karma\*[^4]
- 'Lights And Shadows' by yiyang233
- 'Calamity Fables' by IbanPlay\*[^5]
- 'Calamity Mod' by Ozzatron\*[^6]
- 'Red Sun and Realistic Sky by Waffles22
- 'IDG's Better Night Sky' by IDGCaptainRussia and Trivaxy\*[^7]

This mod also allows for other mod creators to very easily include their own compat;
There are two methods to do so.

1. Mod.Call
   Mod.Call can be used when it may be unnecessary to use a project reference.
   
   Example:
   ```cs
   if (!ModLoader.TryGetMod("ZensSky", out Mod zensSky))
       return;

   zensSky.Call({MethodAlias}, {Arguments});
   ```
   Where {MethodAlias} would be the name of the method you wish to call, and {Arguments} would be the full arguments of that method.

   A full list of the available methods and their aliases can be found [here](https://github.com/search?q=repo%3AZenTheMod%2FZensSky%20ModCall&type=code), or by searching this repository for the ModCall attribute.
2. At times Mod.Call may not allow you the features you want, in which case you may want to use the `weakReferences` build property, you can find specifics [here](https://github.com/tModLoader/tModLoader/wiki/build.txt).
   This will alow you more customization if you require that, most notably with the ability to use `AdditionalMoonDrawing` over `AdditionalMoonStyles`.

If you feel any feature lacks certain cross compatibility that you'd like, please bring up an issue [here](https://github.com/ZenTheMod/ZensSky/issues).

### Footnotes
[^1]: Includes five different star visual styles to choose from.
[^2]: Unique high definition visuals for every vanilla [Moon Style](https://terraria.wiki.gg/wiki/Moon_phase#Notes).
[^3]: Currently no high resolution earth assets; although I will be working on including these (As well as other high resolution assets and models,) in the offical Macrocosm mod.
[^4]: Currently no changes made to any stargazing scenes.
[^5]: As of writing not all [Fables Moon Styles](https://calamityfables.wiki.gg/wiki/Vanilla_changes#Vanity_moons) have been properly accounted for; currently incomplete moons includes:
    - Throne
    - Iris
    - Crater
    - Sea
    - Shatter* (Although all* assets have been created the current implementation could use work.)
    - Artifact
    - Striped
    - Greenhouse
    - Square
    - Aletheia
[^6]: Untested, but I'm going to say that it works.
[^7]: Lacks support for some visuals.

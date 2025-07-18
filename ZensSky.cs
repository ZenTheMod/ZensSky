using ReLogic.Content.Sources;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Core.AssetReaders;
using ZensSky.Core.Systems;
using ZensSky.Core.Systems.ModCall;

#pragma warning disable CS8603 // Possible null reference return.

namespace ZensSky;

public sealed class ZensSky : Mod
{
    #region Public Properties

    public static bool CanDrawSky { get; private set; }

    public static bool Unloading { get; private set; }

    #endregion

    public override void Close()
    {
        Unloading = true;
        MainThreadSystem.ClearQueue();

        base.Close();
    }

    public override void PostSetupContent() => 
        CanDrawSky = true;

    public override IContentSource CreateDefaultContentSource()
    {
        if (!Main.dedServ)
            AddContent(new OBJReader());

        return base.CreateDefaultContentSource();
    }

    public override object Call(params object[] args)
    {
        if (args.Length <= 0)
            throw new ArgumentException("Zero arguments provided!");

        if (args[0] is not string name)
            throw new ArgumentException("Argument zero was not a string!");

        return ModCallSystem.HandleCall(name, [.. args.Skip(1)]);
    }

    /*
        private static IOrderedLoadable?[]? Cache;

        public override void Load()
        {
            Type[] loadable = [.. AssemblyManager.GetLoadableTypes(Code)
                .Where(t => !t.IsAbstract && !t.ContainsGenericParameters && t.GetInterfaces().Contains(typeof(IOrderedLoadable)))];

            if (loadable.Length <= 0)
                return;

            Cache = new IOrderedLoadable[loadable.Length];

            for (int i = 0; i < loadable.Length; i++)
            {
                object? instance = Activator.CreateInstance(loadable[i]);

                if (!AutoloadAttribute.GetValue(loadable[i]).NeedsAutoloading)
                    continue;

                Cache[i] = instance as IOrderedLoadable;
            }

            Array.Sort(Cache, (n, t) => n?.Index.CompareTo(t?.Index) ?? 0);

            Array.ForEach(Cache, l => l?.Load());
        }

        public override void Unload()
        {
            if (Cache is null)
                return;

            for (int i = Cache.Length - 1; i >= 0; i--)
                Cache[i]?.Unload();
        }
    */
}

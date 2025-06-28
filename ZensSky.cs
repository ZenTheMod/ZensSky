using Terraria.ModLoader;

namespace ZensSky;

public sealed class ZensSky : Mod
{
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

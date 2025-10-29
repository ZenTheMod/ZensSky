using static ZensSky.Common.Systems.Sky.Lighting.SkyLight;

namespace ZensSky.Common.Systems.Sky.Lighting;

public static class SkyLightHooks<T> where T : SkyLight
{
    #region Public Hooks

    public static event hook_OnGetInfo OnGetInfo
    {
        add
        {
            foreach (SkyLight item in SkyLightSystem.Lights)
            {
                if (item is not T)
                    continue;

                item.OnGetInfo += value;
                break;
            }
        }
        remove
        {
            foreach (SkyLight item in SkyLightSystem.Lights)
            {
                if (item is not T)
                    continue;

                item.OnGetInfo -= value;
                break;
            }
        }
    }

    #endregion
}

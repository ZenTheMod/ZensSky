using Daybreak.Common.Features.Hooks;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace ZensSky.Core.Net;

public static class NetMessageHooks
{
    #region Public Hooks

    public delegate void hook_OnSyncWorldData(int toClient, int ignoreClient);

    public static event hook_OnSyncWorldData? OnSyncWorldData;

    #endregion

    #region Loading

    [OnLoad]
    public static void Load() =>
        On_NetMessage.SendData += InvokeMethods;

    [OnUnload]
    public static void Unload()
    {
        OnSyncWorldData = null;

        On_NetMessage.SendData -= InvokeMethods;
    }

    #endregion

    #region Private Methods

    private static void InvokeMethods(On_NetMessage.orig_SendData orig,
        int msgType,
        int remoteClient,
        int ignoreClient,
        NetworkText text,
        int number,
        float number2,
        float number3,
        float number4,
        int number5,
        int number6,
        int number7)
    {
        orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);

        if (msgType == MessageID.WorldData)
            OnSyncWorldData?.Invoke(remoteClient, ignoreClient);
    }

    #endregion
}

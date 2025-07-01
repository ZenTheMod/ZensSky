using ReLogic.Content;
using System;
using Terraria.ModLoader;
using ZensSky.Core.DataStructures;

namespace ZensSky.Common.Registries;

public static class Models
{
    private const string Prefix = "ZensSky/Assets/Models/";

    private static readonly Lazy<Asset<OBJModel>> _shatter = new(() => Request("Shatter"));

    public static Asset<OBJModel> Shatter => _shatter.Value;

    private static Asset<OBJModel> Request(string path) => ModContent.Request<OBJModel>(Prefix + path);
}

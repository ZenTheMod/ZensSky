using ReLogic.Content;
using ReLogic.Content.Readers;
using ReLogic.Utilities;
using System.IO;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Core.DataStructures;

namespace ZensSky.Core.AssetReaders;

    // Based on Overhauls OvgReader implementation. - https://github.com/Mirsario/TerrariaOverhaul/blob/dev/Core/VideoPlayback/OgvReader.cs
[Autoload(false)]
public sealed class OBJReader : IAssetReader, ILoadable
{
    public static readonly string Extension = ".obj";

    #region Loading

    public void Load(Mod mod)
    {
        AssetReaderCollection? assetReaderCollection = Main.instance.Services.Get<AssetReaderCollection>();

        if (!assetReaderCollection.TryGetReader(Extension, out IAssetReader reader) || reader != this)
            assetReaderCollection.RegisterReader(this, Extension);
    }

    public void Unload() { }

    /*
        public void Unload()
        {
            AssetReaderCollection? assetReaderCollection = Main.instance.Services.Get<AssetReaderCollection>();

            if (assetReaderCollection.TryGetReader(Extension, out IAssetReader reader) && reader == this)
                assetReaderCollection.RemoveExtension(Extension);
        } 
    */

    #endregion

    public async ValueTask<T> FromStream<T>(Stream stream, MainThreadCreationContext mainThreadCtx) where T : class
    {
        if (typeof(T) != typeof(OBJModel))
            throw AssetLoadException.FromInvalidReader<OBJReader, T>();

        await mainThreadCtx;

        OBJModel? result = OBJModel.Create(stream);

        return (result as T)!;
    }
}

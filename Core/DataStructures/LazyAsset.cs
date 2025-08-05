using ReLogic.Content;
using System;
using Terraria.ModLoader;

namespace ZensSky.Core.DataStructures;

/// <inheritdoc cref="Asset{T}"/>
public readonly record struct LazyAsset<T> where T : class
{
    #region Private Fields

    private readonly Lazy<Asset<T>> _asset;

    #endregion

    #region Public Properties

    public readonly Asset<T> Asset => 
        _asset.Value;

    /// <inheritdoc cref="Asset{T}.Value"/>
    public readonly T Value => 
        Asset.Value;

    public readonly bool IsReady => 
        Asset is not null && 
        Value is not null && 
        Asset.IsLoaded &&
        !Asset.IsDisposed;

    #endregion

    /// <inheritdoc cref="ModContent.Request{T}"/>
    public LazyAsset(string name) =>
        _asset = new(() => ModContent.Request<T>(name));

    #region Public Operators

    public static implicit operator Asset<T>(LazyAsset<T> asset) =>
        asset.Asset;

    public static implicit operator T(LazyAsset<T> asset) =>
        asset.Value;

    #endregion
}

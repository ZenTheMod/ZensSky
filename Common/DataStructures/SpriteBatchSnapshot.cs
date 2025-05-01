using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace ZensSky.Common.DataStructures;

/// <summary>
///     Contains the data for a <see cref="SpriteBatch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, RasterizerState, Effect, Matrix)"/> call.
///     <br/>This implementation requires use of a publicizer, I've chosen to use krafs as it has the simplest setup I've found. 
///     <br/>You can find this in the .csproj file.
/// </summary>
public readonly struct SpriteBatchSnapshot
{
    private static readonly Matrix identityMatrix = Matrix.Identity;

    public SpriteSortMode SortMode { get; }
    public BlendState BlendState { get; }
    public SamplerState SamplerState { get; }
    public DepthStencilState DepthStencilState { get; }
    public RasterizerState RasterizerState { get; }
    public Effect? Effect { get; }
    public Matrix TransformationMatrix { get; }

    public SpriteBatchSnapshot(SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendState? blendState = null, SamplerState? samplerState = null, DepthStencilState? depthStencilState = null, RasterizerState? rasterizerState = null, Effect? effect = null, Matrix? transformationMatrix = null)
    {
        SortMode = sortMode;
        BlendState = blendState ?? BlendState.AlphaBlend;
        SamplerState = samplerState ?? SamplerState.LinearClamp;
        DepthStencilState = depthStencilState ?? DepthStencilState.None;
        RasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;
        Effect = effect;
        TransformationMatrix = transformationMatrix ?? identityMatrix;
    }

    /// <summary>
    /// Pull all the parameters from the last <see cref="SpriteBatch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, RasterizerState, Effect, Matrix)"/> call and create a new <see cref="SpriteBatchSnapshot"/> instance. 
    /// </summary>
    /// <param name="spriteBatch">The target <see cref="SpriteBatch"/> to pull data from.</param>
    /// <returns>A new <see cref="SpriteBatchSnapshot"/> instance with the parameters of the last <see cref="SpriteBatch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, RasterizerState, Effect, Matrix)"/> call.</returns>
    public static SpriteBatchSnapshot Capture(SpriteBatch spriteBatch)
    {
        SpriteSortMode sortMode = spriteBatch.sortMode;
        BlendState blendState = spriteBatch.blendState;
        SamplerState samplerState = spriteBatch.samplerState;
        DepthStencilState depthStencilState = spriteBatch.depthStencilState;
        RasterizerState rasterizerState = spriteBatch.rasterizerState;
        Effect effect = spriteBatch.customEffect;
        Matrix transformMatrix = spriteBatch.transformMatrix;

        return new SpriteBatchSnapshot(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
    }
}

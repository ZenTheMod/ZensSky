using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using ZensSky.Common.Config;
using ZensSky.Core.Particles;

namespace ZensSky.Common.DataStructures;

public record struct WindParticle : IParticle
{
    #region Private Fields

    private const int MaxOldPositions = 43;

    private const float WidthAmplitude = 2f;

    private const float LifeTimeIncrement = 0.004f;

    private const float SinLifeTimeFrequency = 7f;
    private const float SinGlobalTimeFrequency = .6f;

    private const float SinAmplitude = .1f;

    private const float LoopRange = .06f;

    private const float Magnitude = 13f;

    #endregion

    #region Public Properties

    public Vector2 Position { get; set; }

    public Vector2[] OldPositions { get; init; }

    public Vector2 Velocity { get; set; }

    public float Wind { get; init; }

    public bool ShouldLoop { get; init; }

    public float LifeTime { get; set; }

    public bool IsActive { get; set; }

    #endregion

    #region Public Constructors

    public WindParticle(Vector2 position, float wind, bool shouldLoop)
    {
        Position = position;
        OldPositions = new Vector2[MaxOldPositions];
        Velocity = Vector2.Zero;
        Wind = wind;
        ShouldLoop = shouldLoop;
        LifeTime = 0f;
        IsActive = true;
    }

    #endregion

    #region Updating

    void IParticle.Update()
    {
        float increment = LifeTimeIncrement * MathF.Abs(Wind);

        LifeTime += increment;
        if (LifeTime > 1f)
            IsActive = false;

        Vector2 newVelocity = new(Wind, 
            MathF.Sin((LifeTime * SinLifeTimeFrequency + Main.GlobalTimeWrappedHourly) * SinGlobalTimeFrequency) * SinAmplitude);

        if (ShouldLoop)
        {
            float range = LoopRange / MathHelper.Clamp(MathF.Abs(Wind), .01f, 1);
            range *= .5f;

            float interpolator = Utils.Remap(LifeTime, .5f - range, .5f + range, 0f, 1f);

            newVelocity = newVelocity.RotatedBy(MathHelper.TwoPi * interpolator * -MathF.Sign(Wind));
        }

        Velocity = newVelocity.SafeNormalize(Vector2.UnitY) * Magnitude * MathF.Abs(Wind);

        Position += Velocity;

            // Update the old positions.
        for (int i = OldPositions.Length - 2; i >= 0; i--)
            OldPositions[i + 1] = OldPositions[i];

        OldPositions[0] = Position;
    }

    #endregion

    #region Drawing

    readonly void IParticle.Draw(SpriteBatch spriteBatch, GraphicsDevice device)
    {
        Vector2[] positions = [.. OldPositions.Where(pos => pos != default)];

        if (positions.Length <= 2)
            return;

        VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[(positions.Length - 1) * 2];

        float brightness = MathF.Sin(LifeTime * MathHelper.Pi) * Main.atmo * MathF.Abs(Wind);

        float alpha = SkyConfig.Instance.WindOpacity;

        for (int i = 0; i < positions.Length - 1; i++)
        {
            float progress = (float)i / positions.Length;
            float width = MathF.Sin(progress * MathHelper.Pi) * brightness * WidthAmplitude;

            Vector2 position = Vector2.Transform(positions[i], spriteBatch.transformMatrix);

            float direction = (positions[i] - positions[i + 1]).ToRotation();
            Vector2 offset = new Vector2(width, 0).RotatedBy(direction + MathHelper.PiOver2);

            Point tilePosition = (position - Main.screenPosition).ToTileCoordinates();

            Color color = Lighting.GetColor(tilePosition).MultiplyRGB(Main.ColorOfTheSkies) * brightness * alpha;
            color.A = 0;

            vertices[i * 2] = new(new(position - offset, 0), color, new(progress, 0f));
            vertices[i * 2 + 1] = new(new(position + offset, 0), color, new(progress, 1f));
        }

        if (vertices.Length > 3)
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
    }

    #endregion
}

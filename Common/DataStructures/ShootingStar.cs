using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Utilities;
using ZensSky.Core.Utils;

namespace ZensSky.Common.DataStructures;

public record struct ShootingStar
{
    #region Private Fields

    private const int MaxOldPositions = 40;

    private const float WidthAmplitude = 1.3f;

    private const float StarRatio = .15f;

    private const float StarScale = .13f;

    private const float LifeTimeIncrement = .007f;

    private const float MinVelocity = 3.1f;
    private const float MaxVelocity = 6.3f;

    private const float MinRotate = -.009f;
    private const float MaxRotate = .009f;

    private const float VelocityDegrade = .97f;

    private const float StarGameDistance = 4800f;
    private const float StarGameReflect = 10f;

    #endregion

    #region Public Properties

    public Vector2 Position { get; set; }

    public Vector2[] OldPositions { get; init; }

    public Vector2 Velocity { get; set; }

    public float Rotate { get; init; }

    public float LifeTime { get; set; }

    public bool IsActive { get; set; }

    public bool Hit { get; set; }

    #endregion

    #region Public Constructors

    public ShootingStar(Vector2 position, UnifiedRandom rand)
    {
        Position = position;
        OldPositions = new Vector2[MaxOldPositions];
        Velocity = rand.NextVector2CircularEdge(1f, 1f) * rand.NextFloat(MinVelocity, MaxVelocity);
        Rotate = rand.NextFloat(MinRotate, MaxRotate);
        LifeTime = 1f;
        IsActive = true;
        Hit = false;
    }

    #endregion

    #region Drawing

        // TODO: Generic util method for primslop.
    public readonly void Draw(SpriteBatch spriteBatch, GraphicsDevice device, float alpha)
    {
        Vector2 pos = Position;

        Vector2[] positions = [.. OldPositions.Where(p => p != default && p != pos)];

        if (positions.Length <= 2)
            return;

        VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[(positions.Length - 1) * 2];

        Color color = Color.LightGray * alpha * MathF.Sin(LifeTime * MathHelper.Pi);
        color.A = 0;

        for (int i = 0; i < positions.Length - 1; i++)
        {
            float progress = (float)i / positions.Length;
            float width = MathF.Sin(progress * MathHelper.Pi) * WidthAmplitude;

            Vector2 position = positions[i];

            float direction = (position - positions[i + 1]).ToRotation();
            Vector2 offset = new Vector2(width, 0).RotatedBy(direction + MathHelper.PiOver2);

            vertices[i * 2] = new(new(position - offset, 0), color, new(progress, 0f));
            vertices[i * 2 + 1] = new(new(position + offset, 0), color, new(progress, 1f));
        }

        device.Textures[0] = SkyTextures.ShootingStar;

        if (vertices.Length > 3)
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);

        Texture2D starTexture = StarTextures.FourPointedStar;

        Vector2 starOrigin = starTexture.Size() * .5f;

        Vector2 starPosition = positions[(int)(positions.Length * StarRatio)];

        float scale = StarScale;

        spriteBatch.Draw(starTexture, starPosition, null, color, 0f, starOrigin, scale, SpriteEffects.None, 0f);
    }

    #endregion

    #region Updating

    public void Update()
    {
        LifeTime -= LifeTimeIncrement;
        if (LifeTime <= 0f)
            IsActive = false;

            // This is a really excessive way to lessen the velocity over time.
        float exponentialFade = Easings.OutExpo(LifeTime);
        Velocity *= MathHelper.Lerp(1f, VelocityDegrade, exponentialFade);

            // Have the shooting star curve slightly
        Velocity = Velocity.RotatedBy(Rotate);

        Position += Velocity;

            // Update the old positions.
        for (int i = OldPositions.Length - 2; i >= 0; i--)
            OldPositions[i + 1] = OldPositions[i];

        OldPositions[0] = Position;
    }

    public void StarGameUpdate()
    {
        Update();

        if (Hit || Position.DistanceSQ(Utilities.MousePosition) >= StarGameDistance)
            return;

        Main.starsHit++;

        float magnitude = Velocity.Length();

        Velocity = Position - Utilities.MousePosition;
        Velocity = Vector2.Normalize(Velocity) * magnitude * StarGameReflect;

        Hit = true;

        SoundEngine.PlaySound(in SoundID.CoinPickup);
        SoundEngine.PlaySound(in SoundID.Meowmere);
    }

    #endregion
}

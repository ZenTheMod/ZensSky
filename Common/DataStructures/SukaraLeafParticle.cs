using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using ZensSky.Core.Particles;

namespace ZensSky.Common.DataStructures;

public record struct SakuraLeafParticle : IParticle
{
    #region Private Fields

    private const int FrameTime = 8;
    private const int Frames = 4;

    private const float LifeTimeIncrement = .0033f;

    private const float WindX = 6.5f;

    #endregion

    #region Public Properties

    public Vector2 Position { get; set; }

    public float Rotation { get; set; }

    public Vector2 Velocity { get; set; }

    public int FrameTimer { get; set; }

    public int Frame { get; set; }

    public float LifeTime { get; set; }

    public bool IsActive { get; set; }

    #endregion

    #region Public Constructors

    public SakuraLeafParticle(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
        FrameTimer = 0;
        Frame = 0;
        LifeTime = 0f;
        IsActive = true;
    }

    #endregion

    #region Updating

    void IParticle.Update()
    {
        LifeTime += LifeTimeIncrement;

        if (LifeTime >= 1)
            IsActive = false;

            // Handle frames.
        if (++FrameTimer >= FrameTime)
        {
            FrameTimer = 0;

            if (++Frame >= Frames)
                Frame = 0;
        }

            // Modified vanilla logic for tree leaf updating.
        Vector2 newVelocity = Velocity;
        Vector2 newPosition = Position;

        Vector2 vector = Position + new Vector2(12f) / 2f - new Vector2(4f) / 2f;

        vector.Y -= 4f;

        Vector2 vector2 = Position - vector;

        if (newVelocity.Y < 0f)
        {
            Vector2 vector3 = new(newVelocity.X, -0.2f);

            newVelocity.Y = 0.1f;

            vector3.X *= 0.94f;

            newVelocity.X = vector3.X;
            newPosition.X += newVelocity.X;
            return;
        }

        newVelocity.Y += MathF.PI / 180f;

        Vector2 vector4 = Vector2.UnitY.RotatedBy(newVelocity.Y);
        
        vector4.X += WindX;

        newPosition += vector2;

        newPosition += vector4;

        float newRotation = vector4.ToRotation() + MathHelper.PiOver2;

        Velocity = newVelocity;
        Position = newPosition;
        Rotation = newRotation;
    }

    #endregion

    #region Drawing

    readonly void IParticle.Draw(SpriteBatch spriteBatch, GraphicsDevice _)
    {
        Texture2D texture = PanelStyleTextures.Leaf;

        Rectangle frame = texture.Frame(1, Frames, 0, Frame);

        Vector2 origin = frame.Size() * .5f;

        spriteBatch.Draw(PanelStyleTextures.Leaf, Position, frame, Color.White, Rotation, origin, 1f, SpriteEffects.None, 0f);
    }

    #endregion
}

using Microsoft.Xna.Framework.Graphics;
using Terraria;
using ZensSky.Core.Utils;

namespace ZensSky.Common.DataStructures;

public enum SupernovaState : byte
{
    Contracting,
    Expanding
}

/// <summary>
/// Structure that contains data pertaining to a <see cref="Star"/>'s supernova.
/// </summary>
public struct Supernova
{
    #region Private Fields

    private const float Increment = .003f;

    private const float ContractMultiplier = 1.25f;
    private const float ExpandMultiplier = .55f;
    private const float DecayMultiplier = .035f;

    private const float MinStarScale = .3f;
    private const float MaxStarScale = 1.25f;

    private const float SmallMultiplier = 1.5f;
    private const float BigMultiplier = .5f;

    private const float MinAlpha = .25f;
    private const float MaxAlpha = 1f;

    private const float Scale = .04f;

    private readonly Color EndingColor = Color.White;

    private readonly float Multiplier;

    private readonly Color StartingColor;
    private readonly float StartingScale;

    private readonly unsafe Star* Target;

    #endregion

    #region Public Properties

    public Color SupernovaColor { get; private init; }
    public float NebulaHue { get; private init; }

    public SupernovaState State { get; private set; }

    public float Contract { get; set; }
    public float Expand { get; set; }
    public float Decay { get; set; }

    public bool IsActive { get; set; }

    #endregion

    #region Public Constructors

    /// <param name="target">The <see cref="Star"/> that this supernova belongs to; will be modified during updating.</param>
    /// <param name="supernovaColor">The color of the inital explosion effect.</param>
    /// <param name="nebulaHue">The hue of the resulting nebula(e).</param>
    public unsafe Supernova(Star* target, Color? supernovaColor = null, float nebulaHue = -1f)
    {
        Target = target;

        StartingColor = Target->Color;
        StartingScale = Target->Scale;

            // Have smaller stars explode faster.
        Multiplier = Utils.Remap(Target->Scale, MinStarScale, MaxStarScale, SmallMultiplier, BigMultiplier);

        SupernovaColor = supernovaColor ?? Target->Color;

        if (nebulaHue == -1f)
            nebulaHue = Main.rand.NextFloat();

        NebulaHue = nebulaHue;

        Contract = 0f;
        Expand = 0f;
        Decay = 0f;

        IsActive = true;
    }

    #endregion

    #region Drawing

    public readonly void Draw(SpriteBatch spriteBatch, GraphicsDevice device, float alpha, float rotation)
    {
        Vector2 position = Vector2.Zero;

        unsafe
        {
            position = Target->Position;
            rotation = Target->Rotation + rotation;
        }

        SkyEffects.Supernova.Hue = NebulaHue;
        SkyEffects.Supernova.ExplosionColor = SupernovaColor.ToVector4();

        SkyEffects.Supernova.Expand = Expand;
        SkyEffects.Supernova.Decay = Decay;

        SkyEffects.Supernova.GlobalTime = Main.GlobalTimeWrappedHourly;

            // Arbitrary noise offset.
        SkyEffects.Supernova.Offset = position;

            // Textures[1] is reserved for Realistic Sky's atmosphere effect.
        device.Textures[2] = SkyTextures.SupernovaNoise[1];
        device.SamplerStates[2] = SamplerState.PointWrap;

        SkyEffects.Supernova.Apply();

        Texture2D texture = SkyTextures.SupernovaNoise[0];

        Vector2 origin = texture.Size() * .5f;

        Color color = SupernovaColor * Utils.Remap(alpha, 0, 1, MinAlpha, MaxAlpha);

        float scale = Scale * StartingScale;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    #endregion

    #region Updating

    public void Update()
    {
        switch (State)
        {
            case SupernovaState.Contracting:
                unsafe
                {
                    if (!UpdateContracting())
                        return;

                    State = SupernovaState.Expanding;
                    Target->IsActive = false;
                }
                return;

            default:
                if (UpdateExpanding())
                    IsActive = false;
                return;
        }
    }

    private unsafe bool UpdateContracting()
    {
        Contract += Increment * ContractMultiplier * Multiplier;
        Contract = Utilities.Saturate(Contract);

        float colorInterpolator = Easings.OutQuint(Contract);
        Target->Color = Color.Lerp(StartingColor, EndingColor, colorInterpolator);

            // Have the star increase in scale slightly before shrinking.
        float scaleMultiplier = Easings.OutBack(1 - Contract, 7);
        Target->Scale = StartingScale * scaleMultiplier;

        return Contract >= 1f;
    }

    private bool UpdateExpanding()
    {
        Expand += Increment * ExpandMultiplier * Multiplier;
        Expand = Utilities.Saturate(Expand);

        Decay += Increment * DecayMultiplier * Multiplier;
        Decay = Utilities.Saturate(Decay);

        return Expand >= 1f && Decay >= 1f;
    }

    #endregion
}

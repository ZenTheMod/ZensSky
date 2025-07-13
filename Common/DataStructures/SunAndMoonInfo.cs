namespace ZensSky.Common.DataStructures;

public readonly record struct SunAndMoonInfo
{
    #region Public Properties

    public Vector2 SunPosition { get; init; }
    public Color SunColor { get; init; }
    public float SunRotation { get; init; }
    public float SunScale { get; init; }

    public Vector2 MoonPosition { get; init; }
    public Color MoonColor { get; init; }
    public float MoonRotation { get; init; }
    public float MoonScale { get; init; }

    #endregion

    #region Public Constructors

    public SunAndMoonInfo(Vector2 sunPosition, Color sunColor, float sunRotation, float sunScale,
        Vector2 moonPosition, Color moonColor, float moonRotation, float moonScale)
    {
        SunPosition = sunPosition;
        SunColor = sunColor;
        SunRotation = sunRotation;
        SunScale = sunScale;

        MoonPosition = moonPosition;
        MoonColor = moonColor;
        MoonRotation = moonRotation;
        MoonScale = moonScale;
    }

    public SunAndMoonInfo(Vector2 position, Color color, float rotation, float scale)
    {
        SunPosition = position;
        SunColor = color;
        SunRotation = rotation;
        SunScale = scale;

        MoonPosition = position;
        MoonColor = color;
        MoonRotation = rotation;
        MoonScale = scale;
    }

    #endregion
}

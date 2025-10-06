using Newtonsoft.Json.Converters;
using System;
using System.Text.Json.Serialization;
using ZensSky.Core.Utils;

namespace ZensSky.Core.DataStructures;

[Serializable]
public class GradientSegment : IComparable<GradientSegment>
{
    #region Public Properties

    public float Position
        { get; set => field = Utilities.Saturate(value); }

    public Color Color { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public EasingStyle Easing { get; set; }

    #endregion

    #region Public Constructors

    public GradientSegment(float position, Color color, EasingStyle easing = EasingStyle.Linear)
    {
        Position = Utilities.Saturate(position);

        Color = color;

        Easing = easing;
    }

    #endregion

    #region Private Methods

    int IComparable<GradientSegment>.CompareTo(GradientSegment? other) =>
        Position.CompareTo(other?.Position);

    #endregion
}

using System;
using ZensSky.Core.Utils;

namespace ZensSky.Core.DataStructures;

public class GradientSegment : IComparable<GradientSegment>
{
    #region Public Properties

    public float Position { get; set; }

    public Color Color { get; set; }

    public EasingStyle Easing { get; set; }

    #endregion

    #region Public Constructors

    public GradientSegment(float position, Color color)
    {
        Position = Utilities.Saturate(position);

        Color = color;

        Easing = EasingStyle.Linear;
    }

    #endregion

    #region Private Methods

    int IComparable<GradientSegment>.CompareTo(GradientSegment? other) =>
        Position.CompareTo(other?.Position);

    #endregion
}

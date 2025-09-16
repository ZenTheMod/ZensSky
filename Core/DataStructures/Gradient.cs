using System.Collections.Generic;
using ZensSky.Core.Utils;

namespace ZensSky.Core.DataStructures;

public class Gradient : List<GradientSegment>
{
    #region Private Fields

    private const int DefaultMaxColors = 32;

    #endregion

    #region Public Properties

    public int MaxSegments { get; init; }

    #endregion

    #region Public Constructors

    public Gradient(int maxColors = DefaultMaxColors)
    {
        MaxSegments = maxColors;

        Add(new(0f, Color.Black));
        Add(new(1f, Color.White));
    }

    public Gradient(GradientSegment[] segments, int maxColors = DefaultMaxColors)
    {
        MaxSegments = maxColors;

        AddRange(segments);

        Sort();
    }

    public Gradient(Color[] colors, int maxColors = DefaultMaxColors)
    {
        MaxSegments = maxColors;

        for (int i = 0; i < colors.Length; i++)
        {
            float position = i / (float)colors.Length;
            Add(new(position, colors[i]));
        }

        Sort();
    }

    #endregion

    #region Public Methods

    public new void Add(GradientSegment segment)
    {
        if (Count < MaxSegments)
            base.Add(segment);
    }

    public void Add(float position, Color color) =>
        Add(new(position, color));

    public Color GetColor(float position)
    {
        Sort();

        if (Count <= 0)
            return Color.Transparent;

        if (Count == 1)
            return this[0].Color;

        if (position <= this[0].Position)
            return this[0].Color;

        if (position >= this[^1].Position)
            return this[^1].Color;

        for (int i = 0; i < Count - 1; i++)
        {
            if (position <= this[i].Position || position >= this[i + 1].Position)
                continue;

            float t = (position - this[i].Position) / (this[i + 1].Position - this[i].Position);

            t = Easings.Ease(this[i].Easing, t);

            return Color.Lerp(this[i].Color, this[i + 1].Color, t);
        }

        return Color.Transparent;
    }

    #endregion
}

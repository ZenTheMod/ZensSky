using System;
using System.Collections.Generic;
using ZensSky.Core.Utils;

namespace ZensSky.Core.DataStructures;

[Serializable]
public class Gradient : List<GradientSegment>
{
    #region Private Fields

    private const int DefaultMaxColors = 32;

    #endregion

    #region Public Constructors

    public Gradient() : base() { }

    public Gradient(int maxColors) : base(maxColors) { }

    public Gradient(IEnumerable<GradientSegment> segments) : base(segments) { }

    public Gradient(Color[] colors, int maxColors = DefaultMaxColors)
        : base(maxColors)
    {
        for (int i = 0; i < colors.Length; i++)
        {
            float position = i / (float)colors.Length;
            Add(new(position, colors[i]));
        }

        Sort();
    }

    #endregion

    #region Public Methods

    public void Add(float position, Color color) =>
        Add(new(position, color));

    public Color GetColor(float position)
    {
        Sort();

        if (Count <= 0)
            return Color.Transparent;

        if (Count == 1)
            return this[0].Color;

        if (position <= this[0].Position || position >= this[^1].Position)
        {
            float p = position >= this[^1].Position ? position - 1 : position;

            float t = (p - (this[^1].Position - 1)) / (this[0].Position - (this[^1].Position - 1));

            t = Easings.Ease(this[^1].Easing, t);

            return Color.Lerp(this[^1].Color, this[0].Color, t);
        }

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

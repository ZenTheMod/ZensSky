using System;

namespace ZensSky.Common.Config.Elements;

public abstract class LockedFloatSlider : LockedSliderElement<float>
{
    #region Properties

    public override int NumberTicks => (int)((Max - Min) / Increment) + 1;

    public override float TickIncrement => Increment / (Max - Min);

    protected override float Proportion
    {
        get => (GetValue() - Min) / (Max - Min);
        set => SetValue((float)MathF.Round((value * (Max - Min) + Min) * (1f / Increment)) * Increment);
    }

    #endregion

    public LockedFloatSlider()
    {
        Min = 0f;
        Max = 1f;
        Increment = 0.01f;
    }
}

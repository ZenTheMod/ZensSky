using System;

namespace ZensSky.Core.Config.Elements;

public sealed class LockedIntSlider : LockedSliderElement<int>
{
    #region Properties

    public override int NumberTicks => (Max - Min) / Increment + 1;

    public override float TickIncrement => Increment / (Max - Min);

    protected override float Proportion
    {
        get => (GetValue() - Min) / (float)(Max - Min);
        set => SetValue((int)MathF.Round((value * (Max - Min) + Min) * (1f / Increment)) * Increment);
    }

    #endregion

    public LockedIntSlider()
    {
        Min = 0;
        Max = 10;
        Increment = 1;
    }
}
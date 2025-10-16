using Terraria.UI;

namespace ZensSky.Core.Utils;

    // The C# 14.0 'extension' block seems to still be a little buggy.
#pragma warning disable CA1822 // Member does not access instance data and can be marked as static.

public static partial class Utilities
{
    #region UI

    extension(UIElement element)
    {
        /// <inheritdoc cref="UIElement.GetDimensions"/>
        public Rectangle Dimensions => element.GetDimensions().ToRectangle();

        /// <inheritdoc cref="UIElement.GetInnerDimensions"/>
        public Rectangle InnerDimensions => element.GetInnerDimensions().ToRectangle();
    }

    #endregion
}

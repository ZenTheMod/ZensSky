using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI.Chat;
using ZensSky.Core.Utils;
using static System.Reflection.BindingFlags;

namespace ZensSky.Core.Config.Elements;

public sealed class LockedBoolElement : ConfigElement<bool>
{
    #region Private Fields

    private const string LockTooltipKey = "LockReason";

    private const float LockedBackgroundMultiplier = 0.4f;

    #endregion

    #region Properties

    public object? TargetInstance { get; private set; }

    public PropertyFieldWrapper? TargetMember { get; private set; }

    public bool Mode { get; private set; } = false;

    public bool IsLocked =>
        ((bool?)TargetMember?.GetValue(TargetInstance) ?? true) == Mode;

    #endregion

    #region Initialization

    public override void OnBind()
    {
        base.OnBind();

        OnLeftClick += delegate
        {
            if (!IsLocked)
                Value = !Value;
        };

        LockedElementAttribute? attri = ConfigManager.GetCustomAttributeFromMemberThenMemberType<LockedElementAttribute>(MemberInfo, Item, List);

        if (attri is null)
            return;

        Type type = attri.TargetConfig;

        string name = attri.MemberName;

        Mode = attri.Mode;

            // TODO: Switch to using a MemberInfo based impl.
        FieldInfo? field = type.GetField(name, Static | Instance | Public | NonPublic);
        PropertyInfo? property = type.GetProperty(name, Static | Instance | Public | NonPublic);

        if (field is not null)
            TargetMember = new(field);
        else
            TargetMember = new(property);

        if (ConfigManager.Configs.TryGetValue(ModContent.GetInstance<ZensSky>(), out List<ModConfig>? value))
            TargetInstance = value.Find(c => c.Name == type.Name);
        else
            TargetInstance = null;

        string tooltip = ConfigManager.GetLocalizedTooltip(MemberInfo);
        string? lockReason = ConfigManager.GetLocalizedText<LockedKeyAttribute, LockedArgsAttribute>(MemberInfo, LockTooltipKey);

        TooltipFunction = () =>
            tooltip +
            (IsLocked && lockReason is not null ?
            (string.IsNullOrEmpty(tooltip) ? string.Empty : "\n") + $"[c/{Color.Red.Hex3()}:" + lockReason + "]" :
            string.Empty);
    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
            // Change the background color before drawing the base ConfigElement<T>.
        backgroundColor = IsLocked ? UICommon.DefaultUIBlue * LockedBackgroundMultiplier : UICommon.DefaultUIBlue;
        base.DrawSelf(spriteBatch);

        Texture2D texture = UITextures.LockedSettingsToggle;

        Rectangle dims = this.Dimensions;

        string text = Value ? Lang.menu[126].Value : Lang.menu[124].Value; // On / Off

        if (IsLocked)
            text += " " + Language.GetTextValue("Mods.ZensSky.Configs.Locked");

        DynamicSpriteFont font = FontAssets.ItemStack.Value;

        Vector2 textSize = font.MeasureString(text);
        Vector2 origin = new(textSize.X, 0);

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(dims.X + dims.Width - 36f, dims.Y + 8f), Color.White, 0f, origin, new(0.8f));

        Vector2 position = new(dims.X + dims.Width - 28, dims.Y + 4);
        Rectangle rectangle = texture.Frame(2, 2, Value.ToInt(), IsLocked.ToInt());

        spriteBatch.Draw(texture, position, rectangle, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
    }

    #endregion
}

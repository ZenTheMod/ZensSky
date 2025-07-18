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
using Terraria.UI;
using Terraria.UI.Chat;
using ZensSky.Common.Registries;
using static System.Reflection.BindingFlags;

namespace ZensSky.Core.Config.Elements;

public sealed class LockedBoolElement : ConfigElement<bool>
{
    private const float LockedBackgroundMultiplier = 0.4f;

    #region Properties

    public object? TargetInstance { get; private set; }

    public PropertyFieldWrapper? TargetMember { get; private set; }

    public bool Mode { get; private set; } = false;

    public bool IsLocked =>
        (bool)(TargetMember?.GetValue(TargetInstance) ?? false) == Mode;

    #endregion

    #region Binding

    public override void OnBind()
    {
        base.OnBind();

        OnLeftClick += delegate
        {
            if (!IsLocked)
                Value = !Value;
        };

        LockedElementAttribute? attri = ConfigManager.GetCustomAttributeFromMemberThenMemberType<LockedElementAttribute>(MemberInfo, Item, List);

        Type? type = attri?.TargetConfig;

        string? name = attri?.MemberName;

        bool? mode = attri?.Mode;

        if (type is null || string.IsNullOrEmpty(name) || mode is null)
            return;
        
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

        Mode = mode ?? false;
    }

    #endregion

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
            // Change the background color before drawing the base ConfigElement<T>.
        backgroundColor = IsLocked ? UICommon.DefaultUIBlue * LockedBackgroundMultiplier : UICommon.DefaultUIBlue;
        base.DrawSelf(spriteBatch);

        Texture2D texture = Textures.LockedToggle.Value;

        CalculatedStyle dimensions = GetDimensions();

        string text = Value ? Lang.menu[126].Value : Lang.menu[124].Value; // On / Off

        if (IsLocked)
            text += " " + Language.GetTextValue("Mods.ZensSky.Configs.Locked");

        DynamicSpriteFont font = FontAssets.ItemStack.Value;

        Vector2 textSize = font.MeasureString(text);
        Vector2 origin = new(textSize.X, 0);

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(dimensions.X + dimensions.Width - 36f, dimensions.Y + 8f), Color.White, 0f, origin, new(0.8f));

        Vector2 position = new(dimensions.X + dimensions.Width - 28, dimensions.Y + 4);
        Rectangle rectangle = texture.Frame(2, 2, Value.ToInt(), IsLocked.ToInt());

        spriteBatch.Draw(texture, position, rectangle, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
    }
}

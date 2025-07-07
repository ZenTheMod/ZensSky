using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Stars;
using Star = ZensSky.Common.DataStructures.Star;

namespace ZensSky.Common.Config.Elements;

public sealed class StarEnumElement : ConfigElement<StarVisual>
{
    private const float TimeMultiplier = 1.2f;

    private Star DisplayStar = new() 
    { 
        Position = Vector2.Zero,
        BaseColor = Color.White,
        BaseSize = 2f,
        VanillaStyle = 0,
        Rotation = 0,
        Twinkle = 1f
    };

    public string[]? EnumNames;

    public int Max;

    public override void OnBind()
    {
        base.OnBind();

        OnLeftClick += delegate
        {
            if ((int)Value >= Max - 1)
            {
                Value = 0;
                return;
            }

            Value++;
        };

        OnRightClick += delegate
        {
            if (Value == 0)
            {
                Value = (StarVisual)(Max - 1);
                return;
            }

            Value--;
        };

        EnumNames = Enum.GetNames(typeof(StarVisual));

        for (int i = 0; i < EnumNames.Length; i++)
        {
            FieldInfo? enumFieldFieldInfo = MemberInfo.Type.GetField(EnumNames[i]);
            if (enumFieldFieldInfo != null)
            {
                string name = ConfigManager.GetLocalizedLabel(new PropertyFieldWrapper(enumFieldFieldInfo));
                EnumNames[i] = name;
            }
        }

        Max = EnumNames.Length;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        CalculatedStyle dims = GetDimensions();

        string text = EnumNames?[(int)Value] ?? string.Empty;

        DynamicSpriteFont font = FontAssets.ItemStack.Value;

        Vector2 textSize = font.MeasureString(text);
        Vector2 origin = new(textSize.X, 0);

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(dims.X + dims.Width - 36f, dims.Y + 8f), Color.White, 0f, origin, new(0.8f));

        DisplayStar.Position = new(dims.X + dims.Width - (dims.Height * .5f) - 2, dims.Y + (dims.Height * .5f));

        DrawPanel2(spriteBatch, new(dims.X + dims.Width - dims.Height, dims.Y + 2), TextureAssets.SettingsPanel.Value, dims.Height - 4, dims.Height - 4, Color.Black);

        DrawStar(spriteBatch);
    }

    private void DrawStar(SpriteBatch spriteBatch)
    {
        int style = (int)Value;

        if (Value == StarVisual.Random)
            style = ((int)(Main.GlobalTimeWrappedHourly * TimeMultiplier) % 3) + 1;

        StarRenderingSystem.DrawStar(spriteBatch, 1, 0f, DisplayStar, (StarVisual)style);
    }
}

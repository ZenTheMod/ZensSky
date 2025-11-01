using Microsoft.Xna.Framework.Graphics;

namespace ZensSky.Core.Particles;

public interface IParticle
{
    public bool IsActive { get; set; }

    public void Update();

    public void Draw(SpriteBatch spriteBatch, GraphicsDevice device);
}

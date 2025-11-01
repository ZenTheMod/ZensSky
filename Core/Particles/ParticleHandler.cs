using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

namespace ZensSky.Core.Particles;

public class ParticleHandler<T> where T : struct, IParticle
{
    #region Public Properties

    public T[] Particles { get; init; }

    #endregion

    #region Public Constructors

    public ParticleHandler(int maxParticles)
    {
        Particles = new T[maxParticles];

        Array.Clear(Particles);
    }

    #endregion

    #region Public Methods

    public virtual void Update()
    {
        for (int i = 0; i < Particles.Length; i++)
            if (Particles[i].IsActive)
                Particles[i].Update();
    }

    public virtual void Draw(SpriteBatch spriteBatch, GraphicsDevice device)
    {
        ReadOnlySpan<T> activeParticles = [.. Particles.Where(p => p.IsActive)];

        for (int i = 0; i < activeParticles.Length; i++)
            activeParticles[i].Draw(spriteBatch, device);
    }

    public bool Spawn(T particle)
    {
        int index = Array.FindIndex(Particles, p => !p.IsActive);

        if (index == -1)
            return false;

        Particles[index] = particle;

        return true;
    }

    #endregion
}

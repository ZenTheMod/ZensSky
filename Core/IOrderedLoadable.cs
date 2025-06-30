namespace ZensSky.Core;

public interface IOrderedLoadable
{
    public void Load();

    public void Unload();

    public short Index { get; }
}

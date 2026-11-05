// Compatibility shim: minimal System.Windows.Freezable semantics (Freeze / IsFrozen / write guard).
namespace System.Windows;

public abstract class Freezable
{
    public bool IsFrozen { get; private set; }

    public virtual bool CanFreeze => true;

    public void Freeze()
    {
        if (!CanFreeze)
        {
            throw new InvalidOperationException("This Freezable cannot be frozen.");
        }

        IsFrozen = true;
    }

    protected void WritePreamble()
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException("Cannot set a property on an object because it is in a read-only state.");
        }
    }
}

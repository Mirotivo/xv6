/// <summary>
/// Interface for spin lock operations
/// </summary>
public interface ISpinLock
{
    void Acquire();
    void Release();
}

/// <summary>
/// Interface for sleep lock operations
/// </summary>
public interface ISleepLock
{
    void Acquire();
    void Release();
    bool IsLocked();
}

/// <summary>
/// Interface for sleep lock operations
/// </summary>
public interface Isleeplock
{
    void Acquire();
    void Release();
    bool IsLocked();
}

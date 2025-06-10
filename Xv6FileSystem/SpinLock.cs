/// <summary>
/// Simple spinlock implementation using Monitor
/// </summary>
public class spinlock : Ispinlock
{
    private readonly object lockObject = new object();

    public void Acquire()
    {
        Monitor.Enter(lockObject);
    }

    public void Release()
    {
        Monitor.Exit(lockObject);
    }
}

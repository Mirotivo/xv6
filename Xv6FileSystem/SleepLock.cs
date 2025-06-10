/// <summary>
/// Simple sleep lock implementation
/// </summary>
public class sleeplock : Isleeplock
{
    private readonly object lockObject = new object();
    private bool locked = false;

    public void Acquire()
    {
        lock (lockObject)
        {
            while (locked)
            {
                Monitor.Wait(lockObject);
            }
            locked = true;
        }
    }

    public void Release()
    {
        lock (lockObject)
        {
            locked = false;
            Monitor.PulseAll(lockObject);
        }
    }

    public bool IsLocked()
    {
        lock (lockObject)
        {
            return locked;
        }
    }
}

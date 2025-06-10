using System;
using System.Threading;

/// <summary>
/// buf cache structure that mimics the xv6 C bcache struct
/// </summary>
public class bio : Ibio
{
    private readonly Ispinlock lockObject;           // spinlock for buffer cache
    private readonly buf[] buffers;                 // array of buffers
    private readonly buf head;                      // LRU list head
    private readonly ISPIDevice device;             // storage device

    public bio(ISPIDevice device) : this(device, new spinlock())
    {
    }

    public bio(ISPIDevice device, Ispinlock spinLock)
    {
        this.device = device;
        this.lockObject = spinLock;
        this.buffers = new buf[param.NBUF];
        this.head = new buf();

        // Initialize all buffers
        for (int i = 0; i < param.NBUF; i++)
        {
            buffers[i] = new buf();
        }

        // Initialize the LRU linked list
        // head.next is most recent, head.prev is least recent
        head.Prev = head;
        head.Next = head;

        // Add all buffers to the LRU list
        for (int i = 0; i < param.NBUF; i++)
        {
            buffers[i].Next = head.Next;
            buffers[i].Prev = head;
            if (head.Next != null)
            {
                head.Next.Prev = buffers[i];
            }
            head.Next = buffers[i];
        }
    }

    /// <summary>
    /// Look through buffer cache for block on device dev.
    /// If not found, allocate a buffer.
    /// In either case, return locked buffer.
    /// </summary>
    public buf BGet(uint dev, uint blockno)
    {
        lockObject.Acquire();

        // Is the block already cached?
        for (int i = 0; i < param.NBUF; i++)
        {
            buf b = buffers[i];
            if (b.Dev == dev && b.BlockNo == blockno)
            {
                b.RefCnt++;
                lockObject.Release();
                b.Lock.Acquire();
                return b;
            }
        }

        // Not cached; recycle an unused buffer.
        // Even if refcnt==0, B_DIRTY indicates a buffer is in use
        // because log.c has modified it but not yet committed it.
        for (int i = 0; i < param.NBUF; i++)
        {
            buf b = buffers[i];
            if (b.RefCnt == 0 && !b.Disk)
            {
                b.Dev = dev;
                b.BlockNo = blockno;
                b.Valid = false;
                b.RefCnt = 1;
                lockObject.Release();
                b.Lock.Acquire();
                return b;
            }
        }

        lockObject.Release();
        throw new InvalidOperationException("bget: no buffers");
    }

    /// <summary>
    /// Return a locked buf with the contents of the indicated block.
    /// </summary>
    public buf BRead(uint dev, uint blockno)
    {
        buf b = BGet(dev, blockno);
        if (!b.Valid)
        {
            // Read from disk
            b.Data = device.ReadBlock((int)blockno);
            b.Valid = true;
        }
        return b;
    }

    /// <summary>
    /// Write b's contents to disk. Must be locked.
    /// </summary>
    public void BWrite(buf b)
    {
        if (!b.Lock.IsLocked())
            throw new InvalidOperationException("bwrite");

        device.WriteBlock((int)b.BlockNo, b.Data);
    }


    /// <summary>
    /// Release a locked buffer.
    /// Move to the head of the MRU list.
    /// </summary>
    public void BRelse(buf b)
    {
        if (!b.Lock.IsLocked())
            throw new InvalidOperationException("brelse");

        b.Lock.Release();

        lockObject.Acquire();
        b.RefCnt--;
        if (b.RefCnt == 0)
        {
            // No one is waiting for it; move to head of LRU list
            if (b.Next != null)
                b.Next.Prev = b.Prev;
            if (b.Prev != null)
                b.Prev.Next = b.Next;

            b.Next = head.Next;
            b.Prev = head;
            if (head.Next != null)
                head.Next.Prev = b;
            head.Next = b;
        }
        lockObject.Release();
    }
}

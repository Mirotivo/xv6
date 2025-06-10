using System;
using System.Threading;

/// <summary>
/// Buffer cache structure that mimics the xv6 C bcache struct
/// </summary>
public class BufferCache : IBufferCache
{
    private readonly ISpinLock lockObject;           // spinlock for buffer cache
    private readonly Buffer[] buffers;                 // array of buffers
    private readonly Buffer head;                      // LRU list head
    private readonly ISPIDevice device;             // storage device

    public BufferCache(ISPIDevice device) : this(device, new SpinLock())
    {
    }

    public BufferCache(ISPIDevice device, ISpinLock spinLock)
    {
        this.device = device;
        this.lockObject = spinLock;
        this.buffers = new Buffer[Param.NBUF];
        this.head = new Buffer();

        // Initialize all buffers
        for (int i = 0; i < Param.NBUF; i++)
        {
            buffers[i] = new Buffer();
        }

        // Initialize the LRU linked list
        // head.next is most recent, head.prev is least recent
        head.Prev = head;
        head.Next = head;

        // Add all buffers to the LRU list
        for (int i = 0; i < Param.NBUF; i++)
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
    public Buffer BGet(uint dev, uint blockno)
    {
        lockObject.Acquire();

        // Is the block already cached?
        for (int i = 0; i < Param.NBUF; i++)
        {
            Buffer b = buffers[i];
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
        for (int i = 0; i < Param.NBUF; i++)
        {
            Buffer b = buffers[i];
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
    /// Release a locked buffer.
    /// Move to the head of the MRU list.
    /// </summary>
    public void BRelse(Buffer b)
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

    /// <summary>
    /// Return a locked buf with the contents of the indicated block.
    /// </summary>
    public Buffer BRead(uint dev, uint blockno)
    {
        Buffer b = BGet(dev, blockno);
        if (!b.Valid)
        {
            // Read from disk
            b.Data = device.ReadBlock((int)blockno);
            b.Valid = true;
            b.CRC16 = CalculateCRC16(b.Data);
        }
        return b;
    }

    /// <summary>
    /// Write b's contents to disk. Must be locked.
    /// </summary>
    public void BWrite(Buffer b)
    {
        if (!b.Lock.IsLocked())
            throw new InvalidOperationException("bwrite");

        b.CRC16 = CalculateCRC16(b.Data);
        device.WriteBlock((int)b.BlockNo, b.Data);
    }

    /// <summary>
    /// Simple CRC16 calculation
    /// </summary>
    private ushort CalculateCRC16(byte[] data)
    {
        ushort crc = 0xFFFF;
        const ushort polynomial = 0x1021;

        foreach (byte b in data)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                {
                    crc = (ushort)((crc << 1) ^ polynomial);
                }
                else
                {
                    crc <<= 1;
                }
            }
        }

        return crc;
    }

    /// <summary>
    /// Verify CRC16 checksum of buffer data
    /// </summary>
    public bool VerifyCRC16(Buffer b)
    {
        return b.CRC16 == CalculateCRC16(b.Data);
    }

    /// <summary>
    /// Get buffer cache statistics for debugging
    /// </summary>
    public void PrintCacheStats()
    {
        lockObject.Acquire();

        int usedBuffers = 0;
        int validBuffers = 0;

        for (int i = 0; i < Param.NBUF; i++)
        {
            if (buffers[i].RefCnt > 0)
                usedBuffers++;
            if (buffers[i].Valid)
                validBuffers++;
        }

        Console.WriteLine($"Buffer Cache Stats:");
        Console.WriteLine($"  Total buffers: {Param.NBUF}");
        Console.WriteLine($"  Used buffers: {usedBuffers}");
        Console.WriteLine($"  Valid buffers: {validBuffers}");
        Console.WriteLine($"  Buffer size: {FS.BSIZE} bytes");

        lockObject.Release();
    }

    /// <summary>
    /// Force write all dirty buffers to disk (sync operation)
    /// </summary>
    public void Sync()
    {
        lockObject.Acquire();

        for (int i = 0; i < Param.NBUF; i++)
        {
            Buffer b = buffers[i];
            if (b.Valid && b.RefCnt > 0)
            {
                // This buffer might be dirty, write it
                b.Lock.Acquire();
                BWrite(b);
                b.Lock.Release();
            }
        }

        lockObject.Release();
    }
}

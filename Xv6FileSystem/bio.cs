public class Buffer
{
    public int BlockNumber { get; set; }
    public byte[] Data { get; set; }
    public bool Valid { get; set; }  // True if data has been read from disk
    public bool Dirty { get; set; }  // True if data has been modified
    public int RefCount { get; set; } // Reference count
    
    public Buffer(int blockNumber)
    {
        BlockNumber = blockNumber;
        Data = new byte[Param.BlockSize];
        Valid = false;
        Dirty = false;
        RefCount = 0;
    }
}

public class BufferCache
{
    private Dictionary<int, Buffer> cache = new Dictionary<int, Buffer>();
    private BlockDevice device;
    private readonly object lockObject = new object();
    
    public BufferCache(BlockDevice device) 
    { 
        this.device = device; 
    }
    
    // Get a buffer for the given block number
    // This is the xv6 bget function
    public Buffer bget(int blockNumber)
    {
        lock (lockObject)
        {
            // Look for existing buffer
            if (cache.ContainsKey(blockNumber))
            {
                Buffer buf = cache[blockNumber];
                buf.RefCount++;
                return buf;
            }
            
            // Create new buffer
            Buffer newBuf = new Buffer(blockNumber);
            newBuf.RefCount = 1;
            cache[blockNumber] = newBuf;
            return newBuf;
        }
    }
    
    // Release a buffer back to the cache
    // This is the xv6 brelse function
    public void brelse(Buffer buf)
    {
        lock (lockObject)
        {
            buf.RefCount--;
            
            // If buffer is dirty and no longer referenced, write to disk
            if (buf.RefCount == 0 && buf.Dirty)
            {
                device.WriteBlock(buf.BlockNumber, buf.Data);
                buf.Dirty = false;
            }
        }
    }
    
    // Read a block (combines bget + actual read)
    public Buffer bread(int blockNumber)
    {
        Buffer buf = bget(blockNumber);
        
        if (!buf.Valid)
        {
            // Read from disk
            buf.Data = device.ReadBlock(blockNumber);
            buf.Valid = true;
        }
        
        return buf;
    }
    
    // Write a block (marks buffer as dirty)
    public void bwrite(Buffer buf)
    {
        buf.Dirty = true;
        // Actual write happens in brelse when refcount reaches 0
        // or can be forced immediately:
        device.WriteBlock(buf.BlockNumber, buf.Data);
        buf.Dirty = false;
    }
    
    // Force write all dirty buffers to disk
    public void sync()
    {
        lock (lockObject)
        {
            foreach (var buf in cache.Values)
            {
                if (buf.Dirty)
                {
                    device.WriteBlock(buf.BlockNumber, buf.Data);
                    buf.Dirty = false;
                }
            }
        }
    }
}

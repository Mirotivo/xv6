/// <summary>
/// Buffer structure that mimics the xv6 C struct buf
/// </summary>
public class Buffer
{
    public bool Valid { get; set; }         // has data been read from disk?
    public bool Disk { get; set; }          // does disk "own" buf?
    public uint Dev { get; set; }           // device number
    public uint BlockNo { get; set; }       // block number
    public ISleepLock Lock { get; set; }     // sleep lock for this buffer
    public uint RefCnt { get; set; }        // reference count
    public Buffer? Prev { get; set; }          // LRU cache list - previous
    public Buffer? Next { get; set; }          // LRU cache list - next
    public byte[] Data { get; set; }        // buffer data
    public ushort CRC16 { get; set; }       // CRC16 checksum

    public Buffer() : this(new SleepLock())
    {
    }

    public Buffer(ISleepLock sleepLock)
    {
        Valid = false;
        Disk = false;
        Dev = 0;
        BlockNo = 0;
        Lock = sleepLock;
        RefCnt = 0;
        Prev = null;
        Next = null;
        Data = new byte[FS.BSIZE];
        CRC16 = 0;
    }
}

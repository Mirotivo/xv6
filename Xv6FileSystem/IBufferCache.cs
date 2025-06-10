/// <summary>
/// Interface for buffer cache operations
/// </summary>
public interface IBufferCache
{
    Buffer BGet(uint dev, uint blockno);
    void BRelse(Buffer b);
    Buffer BRead(uint dev, uint blockno);
    void BWrite(Buffer b);
    bool VerifyCRC16(Buffer b);
    void PrintCacheStats();
    void Sync();
}

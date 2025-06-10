/// <summary>
/// Interface for buffer cache operations
/// </summary>
public interface Ibio
{
    buf BGet(uint dev, uint blockno);
    buf BRead(uint dev, uint blockno);
    void BWrite(buf b);
    void BRelse(buf b);
}

// Interface for block device operations (equivalent to xv6's disk driver interface)
public interface ISPIDevice
{
    byte[] ReadBlock(int blockNumber);
    void WriteBlock(int blockNumber, byte[] data);
}

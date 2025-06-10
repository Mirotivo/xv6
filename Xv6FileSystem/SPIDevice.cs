public class SPIDevice : ISPIDevice
{
    private byte[][] disk = new byte[FS.MaxBlocks][];
    
    public SPIDevice() 
    { 
        for (int i = 0; i < disk.Length; i++) 
            disk[i] = new byte[FS.BlockSize]; 
    }
    
    public byte[] ReadBlock(int blockNumber) => disk[blockNumber];
    
    public void WriteBlock(int blockNumber, byte[] data) => 
        Array.Copy(data, disk[blockNumber], FS.BlockSize);
}

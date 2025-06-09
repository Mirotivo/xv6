public class BlockDevice
{
    private byte[][] disk = new byte[Param.MaxBlocks][];
    public BlockDevice() { for (int i = 0; i < disk.Length; i++) disk[i] = new byte[Param.BlockSize]; }
    public byte[] ReadBlock(int blockNumber) => disk[blockNumber];
    public void WriteBlock(int blockNumber, byte[] data) => Array.Copy(data, disk[blockNumber], Param.BlockSize);
}

// Factory for creating file system components (mimicking xv6's init pattern)
public static class Xv6Factory
{
    // This mimics xv6's initialization pattern where components are set up in order
    // Similar to how xv6 calls binit(), iinit(), fsinit() in sequence
    public static IFileSystem CreateFileSystem()
    {
        // Initialize in dependency order (like xv6's main.c initialization)
        ISPIDevice blockDevice = new SPIDevice();  // Like disk driver init
        IBufferCache bufferCache = new BufferCache(blockDevice);  // Like binit()
        IFileSystem fileSystem = new FileSystem(bufferCache);  // Like fsinit()
        
        return fileSystem;
    }
    
    // Alternative method for when you need access to individual components
    public static (ISPIDevice blockDevice, IBufferCache bufferCache, IFileSystem fileSystem) CreateComponents()
    {
        ISPIDevice blockDevice = new SPIDevice();
        IBufferCache bufferCache = new BufferCache(blockDevice);
        IFileSystem fileSystem = new FileSystem(bufferCache);
        
        return (blockDevice, bufferCache, fileSystem);
    }
}

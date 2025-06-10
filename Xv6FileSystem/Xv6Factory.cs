// Factory for creating file system components (mimicking xv6's init pattern)
public static class Xv6Factory
{
    // This mimics xv6's initialization pattern where components are set up in order
    // Similar to how xv6 calls binit(), iinit(), fsinit() in sequence
    public static Ifs CreateFileSystem()
    {
        // Initialize in dependency order (like xv6's main.c initialization)
        ISPIDevice blockDevice = new SPIDevice();  // Like disk driver init
        Ibio bufferCache = new bio(blockDevice);  // Like binit()
        Ifs fileSystem = new fs(bufferCache);  // Like fsinit()
        
        return fileSystem;
    }
    
    // Alternative method for when you need access to individual components
    public static (ISPIDevice blockDevice, Ibio bufferCache, Ifs fileSystem) CreateComponents()
    {
        ISPIDevice blockDevice = new SPIDevice();
        Ibio bufferCache = new bio(blockDevice);
        Ifs fileSystem = new fs(bufferCache);
        
        return (blockDevice, bufferCache, fileSystem);
    }
}

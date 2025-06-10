// File system constants (equivalent to xv6's fs.h)
public static class FS
{
    // Block and file system parameters
    public const int BSIZE = 512;               // Block size in bytes
    public const int FSSIZE = 2000;             // Size of file system in blocks
    
    // File system layout parameters
    public const int NDIRECT = 12;              // Number of direct blocks in inode
    public const int NINDIRECT = BSIZE / 4;     // Number of indirect blocks (128)
    public const int MAXFILE = NDIRECT + NINDIRECT; // Max file size in blocks
    
    // Directory parameters
    public const int DIRSIZ = 14;               // Directory name size
    
    // Inode and bitmap parameters
    public const int IPB = BSIZE / 64;          // Inodes per block (8)
    public const int BPB = BSIZE * 8;           // Bits per bitmap block (4096)
    public const int NINODES = 200;             // Total number of inodes
    public const int NINODEBLOCKS = NINODES / IPB; // Number of inode blocks (25)
    
    // File system magic number
    public const uint FSMAGIC = 0x10203040;
    
    // Root inode number
    public const int ROOTINO = 1;
    
    // Legacy compatibility
    public const int BlockSize = BSIZE;
    public const int MaxBlocks = FSSIZE;
}

// Interface for file system operations (equivalent to xv6's file system interface)
public interface Ifs
{
    // File operations
    int open(string filename, OpenFlags flags);
    int read(int fd, byte[] buf, int count);
    int write(int fd, byte[] buf, int count);
    void close(int fd);

    // Block allocation
    int balloc();
    void bfree(int b);

    // Inode operations
    Inode ialloc(short type);
    void iupdate(Inode ip);
    Inode iget(int inum);
    Inode idup(Inode ip);
    void ilock(Inode ip);
    void iunlock(Inode ip);
    void iput(Inode ip);

    // Utility
    void PrintLayout();
}

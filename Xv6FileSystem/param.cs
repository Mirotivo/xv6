// System-wide parameters (equivalent to xv6's param.h)
public static class Param
{
    // Process and system limits
    public const int NPROC = 8;                 // Maximum number of processes
    public const int NCPU = 1;                  // Maximum number of CPUs
    public const int NOFILE = 8;                // Open files per process
    public const int NFILE = 8;                 // Open files per system
    public const int NINODE = 8;                // Maximum number of active i-nodes
    public const int NDEV = 8;                  // Maximum major device number
    public const int ROOTDEV = 1;               // Device number of file system root disk
    
    // Execution parameters
    public const int MAXARG = 16;               // Max exec arguments
    public const int MAXPATH = 32;              // Maximum file path name
    
    // Logging and buffer parameters
    public const int MAXOPBLOCKS = 16;          // Max # of blocks any FS op writes
    public const int LOGSIZE = MAXOPBLOCKS;     // Max data blocks in on-disk log
    public const int NBUF = MAXOPBLOCKS;        // Size of disk block cache
}

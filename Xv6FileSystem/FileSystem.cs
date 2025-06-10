using System;
using System.Collections.Generic;

[Flags]
public enum OpenFlags { None = 0, O_CREATE = 1, O_RDWR = 2 }

// File types
public static class FileType
{
    public const short T_DIR = 1;   // Directory
    public const short T_FILE = 2;  // File
    public const short T_DEV = 3;   // Device
}

// On-disk superblock
public struct SuperBlock
{
    public int size;          // Size of file system image (blocks)
    public int nblocks;       // Number of data blocks
    public int ninodes;       // Number of inodes
    public int nlog;          // Number of log blocks
    public int logstart;      // Block number of first log block
    public int inodestart;    // Block number of first inode block
    public int bmapstart;     // Block number of first free map block
}

// On-disk inode structure
public struct DiskInode
{
    public short type;        // File type
    public short major;       // Major device number (T_DEV only)
    public short minor;       // Minor device number (T_DEV only)
    public short nlink;       // Number of links to inode in file system
    public int size;          // Size of file (bytes)
    public int[] addrs;       // Data block addresses
    
    public DiskInode()
    {
        type = 0;
        major = 0;
        minor = 0;
        nlink = 0;
        size = 0;
        addrs = new int[FS.NDIRECT + 1];
    }
}

// In-memory inode
public class Inode
{
    public int inum;          // Inode number
    public int refCount;      // Reference count
    public bool valid;        // Has inode been read from disk?
    public DiskInode dinode;  // Copy of disk inode
    
    public Inode()
    {
        inum = 0;
        refCount = 0;
        valid = false;
        dinode = new DiskInode();
    }
}

// Directory entry
public struct DirEnt
{
    public short inum;        // Inode number
    public string name;       // File name
    
    public DirEnt(short inum, string name)
    {
        this.inum = inum;
        this.name = name.Length > FS.DIRSIZ ? name.Substring(0, FS.DIRSIZ) : name;
    }
}

// File descriptor
public class File
{
    public int type;          // FD_NONE, FD_PIPE, FD_INODE
    public int refCount;      // Reference count
    public bool readable;
    public bool writable;
    public Inode? ip;         // FD_INODE
    public int off;           // FD_INODE
    
    public File()
    {
        type = 0;
        refCount = 0;
        readable = false;
        writable = false;
        ip = null;
        off = 0;
    }
}

public class FileSystem : IFileSystem
{
    private IBufferCache cache;
    private SuperBlock sb;
    private Inode[] icache;   // In-memory inode cache
    private File[] ftable;    // File table
    private readonly object lockObject = new object();
    
    // Simple directory for demo (in real xv6, this would be in the root inode)
    private Dictionary<string, int> rootDir = new Dictionary<string, int>();
    
    public FileSystem(IBufferCache cache)
    {
        this.cache = cache;
        this.icache = new Inode[Param.NINODE];
        this.ftable = new File[Param.NFILE];
        
        // Initialize inode cache
        for (int i = 0; i < Param.NINODE; i++)
        {
            icache[i] = new Inode();
        }
        
        // Initialize file table
        for (int i = 0; i < Param.NFILE; i++)
        {
            ftable[i] = new File();
        }
        
        // Initialize superblock
        readsb();
        
        // Initialize file system
        fsinit();
    }
    
    // Read superblock from disk
    private void readsb()
    {
        Buffer bp = cache.BRead(1, 1); // Superblock is at block 1
        
        // Set up superblock with xv6 layout
        sb = new SuperBlock
        {
            size = FS.FSSIZE,
            nblocks = FS.FSSIZE - Param.LOGSIZE - FS.NINODEBLOCKS - 3, // boot + sb + bitmap
            ninodes = FS.NINODES,
            nlog = Param.LOGSIZE,
            logstart = 2,
            inodestart = 2 + Param.LOGSIZE,
            bmapstart = 2 + Param.LOGSIZE + FS.NINODEBLOCKS
        };
        
        cache.BRelse(bp);
    }
    
    // Initialize file system
    private void fsinit()
    {
        // In real xv6, this would create the root directory inode
        // For demo purposes, we'll just ensure the root directory exists
    }
    
    // Block containing bit for block b
    private int BBLOCK(int b) => b / FS.BPB + sb.bmapstart;
    
    // Block containing inode i
    private int IBLOCK(int i) => i / FS.IPB + sb.inodestart;
    
    // Allocate a zeroed disk block
    public int balloc()
    {
        for (int b = 0; b < sb.size; b += FS.BPB)
        {
            Buffer bp = cache.BRead(1, (uint)BBLOCK(b));
            
            for (int bi = 0; bi < FS.BPB && b + bi < sb.size; bi++)
            {
                int m = 1 << (bi % 8);
                if ((bp.Data[bi / 8] & m) == 0) // Is block free?
                {
                    bp.Data[bi / 8] |= (byte)m;  // Mark block in use
                    cache.BWrite(bp);
                    cache.BRelse(bp);
                    
                    // Zero the allocated block
                    Buffer zbp = cache.BRead(1, (uint)(b + bi));
                    Array.Clear(zbp.Data, 0, FS.BSIZE);
                    cache.BWrite(zbp);
                    cache.BRelse(zbp);
                    
                    return b + bi;
                }
            }
            cache.BRelse(bp);
        }
        throw new Exception("balloc: out of blocks");
    }
    
    // Free a disk block
    public void bfree(int b)
    {
        Buffer bp = cache.BRead(1, (uint)BBLOCK(b));
        int bi = b % FS.BPB;
        int m = 1 << (bi % 8);
        
        if ((bp.Data[bi / 8] & m) == 0)
            throw new Exception("freeing free block");
            
        bp.Data[bi / 8] &= (byte)~m;
        cache.BWrite(bp);
        cache.BRelse(bp);
    }
    
    // Allocate an inode on device
    public Inode ialloc(short type)
    {
        // Simplified approach for demo - find first free inode in cache
        lock (lockObject)
        {
            for (int inum = 1; inum < sb.ninodes; inum++)
            {
                // Check if inode is already in use in cache
                bool inUse = false;
                for (int i = 0; i < Param.NINODE; i++)
                {
                    if (icache[i].refCount > 0 && icache[i].inum == inum && icache[i].dinode.type != 0)
                    {
                        inUse = true;
                        break;
                    }
                }
                
                if (!inUse)
                {
                    // Allocate this inode
                    Inode ip = iget(inum);
                    ip.dinode.type = type;
                    ip.dinode.major = 0;
                    ip.dinode.minor = 0;
                    ip.dinode.nlink = 0;
                    ip.dinode.size = 0;
                    Array.Clear(ip.dinode.addrs, 0, ip.dinode.addrs.Length);
                    ip.valid = true;
                    
                    return ip; // Return inode (caller should lock if needed)
                }
            }
        }
        throw new Exception("ialloc: no inodes");
    }
    
    // Copy a modified in-memory inode to disk
    public void iupdate(Inode ip)
    {
        Buffer bp = cache.BRead(1, (uint)IBLOCK(ip.inum));
        // In real xv6, we'd serialize the inode to the buffer
        cache.BWrite(bp);
        cache.BRelse(bp);
    }
    
    // Find the inode with number inum and return an in-memory copy
    public Inode iget(int inum)
    {
        lock (lockObject)
        {
            // Is the inode already cached?
            for (int i = 0; i < Param.NINODE; i++)
            {
                if (icache[i].refCount > 0 && icache[i].inum == inum)
                {
                    icache[i].refCount++;
                    return icache[i];
                }
            }
            
            // Recycle an inode cache entry
            for (int i = 0; i < Param.NINODE; i++)
            {
                if (icache[i].refCount == 0)
                {
                    icache[i].inum = inum;
                    icache[i].refCount = 1;
                    icache[i].valid = false;
                    return icache[i];
                }
            }
            
            throw new Exception("iget: no inodes");
        }
    }
    
    // Increment reference count for ip
    public Inode idup(Inode ip)
    {
        lock (lockObject)
        {
            ip.refCount++;
            return ip;
        }
    }
    
    // Lock the given inode
    public void ilock(Inode ip)
    {
        if (ip == null || ip.refCount < 1)
            throw new Exception("ilock");
            
        if (!ip.valid)
        {
            Buffer bp = cache.BRead(1, (uint)IBLOCK(ip.inum));
            // In real xv6, we'd deserialize the inode from the buffer
            // For demo, we'll mark it as valid
            ip.valid = true;
            cache.BRelse(bp);
        }
    }
    
    // Unlock the given inode
    public void iunlock(Inode ip)
    {
        if (ip == null || ip.refCount < 1)
            throw new Exception("iunlock");
    }
    
    // Drop a reference to an in-memory inode
    public void iput(Inode ip)
    {
        lock (lockObject)
        {
            if (ip.refCount == 1 && ip.valid && ip.dinode.nlink == 0)
            {
                // Inode has no links: truncate and free
                itrunc(ip);
                ip.dinode.type = 0;
                iupdate(ip);
                ip.valid = false;
            }
            
            ip.refCount--;
        }
    }
    
    // Truncate inode (discard contents)
    private void itrunc(Inode ip)
    {
        // Free direct blocks
        for (int i = 0; i < FS.NDIRECT; i++)
        {
            if (ip.dinode.addrs[i] != 0)
            {
                bfree(ip.dinode.addrs[i]);
                ip.dinode.addrs[i] = 0;
            }
        }
        
        // Free indirect block
        if (ip.dinode.addrs[FS.NDIRECT] != 0)
        {
            Buffer bp = cache.BRead(1, (uint)ip.dinode.addrs[FS.NDIRECT]);
            
            for (int j = 0; j < FS.NINDIRECT; j++)
            {
                int addr = BitConverter.ToInt32(bp.Data, j * 4);
                if (addr != 0)
                    bfree(addr);
            }
            
            cache.BRelse(bp);
            bfree(ip.dinode.addrs[FS.NDIRECT]);
            ip.dinode.addrs[FS.NDIRECT] = 0;
        }
        
        ip.dinode.size = 0;
        iupdate(ip);
    }
    
    // File operations
    public int open(string filename, OpenFlags flags)
    {
        int inum;
        
        if (!rootDir.ContainsKey(filename))
        {
            if ((flags & OpenFlags.O_CREATE) != 0)
            {
                // Create new file
                Inode ip = ialloc(FileType.T_FILE);
                
                // Allocate a data block
                int blockNum = balloc();
                ip.dinode.addrs[0] = blockNum;
                ip.dinode.size = 0;
                iupdate(ip);
                
                inum = ip.inum;
                rootDir[filename] = inum;
                
                iput(ip);
            }
            else
            {
                throw new Exception("File does not exist");
            }
        }
        else
        {
            inum = rootDir[filename];
        }
        
        // Allocate file descriptor - find free slot in file table
        for (int i = 0; i < Param.NFILE; i++)
        {
            if (ftable[i].type == 0)
            {
                ftable[i].type = 1; // FD_INODE
                ftable[i].ip = iget(inum);
                ftable[i].off = 0;
                ftable[i].readable = true;
                ftable[i].writable = (flags & OpenFlags.O_RDWR) != 0;
                ftable[i].refCount = 1;
                return i + 3; // Return fd (offset by 3 for stdin/stdout/stderr)
            }
        }
        
        throw new Exception("Too many open files");
    }
    
    public int read(int fd, byte[] buf, int count)
    {
        File? f = GetFileByFd(fd);
        if (f == null || !f.readable || f.ip == null)
            throw new Exception("Invalid file descriptor");
            
        ilock(f.ip);
        
        if (f.off >= f.ip.dinode.size)
        {
            iunlock(f.ip);
            return 0; // EOF
        }
        
        int blockNum = f.ip.dinode.addrs[0]; // Simple: first block only
        if (blockNum == 0)
        {
            iunlock(f.ip);
            return 0;
        }
        
        Buffer bp = cache.BRead(1, (uint)blockNum);
        int toRead = Math.Min(count, f.ip.dinode.size - f.off);
        toRead = Math.Min(toRead, FS.BSIZE - f.off);
        
        Array.Copy(bp.Data, f.off, buf, 0, toRead);
        f.off += toRead;
        
        cache.BRelse(bp);
        iunlock(f.ip);
        
        return toRead;
    }
    
    public int write(int fd, byte[] buf, int count)
    {
        File? f = GetFileByFd(fd);
        if (f == null || !f.writable || f.ip == null)
            throw new Exception("Invalid file descriptor");
            
        ilock(f.ip);
        
        int blockNum = f.ip.dinode.addrs[0]; // Simple: first block only
        if (blockNum == 0)
        {
            blockNum = balloc();
            f.ip.dinode.addrs[0] = blockNum;
        }
        
        Buffer bp = cache.BRead(1, (uint)blockNum);
        int toWrite = Math.Min(count, FS.BSIZE - f.off);
        
        Array.Copy(buf, 0, bp.Data, f.off, toWrite);
        f.off += toWrite;
        f.ip.dinode.size = Math.Max(f.ip.dinode.size, f.off);
        
        cache.BWrite(bp);
        cache.BRelse(bp);
        iupdate(f.ip);
        iunlock(f.ip);
        
        return toWrite;
    }
    
    public void close(int fd)
    {
        File? f = GetFileByFd(fd);
        if (f != null)
        {
            f.refCount--;
            if (f.refCount == 0)
            {
                if (f.ip != null)
                    iput(f.ip);
                f.type = 0;
                f.ip = null;
            }
        }
    }
    
    private File? GetFileByFd(int fd)
    {
        int index = fd - 3;
        if (index >= 0 && index < Param.NFILE && ftable[index].type != 0)
            return ftable[index];
        return null;
    }
    
    // Print file system layout
    public void PrintLayout()
    {
        Console.WriteLine("xv6 File System Layout:");
        Console.WriteLine($"Block 0: Boot block");
        Console.WriteLine($"Block 1: Superblock");
        Console.WriteLine($"Blocks {sb.logstart}-{sb.logstart + sb.nlog - 1}: Log blocks ({sb.nlog} blocks)");
        Console.WriteLine($"Blocks {sb.inodestart}-{sb.bmapstart - 1}: Inode blocks ({FS.NINODEBLOCKS} blocks)");
        Console.WriteLine($"Block {sb.bmapstart}: Bitmap block");
        Console.WriteLine($"Blocks {sb.bmapstart + 1}-{sb.size - 1}: Data blocks ({sb.nblocks} blocks)");
        Console.WriteLine($"Total blocks: {sb.size}");
        Console.WriteLine($"Block size: {FS.BSIZE} bytes");
        Console.WriteLine($"Total inodes: {sb.ninodes}");
    }
}

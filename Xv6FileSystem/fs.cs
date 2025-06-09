[Flags]
public enum OpenFlags { None = 0, O_CREATE = 1, O_RDWR = 2 }

public class Inode
{
    public int size = 0;
    public List<int> blockNumbers = new List<int>();
}

public class InodeTable
{
    private int nextInode = 1;
    private int nextBlock = 0;
    private Dictionary<int, Inode> inodes = new Dictionary<int, Inode>();
    // AllocateInode
    public int ialloc() { int inum = nextInode++; inodes[inum] = new Inode(); return inum; }
    // GetInode
    public Inode iget(int inum) => inodes[inum];
    // AllocateBlock
    public int balloc() => nextBlock++;
    // Exists
    public bool iexists(int inum) => inodes.ContainsKey(inum);
}

public class Directory
{
    private Dictionary<string, int> entries = new Dictionary<string, int>();
    public bool Contains(string filename) => entries.ContainsKey(filename);
    public void Add(string filename, int inum) => entries[filename] = inum;
    public int GetInode(string filename) => entries[filename];
    public void Remove(string filename) { if (Contains(filename)) entries.Remove(filename); }
}

public class File
{
    public int inodeNum;
    public int offset;
    public File(int inodeNum) { this.inodeNum = inodeNum; this.offset = 0; }
}

public class FileSystem
{
    private BufferCache cache;
    private Directory rootDirectory = new Directory();
    private InodeTable inodeTable = new InodeTable();
    private Dictionary<int, File> fdTable = new Dictionary<int, File>();
    private int nextFd = 3; // 0,1,2 are usually stdin/out/err

    public FileSystem(BufferCache cache) { this.cache = cache; }

    public int open(string filename, OpenFlags flags)
    {
        int inum;
        if (!rootDirectory.Contains(filename))
        {
            if ((flags & OpenFlags.O_CREATE) != 0)
            {
                inum = inodeTable.ialloc();
                int blockNum = inodeTable.balloc();
                Inode inode = inodeTable.iget(inum);
                inode.blockNumbers.Add(blockNum);
                rootDirectory.Add(filename, inum);
            }
            else
            {
                throw new Exception("File does not exist.");
            }
        }
        else
        {
            inum = rootDirectory.GetInode(filename);
        }
        int fd = nextFd++;
        fdTable[fd] = new File(inum);
        return fd;
    }

    public int read(int fd, byte[] buf, int count)
    {
        if (!fdTable.ContainsKey(fd)) throw new Exception("Invalid file descriptor");
        File file = fdTable[fd];
        Inode inode = inodeTable.iget(file.inodeNum);
        if (file.offset >= inode.size) return 0;
        int blockNum = inode.blockNumbers[0]; // single-block file for simplicity
        Buffer buffer = cache.bread(blockNum);
        int toRead = Math.Min(count, inode.size - file.offset);
        Array.Copy(buffer.Data, file.offset, buf, 0, toRead);
        file.offset += toRead;
        cache.brelse(buffer);
        return toRead;
    }

    public int write(int fd, byte[] buf, int count)
    {
        if (!fdTable.ContainsKey(fd)) throw new Exception("Invalid file descriptor");
        File file = fdTable[fd];
        Inode inode = inodeTable.iget(file.inodeNum);
        int blockNum = inode.blockNumbers[0]; // single-block file for simplicity
        Buffer buffer = cache.bread(blockNum);
        int toWrite = Math.Min(count, Param.BlockSize - file.offset);
        Array.Copy(buf, 0, buffer.Data, file.offset, toWrite);
        cache.bwrite(buffer);
        file.offset += toWrite;
        inode.size = Math.Max(inode.size, file.offset);
        cache.brelse(buffer);
        return toWrite;
    }

    public void close(int fd)
    {
        if (fdTable.ContainsKey(fd)) fdTable.Remove(fd);
    }
}

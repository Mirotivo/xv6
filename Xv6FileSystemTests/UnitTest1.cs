namespace Xv6FileSystemTests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var sd = new SPIDevice();
        var cache = new BufferCache(sd);
        var fs = new FileSystem(cache);

        // Open file for writing (create if missing)
        int fd = fs.open("test.txt", OpenFlags.O_CREATE | OpenFlags.O_RDWR);
        byte[] data = { 1, 2, 3, 4, 5 };
        fs.write(fd, data, data.Length);
        fs.close(fd);

        // Open file for reading
        int fd2 = fs.open("test.txt", OpenFlags.O_RDWR);
        byte[] buf = new byte[5];
        int n = fs.read(fd2, buf, 5);
        Console.WriteLine("Bytes read: " + n + " Data: " + string.Join(", ", buf));
        fs.close(fd2);

        // Try reading a non-existing file
        try
        {
            fs.open("notfound.txt", OpenFlags.O_RDWR);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message); // Should print "File does not exist."
        }
    }
}

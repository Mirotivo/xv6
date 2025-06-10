using System;
using System.Text;

class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Xv6 File System Demo ===");
            Console.WriteLine($"Block Size: {FS.BlockSize} bytes");
            Console.WriteLine($"Max Blocks: {FS.MaxBlocks}");
            Console.WriteLine();

            // Initialize the file system components (mimicking xv6's init sequence)
            Ifs fileSystem = Xv6Factory.CreateFileSystem();

            try
            {
                // Demo 1: Basic File Operations
                Console.WriteLine("Demo 1: Basic File Operations");
                Console.WriteLine("-----------------------------");
                BasicFileOperationsDemo(fileSystem);
                Console.WriteLine();

                // Demo 2: Multiple Files
                Console.WriteLine("Demo 2: Multiple Files");
                Console.WriteLine("----------------------");
                MultipleFilesDemo(fileSystem);
                Console.WriteLine();

                // Demo 3: File Size Limits
                Console.WriteLine("Demo 3: File Size Limits");
                Console.WriteLine("------------------------");
                FileSizeLimitsDemo(fileSystem);
                Console.WriteLine();

                // Demo 4: Error Handling
                Console.WriteLine("Demo 4: Error Handling");
                Console.WriteLine("----------------------");
                ErrorHandlingDemo(fileSystem);
                Console.WriteLine();

                Console.WriteLine("All demos completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Demo failed with error: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void BasicFileOperationsDemo(Ifs fs)
        {
            // Create and write to a file
            Console.WriteLine("Creating file 'hello.txt'...");
            int fd = fs.open("hello.txt", OpenFlags.O_CREATE | OpenFlags.O_RDWR);
            Console.WriteLine($"File descriptor: {fd}");

            string message = "Hello, Xv6 File System!";
            byte[] writeData = Encoding.UTF8.GetBytes(message);
            
            Console.WriteLine($"Writing: '{message}'");
            int bytesWritten = fs.write(fd, writeData, writeData.Length);
            Console.WriteLine($"Bytes written: {bytesWritten}");

            // Close and reopen the file
            fs.close(fd);
            Console.WriteLine("File closed and reopened for reading...");
            
            fd = fs.open("hello.txt", OpenFlags.O_RDWR);
            
            // Read from the file
            byte[] readBuffer = new byte[100];
            int bytesRead = fs.read(fd, readBuffer, readBuffer.Length);
            string readMessage = Encoding.UTF8.GetString(readBuffer, 0, bytesRead);
            
            Console.WriteLine($"Bytes read: {bytesRead}");
            Console.WriteLine($"Content: '{readMessage}'");
            
            fs.close(fd);
        }

        static void MultipleFilesDemo(Ifs fs)
        {
            string[] filenames = { "file1.txt", "file2.txt", "file3.txt" };
            int[] fileDescriptors = new int[filenames.Length];

            // Create multiple files
            for (int i = 0; i < filenames.Length; i++)
            {
                Console.WriteLine($"Creating {filenames[i]}...");
                fileDescriptors[i] = fs.open(filenames[i], OpenFlags.O_CREATE | OpenFlags.O_RDWR);
                
                string content = $"This is content for {filenames[i]} - File #{i + 1}";
                byte[] data = Encoding.UTF8.GetBytes(content);
                fs.write(fileDescriptors[i], data, data.Length);
                
                Console.WriteLine($"Written to {filenames[i]}: '{content}'");
            }

            // Read from all files
            Console.WriteLine("\nReading from all files:");
            for (int i = 0; i < filenames.Length; i++)
            {
                // Reset file position by closing and reopening
                fs.close(fileDescriptors[i]);
                fileDescriptors[i] = fs.open(filenames[i], OpenFlags.O_RDWR);
                
                byte[] buffer = new byte[100];
                int bytesRead = fs.read(fileDescriptors[i], buffer, buffer.Length);
                string content = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                Console.WriteLine($"{filenames[i]}: '{content}'");
                fs.close(fileDescriptors[i]);
            }
        }

        static void FileSizeLimitsDemo(Ifs fs)
        {
            Console.WriteLine("Testing file size limits...");
            int fd = fs.open("large.txt", OpenFlags.O_CREATE | OpenFlags.O_RDWR);

            // Try to write data that approaches block size limit
            byte[] largeData = new byte[FS.BlockSize - 10]; // Leave some room
            for (int i = 0; i < largeData.Length; i++)
            {
                largeData[i] = (byte)('A' + (i % 26)); // Fill with letters
            }

            Console.WriteLine($"Writing {largeData.Length} bytes to file...");
            int bytesWritten = fs.write(fd, largeData, largeData.Length);
            Console.WriteLine($"Successfully wrote {bytesWritten} bytes");

            // Try to write more data (should be limited by remaining space)
            byte[] moreData = Encoding.UTF8.GetBytes("This might not fit completely!");
            Console.WriteLine($"Attempting to write {moreData.Length} more bytes...");
            int moreBytesWritten = fs.write(fd, moreData, moreData.Length);
            Console.WriteLine($"Additional bytes written: {moreBytesWritten}");

            fs.close(fd);
        }

        static void ErrorHandlingDemo(Ifs fs)
        {
            Console.WriteLine("Testing error conditions...");

            // Try to open a non-existent file without O_CREATE
            try
            {
                Console.WriteLine("Attempting to open non-existent file without O_CREATE...");
                fs.open("nonexistent.txt", OpenFlags.O_RDWR);
                Console.WriteLine("ERROR: Should have thrown an exception!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Expected error caught: {ex.Message}");
            }

            // Try to use invalid file descriptor
            try
            {
                Console.WriteLine("Attempting to read from invalid file descriptor...");
                byte[] buffer = new byte[10];
                fs.read(999, buffer, buffer.Length);
                Console.WriteLine("ERROR: Should have thrown an exception!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Expected error caught: {ex.Message}");
            }

            // Try to write to invalid file descriptor
            try
            {
                Console.WriteLine("Attempting to write to invalid file descriptor...");
                byte[] data = Encoding.UTF8.GetBytes("test");
                fs.write(999, data, data.Length);
                Console.WriteLine("ERROR: Should have thrown an exception!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Expected error caught: {ex.Message}");
            }
        }
    }

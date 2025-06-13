using System;

/// <summary>
/// Demonstrates the xv6-style buffer cache implementation
/// </summary>
public class BufferCacheDemo
    {
        public static void RunDemo()
        {
            Console.WriteLine("=== xv6 Buffer Cache Demo ===\n");

            // Create a simulated SPI device (disk)
            ISPIDevice device = new SPIDevice();
            
            // Create the buffer cache
            bio bcache = new bio(device);
            
            Console.WriteLine("1. Buffer cache initialized");
            Console.WriteLine();

            // Test 1: Read some blocks
            Console.WriteLine("2. Reading blocks 0, 1, 2...");
            buf buf0 = bcache.BRead(0, 0);  // device 0, block 0
            buf buf1 = bcache.BRead(0, 1);  // device 0, block 1
            buf buf2 = bcache.BRead(0, 2);  // device 0, block 2
            
            Console.WriteLine($"   Block 0 valid: {buf0.Valid}, RefCnt: {buf0.RefCnt}");
            Console.WriteLine($"   Block 1 valid: {buf1.Valid}, RefCnt: {buf1.RefCnt}");
            Console.WriteLine($"   Block 2 valid: {buf2.Valid}, RefCnt: {buf2.RefCnt}");
            Console.WriteLine();

            // Test 2: Write some data to a buffer
            Console.WriteLine("3. Writing data to block 0...");
            string testData = "Hello, xv6 buffer cache!";
            byte[] dataBytes = System.Text.Encoding.UTF8.GetBytes(testData);
            Array.Copy(dataBytes, buf0.Data, Math.Min(dataBytes.Length, buf0.Data.Length));
            
            bcache.BWrite(buf0);
            Console.WriteLine($"   Written: '{testData}'");
            Console.WriteLine();

            // Test 3: Release buffers
            Console.WriteLine("4. Releasing buffers...");
            bcache.BRelse(buf0);
            bcache.BRelse(buf1);
            bcache.BRelse(buf2);
            Console.WriteLine();

            // Test 4: Read the same block again (should be cached)
            Console.WriteLine("5. Reading block 0 again (should be cached)...");
            buf buf0Again = bcache.BRead(0, 0);
            string readData = System.Text.Encoding.UTF8.GetString(buf0Again.Data, 0, testData.Length);
            Console.WriteLine($"   Read back: '{readData}'");
            bcache.BRelse(buf0Again);
            Console.WriteLine();

            // Test 5: Test LRU behavior by reading many blocks
            Console.WriteLine("6. Testing LRU behavior (reading more blocks than cache size)...");
            buf[] buffers = new buf[param.NBUF + 5]; // More than cache size
            
            for (int i = 0; i < buffers.Length; i++)
            {
                try
                {
                    buffers[i] = bcache.BRead(0, (uint)(10 + i));
                    Console.WriteLine($"   Read block {10 + i}");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"   Expected error at block {10 + i}: {ex.Message}");
                    break;
                }
            }
            
            // Release the buffers we successfully got
            for (int i = 0; i < buffers.Length && buffers[i] != null; i++)
            {
                bcache.BRelse(buffers[i]);
            }
            Console.WriteLine();

            Console.WriteLine("=== Buffer Cache Demo Complete ===");
        }
    }

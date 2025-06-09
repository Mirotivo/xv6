# Xv6 File System Demos

This project demonstrates the functionality of the Xv6FileSystem class library through comprehensive examples.

## Overview

The Xv6FileSystem is a simplified implementation of a file system inspired by the xv6 operating system. It provides basic file operations including:

- File creation and opening
- Reading and writing data
- File descriptor management
- Error handling

## System Parameters

The file system uses configurable parameters defined in `Param.cs`:

- **Block Size**: 512 bytes per block
- **Max Blocks**: 10,000 maximum blocks

## Demo Features

### Demo 1: Basic File Operations
- Creates a new file
- Writes text data to the file
- Closes and reopens the file
- Reads the data back and displays it

### Demo 2: Multiple Files
- Creates multiple files simultaneously
- Writes unique content to each file
- Demonstrates file descriptor management
- Reads from all files to verify data integrity

### Demo 3: File Size Limits
- Tests writing data that approaches the block size limit
- Demonstrates how the system handles size constraints
- Shows partial writes when space is limited

### Demo 4: Error Handling
- Tests opening non-existent files without create flag
- Tests invalid file descriptor operations
- Demonstrates proper exception handling

## Running the Demo

### Prerequisites
- .NET 9.0 or later
- Visual Studio or Visual Studio Code (optional)

### Command Line
```bash
# Build the solution
dotnet build

# Run the demo
dotnet run --project Xv6FileSystemDemos
```

### Visual Studio
1. Open the `Xv6.sln` solution file
2. Set `Xv6FileSystemDemos` as the startup project
3. Press F5 or click "Start Debugging"

## Expected Output

The demo will display:
1. System configuration (block size and max blocks)
2. Step-by-step execution of each demo
3. Results of file operations
4. Error handling demonstrations
5. Success confirmation

## Architecture

The demo uses the following components from the Xv6FileSystem library:

- **BlockDevice**: Simulates disk storage with configurable block size
- **BufferCache**: Provides caching layer for block operations
- **FileSystem**: Main interface for file operations
- **OpenFlags**: Enumeration for file opening modes

## Code Structure

```
Xv6FileSystemDemos/
├── Program.cs              # Main demo application
├── README.md              # This documentation
└── Xv6FileSystemDemos.csproj  # Project configuration
```

## Key Learning Points

1. **Parameter Usage**: The demo shows how hardcoded values have been replaced with configurable parameters from `Param.BlockSize` and `Param.MaxBlocks`.

2. **File System Operations**: Demonstrates the complete lifecycle of file operations from creation to cleanup.

3. **Error Handling**: Shows proper exception handling for common error scenarios.

4. **Resource Management**: Illustrates proper file descriptor management and cleanup.

## Extending the Demo

You can extend this demo by:

1. Adding more complex file operations
2. Testing concurrent file access
3. Implementing directory operations
4. Adding performance benchmarks
5. Testing edge cases and error conditions

## Related Projects

- **Xv6FileSystem**: The core class library
- **Xv6FileSystemTests**: Unit tests for the file system

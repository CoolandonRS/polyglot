using System.Runtime.InteropServices;

namespace SharedMemory;

internal static partial class LinuxLibraryImports {
    [LibraryImport("libc", EntryPoint = "shm_open", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial int ShmOpen(string name, int oflag, int mode);
    
    [LibraryImport("libc", EntryPoint = "ftruncate", SetLastError = true)]
    internal static partial int Ftruncate(int fd, long length);
    
    [LibraryImport("libc", EntryPoint = "mmap", SetLastError = true)]
    internal static partial IntPtr MMap(IntPtr addr, ulong length, int prot, int flags, int fd, long offset);
    
    [LibraryImport("libc", EntryPoint = "munmap", SetLastError = true)]
    internal static partial int MUnmap(IntPtr addr, ulong length);
    
    [LibraryImport("libc", EntryPoint = "close", SetLastError = true)]
    internal static partial int Close(int fd);
    
    [LibraryImport("libc", EntryPoint = "shm_unlink", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)] 
    internal static partial int ShmUnlink(string name);
}
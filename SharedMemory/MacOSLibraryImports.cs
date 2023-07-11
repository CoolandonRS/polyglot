using System.Runtime.InteropServices;

namespace SharedMemory;

internal static partial class MacOSLibraryImports {
    [LibraryImport("libSystem", EntryPoint = "shm_open", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial IntPtr ShmOpen(string name, int oflag, int mode);

    [LibraryImport("libSystem", EntryPoint = "ftruncate", SetLastError = true)]
    internal static partial int Ftruncate(IntPtr fd, long length);

    [LibraryImport("libSystem", EntryPoint = "mmap", SetLastError = true)]
    internal static partial IntPtr MMap(IntPtr addr, ulong length, int prot, int flags, IntPtr fd, long offset);

    [LibraryImport("libSystem", EntryPoint = "munmap", SetLastError = true)]
    internal static partial int MUnmap(IntPtr addr, ulong length);

    [LibraryImport("libSystem", EntryPoint = "close", SetLastError = true)]
    internal static partial int Close(IntPtr fd);

    [LibraryImport("libSystem", EntryPoint = "shm_unlink", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial int ShmUnlink(string name);
}
using System.Runtime.Versioning;

using static SharedMemory.LinuxLibraryImports;

namespace SharedMemory;

[SupportedOSPlatform("linux")]
internal class LinuxSharedMemory : AbstractSharedMemory {
    private readonly int fd;
    private readonly IntPtr sharedMemoryPtr;

    protected override unsafe Span<byte> GetRaw() {
        ThrowIfDisposed();
        return new Span<byte>(sharedMemoryPtr.ToPointer(), Size);
    }

    public override void Dispose() {
        ThrowIfDisposed();
        Disposed = true;
        MUnmap(sharedMemoryPtr, (ulong)Size);
        Close(fd);
        ShmUnlink(Name);
        GC.SuppressFinalize(this);
    }

    public LinuxSharedMemory(string name, int bufferSize = 256) {
        this.Name = name;
        this.Size = bufferSize + 1;

        fd = ShmOpen(this.Name, 2 /* O_RDWR */ | 64 /* O_CREAT */, 420 /* S_IRUSR | S_IWUSR */);
        if (fd == -1) throw new InvalidOperationException("Failed to create or open shared memory segment");

        var result = Ftruncate(fd, Size);
        if (result == -1) {
            Close(fd);
            throw new InvalidOperationException("Failed to set the size of shared memory segment");
        }

        sharedMemoryPtr = MMap(IntPtr.Zero, (ulong)this.Size, 1 /* PROT_READ | PROT_WRITE */, 2 /* MAP_SHARED */, fd, 0);
        if (sharedMemoryPtr != IntPtr.Zero) return;
        Close(fd);
        throw new InvalidOperationException("Failed to map shared memory segment");
    }
}
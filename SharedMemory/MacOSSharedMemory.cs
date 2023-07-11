using System.Runtime.Versioning;

using static SharedMemory.MacOSLibraryImports;

namespace SharedMemory;

[SupportedOSPlatform("macos")]
internal class MacOsSharedMemory : AbstractSharedMemory {
    private readonly IntPtr sharedMemoryPtr;
    private IntPtr memoryHandle;

    protected override unsafe Span<byte> GetRaw() {
        ThrowIfDisposed();
        return new Span<byte>(sharedMemoryPtr.ToPointer(), Size);
    }

    public override void Dispose() {
        ThrowIfDisposed();
        Disposed = true;
        MUnmap(sharedMemoryPtr, (ulong)Size);
        Close(memoryHandle);
        ShmUnlink(Name);
        GC.SuppressFinalize(this);
    }

    public MacOsSharedMemory(string name, int bufferSize = 256) {
        this.Name = name;
        this.Size = bufferSize + 1;

        memoryHandle = ShmOpen(this.Name, 2 /* O_RDWR */ | 64 /* O_CREAT */, 420 /* S_IRUSR | S_IWUSR */);
        if (memoryHandle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create or open shared memory segment");

        var result = Ftruncate(memoryHandle, this.Size);
        if (result == -1) {
            Close(memoryHandle);
            throw new InvalidOperationException("Failed to set the size of shared memory segment");
        }

        sharedMemoryPtr = MMap(IntPtr.Zero, (ulong)this.Size, 1 /* PROT_READ | PROT_WRITE */, 2 /* MAP_SHARED */, memoryHandle, 0);
        if (sharedMemoryPtr != IntPtr.Zero) return;
        Close(memoryHandle);
        throw new InvalidOperationException("Failed to map shared memory segment");
    }
}
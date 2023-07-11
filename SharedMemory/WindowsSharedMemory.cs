using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;

namespace SharedMemory; 

[SupportedOSPlatform("windows")]
internal class WindowsSharedMemory : AbstractSharedMemory {
    private MemoryMappedFile mmf;

    
    protected override Span<byte> GetRaw() {
        ThrowIfDisposed();
        var dat = new byte[Size];
        mmf.CreateViewStream().Read(dat);
        return dat;
    }

    public override void Dispose() {
        ThrowIfDisposed();
        Disposed = true;
        mmf.Dispose();
        GC.SuppressFinalize(this);
    }

    public WindowsSharedMemory(string name, int size = 256) {
        this.Name = name;
        this.Size = size + 1;
        this.mmf = MemoryMappedFile.CreateOrOpen(name, size, MemoryMappedFileAccess.ReadWrite);
    }
}
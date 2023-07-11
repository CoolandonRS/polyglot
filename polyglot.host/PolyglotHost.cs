using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharedMemory;

namespace CoolandonRS.polyglot; 

[SupportedOSPlatform("windows"), SupportedOSPlatform("linux"), SupportedOSPlatform("macos")]
public class PolyglotHost : IDisposable {
    private AbstractSharedMemory? memory;
    private string name;
    private int size;
    private bool init;
    private CancellationTokenSource cancel;
        
    public void Start() {
        AssertInitState(false);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            memory = new WindowsSharedMemory(name, size);
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            memory = new LinuxSharedMemory(name, size);
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            memory = new MacOsSharedMemory(name, size);
        } else {
            throw new PlatformNotSupportedException();
        }
        cancel = new CancellationTokenSource();
        init = true;
        #pragma warning disable CS4014
            Listen(cancel.Token);
        #pragma warning restore CS4014
    }
    
    private async Task Listen(CancellationToken token) {
        AssertInitState();
        while (true) {
            if (token.IsCancellationRequested) return;
            AssertInitState();
            try {
                DataReceived.Invoke(await memory!.ToByteCollection());
            } catch (TaskCanceledException) {
                return;
            }
        }
    }

    public void AssertInitState(bool goal = true) {
        if (init != goal || (memory == null) != goal) throw new InvalidOperationException();
    }
    
    public delegate void DataReception(ByteCollection mem);
    public event DataReception DataReceived;

    public void Dispose() {
        memory?.Dispose();
        memory = null;
        init = false;
        GC.SuppressFinalize(this);
    }

    public PolyglotHost(string name, int size = 256) {
        this.init = false;
        this.name = name;
        this.size = size;
    }
}
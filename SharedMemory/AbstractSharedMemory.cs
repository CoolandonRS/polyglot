using System.ComponentModel;
using System.Data;
using System.Xml;

namespace SharedMemory;

public abstract class AbstractSharedMemory : IDisposable {
    protected string Name;
    protected int Size;
    protected bool Disposed = false;
    protected bool Claimer = false;
    
    public byte this[int idx] {
        set => SyncSet(idx, value);
    }

    protected abstract Span<byte> GetRaw();
    public async Task<byte[]> Get(CancellationToken? cancel = null) => await Claim(GetRaw()[1..].ToArray(), Operation.Read, cancel);

    /// <summary>
    /// This is the only write operation that <b>REQUIRES</b> read mode, and cannot be run in write mode.
    /// </summary>
    public async Task Empty() => await Set(0, Array.Empty<byte>());

    /// <summary>
    /// Should only be used if catastrophic errors occur, typically caused by cancelling. <br/>
    /// Sets the status to AwaitingWrite, and completely clears all contents, losing all data.
    /// </summary>
    public void Nuke() {
        var arr = GetRaw();
        for (var i = arr.Length; i > 0; i--) arr[i] = 0;
    }

    /// <summary> Overwrites current data with val, padding with 0s. </summary>
    public async Task Set(byte[] val) => await SetAfter(0, val);

    /// <summary> Overwrites current data with val, starting at idx, padding with 0s. </summary>
    public async Task SetAfter(int idx, byte[] val, CancellationToken? cancel = null) {
        ThrowIfDisposed();
        await Claim(Operation.Write, cancel);
        if (val.Length + idx + 1 >= Size) throw new IndexOutOfRangeException();
        var diff = (Size - 2) - val.Length;
        var list = new List<byte>(val);
        for (var i = 0; i < diff; i++) list.Add(0);
        await Set(idx, list.ToArray());
    }

    public void SyncSet(int idx, byte val) {
        ThrowIfDisposed();
        if (!IsClaimed(Operation.Write)) throw new InvalidMemoryStateException();
        idx++;
        if (idx >= Size) throw new IndexOutOfRangeException();
        GetRaw()[idx] = val;
    }
    public async Task Set(int idx, byte val, CancellationToken? cancel = null) {
        ThrowIfDisposed();
        await Claim(Operation.Write, cancel);
        idx++;
        if (idx >= Size) throw new IndexOutOfRangeException();
        GetRaw()[idx] = val;
    }

    /// <exception cref="IndexOutOfRangeException">Thrown before any modifications are made if the data cannot fit</exception>
    public async Task Set(int idx, byte[] val, CancellationToken? cancel = null) {
        ThrowIfDisposed();
        await Claim(Operation.Write, cancel);
        if (idx + val.Length + 1 >= Size) throw new IndexOutOfRangeException();
        void SetData() { // Allows span in async
            var span = GetRaw();
            foreach (var d in val) {
                span[idx] = d;
                idx++;
            }
        }
        SetData();
    }

    protected SharedMemoryStatus GetStatus() => MakeStatus(GetRaw()[0]);
    protected void SetStatus(SharedMemoryStatus newStatus) => GetRaw()[0] = (byte)ValidateStatus(newStatus);

    protected static SharedMemoryStatus MakeStatus(byte b) {
        return ValidateStatus((SharedMemoryStatus)b);
    }

    protected static SharedMemoryStatus ValidateStatus(SharedMemoryStatus status) {
        if (!Enum.IsDefined(status)) throw new InvalidEnumArgumentException();
        return status;
    }

    protected async Task AwaitStatus(SharedMemoryStatus status, CancellationToken? cancel = null, int delay = 100) {
        Func<Task> wait = cancel == null
            ? async () => { await Task.Delay(delay); }
            : async () => { await Task.Delay(delay, cancel.Value); };
        while (true) {
            cancel?.ThrowIfCancellationRequested();
            if (GetStatus() == status) return;
            await wait();
        }
    }

    public enum Operation {
        Read, Write
    }

    protected async Task<T> Claim<T>(T value, Operation op, CancellationToken? cancel = null, int delay = 100) {
        await Claim(op, cancel, delay);
        return value;
    }

    public bool IsClaimed(Operation op) {
        switch (op) {
            case Operation.Read when Claimer:
                switch (GetStatus()) {
                    case SharedMemoryStatus.Reading: return true;
                    case SharedMemoryStatus.Writing: throw new InvalidOperationException();
                    case SharedMemoryStatus.AwaitingRead or SharedMemoryStatus.AwaitingWrite: break;
                    default: throw new InvalidEnumArgumentException();
                }
                break;
            case Operation.Write when Claimer:
                switch (GetStatus()) {
                    case SharedMemoryStatus.Reading: throw new InvalidOperationException();
                    case SharedMemoryStatus.Writing: return true;
                    case SharedMemoryStatus.AwaitingRead or SharedMemoryStatus.AwaitingWrite: break;
                    default: throw new InvalidEnumArgumentException();
                }
                break;
            case Operation.Read or Operation.Write: break;
            default: throw new InvalidEnumArgumentException();
        }
        return false;
    }

    public async Task Claim(Operation op, CancellationToken? cancel = null, int delay = 100) {
        if (IsClaimed(op)) return;
        await AwaitStatus(op switch {
            Operation.Read => SharedMemoryStatus.AwaitingRead,
            Operation.Write => SharedMemoryStatus.AwaitingWrite,
            _ => throw new InvalidEnumArgumentException()
        }, cancel, delay);
        SetStatus(op switch {
            Operation.Read => SharedMemoryStatus.Reading,
            Operation.Write => SharedMemoryStatus.Writing,
            _ => throw new InvalidEnumArgumentException()
        });
        Claimer = true;
    }

    /// <summary>
    /// Gets, Empties, and then Finalizes, storing the data in a ByteCollection.
    /// </summary>
    public async Task<ByteCollection> ToByteCollection(CancellationToken? cancel = null) {
        var output = new ByteCollection(await Get(cancel));
        await Empty();
        Finalize(Operation.Read);
        return output;
    }

    public void Finalize(Operation op) {
        if (!Claimer) throw new InvalidOperationException();
        switch (GetStatus()) {
            case SharedMemoryStatus.Reading:
                switch (op) {
                    case Operation.Read:
                        SetStatus(SharedMemoryStatus.AwaitingWrite);
                        return;
                    case Operation.Write: throw new InvalidOperationException();
                    default: throw new InvalidEnumArgumentException();
                }
            case SharedMemoryStatus.Writing:
                switch (op) {
                    case Operation.Read: throw new InvalidOperationException();
                    case Operation.Write:
                        SetStatus(SharedMemoryStatus.AwaitingRead);
                        return;
                    default: throw new InvalidEnumArgumentException();
                }
            case SharedMemoryStatus.AwaitingRead or SharedMemoryStatus.AwaitingWrite:
                throw new InvalidOperationException();
            default: throw new InvalidEnumArgumentException();
        }
    }

    protected void ThrowIfDisposed() {
        if (Disposed) throw new ObjectDisposedException("AbstractSharedMemory");
    }

    public abstract void Dispose();
}
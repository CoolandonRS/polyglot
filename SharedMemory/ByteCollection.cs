using System.Text;

namespace SharedMemory;

/// <summary>
/// A copy of the contents of a shared memory made when reading.
/// </summary>
public class ByteCollection {
    private readonly byte[] bytes;

    public byte this[int idx] => Get()[idx];
    public byte[] this[Range range] => Get()[range];

    public byte[] Get() => bytes;
    public byte Get(int idx) => bytes[idx];
    public byte[] GetN(int idx, int count) => bytes[idx..(idx + count)];
    public bool GetBool(int idx) => bytes[idx] == 1;
    public short GetShort(int idx) => BitConverter.ToInt16(GetN(idx, 2));
    public int GetInt(int idx) => BitConverter.ToInt32(GetN(idx, 4));
    public long GetLong(int idx) => BitConverter.ToInt64(GetN(idx, 8));
    public float GetFloat(int idx) => BitConverter.ToSingle(GetN(idx, 4));
    public double GetDouble(int idx) => BitConverter.ToDouble(GetN(idx, 8));
    public char ToChar(int idx) => BitConverter.ToChar(GetN(idx, 2));
    public string ToString(int startIdx, Encoding encoding) => encoding.GetString(bytes[startIdx..]);


    public ByteCollection(byte[] bytes) => this.bytes = bytes;
    public ByteCollection(int size) => this.bytes = new byte[size];
}
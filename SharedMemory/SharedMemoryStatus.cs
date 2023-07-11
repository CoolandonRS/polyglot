namespace SharedMemory; 

public enum SharedMemoryStatus {
    AwaitingWrite = 0,
    Writing = 1,
    AwaitingRead = 2,
    Reading = 3
}
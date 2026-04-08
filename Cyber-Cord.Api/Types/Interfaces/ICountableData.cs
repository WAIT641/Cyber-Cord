namespace Cyber_Cord.Api.Types.Interfaces;

public interface ICountableData<T>
{
    void Add(T value);
    void Remove(T value);
    int Count { get; }
    bool IsValid();
    void Invalidate();
    ReaderWriterLockSlim Mutex { get; }
}

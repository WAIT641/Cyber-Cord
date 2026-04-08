namespace Cyber_Cord.Api.Types;

public readonly struct AutoReadLock : IDisposable
{
    private readonly ReaderWriterLockSlim _mutex;

    public AutoReadLock(ReaderWriterLockSlim mutex)
    {
        mutex.EnterReadLock();

        _mutex = mutex;
    }

    public void Dispose()
    {
        _mutex.ExitReadLock();
    }
}

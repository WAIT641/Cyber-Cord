namespace Cyber_Cord.Api.Types;

public readonly struct AutoWriteLock : IDisposable
{
    private readonly ReaderWriterLockSlim _mutex;

    public AutoWriteLock(ReaderWriterLockSlim mutex)
    {
        mutex.EnterWriteLock();

        _mutex = mutex;
    }

    public void Dispose()
    {
        _mutex.ExitWriteLock();
    }
}

using System.Security.Principal;

namespace SoundSwitcher.Infrastructure;

/// <summary>
/// Owns a per-user named mutex for the lifetime of the application.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly string _mutexName;
    private Mutex? _mutex;

    public SingleInstanceGuard(string? mutexName = null)
    {
        _mutexName = mutexName ?? CreateDefaultMutexName();
    }

    public bool TryAcquire()
    {
        if (_mutex is not null)
        {
            return true;
        }

        var candidate = new Mutex(initiallyOwned: true, _mutexName, out var createdNew);

        if (!createdNew)
        {
            candidate.Dispose();
            return false;
        }

        _mutex = candidate;
        return true;
    }

    public void Dispose()
    {
        if (_mutex is null)
        {
            return;
        }

        try
        {
            _mutex.ReleaseMutex();
        }
        finally
        {
            _mutex.Dispose();
            _mutex = null;
        }
    }

    private static string CreateDefaultMutexName()
    {
        var userSid = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        return $"Local\\SoundSwitcher.{userSid}";
    }
}

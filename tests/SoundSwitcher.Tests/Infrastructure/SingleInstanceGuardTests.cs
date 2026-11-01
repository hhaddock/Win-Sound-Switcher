using SoundSwitcher.Infrastructure;

namespace SoundSwitcher.Tests.Infrastructure;

public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void OnlyTheFirstGuardCanAcquireTheSameMutex()
    {
        var mutexName = $"Local\\SoundSwitcher.Tests.{Guid.NewGuid():N}";
        using var first = new SingleInstanceGuard(mutexName);
        using var second = new SingleInstanceGuard(mutexName);

        Assert.True(first.TryAcquire());
        Assert.False(second.TryAcquire());
    }

    [Fact]
    public void AReleasedMutexCanBeAcquiredAgain()
    {
        var mutexName = $"Local\\SoundSwitcher.Tests.{Guid.NewGuid():N}";

        using (var first = new SingleInstanceGuard(mutexName))
        {
            Assert.True(first.TryAcquire());
        }

        using var second = new SingleInstanceGuard(mutexName);
        Assert.True(second.TryAcquire());
    }
}

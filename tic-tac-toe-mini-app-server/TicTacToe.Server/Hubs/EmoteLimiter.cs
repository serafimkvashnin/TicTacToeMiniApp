using System.Collections.Concurrent;

namespace TicTacToe.Server.Hubs;

/// <summary>Не даёт засыпать соперника стикерами: не чаще одного раза в <see cref="MinInterval"/> с подключения.</summary>
public sealed class EmoteLimiter(TimeProvider time)
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1);

    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSent = new();

    public bool TryAcquire(string connectionId)
    {
        var now = time.GetUtcNow();
        var allowed = true;

        _lastSent.AddOrUpdate(
            connectionId,
            now,
            (_, last) =>
            {
                allowed = now - last >= MinInterval;
                return allowed ? now : last;
            });

        return allowed;
    }

    public void Forget(string connectionId) => _lastSent.TryRemove(connectionId, out _);
}

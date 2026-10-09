using System.Collections.Concurrent;

namespace TicTacToe.Server.Hubs;

/// <summary>
/// Защита от скриптов: не чаще одного стикера в <see cref="MinInterval"/> с подключения.
/// Человеку столько не нажать, так что обычной игре ограничение не мешает.
/// </summary>
public sealed class EmoteLimiter(TimeProvider time)
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(100);

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

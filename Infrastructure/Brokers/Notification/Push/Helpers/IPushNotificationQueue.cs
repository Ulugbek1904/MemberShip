using Infrastructure.Brokers.Notification.Push.Contracts;
using System.Threading.Channels;

namespace Infrastructure.Brokers.Notification.Push.Helpers;

public interface IPushNotificationQueue
{
    void Enqueue(PushNotificationRequest notification, long[] userId);
    ValueTask<(PushNotificationRequest, long[])> DequeueAsync(CancellationToken token);
}
public class PushNotificationQueue : IPushNotificationQueue
{
    private readonly Channel<(PushNotificationRequest notification, long[] userId)> _channel;
    /// <inheritdoc />
    public PushNotificationQueue()
    {
        _channel = Channel.CreateUnbounded<(PushNotificationRequest, long[])>(
            new UnboundedChannelOptions
            {
                SingleReader = true,  // faqat bitta consumer
                SingleWriter = false  // bir nechta threadlar yozishi mumkin
            });
    }
    public void Enqueue(PushNotificationRequest notification, long[] userId)
    {
        if (!_channel.Writer.TryWrite((notification, userId)))
        {
            Console.WriteLine("Failed to enqueue push notification.");
        }
    }
    public bool TryDequeue(out (PushNotificationRequest notification, long[] userId) item)
    {
        if (_channel.Reader.TryRead(out var result))
        {
            item = result;
            return true;
        }

        item = default;
        return false;
    }
    public async ValueTask<(PushNotificationRequest, long[])> DequeueAsync(CancellationToken token)
    {
        var result = await _channel.Reader.ReadAsync(token);
        return result;
    }
}
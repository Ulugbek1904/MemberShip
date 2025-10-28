using Infrastructure.Brokers.Notification.Push.Contracts;

namespace Infrastructure.Brokers.Notification.Push;

public interface INotifyBroker
{
    Task Push(PushNotificationRequest request, long[] userIds);
}

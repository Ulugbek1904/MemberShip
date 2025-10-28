using Domain.Models.Common;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Infrastructure.Brokers.Notification.Push.Helpers;

public class PushNotificationBackgroundService(
    IServiceProvider serviceProvider,
    IPushNotificationQueue queue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("PushNotificationBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (request, userIds) = await queue.DequeueAsync(stoppingToken);

                // Skip if queue was empty
                if (userIds.Length == 0)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                using var scope = serviceProvider.CreateScope();
                var androidBroker = scope.ServiceProvider.GetRequiredService<INotifyBroker>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                Log.Information($"[Worker] Sending push to {userIds.Length} user(s).");

                await androidBroker.Push(request, userIds);

                var createdAt = DateTime.Now;
                var notifications = userIds.Select(userId => request.SourceId.HasValue
                        ? new PushNotification
                        { UserId = userId, SourceId = request.SourceId, IsRead = false, CreatedAt = createdAt }
                        : new PushNotification
                        {
                            UserId = userId,
                            Title = request.Title,
                            Description = request.Description,
                            ThumbUrl = request.ThumbUrl,
                            IsRead = false,
                            CreatedAt = createdAt
                        })
                    .ToList();

                await context.PushNotifications.AddRangeAsync(notifications, stoppingToken);
                await context.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing push notification");
                await Task.Delay(2000, stoppingToken); // delay before retry
            }
        }

        Log.Information("PushNotificationBackgroundService stopped.");
    }
}

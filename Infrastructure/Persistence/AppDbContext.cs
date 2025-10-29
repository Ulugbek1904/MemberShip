using Common.EF.DbContext;
using Domain.Models.Auth;
using Domain.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DefaultConfiguredDbContext
{
    public AppDbContext()
    {
    }
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    #region Auth
    public DbSet<User> Users { get; set; }
    public DbSet<UserToCompany> UserToCompanies { get; set; }
    #endregion

    #region Common
    public DbSet<PushNotification> PushNotifications { get; set; }
    public DbSet<PushNotificationSource> PushNotificationSources { get; set; }
    public DbSet<PopUpNotificationSource> PopUpNotificationSources { get; set; }
    public DbSet<PopUpNotification> PopUpNotifications { get; set; }
    #endregion


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

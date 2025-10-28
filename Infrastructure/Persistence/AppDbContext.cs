using Common.EF.DbContext;
using Domain.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal class AppDbContext : DefaultConfiguredDbContext
{
    public AppDbContext()
    {
    }
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    #region Common
    public virtual DbSet<PushNotification> PushNotifications { get; set; }
    #endregion


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

using Common.EF.DbContext;
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
    #endregion
    #region Common
    public virtual DbSet<PushNotification> PushNotifications { get; set; }
    #endregion


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

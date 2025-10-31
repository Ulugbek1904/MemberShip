using Common.EF.DbContext;
using Domain.Models.Auth;
using Domain.Models.Common;
using Domain.Models.Org;
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
    public DbSet<VendorUser> VendorUsers { get; set; }
    public DbSet<UserToCompany> UserToCompanies { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<VerificationCode> VerificationCodes { get; set; }
    #endregion

    #region Common
    public DbSet<PushNotification> PushNotifications { get; set; }
    public DbSet<PushNotificationSource> PushNotificationSources { get; set; }
    #endregion

    #region Org
    public DbSet<Company> Companies { get; set; }
    #endregion
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

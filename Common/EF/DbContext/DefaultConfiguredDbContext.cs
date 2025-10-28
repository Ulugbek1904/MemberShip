using Common.Common.Models;
using Common.Models.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;

namespace Common.EF.DbContext;

public abstract class DefaultConfiguredDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    protected DefaultConfiguredDbContext()
    {
    }

    protected DefaultConfiguredDbContext(DbContextOptions options)
        : base(options)
    {
    }

    private void TrackActionsAtV2()
    {
        DateTime now = DateTime.Now;
        foreach (EntityEntry item in ChangeTracker.Entries().Where(delegate (EntityEntry x)
        {
            EntityState state2 = x.State;
            return (uint)(state2 - 3) <= 1u;
        }))
        {
            if (item.State == EntityState.Added)
            {
                item.Entity.GetType().GetProperty("CreatedAt")?.SetValue(item.Entity, now);
            }

            EntityState state = item.State;
            if ((uint)(state - 3) <= 1u)
            {
                item.Entity.GetType().GetProperty("UpdatedAt")?.SetValue(item.Entity, now);
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        TrackActionsAtV2();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
    {
        TrackActionsAtV2();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public async Task Transactional(Func<Task> action)
    {
        await Transactional(action, (IDbContextTransaction transaction) => transaction.CommitAsync(), (IDbContextTransaction transaction, Exception e) => transaction.RollbackAsync());
    }

    public async Task<T> Transactional<T>(Func<Task<T>> action)
    {
        return await Transactional(action, (IDbContextTransaction transaction) => transaction.CommitAsync(), (IDbContextTransaction transaction, Exception e) => transaction.RollbackAsync());
    }

    public async Task Transactional(Func<Task> action, Func<IDbContextTransaction, Task> onCommit, Func<IDbContextTransaction, Exception, Task> onRollback)
    {
        if (Database.CurrentTransaction != null)
        {
            await action();
            return;
        }

        await using IDbContextTransaction transaction = await Database.BeginTransactionAsync();
        try
        {
            await action();
            await onCommit(transaction);
        }
        catch (Exception arg)
        {
            await onRollback(transaction, arg);
            throw;
        }
    }

    public async Task<T> Transactional<T>(Func<Task<T>> action, Func<IDbContextTransaction, Task> onCommit, Func<IDbContextTransaction, Exception, Task> onRollback)
    {
        if (Database.CurrentTransaction != null)
        {
            return await action();
        }

        T result2;
        await using (IDbContextTransaction transaction = await Database.BeginTransactionAsync())
        {
            try
            {
                T result = await action();
                await onCommit(transaction);
                result2 = result;
            }
            catch (Exception arg)
            {
                await onRollback(transaction, arg);
                throw;
            }
        }

        return result2;
    }

    //public async Task<T?> GetNextSequenceValue<T>(string name, string? schemaName = null)
    //{
    //    var result = await Database.SqlQuery<long>($"select nextval('{schema}.{name}') as \"Value\"").ToListAsync();
    //    return result.FirstOrDefault();
    //}

    public async Task<T> GetNextSequenceValueRequired<T>(string name, string? schemaName = null)
    {
        T val = await GetNextSequenceValue<T>(name, schemaName);
        if (val == null)
        {
            throw new Exception("Sequence value is null");
        }

        return val;
    }

    public async Task<long> GetNextSequenceValue(string name, string? schemaName = null)
    {
        return await GetNextSequenceValueRequired<long>(name, schemaName);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (PropertyInfo item in from x in (from x in GetType().GetProperties()
                                                 where x.PropertyType.IsGenericType
                                                 where x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
                                                 select x.PropertyType.GetGenericArguments().FirstOrDefault()).SelectMany((Type x) => x.GetProperties())
                                      where x.PropertyType == typeof(MultiLanguageField)
                                      select x)
        {
            var entityBuilder = modelBuilder.Entity(item.ReflectedType);
            var propertyMethod = typeof(EntityTypeBuilder<>)
                .MakeGenericType(item.ReflectedType)
                .GetMethod("Property", new[] { typeof(string) });

            var propertyBuilder = propertyMethod.Invoke(entityBuilder, new object[] { item.Name });
            propertyBuilder!.GetType().GetMethod("HasColumnType")?.Invoke(propertyBuilder, new object[] { "jsonb" });
        }

        foreach (IMutableEntityType item2 in from x in modelBuilder.Model.GetEntityTypes()
                                             where (object)x.ClrType.BaseType != null && x.ClrType.BaseType.IsGenericType && x.ClrType.BaseType?.GetGenericTypeDefinition() == typeof(ReferenceModelBase<>)
                                             select x)
        {
            modelBuilder.Entity(item2.ClrType).HasIndex("Id").IsUnique();
            modelBuilder.Entity(item2.ClrType).HasKey("Id");
        }
    }
}
using FiapCloudGames.UsersApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.UsersApi.Data;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.ToTable("Users");
        user.HasKey(x => x.Id);
        user.Property(x => x.Name).HasMaxLength(120).IsRequired();
        user.Property(x => x.Email).HasMaxLength(160).IsRequired();
        user.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        user.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        user.HasIndex(x => x.Email).IsUnique();
    }
}

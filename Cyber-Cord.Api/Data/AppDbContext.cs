using Cyber_Cord.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cyber_Cord.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    public DbSet<Settings> Settings { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Server> Servers { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<UserChat> UserChats { get; set; }
    public DbSet<UserServer> UserServers { get; set; }
    public DbSet<BanUserServer> BanUserServers { get; set; }
    public DbSet<Channel> Channels { get; set; }
    public DbSet<LogEntry> LogEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
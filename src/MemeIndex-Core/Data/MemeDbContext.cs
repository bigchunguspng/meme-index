using MemeIndex_Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Data;

public class MemeDbContext : DbContext
{
    private readonly IConfigProvider<Config> _configProvider;

    public MemeDbContext(DbContextOptions<MemeDbContext> options, IConfigProvider<Config> configProvider) : base(options)
    {
        _configProvider = configProvider;
    }

    public AccessGate Access { get; } = new();

    public DbSet<Entities.Tag>  Tags  { get; set; } = default!;
    public DbSet<Entities.File> Files { get; set; } = default!;
    public DbSet<Entities.Word> Words { get; set; } = default!;
    public DbSet<Entities.Mean> Means { get; set; } = default!;

    public DbSet<Entities.IndexingOption>     IndexingOptions      { get; set; } = default!;
    public DbSet<Entities.MonitoredDirectory> MonitoredDirectories { get; set; } = default!;
    public DbSet<Entities.         Directory>          Directories { get; set; } = default!;

    // 1. open MemeIndex-Core in Terminal
    // 2. dotnet ef --startup-project ..\MemeIndex-Console\ migrations add Initial
    // 3. dotnet ef --startup-project ..\MemeIndex-Console\ database update

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _configProvider.GetConfig().GetDbConnectionString()!;
        optionsBuilder.UseSqlite(connectionString);
    }
}
using MemeIndex_Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Directory = MemeIndex_Core.Data.Entities.Directory;
using File = MemeIndex_Core.Data.Entities.File;

namespace MemeIndex_Core.Data;

public class MemeDbContext : DbContext
{
    private readonly IConfigProvider<Config> _configProvider;

    public MemeDbContext
    (
        DbContextOptions<MemeDbContext> options,
        IConfigProvider<Config> configProvider
    )
        : base(options)
    {
        _configProvider = configProvider;
    }

    public SemaphoreSlim Access = new(1);

    public DbSet<Tag>  Tags  { get; set; } = default!;
    public DbSet<File> Files { get; set; } = default!;
    public DbSet<Word> Words { get; set; } = default!;
    public DbSet<Mean> Means { get; set; } = default!;

    public DbSet<Directory> Directories { get; set; } = default!;

    public DbSet<IndexingOption>     IndexingOptions      { get; set; } = default!;
    public DbSet<MonitoredDirectory> MonitoredDirectories { get; set; } = default!;

    // 1. open MemeIndex-Core in Terminal
    // 2. dotnet ef --startup-project ..\MemeIndex-Console\ migrations add Initial -o .\Data\Migrations\

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _configProvider.GetConfig().GetDbConnectionString()!;
        optionsBuilder.UseSqlite(connectionString);
    }
}
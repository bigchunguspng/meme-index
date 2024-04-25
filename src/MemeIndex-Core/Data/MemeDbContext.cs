using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Data;

public class MemeDbContext : DbContext
{
    public MemeDbContext(DbContextOptions<MemeDbContext> options) : base(options)
    {
    }

    public bool IsReadyToUse { get; set; } = true;

    public DbSet<Entities.Tag>  Tags  { get; set; } = default!;
    public DbSet<Entities.File> Files { get; set; } = default!;
    public DbSet<Entities.Word> Words { get; set; } = default!;
    public DbSet<Entities.Mean> Means { get; set; } = default!;

    public DbSet<Entities.IndexingOption>     IndexingOptions      { get; set; } = default!;
    public DbSet<Entities.MonitoredDirectory> MonitoredDirectories { get; set; } = default!;
    public DbSet<Entities.         Directory>          Directories { get; set; } = default!;

    // dotnet ef --startup-project ..\MemeIndex-Console\ migrations add Initial
    // dotnet ef --startup-project ..\MemeIndex-Console\ database update

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = ConfigRepository.GetConfig();
        var connectionString = config.DbConnectionString!
            .Replace("[data]", config.DataPath)
            .Replace("[/]", Path.DirectorySeparatorChar.ToString());
        optionsBuilder.UseSqlite(connectionString);
    }
}
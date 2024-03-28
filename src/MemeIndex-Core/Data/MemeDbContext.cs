using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Data;

public class MemeDbContext : DbContext
{
    public MemeDbContext(DbContextOptions<MemeDbContext> options) : base(options)
    {
    }

    public DbSet<Entities.File> Files { get; set; } = default!;
    public DbSet<Entities.Mean> Means { get; set; } = default!;
    public DbSet<Entities.Text> Texts { get; set; } = default!;

    public DbSet<Entities.Directory> Directories { get; set; } = default!;

    // dotnet ef --startup-project ..\MemeIndex-Console\ migrations add Initial
    // dotnet ef --startup-project ..\MemeIndex-Console\ database update

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = ConfigRepository.GetConfig();
        optionsBuilder.UseSqlite(config.DbConnectionString!.Replace("[data]", config.DataPath));
    }
}
using MemeIndex_Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Data;

public static class DatabaseInitializer
{
    public static void EnsureCreated(MemeDbContext context)
    {
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }

        if (!context.Means.Any())
        {
            context.Means.AddRange
            (
                new Mean { Id = 1, Code = "rgb" },
                new Mean { Id = 2, Code = "eng" }
            );

            context.SaveChanges();
        }
    }
}
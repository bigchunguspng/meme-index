using MemeIndex_Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Data;

public static class DatabaseInitializer
{
    public static void EnsureCreated(this MemeDbContext context)
    {
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }

        if (!context.Means.Any())
        {
            context.Means.AddRange
            (
                new Mean
                {
                    Id = Mean.RGB_CODE,
                    Code = "rgb",
                    Title = "Color",
                    Subtitle = "Color"
                },
                new Mean
                {
                    Id = Mean.ENG_CODE,
                    Code = "eng",
                    Title = "Text",
                    Subtitle = "OCR (English)"
                }
            );

            context.SaveChanges();
        }
    }
}
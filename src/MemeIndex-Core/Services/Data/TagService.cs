using MemeIndex_Core.Data;
using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Services.Indexing;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Services.Data;

public class TagService(MemeDbContext context)
{
    public async Task AddRange(IEnumerable<ImageContent> contents)
    {
        foreach (var content in contents)
        {
            var wordIds = new Queue<int>();
            foreach (var word in content.Words)
            {
                var entity = await GetOrCreateWordEntity(word.Word);
                wordIds.Enqueue(entity.Id);
            }

            var tags = content.Words.Select(x => new Tag
            {
                FileId = content.File.Id,
                WordId = wordIds.Dequeue(),
                MeanId = content.MeanId,
                Rank = x.Rank,
            });
            await context.Tags.AddRangeAsync(tags);
            await context.SaveChangesAsync();
        }
    }

    private async Task<Word> GetOrCreateWordEntity(string word)
    {
        var existing = context.Words.FirstOrDefault(x => x.Text == word);
        if (existing != null)
        {
            return existing;
        }

        var entity = new Word { Text = word };

        await context.Words.AddAsync(entity);
        await context.SaveChangesAsync();

        return entity;
    }

    public async Task RemoveTagsFromFiles(IEnumerable<int> fileIds)
    {
        var tags = context.Tags.Where(x => fileIds.Contains(x.FileId));
        context.Tags.RemoveRange(tags);
        await context.SaveChangesAsync();
    }

    public Task<int> RemoveTagsByMean(int meanId)
    {
        return context.Tags.Where(x => x.MeanId == meanId).ExecuteDeleteAsync();
    }
}
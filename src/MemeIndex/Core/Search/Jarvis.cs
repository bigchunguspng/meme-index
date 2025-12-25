using MemeIndex.DB;

namespace MemeIndex.Core.Search;

public static class Jarvis
{
    public static async Task<IEnumerable<DB_Tag>> Search(string[] terms)
    {
        // SQL all from tags where term in (terms)
        var con = await AppDB.ConnectTo_Main();
        var tags = await con.Tags_GetByTerms(terms);
        return tags;
        // C# group tags by file_id -> (term, score)
        // C# rm ones with missing tags (count < tags.length)
        // C# calc score,order by score, take 100
        // SQL set id name path from files where id in file_ids join dirs
        // c# return
    }
}
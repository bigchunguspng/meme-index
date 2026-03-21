using MemeIndex.DB;

namespace MemeIndex.Core.Search;

public static partial class Jarvis
{
    public static void Test_SQL()
    {
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Expression: ");
            Console.ForegroundColor = ConsoleColor.Green;
            var input = Console.ReadLine();
            Console.ResetColor();
            Console.WriteLine();
            try
            {
                if (input == null) return;

                var tokens = Lex(input);
                var sql = Build_SQL_GetFiles_Simple(tokens, 0, 100);
                Console.WriteLine(sql);
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Print($"{e}", ConsoleColor.DarkRed);
            }
        }
    }

    // SEARCH

    public static async Task<SearchResponse> Search_ByColor
        (string expression, int skip, int take)
    {
        Log("[Jv2/FILES]", expression);

        var sw = Stopwatch.StartNew();
        await using var con = await AppDB.ConnectTo_Main();

        var key = new CacheKey(expression, skip, take);
        var files = Cache_TryGetValue(key);
        var count = Cache_TryGetValue(expression);

        if (count < 0)
        {
            var tokens = Lex(expression);
            var sql = Build_SQL_GetFiles_WithCount(tokens, skip, take);
            var files_db = await con.Files_UI_GetBySQL_WithCount(sql);

            if (files == null) // TYPICAL, count and files
            {
                files = new List<File_UI>(100);
                files.AddRange(files_db.Select(file_db_wc =>
                {
                    if (count < 0) count = file_db_wc.total;
                    return File_UI_GetCached_OrCreate(file_db_wc);
                }));
                sw.Log("[Jv2/FILES] DB GET FILES w/ COUNT");

                Cache(key, files);

                if (count < 0) count = 0;
            }
            else // RARE, count only
            {
                count = files_db.FirstOrDefault()?.total ?? 0;
                sw.Log("[Jv2/FILES] DB GET FILES COUNT");
            }

            Cache(expression, count);
        }
        else if (files == null)
        {
            var tokens = Lex(expression);
            var sql = Build_SQL_GetFiles_Simple(tokens, skip, take);
            var files_db = await con.Files_UI_GetBySQL_Simple(sql);

            files = new List<File_UI>(100);
            files.AddRange(files_db.Select(File_UI_GetCached_OrCreate));
            sw.Log("[Jv2/FILES] DB GET FILES");

            Cache(key, files);
        }

        var dir_ids = files.Select(x => x.D).Distinct();
        var dirs_db = await con.Dirs_GetByIds(dir_ids);
        var dirs = dirs_db.ToDictionary(x => x.Id, x => x.Path + Path.DirectorySeparatorChar);
        sw.Log("[Jv2/FILES] DB GET DIRS");

        return new SearchResponse
        {
            P = new Pagination(skip, files.Count, count),
            D = dirs,
            F = files,
        };
    }
}
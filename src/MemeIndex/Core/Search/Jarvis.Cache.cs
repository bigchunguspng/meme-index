using System.Diagnostics.CodeAnalysis;
using MemeIndex.DB;

namespace MemeIndex.Core.Search;

public static partial class Jarvis
{
    private const int
        Config_CACHE_QUERIES_COUNT   = 32, // 1024
        Config_CACHE_QUERIES_MINUTES = 15; // 30 todo implement time based eviction

    private record struct CacheKey(string Expression, int Skip, int Take);

    private static readonly LimitedCache<CacheKey, List<int>> _cache_file_ids  = new(Config_CACHE_QUERIES_COUNT);
    private static readonly LimitedCache<string,   int>       _cache_counts    = new(Config_CACHE_QUERIES_COUNT); // file counts by expr
    private static readonly Dictionary  <int,      File_UI>   _cache_files     = new(); // files by file id
    private static readonly Dictionary  <int,      int>       _cache_relevance = new(); // reference count by file id

    public static void Cache_Clear()
    {
        Log("[Jv2/$]", $"CLEARING >> F: {_cache_files.Count,5} | Q: {_cache_file_ids.Count,5}");
        _cache_file_ids .Clear();
        _cache_counts   .Clear();
        _cache_files    .Clear();
        _cache_relevance.Clear();
        Log("[Jv2/$]", "CLEARED!");
    }

    // FILES

    private static void Cache
        (CacheKey key, IEnumerable<File_UI> files)
    {
        var count_pages_old = _cache_file_ids.Count;
        var count_files_old = _cache_files   .Count;

        var file_ids = new List<int>(100);
        foreach (var file in files)
        {
            var id = file.I;
            file_ids.Add(id);
            if (_cache_files.ContainsKey(id).Janai())
            {
                _cache_files    [id] = file;
                _cache_relevance[id] = 1;
            }
            else
                _cache_relevance[id]++;
        }

        _cache_file_ids.Add(key, file_ids, out var file_ids_evicted);

        var diff_pages = _cache_file_ids.Count - count_pages_old;
        var diff_files = _cache_files   .Count - count_files_old;
        Log("[Jv2/$]", $"UPDATE >> F: {diff_files,5:+0} -> {_cache_files.Count,5} | Q: {diff_pages,5:+0} -> {_cache_file_ids.Count,5}");

        if (file_ids_evicted != null)
        {
            count_files_old = _cache_files.Count;
            foreach (var id in file_ids_evicted)
            {
                if (--_cache_relevance[id] == 0)
                {
                    _cache_files    .Remove(id);
                    _cache_relevance.Remove(id);
                }
            }

            diff_files = _cache_files.Count - count_files_old;
            Log("[Jv2/$]", $"EVICT  >> F: {diff_files,5} -> {_cache_files.Count,5}");
        }
    }

    private static List<File_UI>? Cache_TryGetValue
        (CacheKey key)
    {
        var file_ids = _cache_file_ids.GetValueOrDefault(key);
        if (file_ids == null)
            return null;

        var files = new List<File_UI>(file_ids.Count);
        files.AddRange(file_ids.Select(id => _cache_files[id]));
        return files;
    }

    private static File_UI File_UI_GetCached_OrCreate
        (DB_File_UI file)
    {
        return _cache_files.TryGetValue(file.id, out var file_ui)
            ? file_ui
            : new File_UI(file);
    }

    // COUNT

    private static void Cache
        (string expression, int count)
    {
        _cache_counts.Add(expression, count, out _);
    }

    private static int Cache_TryGetValue
        (string expression)
    {
        return _cache_counts.GetValueOrDefault(expression, -1);
    }
}
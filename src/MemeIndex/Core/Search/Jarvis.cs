using MemeIndex.DB;
using SixLabors.ImageSharp;

namespace MemeIndex.Core.Search;

public record struct Pagination(int o, int r, int t); // offset, returned, total

public class SearchResponse
{
    public required Pagination              p { get; set; }
    public required Dictionary<int, string> d { get; set; }
    public required File_UI_SoA_Slice       f { get; set; }
}

public record File_UI_SoA_Slice(File_UI_SoA Files, int Skip, int Take)
{
    public Pagination GetPagination() => new (Skip, Math.Min(Take, Files.I.Count - Skip), Files.I.Count);

    public IEnumerable<int> GetDirIds
        () => Files.D.Skip(Skip).Take(Take).Distinct();
}

public class File_UI_SoA(int capacity = 16)
{
    public List<int>      I { get; } = new(capacity);
    public List<int>      D { get; } = new(capacity);
    public List<string>   N { get; } = new(capacity);
    public List<long>     S { get; } = new(capacity);
    public List<DateTime> M { get; } = new(capacity);
    public List<Size>     X { get; } = new(capacity);

    public void Add(DB_File_UI file)
    {
        I.Add(file.id);
        D.Add(file.dir_id);
        N.Add(file.name);
        S.Add(file.size);
        M.Add(DateTime.FromFileTimeUtc(file.mdate));
        X.Add(file is { image_w: not null, image_h: not null }
            ? new Size(file.image_w.Value, file.image_h.Value)
            : Size.Empty);
    }
}
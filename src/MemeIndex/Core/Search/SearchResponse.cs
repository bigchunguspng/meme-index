using MemeIndex.DB;
using SixLabors.ImageSharp;

namespace MemeIndex.Core.Search;

public class SearchResponse
{
    public required Pagination              P { get; set; }
    public required Dictionary<int, string> D { get; set; }
    public required List<File_UI>           F { get; set; }
}

public record struct Pagination(int O, int R, int? T = null); // Offset, Returned, Total

public class File_UI(DB_File_UI file)
{
    public int      I { get; } = file.id;
    public int      D { get; } = file.dir_id;
    public string   N { get; } = file.name;
    public long     S { get; } = file.size;
    public DateTime M { get; } = DateTime.FromFileTimeUtc(file.mdate);
    public Size     X { get; } = file.image_w is null
                              || file.image_h is null
        ?     Size.Empty
        : new Size(file.image_w.Value, file.image_h.Value);
}
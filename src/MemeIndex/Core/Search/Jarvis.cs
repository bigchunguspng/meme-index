using MemeIndex.DB;
using SixLabors.ImageSharp;

namespace MemeIndex.Core.Search;

public record struct Pagination(int o, int r, int t); // offset, returned, total

public class SearchResponse
{
    public required Pagination              p { get; set; }
    public required Dictionary<int, string> d { get; set; }
    public required List<File_UI>           f { get; set; }
}

public class File_UI(DB_File_UI file)
{
    public int      I { get; } = file.id;
    public int      D { get; } = file.dir_id;
    public string   N { get; } = file.name;
    public long     S { get; } = file.size;
    public DateTime M { get; } = DateTime.FromFileTimeUtc(file.mdate);
    public Size     X { get; } = file is { image_w: not null, image_h: not null }
        ? new Size(file.image_w.Value, file.image_h.Value)
        : Size.Empty;
}
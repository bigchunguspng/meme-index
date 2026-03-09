using MemeIndex.DB;
using SixLabors.ImageSharp;

namespace MemeIndex.Core.Search;

public record struct Pagination(int o, int r, int t); // offset, returned, total

public class SearchResponse
{
    public required Pagination              p { get; set; }
    public required Dictionary<int, string> d { get; set; }
    public required IEnumerable   <File_UI> f { get; set; }
}

public readonly struct File_UI(DB_File_UI file)
{
    public int      i { get; } = file.id;
    public int      d { get; } = file.dir_id;
    public string   n { get; } = file.name;
    public long     s { get; } = file.size;
    public DateTime m { get; } = DateTime.FromFileTimeUtc(file.mdate);
    public Size     x { get; } = file is { image_w: not null, image_h: not null }
        ? new Size(file.image_w.Value, file.image_h.Value)
        : Size.Empty;
}
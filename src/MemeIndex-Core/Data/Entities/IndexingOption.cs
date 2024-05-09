namespace MemeIndex_Core.Data.Entities;

/// <summary>
/// Relationship between the <b>folder</b> and its <b>indexing method</b>.
/// Different folder can be indexed by different means.  
/// </summary>
public class IndexingOption : AbstractEntity
{
    public int MonitoredDirectoryId { get; set; }
    public int MeanId { get; set; }

    public MonitoredDirectory MonitoredDirectory { get; set; } = default!;
    public Mean Mean { get; set; } = default!;
}
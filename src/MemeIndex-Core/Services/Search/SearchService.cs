using MemeIndex_Core.Data;
using MemeIndex_Core.Services.Data;
using Microsoft.EntityFrameworkCore;
using File = MemeIndex_Core.Entities.File;

namespace MemeIndex_Core.Services.Search;

public class SearchService
{
    private readonly IFileService _fileService;
    private readonly IDirectoryService _directoryService;
    private readonly MemeDbContext _context;

    private bool _dirty = true;
    private IList<File>? _files;

    public SearchService(IFileService fileService, IDirectoryService directoryService, MemeDbContext context)
    {
        _fileService = fileService;
        _directoryService = directoryService;
        _context = context;
        _context.SavedChanges += ContextOnSavedChanges;
    }

    public async Task<IList<File>?> SearchByText(string text)
    {
        try
        {
            if (_dirty || _files is null)
            {
                _dirty = false;
                _files = await _fileService.GetAllFilesWithPath();
            }
        }
        catch
        {
            // cry about it
        }

        return _files?.Where(x => x.Name.ToLower().Contains(text.ToLower())).ToList();
    }

    private void ContextOnSavedChanges(object? sender, SavedChangesEventArgs e)
    {
        _dirty = true;
    }
}
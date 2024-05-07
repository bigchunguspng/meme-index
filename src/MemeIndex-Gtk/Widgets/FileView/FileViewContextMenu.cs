using Gdk;
using Gtk;
using Humanizer;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils.FileOpener;
using TextCopy;
using File = MemeIndex_Core.Entities.File;

namespace MemeIndex_Gtk.Widgets.FileView;

public class FileViewContextMenu
{
    private readonly FileOpener FileOpener = FileOpenerFactory.GetFileOpener();
    private File? _selectedFile;

    public File? SelectedFile
    {
        get => _selectedFile;
        set
        {
            _selectedFile = value;
            ShowFileDetailsInStatus();
        }
    }

    public FileViewContextMenu(IFileView fileView)
    {
        var widget = fileView.AsWidget();
        widget.ButtonPressEvent += (o, args) =>
        {
            if (args.Event.Type == EventType.DoubleButtonPress) OpenFile(o, EventArgs.Empty);
        };
        widget.PopupMenu += (_, _) => OpenFilesContextMenu();
        widget.ButtonReleaseEvent += (_, args) =>
        {
            if (args.Event.Button == 3) OpenFilesContextMenu();
        };
    }

    public void OpenFilesContextMenu()
    {
        var menu = new Menu();

        var itemO = new MenuItem("Open");
        var itemE = new MenuItem("Show in Explorer");
        var itemC = new MenuItem("Copy path");

        var fileSelected = SelectedFile is not null;

        itemO.Sensitive = fileSelected;
        itemE.Sensitive = fileSelected;
        itemC.Sensitive = fileSelected;

        itemO.Activated += OpenFile;
        itemE.Activated += ShowFileInExplorer;
        itemC.Activated += CopyFilePath;

        menu.Add(itemO);
        menu.Add(itemE);
        menu.Add(itemC);

        menu.ShowAll();
        menu.Popup();
    }

    public void OpenFile(object? sender, EventArgs e)
    {
        if (SelectedFile is null) return;

        FileOpener.OpenFileWithDefaultApp(SelectedFile.GetFullPath());
    }

    public void ShowFileInExplorer(object? sender, EventArgs e)
    {
        if (SelectedFile is null) return;

        FileOpener.ShowFileInExplorer(SelectedFile.GetFullPath());
    }

    public async void CopyFilePath(object? sender, EventArgs args)
    {
        if (SelectedFile is null) return;

        await ClipboardService.SetTextAsync(SelectedFile.GetFullPath().Quote());
    }

    public void ShowFileDetailsInStatus()
    {
        if (SelectedFile is null) return;

        var path = SelectedFile.GetFullPath();
        var size = SelectedFile.Size.Bytes().ToString("#.#");
        var date = SelectedFile.Modified.ToLocalTime().ToString("dd.MM.yyyy' 'HH:mm");
        Logger.Status($"Size: {size}, Modified: {date}, Path: {path}");
    }
}
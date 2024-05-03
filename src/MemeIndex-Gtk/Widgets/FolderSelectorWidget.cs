using Gtk;
using MemeIndex_Core.Entities;
using MemeIndex_Core.Model;
using MemeIndex_Gtk.Windows;

namespace MemeIndex_Gtk.Widgets;

public class FolderSelectorWidget : Box
{
    private readonly ManageFoldersDialog _dialog;
    private readonly Container _container;

    private FileChooserButton Chooser { get; }
    private            Button Remover { get; }

    public CheckButton Recursive { get; }
    public CheckButton Eng { get; }
    public CheckButton RGB { get; }

    public string? PreviousChoice { get; set; }
    public string?         Choice => Chooser.Filename;

    public bool DirectorySelected => !string.IsNullOrEmpty(Choice);

    private IEnumerable<string?> Directories => _container.Children.Select(x =>
    {
        var row = (Bin)x;
        var box = (FolderSelectorWidget)row.Child;
        return box.Choice;
    });

    public FolderSelectorWidget
    (
        ManageFoldersDialog dialog, Container container, MonitoringOptions? directory = null
    )
        : base(Orientation.Horizontal, 5)
    {
        _dialog = dialog;
        _container = container;

        Chooser = new FileChooserButton(string.Empty, FileChooserAction.SelectFolder) { Expand = true };
        Remover = new Button { Label = "-" };

        Recursive = new CheckButton
        {
            Label = "Recursive",
            Active = directory?.Recursive ?? false
        };
        Eng = new CheckButton
        {
            Label = dialog.Means[Mean.ENG_CODE].Title,
            Active = directory?.Means.Contains(Mean.ENG_CODE) ?? false
        };
        RGB = new CheckButton
        {
            Label = dialog.Means[Mean.RGB_CODE].Title,
            Active = directory?.Means.Contains(Mean.RGB_CODE) ?? false
        };

        if (directory is not null) SelectDirectory(directory.Path);

        Chooser.SelectionChanged += ChooserOnFileSet;
        Remover.Clicked += RemoverOnClicked;

        UpdateSensitives();

        Add(Chooser);
        Add(Recursive);
        Add(Eng);
        Add(RGB);
        Add(Remover);
    }

    private void SelectDirectory(string? path)
    {
        PreviousChoice = path;

        if (path is null) Chooser.UnselectAll();
        else /*        */ Chooser.SelectFilename(path);
    }

    private void ChooserOnFileSet(object? sender, EventArgs e)
    {
        // prevent duplicates
        if (Directories.Count(x => x == Choice) > 1)
        {
            SelectDirectory(PreviousChoice);
        }

        // add new selector if no empty ones left
        var noEmptySelector = !Directories.Any(string.IsNullOrEmpty);
        if (noEmptySelector)
        {
            _container.Add(new FolderSelectorWidget(_dialog, _container));
            _container.ShowAll();
        }

        if (PreviousChoice is null && Choice is not null)
        {
            Recursive.Active = true;
            Eng.Active = true;
            RGB.Active = true;
        }

        PreviousChoice = Choice;
        UpdateSensitives();
    }

    private void UpdateSensitives()
    {
        Remover.Sensitive = DirectorySelected;
        Recursive.Sensitive = DirectorySelected;
        Eng.Sensitive = DirectorySelected;
        RGB.Sensitive = DirectorySelected;
    }

    private void RemoverOnClicked(object? sender, EventArgs e)
    {
        if (!DirectorySelected) return; // can't remove empty cell

        _container.Remove(Parent);
        _container.ShowAll();
    }
}
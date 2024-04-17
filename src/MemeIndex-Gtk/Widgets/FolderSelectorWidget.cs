using Gtk;

namespace MemeIndex_Gtk.Widgets;

public class FolderSelectorWidget : Box
{
    private readonly Container _container;

    private FileChooserButton Chooser { get; }
    private            Button Remover { get; }

    public string? PreviousChoice { get; set; }
    public string?         Choice => Chooser.Filename;

    public bool DirectorySelected => !string.IsNullOrEmpty(Choice);

    private IEnumerable<string?> Directories => _container.Children.Select(x =>
    {
        var row = (Bin)x;
        var box = (FolderSelectorWidget)row.Child;
        return box.Choice;
    });

    public FolderSelectorWidget(Container container, string? path = null) : base(Orientation.Horizontal, 5)
    {
        _container = container;

        Chooser = new FileChooserButton(string.Empty, FileChooserAction.SelectFolder) { Expand = true };
        Remover = new Button { Label = "-" };

        if (path != null) SelectDirectory(path);

        Chooser.SelectionChanged += ChooserOnFileSet;
        Remover.Clicked += RemoverOnClicked;
        Remover.Sensitive = DirectorySelected;

        Add(Chooser);
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
            _container.Add(new FolderSelectorWidget(_container));
            _container.ShowAll();
        }

        PreviousChoice = Choice;
        Remover.Sensitive = DirectorySelected;
    }

    private void RemoverOnClicked(object? sender, EventArgs e)
    {
        if (!DirectorySelected) return; // can't remove empty cell

        _container.Remove(Parent);
        _container.ShowAll();
    }
}
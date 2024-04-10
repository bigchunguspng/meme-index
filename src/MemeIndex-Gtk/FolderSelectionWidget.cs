using Gtk;

namespace MemeIndex_Gtk;

public class FolderSelectionWidget : Box
{
    private readonly ListBox _listBox;

    public FileChooserButton Chooser { get; set; }
    public Button RemoveButton { get; set; }

    public string? PreviousChoice { get; set; }
    public string? Choice => Chooser.Filename;
    public bool DirectorySelected => !string.IsNullOrEmpty(Choice);

    private IEnumerable<string?> Directories => _listBox.Children.Select(x =>
    {
        var row = (ListBoxRow)x;
        var box = (FolderSelectionWidget)row.Child;
        return box.Choice;
    });

    public FolderSelectionWidget(ListBox listBox, string? path = null) : base(Orientation.Horizontal, 5)
    {
        _listBox = listBox;

        RemoveButton = new Button { Label = "-" };
        Chooser = new FileChooserButton(string.Empty, FileChooserAction.SelectFolder) { Expand = true };

        if (path != null) SelectDirectory(path);

        RemoveButton.Sensitive = DirectorySelected;

        Chooser.SelectionChanged += ChooserOnFileSet;
        RemoveButton.Clicked += RemoveButtonOnClicked;
        Add(Chooser);
        Add(RemoveButton);
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

        // add new cell if no empty
        var noEmptyCell = !Directories.Any(string.IsNullOrEmpty);
        if (noEmptyCell)
        {
            _listBox.Add(new FolderSelectionWidget(_listBox));
        }

        PreviousChoice = Choice;
        RemoveButton.Sensitive = DirectorySelected;

        _listBox.ShowAll();
    }

    private void RemoveButtonOnClicked(object? sender, EventArgs e)
    {
        if (!DirectorySelected) return; // can't remove empty cell

        _listBox.Remove(Parent);
        _listBox.ShowAll();
    }
}
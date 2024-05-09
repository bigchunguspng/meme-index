using Gtk;
using File = MemeIndex_Core.Data.Entities.File;

namespace MemeIndex_Gtk.Widgets.FileView;

public interface IFileView
{
    Task ShowFiles(List<File> files);
    Widget AsWidget();
}
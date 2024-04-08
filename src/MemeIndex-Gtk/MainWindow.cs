using Gtk;
using Pango;
using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace MemeIndex_Gtk
{
    internal class MainWindow : Window
    {
        [UI] private readonly SearchEntry _search = default!;
      //[UI] private readonly ScrolledWindow _scroll = default!;
        [UI] private readonly TreeView _files = default!;
        [UI] private readonly Statusbar _status = default!;

        [UI] private readonly MenuItem _menuFileQuit = default!;

        public MainWindow() : this(new Builder("meme-index.glade"))
        {
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            _files.AppendColumn("Name", new CellRendererText { Ellipsize = EllipsizeMode.End }, "text", 0);
            _files.AppendColumn("Path", new CellRendererText { Ellipsize = EllipsizeMode.End }, "text", 1);
            _files.EnableSearch = false;

            foreach (var column in _files.Columns)
            {
                column.Resizable = true;
                column.Reorderable = true;
                column.FixedWidth = 200;
                column.Expand = true;
            }

            DeleteEvent += Window_DeleteEvent;
            _menuFileQuit.Activated += Window_DeleteEvent;
            _search.SearchChanged += OnSearchChanged;
        }

        private void OnSearchChanged(object? sender, EventArgs e)
        {
            var store = CreateStore();
            FillStore(store, new DirectoryInfo(@"D:\Desktop\…"), _search.Text);
            _files.Model = store;
        }

        private void Window_DeleteEvent(object? sender, EventArgs a)
        {
            Application.Quit();
        }

        ListStore CreateStore()
        {
            var store = new ListStore(typeof(string), typeof(string));

            //store.DefaultSortFunc = SortFunc;
            store.SetSortColumnId(1, SortType.Ascending);

            return store;
        }

        void FillStore(ListStore store, DirectoryInfo directory, string search)
        {
            store.Clear();

            var files = directory.GetFiles($"*{search}*", SearchOption.AllDirectories);
            _status.Push(0, $"Files: {files.Length}, search: {search}.");
            foreach (var file in files)
            {
                if (!file.Name.StartsWith(".")) store.AppendValues(file.Name, file.DirectoryName);
            }
        }

        /*int SortFunc (TreeModel model, TreeIter a, TreeIter b)
        {
            // sorts folders before files
            bool a_is_dir = (bool) model.GetValue (a, COL_IS_DIRECTORY);
            bool b_is_dir = (bool) model.GetValue (b, COL_IS_DIRECTORY);
            string a_name = (string) model.GetValue (a, COL_DISPLAY_NAME);
            string b_name = (string) model.GetValue (b, COL_DISPLAY_NAME);

            if (!a_is_dir && b_is_dir)
                return 1;
            else if (a_is_dir && !b_is_dir)
                return -1;
            else
                return String.Compare (a_name, b_name);
        }*/
    }
}
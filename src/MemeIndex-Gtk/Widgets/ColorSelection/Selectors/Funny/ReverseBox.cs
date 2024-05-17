using Gtk;

namespace MemeIndex_Gtk.Widgets.ColorSelection.Selectors.Funny;

public abstract class ReverseBox : Box
{
    private bool _reverse;

    protected ReverseBox(Orientation orientation, int spacing, bool reverse) : base(orientation, spacing)
    {
        Homogeneous = true;

        _reverse = reverse;
    }

    public bool Reverse
    {
        get => _reverse;
        set
        {
            var sameValue = _reverse == value;
            if (sameValue) return;

            _reverse = value;

            var children = Children.Reverse().ToArray();

            foreach (var child in children)
            {
                ReorderChild(child, -1);
            }
        }
    }
}
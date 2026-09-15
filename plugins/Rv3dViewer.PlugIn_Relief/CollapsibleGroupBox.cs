using System.Diagnostics.CodeAnalysis;

namespace Rv3dViewer.ReliefPlugin;

internal sealed class CollapsibleGroupBox : GroupBox
{
    private const int CollapsedHeight = 35;
    private string _caption = string.Empty;
    private bool _expanded = true;

    public event EventHandler? ExpandedChanged;

    [AllowNull]
    public override string Text
    {
        get => _caption;
        set
        {
            _caption = value ?? string.Empty;
            UpdateHeader();
        }
    }

    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value) return;
            _expanded = value;
            foreach (Control child in Controls) child.Visible = value;
            AutoSize = value;
            if (!value) Height = CollapsedHeight;
            UpdateHeader();
            Parent?.PerformLayout();
            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left && e.Y <= CollapsedHeight)
            Expanded = !Expanded;
    }

    private void UpdateHeader()
    {
        base.Text = $"{(_expanded ? '▼' : '▶')} {_caption}";
        Invalidate();
    }
}

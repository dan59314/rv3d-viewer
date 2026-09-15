using System.ComponentModel;

namespace Rv3dViewer.Plugin.WinForms;

[DefaultEvent(nameof(ExpandedChanged))]
public partial class CollapsibleGroupPanel : UserControl
{
    private bool _expanded = true;
    private int _expandedHeight = 220;
    private string _groupText = "設定";

    public CollapsibleGroupPanel()
    {
        InitializeComponent();
        UpdateExpandedState();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public FlowLayoutPanel ContentPanel => contentFlowLayoutPanel;

    [DefaultValue(true)]
    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value) return;
            _expanded = value;
            UpdateExpandedState();
            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [DefaultValue(220)]
    public int ExpandedHeight
    {
        get => _expandedHeight;
        set
        {
            _expandedHeight = Math.Max(headerButton.Height + 1, value);
            if (_expanded) Height = _expandedHeight;
        }
    }

    [DefaultValue("設定")]
    public string GroupText
    {
        get => _groupText;
        set
        {
            _groupText = value ?? string.Empty;
            UpdateHeaderText();
        }
    }

    public event EventHandler? ExpandedChanged;

    private void HeaderButton_Click(object? sender, EventArgs e) => Expanded = !Expanded;

    private void UpdateExpandedState()
    {
        contentFlowLayoutPanel.Visible = _expanded;
        UpdateHeaderText();
        Height = _expanded ? _expandedHeight : headerButton.Height;
    }

    private void UpdateHeaderText() => headerButton.Text = $"{(_expanded ? "▼" : "▶")} {_groupText}";
}

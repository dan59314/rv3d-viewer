namespace Rv3dViewer.RVInteriorDesignPlugin;

internal sealed class InteriorSceneTreeView : TreeView
{
    private readonly HashSet<TreeNode> _selectedNodes = [];
    private bool _updatingSelection;
    private Keys _mouseModifiers;

    internal event EventHandler? SelectedNodesChanged;

    internal IReadOnlyList<TreeNode> SelectedNodes => _selectedNodes
        .Where(node => node.TreeView == this)
        .ToArray();

    internal void SetSelectedNodes(IEnumerable<TreeNode> nodes, TreeNode? primaryNode)
    {
        var next = nodes.Where(node => node.TreeView == this).Distinct().ToArray();
        if (_selectedNodes.SetEquals(next) && ReferenceEquals(SelectedNode, primaryNode))
            return;

        _updatingSelection = true;
        try
        {
            _selectedNodes.Clear();
            _selectedNodes.UnionWith(next);
            SelectedNode = primaryNode?.TreeView == this ? primaryNode : next.LastOrDefault();
        }
        finally
        {
            _updatingSelection = false;
        }
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        var nodeBeforeClick = SelectedNode;
        _mouseModifiers = ModifierKeys;
        base.OnMouseDown(e);
        var clickedNode = GetNodeAt(e.Location);
        if ((_mouseModifiers.HasFlag(Keys.Control) || _mouseModifiers.HasFlag(Keys.Alt)) && clickedNode is not null &&
            ReferenceEquals(clickedNode, nodeBeforeClick) && _selectedNodes.Remove(clickedNode))
        {
            _updatingSelection = true;
            try
            {
                SelectedNode = _selectedNodes.LastOrDefault();
            }
            finally
            {
                _updatingSelection = false;
            }
            Invalidate();
            SelectedNodesChanged?.Invoke(this, EventArgs.Empty);
        }
        _mouseModifiers = Keys.None;
    }

    protected override void OnAfterSelect(TreeViewEventArgs e)
    {
        base.OnAfterSelect(e);
        if (_updatingSelection || e.Node is null)
            return;

        if (_mouseModifiers.HasFlag(Keys.Alt))
        {
            _selectedNodes.Remove(e.Node);
            _updatingSelection = true;
            try
            {
                SelectedNode = _selectedNodes.LastOrDefault();
            }
            finally
            {
                _updatingSelection = false;
            }
        }
        else if (_mouseModifiers.HasFlag(Keys.Shift))
        {
            _selectedNodes.Add(e.Node);
        }
        else if (_mouseModifiers.HasFlag(Keys.Control))
        {
            if (!_selectedNodes.Add(e.Node))
                _selectedNodes.Remove(e.Node);
        }
        else
        {
            _selectedNodes.Clear();
            _selectedNodes.Add(e.Node);
        }
        Invalidate();
        SelectedNodesChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnDrawNode(DrawTreeNodeEventArgs e)
    {
        if (e.Node is null)
            return;
        var selected = _selectedNodes.Contains(e.Node);
        var backColor = selected ? SystemColors.Highlight : BackColor;
        var foreColor = selected ? SystemColors.HighlightText : ForeColor;
        using var background = new SolidBrush(backColor);
        e.Graphics.FillRectangle(background, e.Bounds);
        TextRenderer.DrawText(e.Graphics, e.Node.Text, Font, e.Bounds, foreColor,
            TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
        if ((e.State & TreeNodeStates.Focused) != 0)
            ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds, foreColor, backColor);
    }
}

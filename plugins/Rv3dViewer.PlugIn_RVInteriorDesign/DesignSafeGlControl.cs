namespace Rv3dViewer.RVInteriorDesignPlugin;

/// <summary>
/// Prevents OpenTK from creating a GLFW window while Visual Studio is rendering the WinForms designer.
/// The containing form explicitly enables the normal GLControl lifecycle from its Shown event.
/// </summary>
internal sealed class DesignSafeGlControl : OpenTK.GLControl.GLControl
{
    private bool _runtimeContextEnabled;
    private bool _nativeContextCreated;

    internal bool RuntimeContextEnabled => _runtimeContextEnabled;

    internal void EnableRuntimeContext() => _runtimeContextEnabled = true;

    internal void EnsureRuntimeControlCreated()
    {
        if (!_runtimeContextEnabled || IsDisposed)
            return;
        if (!IsHandleCreated)
        {
            CreateControl();
            return;
        }
        if (!_nativeContextCreated)
            RecreateHandle();
    }

    protected override void OnCreateControl()
    {
        if (!_runtimeContextEnabled)
            return;
        base.OnCreateControl();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        if (!_runtimeContextEnabled)
            return;
        base.OnHandleCreated(e);
        _nativeContextCreated = true;
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (!_nativeContextCreated)
            return;
        try
        {
            base.OnHandleDestroyed(e);
        }
        finally
        {
            _nativeContextCreated = false;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        if (!_runtimeContextEnabled)
            return;
        base.OnLoad(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_runtimeContextEnabled)
        {
            base.OnPaint(e);
            return;
        }
        e.Graphics.Clear(Color.FromArgb(25, 29, 34));
        TextRenderer.DrawText(e.Graphics, "PBR 預覽將於程式執行時載入", Font, ClientRectangle,
            Color.DarkGray, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                            TextFormatFlags.EndEllipsis);
    }

    protected override void OnResize(EventArgs e)
    {
        if (!_runtimeContextEnabled)
            return;
        base.OnResize(e);
    }

    protected override void OnParentChanged(EventArgs e)
    {
        if (!_runtimeContextEnabled)
            return;
        base.OnParentChanged(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        if (!_runtimeContextEnabled)
            return;
        base.OnGotFocus(e);
    }
}

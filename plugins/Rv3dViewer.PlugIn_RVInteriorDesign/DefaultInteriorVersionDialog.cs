namespace Rv3dViewer.RVInteriorDesignPlugin;

internal enum DefaultInteriorVersion { None, Simple, Real }

internal sealed partial class DefaultInteriorVersionDialog : Form
{
    internal DefaultInteriorVersionDialog() => InitializeComponent();

    internal DefaultInteriorVersion Selection { get; private set; }

    private void SimpleButton_Click(object? sender, EventArgs e) =>
        Selection = DefaultInteriorVersion.Simple;

    private void RealButton_Click(object? sender, EventArgs e) =>
        Selection = DefaultInteriorVersion.Real;
}


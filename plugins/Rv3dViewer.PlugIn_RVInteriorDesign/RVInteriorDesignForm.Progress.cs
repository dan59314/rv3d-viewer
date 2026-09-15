namespace Rv3dViewer.RVInteriorDesignPlugin;

internal sealed partial class RVInteriorDesignForm
{
    private void BeginOperationProgress(string message, bool indeterminate = false)
    {
        statusLabel.Text = message;
        statusProgressBar.Style = indeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
        statusProgressBar.Value = 0;
        statusProgressBar.Visible = true;
    }

    private void ReportOperationProgress(int percent, string? message = null)
    {
        if (IsDisposed || Disposing) return;
        statusProgressBar.Style = ProgressBarStyle.Continuous;
        statusProgressBar.Value = Math.Clamp(percent, statusProgressBar.Minimum, statusProgressBar.Maximum);
        statusProgressBar.Visible = true;
        if (!string.IsNullOrWhiteSpace(message)) statusLabel.Text = message;
    }

    private void EndOperationProgress()
    {
        if (IsDisposed || Disposing) return;
        statusProgressBar.Style = ProgressBarStyle.Continuous;
        statusProgressBar.Value = 0;
        statusProgressBar.Visible = false;
    }
}

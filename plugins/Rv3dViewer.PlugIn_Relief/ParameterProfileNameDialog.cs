namespace Rv3dViewer.ReliefPlugin;

internal partial class ParameterProfileNameDialog : Form
{
    public ParameterProfileNameDialog(string title, string initialName = "")
    {
        InitializeComponent();
        Text = title;
        nameTextBox.Text = initialName;
        nameTextBox.SelectAll();
    }

    public string ProfileName => nameTextBox.Text.Trim();
}

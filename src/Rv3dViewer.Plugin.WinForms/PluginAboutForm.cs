using System.Diagnostics;

namespace Rv3dViewer.Plugin.WinForms;

public partial class PluginAboutForm : Form
{
    public PluginAboutForm(string pluginName, string pluginDescription)
    {
        InitializeComponent();
        pluginNameLabel.Text = pluginName;
        descriptionLabel.Text = pluginDescription;
    }

    private void CompanyLinkLabel_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.RasVector.url.tw/",
                UseShellExecute = true,
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(this, ex.Message, "開啟網址失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

using System.Diagnostics;

namespace WiseCooling_TwoPhase;

public partial class AboutForm : Form
{
    public AboutForm(bool darkMode)
    {
        InitializeComponent();
        ApplyTheme(darkMode);
    }

    private void lnkCompany_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.RasVector.url.tw/",
            UseShellExecute = true,
        });
    }

    private void btnOk_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void ApplyTheme(bool darkMode)
    {
        if (!darkMode)
        {
            return;
        }

        BackColor = Color.FromArgb(40, 40, 40);
        ForeColor = Color.WhiteSmoke;
        ApplyThemeToControls(Controls);
        lnkCompany.LinkColor = Color.LightSkyBlue;
        lnkCompany.ActiveLinkColor = Color.DeepSkyBlue;
        lnkCompany.VisitedLinkColor = Color.Plum;
    }

    private static void ApplyThemeToControls(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            control.BackColor = Color.FromArgb(40, 40, 40);
            control.ForeColor = Color.WhiteSmoke;

            if (control is Button button)
            {
                button.BackColor = Color.FromArgb(64, 64, 64);
                button.ForeColor = Color.WhiteSmoke;
                button.UseVisualStyleBackColor = false;
            }
            else if (control is LinkLabel linkLabel)
            {
                linkLabel.BackColor = Color.FromArgb(40, 40, 40);
                linkLabel.ForeColor = Color.WhiteSmoke;
            }

            if (control.HasChildren)
            {
                ApplyThemeToControls(control.Controls);
            }
        }
    }
}

namespace Rv3dViewer.Plugin.WinForms;

partial class PluginAboutForm
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel layoutPanel = null!;
    private Label pluginNameLabel = null!;
    private Label descriptionLabel = null!;
    private LinkLabel companyLinkLabel = null!;
    private Label authorLabel = null!;
    private Button closeButton = null!;

    protected override void Dispose(bool disposing) { if (disposing) components.Dispose(); base.Dispose(disposing); }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        layoutPanel = new TableLayoutPanel();
        pluginNameLabel = new Label();
        descriptionLabel = new Label();
        companyLinkLabel = new LinkLabel();
        authorLabel = new Label();
        closeButton = new Button();
        layoutPanel.SuspendLayout(); SuspendLayout();
        layoutPanel.ColumnCount = 1; layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutPanel.Controls.Add(pluginNameLabel, 0, 0);
        layoutPanel.Controls.Add(descriptionLabel, 0, 1);
        layoutPanel.Controls.Add(companyLinkLabel, 0, 2);
        layoutPanel.Controls.Add(authorLabel, 0, 3);
        layoutPanel.Controls.Add(closeButton, 0, 4);
        layoutPanel.Dock = DockStyle.Fill;
        layoutPanel.Padding = new Padding(24);
        layoutPanel.RowCount = 5;
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pluginNameLabel.Dock = DockStyle.Fill; pluginNameLabel.Font = new Font("Microsoft JhengHei UI", 16F, FontStyle.Bold);
        pluginNameLabel.ForeColor = SystemColors.HotTrack; pluginNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        descriptionLabel.AutoSize = true; descriptionLabel.Dock = DockStyle.Fill;
        descriptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        companyLinkLabel.AutoSize = true; companyLinkLabel.Dock = DockStyle.Fill;
        companyLinkLabel.Font = new Font("Microsoft JhengHei UI", 12F);
        companyLinkLabel.LinkBehavior = LinkBehavior.HoverUnderline; companyLinkLabel.Text = "影量科技有限公司";
        companyLinkLabel.TextAlign = ContentAlignment.MiddleLeft; companyLinkLabel.LinkClicked += CompanyLinkLabel_LinkClicked;
        authorLabel.AutoSize = true; authorLabel.Dock = DockStyle.Fill;
        authorLabel.Text = "作者: 呂芳元 Daniel Lu"; authorLabel.TextAlign = ContentAlignment.MiddleLeft;
        closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right; closeButton.AutoSize = true; closeButton.DialogResult = DialogResult.OK; closeButton.Text = "確定";
        AcceptButton = closeButton; CancelButton = closeButton; AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(520, 360); Controls.Add(layoutPanel); FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; Name = "PluginAboutForm"; StartPosition = FormStartPosition.CenterParent; Text = "關於";
        layoutPanel.ResumeLayout(false); layoutPanel.PerformLayout(); ResumeLayout(false);
    }
}

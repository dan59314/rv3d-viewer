#nullable enable

namespace Rv3dViewer.RVInteriorDesignPlugin;

partial class DefaultInteriorVersionDialog
{
    private System.ComponentModel.IContainer? components;
    private Label descriptionLabel = null!;
    private TableLayoutPanel buttonTableLayoutPanel = null!;
    private Button simpleButton = null!;
    private Button realButton = null!;
    private Button cancelButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        descriptionLabel = new Label();
        buttonTableLayoutPanel = new TableLayoutPanel();
        simpleButton = new Button();
        realButton = new Button();
        cancelButton = new Button();
        buttonTableLayoutPanel.SuspendLayout();
        SuspendLayout();
        descriptionLabel.Dock = DockStyle.Fill;
        descriptionLabel.Name = "descriptionLabel";
        descriptionLabel.Padding = new Padding(18, 14, 18, 8);
        descriptionLabel.Size = new Size(514, 126);
        descriptionLabel.TabIndex = 0;
        descriptionLabel.Text = "請選擇預設室內裝潢版本：\r\n\r\n簡單版：使用目前的參數化模型，建立快速。\r\n真實版：使用隨 Plugin 部署的 CC0 PBR 家具與植栽模型。";
        descriptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        buttonTableLayoutPanel.ColumnCount = 3;
        buttonTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
        buttonTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
        buttonTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33334F));
        buttonTableLayoutPanel.Controls.Add(simpleButton, 0, 0);
        buttonTableLayoutPanel.Controls.Add(realButton, 1, 0);
        buttonTableLayoutPanel.Controls.Add(cancelButton, 2, 0);
        buttonTableLayoutPanel.Dock = DockStyle.Bottom;
        buttonTableLayoutPanel.Name = "buttonTableLayoutPanel";
        buttonTableLayoutPanel.Padding = new Padding(12, 8, 12, 12);
        buttonTableLayoutPanel.RowCount = 1;
        buttonTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        buttonTableLayoutPanel.Size = new Size(514, 62);
        buttonTableLayoutPanel.TabIndex = 1;
        simpleButton.DialogResult = DialogResult.OK;
        simpleButton.Dock = DockStyle.Fill;
        simpleButton.Margin = new Padding(4);
        simpleButton.Name = "simpleButton";
        simpleButton.TabIndex = 0;
        simpleButton.Text = "簡單版";
        simpleButton.UseVisualStyleBackColor = true;
        simpleButton.Click += SimpleButton_Click;
        realButton.DialogResult = DialogResult.OK;
        realButton.Dock = DockStyle.Fill;
        realButton.Margin = new Padding(4);
        realButton.Name = "realButton";
        realButton.TabIndex = 1;
        realButton.Text = "真實版";
        realButton.UseVisualStyleBackColor = true;
        realButton.Click += RealButton_Click;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Dock = DockStyle.Fill;
        cancelButton.Margin = new Padding(4);
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 2;
        cancelButton.Text = "取消";
        cancelButton.UseVisualStyleBackColor = true;
        AcceptButton = realButton;
        AutoScaleMode = AutoScaleMode.None;
        CancelButton = cancelButton;
        ClientSize = new Size(514, 188);
        Controls.Add(descriptionLabel);
        Controls.Add(buttonTableLayoutPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "DefaultInteriorVersionDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "產生預設室內裝潢";
        buttonTableLayoutPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}


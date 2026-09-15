#nullable enable

namespace Rv3dViewer.Plugin.WinForms;

partial class InputOverlaySettingsForm
{
    private System.ComponentModel.IContainer? components = null;
    private TableLayoutPanel settingsLayoutPanel = null!;
    private Label textColorLabel = null!;
    private Panel colorPreviewPanel = null!;
    private Button textColorButton = null!;
    private Label fontSizeLabel = null!;
    private NumericUpDown fontSizeNumericUpDown = null!;
    private FlowLayoutPanel commandPanel = null!;
    private Button okButton = null!;
    private Button cancelButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        settingsLayoutPanel = new TableLayoutPanel();
        textColorLabel = new Label();
        colorPreviewPanel = new Panel();
        textColorButton = new Button();
        fontSizeLabel = new Label();
        fontSizeNumericUpDown = new NumericUpDown();
        commandPanel = new FlowLayoutPanel();
        okButton = new Button();
        cancelButton = new Button();
        settingsLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)fontSizeNumericUpDown).BeginInit();
        commandPanel.SuspendLayout();
        SuspendLayout();
        settingsLayoutPanel.ColumnCount = 3;
        settingsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        settingsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        settingsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
        settingsLayoutPanel.Controls.Add(textColorLabel, 0, 0);
        settingsLayoutPanel.Controls.Add(colorPreviewPanel, 1, 0);
        settingsLayoutPanel.Controls.Add(textColorButton, 2, 0);
        settingsLayoutPanel.Controls.Add(fontSizeLabel, 0, 1);
        settingsLayoutPanel.Controls.Add(fontSizeNumericUpDown, 1, 1);
        settingsLayoutPanel.SetColumnSpan(fontSizeNumericUpDown, 2);
        settingsLayoutPanel.Controls.Add(commandPanel, 0, 2);
        settingsLayoutPanel.SetColumnSpan(commandPanel, 3);
        settingsLayoutPanel.Dock = DockStyle.Fill;
        settingsLayoutPanel.Padding = new Padding(12);
        settingsLayoutPanel.RowCount = 3;
        settingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        settingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        settingsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        textColorLabel.Dock = DockStyle.Fill;
        textColorLabel.Text = "文字顏色";
        textColorLabel.TextAlign = ContentAlignment.MiddleLeft;
        colorPreviewPanel.BorderStyle = BorderStyle.FixedSingle;
        colorPreviewPanel.Dock = DockStyle.Fill;
        colorPreviewPanel.Margin = new Padding(3, 6, 3, 6);
        textColorButton.Dock = DockStyle.Fill;
        textColorButton.Text = "選擇…";
        textColorButton.Click += TextColorButton_Click;
        fontSizeLabel.Dock = DockStyle.Fill;
        fontSizeLabel.Text = "文字大小";
        fontSizeLabel.TextAlign = ContentAlignment.MiddleLeft;
        fontSizeNumericUpDown.DecimalPlaces = 1;
        fontSizeNumericUpDown.Dock = DockStyle.Fill;
        fontSizeNumericUpDown.Increment = 0.5M;
        fontSizeNumericUpDown.Minimum = 6M;
        fontSizeNumericUpDown.Maximum = 48M;
        commandPanel.Controls.Add(cancelButton);
        commandPanel.Controls.Add(okButton);
        commandPanel.Dock = DockStyle.Fill;
        commandPanel.FlowDirection = FlowDirection.RightToLeft;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Text = "取消";
        okButton.DialogResult = DialogResult.OK;
        okButton.Text = "確定";
        AcceptButton = okButton;
        AutoScaleMode = AutoScaleMode.None;
        CancelButton = cancelButton;
        ClientSize = new Size(340, 150);
        Controls.Add(settingsLayoutPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "InputOverlaySettingsForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Overlay 設定";
        settingsLayoutPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)fontSizeNumericUpDown).EndInit();
        commandPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}

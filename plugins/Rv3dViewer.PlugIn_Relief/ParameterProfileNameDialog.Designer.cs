#nullable enable

namespace Rv3dViewer.ReliefPlugin;

partial class ParameterProfileNameDialog
{
    private System.ComponentModel.IContainer? components = null;
    private TableLayoutPanel rootTableLayoutPanel = null!;
    private Label promptLabel = null!;
    private TextBox nameTextBox = null!;
    private FlowLayoutPanel buttonFlowLayoutPanel = null!;
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
        rootTableLayoutPanel = new TableLayoutPanel();
        promptLabel = new Label();
        nameTextBox = new TextBox();
        buttonFlowLayoutPanel = new FlowLayoutPanel();
        okButton = new Button();
        cancelButton = new Button();
        rootTableLayoutPanel.SuspendLayout();
        buttonFlowLayoutPanel.SuspendLayout();
        SuspendLayout();
        // 
        // rootTableLayoutPanel
        // 
        rootTableLayoutPanel.ColumnCount = 1;
        rootTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootTableLayoutPanel.Controls.Add(promptLabel, 0, 0);
        rootTableLayoutPanel.Controls.Add(nameTextBox, 0, 1);
        rootTableLayoutPanel.Controls.Add(buttonFlowLayoutPanel, 0, 2);
        rootTableLayoutPanel.Dock = DockStyle.Fill;
        rootTableLayoutPanel.Padding = new Padding(14);
        rootTableLayoutPanel.RowCount = 3;
        rootTableLayoutPanel.RowStyles.Add(new RowStyle());
        rootTableLayoutPanel.RowStyles.Add(new RowStyle());
        rootTableLayoutPanel.RowStyles.Add(new RowStyle());
        rootTableLayoutPanel.TabIndex = 0;
        // 
        // promptLabel
        // 
        promptLabel.AutoSize = true;
        promptLabel.Margin = new Padding(4, 4, 4, 6);
        promptLabel.Text = "參數設定名稱";
        // 
        // nameTextBox
        // 
        nameTextBox.Dock = DockStyle.Fill;
        nameTextBox.Margin = new Padding(4);
        nameTextBox.MaxLength = 80;
        nameTextBox.Name = "nameTextBox";
        nameTextBox.TabIndex = 0;
        // 
        // buttonFlowLayoutPanel
        // 
        buttonFlowLayoutPanel.AutoSize = true;
        buttonFlowLayoutPanel.Controls.Add(okButton);
        buttonFlowLayoutPanel.Controls.Add(cancelButton);
        buttonFlowLayoutPanel.Dock = DockStyle.Fill;
        buttonFlowLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonFlowLayoutPanel.Margin = new Padding(0, 12, 0, 0);
        buttonFlowLayoutPanel.WrapContents = false;
        // 
        // okButton
        // 
        okButton.AutoSize = true;
        okButton.DialogResult = DialogResult.OK;
        okButton.Margin = new Padding(5);
        okButton.Text = "確定";
        okButton.UseVisualStyleBackColor = true;
        // 
        // cancelButton
        // 
        cancelButton.AutoSize = true;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Margin = new Padding(5);
        cancelButton.Text = "取消";
        cancelButton.UseVisualStyleBackColor = true;
        // 
        // ParameterProfileNameDialog
        // 
        AcceptButton = okButton;
        AutoScaleDimensions = new SizeF(9F, 23F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(430, 170);
        Controls.Add(rootTableLayoutPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ParameterProfileNameDialog";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "參數設定名稱";
        rootTableLayoutPanel.ResumeLayout(false);
        rootTableLayoutPanel.PerformLayout();
        buttonFlowLayoutPanel.ResumeLayout(false);
        buttonFlowLayoutPanel.PerformLayout();
        ResumeLayout(false);
    }
}

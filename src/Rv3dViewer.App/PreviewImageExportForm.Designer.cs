#nullable enable

namespace Rv3dViewer.App;

partial class PreviewImageExportForm
{
    private System.ComponentModel.IContainer? components = null;
    private Label widthLabel = null!;
    private Label heightLabel = null!;
    private Label formatLabel = null!;
    private Label outputLabel = null!;
    private NumericUpDown widthNumericUpDown = null!;
    private NumericUpDown heightNumericUpDown = null!;
    private CheckBox keepAspectRatioCheckBox = null!;
    private ComboBox formatComboBox = null!;
    private TextBox outputPathTextBox = null!;
    private Button browseButton = null!;
    private Button exportButton = null!;
    private Button cancelButton = null!;
    private SaveFileDialog saveFileDialog = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        widthLabel = new Label();
        heightLabel = new Label();
        formatLabel = new Label();
        outputLabel = new Label();
        widthNumericUpDown = new NumericUpDown();
        heightNumericUpDown = new NumericUpDown();
        keepAspectRatioCheckBox = new CheckBox();
        formatComboBox = new ComboBox();
        outputPathTextBox = new TextBox();
        browseButton = new Button();
        exportButton = new Button();
        cancelButton = new Button();
        saveFileDialog = new SaveFileDialog();
        ((System.ComponentModel.ISupportInitialize)widthNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)heightNumericUpDown).BeginInit();
        SuspendLayout();
        // 
        // widthLabel
        // 
        widthLabel.AutoSize = true;
        widthLabel.Location = new Point(16, 20);
        widthLabel.Name = "widthLabel";
        widthLabel.Size = new Size(64, 18);
        widthLabel.TabIndex = 0;
        widthLabel.Text = "寬度 (px)";
        // 
        // heightLabel
        // 
        heightLabel.AutoSize = true;
        heightLabel.Location = new Point(16, 55);
        heightLabel.Name = "heightLabel";
        heightLabel.Size = new Size(64, 18);
        heightLabel.TabIndex = 1;
        heightLabel.Text = "高度 (px)";
        // 
        // formatLabel
        // 
        formatLabel.AutoSize = true;
        formatLabel.Location = new Point(16, 90);
        formatLabel.Name = "formatLabel";
        formatLabel.Size = new Size(36, 18);
        formatLabel.TabIndex = 2;
        formatLabel.Text = "格式";
        // 
        // outputLabel
        // 
        outputLabel.AutoSize = true;
        outputLabel.Location = new Point(16, 125);
        outputLabel.Name = "outputLabel";
        outputLabel.Size = new Size(36, 18);
        outputLabel.TabIndex = 3;
        outputLabel.Text = "輸出";
        // 
        // widthNumericUpDown
        // 
        widthNumericUpDown.Location = new Point(104, 20);
        widthNumericUpDown.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
        widthNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        widthNumericUpDown.Name = "widthNumericUpDown";
        widthNumericUpDown.Size = new Size(163, 24);
        widthNumericUpDown.TabIndex = 4;
        widthNumericUpDown.Value = new decimal(new int[] { 1920, 0, 0, 0 });
        widthNumericUpDown.ValueChanged += WidthNumericUpDown_ValueChanged;
        // 
        // heightNumericUpDown
        // 
        heightNumericUpDown.Location = new Point(104, 55);
        heightNumericUpDown.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
        heightNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        heightNumericUpDown.Name = "heightNumericUpDown";
        heightNumericUpDown.Size = new Size(163, 24);
        heightNumericUpDown.TabIndex = 5;
        heightNumericUpDown.Value = new decimal(new int[] { 1080, 0, 0, 0 });
        heightNumericUpDown.ValueChanged += HeightNumericUpDown_ValueChanged;
        // 
        // keepAspectRatioCheckBox
        // 
        keepAspectRatioCheckBox.AutoSize = true;
        keepAspectRatioCheckBox.Checked = true;
        keepAspectRatioCheckBox.CheckState = CheckState.Checked;
        keepAspectRatioCheckBox.Location = new Point(290, 22);
        keepAspectRatioCheckBox.Name = "keepAspectRatioCheckBox";
        keepAspectRatioCheckBox.Size = new Size(87, 22);
        keepAspectRatioCheckBox.TabIndex = 6;
        keepAspectRatioCheckBox.Text = "維持比例";
        keepAspectRatioCheckBox.CheckedChanged += KeepAspectRatioCheckBox_CheckedChanged;
        // 
        // formatComboBox
        // 
        formatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        formatComboBox.FormattingEnabled = true;
        formatComboBox.Items.AddRange(new object[] { "BMP", "JPG", "PNG" });
        formatComboBox.Location = new Point(104, 90);
        formatComboBox.Name = "formatComboBox";
        formatComboBox.Size = new Size(163, 25);
        formatComboBox.TabIndex = 7;
        formatComboBox.SelectedIndexChanged += FormatComboBox_SelectedIndexChanged;
        // 
        // outputPathTextBox
        // 
        outputPathTextBox.Location = new Point(104, 125);
        outputPathTextBox.Name = "outputPathTextBox";
        outputPathTextBox.Size = new Size(323, 24);
        outputPathTextBox.TabIndex = 8;
        // 
        // browseButton
        // 
        browseButton.Location = new Point(433, 118);
        browseButton.Name = "browseButton";
        browseButton.Size = new Size(75, 31);
        browseButton.TabIndex = 9;
        browseButton.Text = "瀏覽...";
        browseButton.Click += BrowseButton_Click;
        // 
        // exportButton
        // 
        exportButton.Location = new Point(352, 170);
        exportButton.Name = "exportButton";
        exportButton.Size = new Size(75, 38);
        exportButton.TabIndex = 10;
        exportButton.Text = "輸出";
        exportButton.Click += ExportButton_Click;
        // 
        // cancelButton
        // 
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(433, 170);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 38);
        cancelButton.TabIndex = 11;
        cancelButton.Text = "取消";
        cancelButton.Click += CancelButton_Click;
        // 
        // PreviewImageExportForm
        // 
        AcceptButton = exportButton;
        AutoScaleDimensions = new SizeF(8F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(546, 212);
        Controls.Add(widthLabel);
        Controls.Add(heightLabel);
        Controls.Add(formatLabel);
        Controls.Add(outputLabel);
        Controls.Add(widthNumericUpDown);
        Controls.Add(heightNumericUpDown);
        Controls.Add(keepAspectRatioCheckBox);
        Controls.Add(formatComboBox);
        Controls.Add(outputPathTextBox);
        Controls.Add(browseButton);
        Controls.Add(exportButton);
        Controls.Add(cancelButton);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "PreviewImageExportForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "輸出影像";
        ((System.ComponentModel.ISupportInitialize)widthNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)heightNumericUpDown).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}

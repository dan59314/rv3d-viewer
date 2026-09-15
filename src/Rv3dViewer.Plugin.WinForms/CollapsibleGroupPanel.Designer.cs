#nullable enable

namespace Rv3dViewer.Plugin.WinForms;

partial class CollapsibleGroupPanel
{
    private System.ComponentModel.IContainer? components = null;
    private Button headerButton = null!;
    private FlowLayoutPanel contentFlowLayoutPanel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        headerButton = new Button();
        contentFlowLayoutPanel = new FlowLayoutPanel();
        SuspendLayout();
        // 
        // headerButton
        // 
        headerButton.Dock = DockStyle.Top;
        headerButton.FlatStyle = FlatStyle.System;
        headerButton.Location = new Point(0, 0);
        headerButton.Name = "headerButton";
        headerButton.Size = new Size(380, 30);
        headerButton.TabIndex = 0;
        headerButton.Text = "▼ 設定";
        headerButton.TextAlign = ContentAlignment.MiddleLeft;
        headerButton.UseVisualStyleBackColor = true;
        headerButton.Click += HeaderButton_Click;
        // 
        // contentFlowLayoutPanel
        // 
        contentFlowLayoutPanel.AutoScroll = true;
        contentFlowLayoutPanel.Dock = DockStyle.Fill;
        contentFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        contentFlowLayoutPanel.Location = new Point(0, 30);
        contentFlowLayoutPanel.Name = "contentFlowLayoutPanel";
        contentFlowLayoutPanel.Padding = new Padding(8);
        contentFlowLayoutPanel.Size = new Size(380, 190);
        contentFlowLayoutPanel.TabIndex = 1;
        contentFlowLayoutPanel.WrapContents = false;
        // 
        // CollapsibleGroupPanel
        // 
        AutoScaleMode = AutoScaleMode.None;
        BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(contentFlowLayoutPanel);
        Controls.Add(headerButton);
        Name = "CollapsibleGroupPanel";
        Size = new Size(380, 220);
        ResumeLayout(false);
    }
}

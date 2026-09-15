#nullable enable

namespace Rv3dViewer.RVInteriorDesignPlugin;

partial class InteriorPreviewViewportControl
{
    private System.ComponentModel.IContainer? components;
    private Panel headerPanel = null!;
    private Label titleLabel = null!;
    private Button maximizeButton = null!;
    private DesignSafeGlControl previewGlControl = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeRenderer();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        headerPanel = new Panel();
        titleLabel = new Label();
        maximizeButton = new Button();
        previewGlControl = new DesignSafeGlControl();
        headerPanel.SuspendLayout();
        SuspendLayout();
        headerPanel.BackColor = Color.FromArgb(38, 43, 50);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(maximizeButton);
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(420, 28);
        headerPanel.TabIndex = 0;
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.ForeColor = Color.Gainsboro;
        titleLabel.Name = "titleLabel";
        titleLabel.Padding = new Padding(8, 0, 0, 0);
        titleLabel.Size = new Size(388, 28);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "透視圖｜PBR 預覽";
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        maximizeButton.Dock = DockStyle.Right;
        maximizeButton.FlatStyle = FlatStyle.Flat;
        maximizeButton.ForeColor = Color.Gainsboro;
        maximizeButton.Name = "maximizeButton";
        maximizeButton.Size = new Size(32, 28);
        maximizeButton.TabIndex = 1;
        maximizeButton.Text = "□";
        maximizeButton.UseVisualStyleBackColor = true;
        maximizeButton.Click += MaximizeButton_Click;
        previewGlControl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
        previewGlControl.APIVersion = new Version(3, 3, 0, 0);
        previewGlControl.AllowDrop = true;
        previewGlControl.Dock = DockStyle.Fill;
        previewGlControl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
        previewGlControl.IsEventDriven = true;
        previewGlControl.Location = new Point(0, 28);
        previewGlControl.Name = "previewGlControl";
        previewGlControl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
        previewGlControl.Size = new Size(418, 270);
        previewGlControl.TabIndex = 1;
        previewGlControl.MouseDown += PreviewGlControl_MouseDown;
        previewGlControl.MouseMove += PreviewGlControl_MouseMove;
        previewGlControl.MouseUp += PreviewGlControl_MouseUp;
        previewGlControl.MouseWheel += PreviewGlControl_MouseWheel;
        previewGlControl.DragEnter += PreviewGlControl_DragEnter;
        previewGlControl.DragOver += PreviewGlControl_DragOver;
        previewGlControl.DragDrop += PreviewGlControl_DragDrop;
        BackColor = Color.FromArgb(25, 29, 34);
        BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(previewGlControl);
        Controls.Add(headerPanel);
        Name = "InteriorPreviewViewportControl";
        Size = new Size(420, 300);
        headerPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}

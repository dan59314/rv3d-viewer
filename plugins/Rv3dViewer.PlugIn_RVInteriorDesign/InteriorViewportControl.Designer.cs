#nullable enable

namespace Rv3dViewer.RVInteriorDesignPlugin;

partial class InteriorViewportControl
{
    private System.ComponentModel.IContainer? components;
    private Panel headerPanel = null!;
    private Label titleLabel = null!;
    private Button maximizeButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        headerPanel = new Panel();
        titleLabel = new Label();
        maximizeButton = new Button();
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
        titleLabel.Text = "透視圖";
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
        BackColor = Color.FromArgb(25, 29, 34);
        BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(headerPanel);
        Name = "InteriorViewportControl";
        Size = new Size(420, 300);
        MouseDown += Viewport_MouseDown;
        MouseEnter += Viewport_MouseEnter;
        MouseMove += Viewport_MouseMove;
        MouseUp += Viewport_MouseUp;
        MouseWheel += Viewport_MouseWheel;
        headerPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}

namespace WiseCooling_TwoPhase;

partial class AboutForm
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel tlpAbout = null!;
    private Label lblTitle = null!;
    private LinkLabel lnkCompany = null!;
    private Label lblAuthor = null!;
    private Button btnOk = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
        tlpAbout = new TableLayoutPanel();
        lblTitle = new Label();
        lnkCompany = new LinkLabel();
        lblAuthor = new Label();
        btnOk = new Button();
        tlpAbout.SuspendLayout();
        SuspendLayout();
        // 
        // tlpAbout
        // 
        tlpAbout.ColumnCount = 1;
        tlpAbout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpAbout.Controls.Add(lblTitle, 0, 0);
        tlpAbout.Controls.Add(lnkCompany, 0, 1);
        tlpAbout.Controls.Add(lblAuthor, 0, 2);
        tlpAbout.Controls.Add(btnOk, 0, 3);
        tlpAbout.Dock = DockStyle.Fill;
        tlpAbout.Location = new Point(0, 0);
        tlpAbout.Name = "tlpAbout";
        tlpAbout.Padding = new Padding(14);
        tlpAbout.RowCount = 4;
        tlpAbout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        tlpAbout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        tlpAbout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        tlpAbout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpAbout.Size = new Size(363, 315);
        tlpAbout.TabIndex = 0;
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Dock = DockStyle.Fill;
        lblTitle.Font = new Font("Microsoft JhengHei UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 136);
        lblTitle.ForeColor = SystemColors.HotTrack;
        lblTitle.Location = new Point(17, 14);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(329, 70);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "RasVector 3D Viewer";
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lnkCompany
        // 
        lnkCompany.AutoSize = true;
        lnkCompany.Dock = DockStyle.Fill;
        lnkCompany.Font = new Font("Microsoft JhengHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
        lnkCompany.LinkBehavior = LinkBehavior.HoverUnderline;
        lnkCompany.Location = new Point(17, 84);
        lnkCompany.Name = "lnkCompany";
        lnkCompany.Size = new Size(329, 70);
        lnkCompany.TabIndex = 1;
        lnkCompany.TabStop = true;
        lnkCompany.Text = "影量科技有限公司";
        lnkCompany.TextAlign = ContentAlignment.MiddleLeft;
        lnkCompany.LinkClicked += lnkCompany_LinkClicked;
        // 
        // lblAuthor
        // 
        lblAuthor.AutoSize = true;
        lblAuthor.Dock = DockStyle.Fill;
        lblAuthor.Font = new Font("Microsoft JhengHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 136);
        lblAuthor.Location = new Point(17, 154);
        lblAuthor.Name = "lblAuthor";
        lblAuthor.Size = new Size(329, 70);
        lblAuthor.TabIndex = 2;
        lblAuthor.Text = "作者: 呂芳元 Daniel Lu";
        lblAuthor.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // btnOk
        // 
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.AutoSize = true;
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new Point(287, 270);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(59, 28);
        btnOk.TabIndex = 3;
        btnOk.Text = "確定";
        btnOk.UseVisualStyleBackColor = true;
        btnOk.Click += btnOk_Click;
        // 
        // AboutForm
        // 
        AcceptButton = btnOk;
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(363, 315);
        Controls.Add(tlpAbout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AboutForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "關於";
        tlpAbout.ResumeLayout(false);
        tlpAbout.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
}

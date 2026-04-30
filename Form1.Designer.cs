namespace PrintBatchMaster
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            txtSourceDir = new TextBox();
            btnBrowseSource = new Button();
            txtOutputDir = new TextBox();
            btnBrowseOutput = new Button();
            grpPadding = new GroupBox();
            label1 = new Label();
            comboBox1 = new ComboBox();
            lblGap = new Label();
            numSafeGap = new NumericUpDown();
            lblSize = new Label();
            numFontSize = new NumericUpDown();
            lblY = new Label();
            lblX = new Label();
            lblR = new Label();
            lblLW = new Label();
            lblL = new Label();
            chkAddText = new CheckBox();
            lblB = new Label();
            lblT = new Label();
            numTextY = new NumericUpDown();
            numTextX = new NumericUpDown();
            numPadRight = new NumericUpDown();
            numPadLeft = new NumericUpDown();
            numPadBottom = new NumericUpDown();
            numPadTop = new NumericUpDown();
            numLineWidth = new NumericUpDown();
            grpColor = new GroupBox();
            grptxtColor = new GroupBox();
            rdoWhite = new RadioButton();
            rdoGray = new RadioButton();
            rdoBlack = new RadioButton();
            rdotxtWhite = new RadioButton();
            rdotxtGray = new RadioButton();
            rdotxtBlack = new RadioButton();
            btnStart = new Button();
            progressBar1 = new ProgressBar();
            txtLog = new RichTextBox();
            lblS = new Label();
            lblO = new Label();
            grpPadding.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numSafeGap).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFontSize).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numTextY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numTextX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numPadRight).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numPadLeft).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numPadBottom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numPadTop).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLineWidth).BeginInit();
            grpColor.SuspendLayout();
            grptxtColor.SuspendLayout();
            SuspendLayout();
            // 
            // txtSourceDir
            // 
            txtSourceDir.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSourceDir.Location = new Point(100, 20);
            txtSourceDir.Name = "txtSourceDir";
            txtSourceDir.Size = new Size(400, 23);
            txtSourceDir.TabIndex = 10;
            // 
            // btnBrowseSource
            // 
            btnBrowseSource.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseSource.Location = new Point(510, 18);
            btnBrowseSource.Name = "btnBrowseSource";
            btnBrowseSource.Size = new Size(75, 25);
            btnBrowseSource.TabIndex = 9;
            btnBrowseSource.Text = "瀏覽來源";
            // 
            // txtOutputDir
            // 
            txtOutputDir.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtOutputDir.Location = new Point(100, 55);
            txtOutputDir.Name = "txtOutputDir";
            txtOutputDir.Size = new Size(400, 23);
            txtOutputDir.TabIndex = 8;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseOutput.Location = new Point(510, 53);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new Size(75, 25);
            btnBrowseOutput.TabIndex = 7;
            btnBrowseOutput.Text = "瀏覽目的";
            // 
            // grpPadding
            // 
            grpPadding.Controls.Add(label1);
            grpPadding.Controls.Add(comboBox1);
            grpPadding.Controls.Add(lblGap);
            grpPadding.Controls.Add(numSafeGap);
            grpPadding.Controls.Add(lblSize);
            grpPadding.Controls.Add(numFontSize);
            grpPadding.Controls.Add(lblY);
            grpPadding.Controls.Add(lblX);
            grpPadding.Controls.Add(lblR);
            grpPadding.Controls.Add(lblLW);
            grpPadding.Controls.Add(lblL);
            grpPadding.Controls.Add(chkAddText);
            grpPadding.Controls.Add(lblB);
            grpPadding.Controls.Add(lblT);
            grpPadding.Controls.Add(numTextY);
            grpPadding.Controls.Add(numTextX);
            grpPadding.Controls.Add(numPadRight);
            grpPadding.Controls.Add(numPadLeft);
            grpPadding.Controls.Add(numPadBottom);
            grpPadding.Controls.Add(numPadTop);
            grpPadding.Controls.Add(numLineWidth);
            grpPadding.Location = new Point(20, 100);
            grpPadding.Name = "grpPadding";
            grpPadding.Size = new Size(350, 150);
            grpPadding.TabIndex = 6;
            grpPadding.TabStop = false;
            grpPadding.Text = "印刷邊距與標記 (mm / pt)";
            // 
            // label1
            // 
            label1.Location = new Point(10, 122);
            label1.Name = "label1";
            label1.Size = new Size(81, 23);
            label1.TabIndex = 20;
            label1.Text = "文字起始位置";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(115, 119);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(95, 23);
            comboBox1.TabIndex = 19;
            // 
            // lblGap
            // 
            lblGap.Location = new Point(220, 92);
            lblGap.Name = "lblGap";
            lblGap.Size = new Size(35, 23);
            lblGap.TabIndex = 0;
            lblGap.Text = "線距";
            // 
            // numSafeGap
            // 
            numSafeGap.DecimalPlaces = 1;
            numSafeGap.Location = new Point(260, 90);
            numSafeGap.Name = "numSafeGap";
            numSafeGap.Size = new Size(75, 23);
            numSafeGap.TabIndex = 1;
            // 
            // lblSize
            // 
            lblSize.Location = new Point(10, 92);
            lblSize.Name = "lblSize";
            lblSize.Size = new Size(35, 23);
            lblSize.TabIndex = 2;
            lblSize.Text = "字級";
            // 
            // numFontSize
            // 
            numFontSize.Location = new Point(45, 90);
            numFontSize.Name = "numFontSize";
            numFontSize.Size = new Size(60, 23);
            numFontSize.TabIndex = 3;
            // 
            // lblY
            // 
            lblY.Location = new Point(220, 57);
            lblY.Name = "lblY";
            lblY.Size = new Size(35, 23);
            lblY.TabIndex = 4;
            lblY.Text = "文Y";
            // 
            // lblX
            // 
            lblX.Location = new Point(220, 27);
            lblX.Name = "lblX";
            lblX.Size = new Size(35, 23);
            lblX.TabIndex = 5;
            lblX.Text = "文X";
            // 
            // lblR
            // 
            lblR.Location = new Point(115, 57);
            lblR.Name = "lblR";
            lblR.Size = new Size(30, 23);
            lblR.TabIndex = 6;
            lblR.Text = "右";
            // 
            // lblLW
            // 
            lblLW.Location = new Point(115, 92);
            lblLW.Name = "lblLW";
            lblLW.Size = new Size(35, 23);
            lblLW.TabIndex = 7;
            lblLW.Text = "線寬";
            // 
            // lblL
            // 
            lblL.Location = new Point(115, 27);
            lblL.Name = "lblL";
            lblL.Size = new Size(30, 23);
            lblL.TabIndex = 8;
            lblL.Text = "左";
            // 
            // chkAddText
            // 
            chkAddText.AutoSize = true;
            chkAddText.Checked = true;
            chkAddText.CheckState = CheckState.Checked;
            chkAddText.Location = new Point(260, 121);
            chkAddText.Name = "chkAddText";
            chkAddText.Size = new Size(74, 19);
            chkAddText.TabIndex = 18;
            chkAddText.Text = "是否加字";
            // 
            // lblB
            // 
            lblB.Location = new Point(10, 57);
            lblB.Name = "lblB";
            lblB.Size = new Size(30, 23);
            lblB.TabIndex = 9;
            lblB.Text = "下";
            // 
            // lblT
            // 
            lblT.Location = new Point(10, 27);
            lblT.Name = "lblT";
            lblT.Size = new Size(30, 23);
            lblT.TabIndex = 10;
            lblT.Text = "上";
            // 
            // numTextY
            // 
            numTextY.Location = new Point(260, 55);
            numTextY.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
            numTextY.Name = "numTextY";
            numTextY.Size = new Size(75, 23);
            numTextY.TabIndex = 11;
            // 
            // numTextX
            // 
            numTextX.Location = new Point(260, 25);
            numTextX.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
            numTextX.Name = "numTextX";
            numTextX.Size = new Size(75, 23);
            numTextX.TabIndex = 12;
            // 
            // numPadRight
            // 
            numPadRight.DecimalPlaces = 1;
            numPadRight.Location = new Point(150, 55);
            numPadRight.Name = "numPadRight";
            numPadRight.Size = new Size(60, 23);
            numPadRight.TabIndex = 13;
            // 
            // numPadLeft
            // 
            numPadLeft.DecimalPlaces = 1;
            numPadLeft.Location = new Point(150, 25);
            numPadLeft.Name = "numPadLeft";
            numPadLeft.Size = new Size(60, 23);
            numPadLeft.TabIndex = 14;
            // 
            // numPadBottom
            // 
            numPadBottom.DecimalPlaces = 1;
            numPadBottom.Location = new Point(45, 55);
            numPadBottom.Name = "numPadBottom";
            numPadBottom.Size = new Size(60, 23);
            numPadBottom.TabIndex = 15;
            // 
            // numPadTop
            // 
            numPadTop.DecimalPlaces = 1;
            numPadTop.Location = new Point(45, 25);
            numPadTop.Name = "numPadTop";
            numPadTop.Size = new Size(60, 23);
            numPadTop.TabIndex = 16;
            // 
            // numLineWidth
            // 
            numLineWidth.DecimalPlaces = 1;
            numLineWidth.Location = new Point(150, 90);
            numLineWidth.Name = "numLineWidth";
            numLineWidth.Size = new Size(60, 23);
            numLineWidth.TabIndex = 17;
            // 
            // grpColor
            // 
            grpColor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            grpColor.Controls.Add(rdoWhite);
            grpColor.Controls.Add(rdoGray);
            grpColor.Controls.Add(rdoBlack);
            grpColor.Location = new Point(380, 100);
            grpColor.Name = "grpColor";
            grpColor.Size = new Size(100, 150);
            grpColor.TabIndex = 5;
            grpColor.TabStop = false;
            grpColor.Text = "標記顏色";
            // 
            // grptxtColor
            // 
            grptxtColor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            grptxtColor.Controls.Add(rdotxtWhite);
            grptxtColor.Controls.Add(rdotxtGray);
            grptxtColor.Controls.Add(rdotxtBlack);
            grptxtColor.Location = new Point(480, 100);
            grptxtColor.Name = "grpColor";
            grptxtColor.Size = new Size(100, 150);
            grptxtColor.TabIndex = 5;
            grptxtColor.TabStop = false;
            grptxtColor.Text = "文字顏色";
            // 
            // rdoWhite
            // 
            rdoWhite.Location = new Point(20, 105);
            rdoWhite.Name = "rdoWhite";
            rdoWhite.Size = new Size(54, 24);
            rdoWhite.TabIndex = 0;
            rdoWhite.Text = "白色";
            // 
            // rdoGray
            // 
            rdoGray.Checked = true;
            rdoGray.Location = new Point(20, 35);
            rdoGray.Name = "rdoGray";
            rdoGray.Size = new Size(54, 24);
            rdoGray.TabIndex = 1;
            rdoGray.TabStop = true;
            rdoGray.Text = "灰色";
            // 
            // rdoBlack
            // 
            rdoBlack.Location = new Point(20, 70);
            rdoBlack.Name = "rdoBlack";
            rdoBlack.Size = new Size(54, 24);
            rdoBlack.TabIndex = 2;
            rdoBlack.Text = "黑色";

            // 
            // rdotxtWhite
            // 
            rdotxtWhite.Location = new Point(20, 105);
            rdotxtWhite.Name = "rdoWhite";
            rdotxtWhite.Size = new Size(54, 24);
            rdotxtWhite.TabIndex = 0;
            rdotxtWhite.Text = "白色";
            // 
            // rdotxtGray
            // 
            rdotxtGray.Checked = true;
            rdotxtGray.Location = new Point(20, 35);
            rdotxtGray.Name = "rdoGray";
            rdotxtGray.Size = new Size(54, 24);
            rdotxtGray.TabIndex = 1;
            rdotxtGray.TabStop = true;
            rdotxtGray.Text = "灰色";
            // 
            // rdotxtBlack
            // 
            rdotxtBlack.Location = new Point(20, 70);
            rdotxtBlack.Name = "rdoBlack";
            rdotxtBlack.Size = new Size(54, 24);
            rdotxtBlack.TabIndex = 2;
            rdotxtBlack.Text = "黑色";
            // 
            // btnStart
            // 
            btnStart.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnStart.BackColor = Color.LightSteelBlue;
            btnStart.Location = new Point(20, 260);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(565, 40);
            btnStart.TabIndex = 4;
            btnStart.Text = "開始批次生成";
            btnStart.UseVisualStyleBackColor = false;
            // 
            // progressBar1
            // 
            progressBar1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar1.Location = new Point(20, 310);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(565, 23);
            progressBar1.TabIndex = 3;
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.BackColor = Color.Black;
            txtLog.ForeColor = Color.Lime;
            txtLog.Location = new Point(20, 345);
            txtLog.Name = "txtLog";
            txtLog.Size = new Size(565, 90);
            txtLog.TabIndex = 2;
            txtLog.Text = "";
            // 
            // lblS
            // 
            lblS.Location = new Point(20, 23);
            lblS.Name = "lblS";
            lblS.Size = new Size(74, 23);
            lblS.TabIndex = 1;
            lblS.Text = "來源目錄:";
            // 
            // lblO
            // 
            lblO.Location = new Point(20, 58);
            lblO.Name = "lblO";
            lblO.Size = new Size(74, 23);
            lblO.TabIndex = 0;
            lblO.Text = "目的目錄:";
            // 
            // Form1
            // 
            ClientSize = new Size(610, 455);
            Controls.Add(lblO);
            Controls.Add(lblS);
            Controls.Add(txtLog);
            Controls.Add(progressBar1);
            Controls.Add(btnStart);
            Controls.Add(grpColor);
            Controls.Add(grptxtColor);
            Controls.Add(grpPadding);
            Controls.Add(btnBrowseOutput);
            Controls.Add(txtOutputDir);
            Controls.Add(btnBrowseSource);
            Controls.Add(txtSourceDir);
            MinimumSize = new Size(620, 480);
            Name = "Form1";
            Text = "創衣 PrintFlow AutoMarker - 完整版";
            grpPadding.ResumeLayout(false);
            grpPadding.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numSafeGap).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFontSize).EndInit();
            ((System.ComponentModel.ISupportInitialize)numTextY).EndInit();
            ((System.ComponentModel.ISupportInitialize)numTextX).EndInit();
            ((System.ComponentModel.ISupportInitialize)numPadRight).EndInit();
            ((System.ComponentModel.ISupportInitialize)numPadLeft).EndInit();
            ((System.ComponentModel.ISupportInitialize)numPadBottom).EndInit();
            ((System.ComponentModel.ISupportInitialize)numPadTop).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLineWidth).EndInit();
            grpColor.ResumeLayout(false);
            grptxtColor.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // ... 控制項定義保持不變 ...
        private System.Windows.Forms.TextBox txtSourceDir;
        private System.Windows.Forms.Button btnBrowseSource;
        private System.Windows.Forms.TextBox txtOutputDir;
        private System.Windows.Forms.Button btnBrowseOutput;
        private System.Windows.Forms.GroupBox grpPadding;
        private System.Windows.Forms.NumericUpDown numPadTop, numPadBottom, numPadLeft, numPadRight, numLineWidth, numTextX, numTextY, numFontSize, numSafeGap;
        private System.Windows.Forms.GroupBox grpColor;
        private System.Windows.Forms.GroupBox grptxtColor;
        private System.Windows.Forms.RadioButton rdoWhite, rdoGray, rdoBlack;
        private System.Windows.Forms.RadioButton rdotxtWhite, rdotxtGray, rdotxtBlack;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Label lblS, lblO, lblT, lblB, lblL, lblR, lblLW, lblX, lblY, lblSize, lblGap;
        private System.Windows.Forms.CheckBox chkAddText;
        private ComboBox comboBox1;
        private Label label1;
    }
}
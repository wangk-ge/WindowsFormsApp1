namespace WindowsFormsApp1
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.cmdTextBox = new System.Windows.Forms.TextBox();
            this.sendCmdButton = new System.Windows.Forms.Button();
            this.cmdRespTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();
            // 
            // chart1
            // 
            chartArea2.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea2);
            legend2.Name = "Legend1";
            this.chart1.Legends.Add(legend2);
            this.chart1.Location = new System.Drawing.Point(12, 67);
            this.chart1.Name = "chart1";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.chart1.Series.Add(series2);
            this.chart1.Size = new System.Drawing.Size(776, 300);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "chart1";
            // 
            // cmdTextBox
            // 
            this.cmdTextBox.Location = new System.Drawing.Point(53, 373);
            this.cmdTextBox.Name = "cmdTextBox";
            this.cmdTextBox.Size = new System.Drawing.Size(656, 21);
            this.cmdTextBox.TabIndex = 1;
            // 
            // sendCmdButton
            // 
            this.sendCmdButton.Location = new System.Drawing.Point(713, 371);
            this.sendCmdButton.Name = "sendCmdButton";
            this.sendCmdButton.Size = new System.Drawing.Size(75, 23);
            this.sendCmdButton.TabIndex = 2;
            this.sendCmdButton.Text = "发送";
            this.sendCmdButton.UseVisualStyleBackColor = true;
            this.sendCmdButton.Click += new System.EventHandler(this.sendCmdButton_Click);
            // 
            // cmdRespTextBox
            // 
            this.cmdRespTextBox.Location = new System.Drawing.Point(12, 400);
            this.cmdRespTextBox.Multiline = true;
            this.cmdRespTextBox.Name = "cmdRespTextBox";
            this.cmdRespTextBox.Size = new System.Drawing.Size(776, 97);
            this.cmdRespTextBox.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 376);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "命令：";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 509);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.sendCmdButton);
            this.Controls.Add(this.cmdRespTextBox);
            this.Controls.Add(this.cmdTextBox);
            this.Controls.Add(this.chart1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.TextBox cmdTextBox;
        private System.Windows.Forms.Button sendCmdButton;
        private System.Windows.Forms.TextBox cmdRespTextBox;
        private System.Windows.Forms.Label label1;
    }
}


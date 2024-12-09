namespace GenshinFilePlayer
{
    partial class LoadingWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            progressBar = new ProgressBar();
            label = new Label();
            SuspendLayout();
            // 
            // progressBar
            // 
            progressBar.Location = new Point(12, 12);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(402, 23);
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.TabIndex = 0;
            // 
            // label
            // 
            label.Location = new Point(12, 43);
            label.Name = "label";
            label.Size = new Size(402, 15);
            label.TabIndex = 1;
            label.Text = "Loading...";
            label.TextAlign = ContentAlignment.TopCenter;
            // 
            // LoadingWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(426, 70);
            Controls.Add(label);
            Controls.Add(progressBar);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "LoadingWindow";
            ShowInTaskbar = false;
            Text = "Loading...";
            TopMost = true;
            FormClosing += OnFormClosing;
            ResumeLayout(false);
        }

        #endregion

        public ProgressBar progressBar;
        public Label label;
    }
}
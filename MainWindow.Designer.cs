namespace GenshinFilePlayer
{
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
            treeView = new TreeView();
            panelHandle = new Panel();
            goToFileButton = new Button();
            button1 = new Button();
            pauseButton = new Button();
            songData = new TextBox();
            songPosition = new TrackBar();
            playButton = new Button();
            exportAllButton = new Button();
            loadFolderButton = new Button();
            loadFileButton = new Button();
            button2 = new Button();
            logTextBox = new TextBox();
            clearAllButton = new Button();
            panelHandle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)songPosition).BeginInit();
            SuspendLayout();
            // 
            // treeView
            // 
            treeView.Location = new Point(7, 8);
            treeView.Name = "treeView";
            treeView.Size = new Size(481, 430);
            treeView.TabIndex = 0;
            treeView.AfterSelect += SelectTreeView;
            treeView.MouseDoubleClick += MouseDoubleClickTreeView;
            // 
            // panelHandle
            // 
            panelHandle.Controls.Add(goToFileButton);
            panelHandle.Controls.Add(button1);
            panelHandle.Controls.Add(pauseButton);
            panelHandle.Controls.Add(songData);
            panelHandle.Controls.Add(songPosition);
            panelHandle.Controls.Add(playButton);
            panelHandle.Enabled = false;
            panelHandle.Location = new Point(494, 30);
            panelHandle.Name = "panelHandle";
            panelHandle.Size = new Size(410, 408);
            panelHandle.TabIndex = 2;
            // 
            // goToFileButton
            // 
            goToFileButton.Location = new Point(246, 4);
            goToFileButton.Name = "goToFileButton";
            goToFileButton.Size = new Size(75, 23);
            goToFileButton.TabIndex = 5;
            goToFileButton.Text = "Go to file";
            goToFileButton.UseVisualStyleBackColor = true;
            goToFileButton.Click += OnGoToFileClick;
            // 
            // button1
            // 
            button1.Location = new Point(165, 4);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 4;
            button1.Text = "Export";
            button1.UseVisualStyleBackColor = true;
            button1.Click += OnExportClick;
            // 
            // pauseButton
            // 
            pauseButton.Location = new Point(84, 4);
            pauseButton.Name = "pauseButton";
            pauseButton.Size = new Size(75, 23);
            pauseButton.TabIndex = 3;
            pauseButton.Text = "Pause";
            pauseButton.UseVisualStyleBackColor = true;
            pauseButton.Click += OnPauseClick;
            // 
            // songData
            // 
            songData.Location = new Point(10, 68);
            songData.Multiline = true;
            songData.Name = "songData";
            songData.ReadOnly = true;
            songData.RightToLeft = RightToLeft.No;
            songData.ScrollBars = ScrollBars.Horizontal;
            songData.Size = new Size(392, 84);
            songData.TabIndex = 2;
            songData.Text = "Name: --:-- / --:--\r\nSHA512: --------------------------------------------------------------------------------------------------------------------------------";
            songData.TextAlign = HorizontalAlignment.Center;
            // 
            // songPosition
            // 
            songPosition.AutoSize = false;
            songPosition.Location = new Point(3, 33);
            songPosition.Name = "songPosition";
            songPosition.Size = new Size(399, 29);
            songPosition.TabIndex = 1;
            songPosition.TickStyle = TickStyle.None;
            songPosition.ValueChanged += SongPositionOnValueChange;
            // 
            // playButton
            // 
            playButton.Location = new Point(3, 4);
            playButton.Name = "playButton";
            playButton.Size = new Size(75, 23);
            playButton.TabIndex = 0;
            playButton.Text = "Play";
            playButton.UseVisualStyleBackColor = true;
            playButton.Click += OnPlayClick;
            // 
            // exportAllButton
            // 
            exportAllButton.Location = new Point(497, 7);
            exportAllButton.Name = "exportAllButton";
            exportAllButton.Size = new Size(75, 23);
            exportAllButton.TabIndex = 6;
            exportAllButton.Text = "Export All";
            exportAllButton.UseVisualStyleBackColor = true;
            exportAllButton.Click += OnExportAllClick;
            // 
            // loadFolderButton
            // 
            loadFolderButton.Location = new Point(578, 7);
            loadFolderButton.Name = "loadFolderButton";
            loadFolderButton.Size = new Size(81, 23);
            loadFolderButton.TabIndex = 7;
            loadFolderButton.Text = "Load Folder";
            loadFolderButton.UseVisualStyleBackColor = true;
            loadFolderButton.Click += OnLoadFolderClick;
            // 
            // loadFileButton
            // 
            loadFileButton.Location = new Point(665, 7);
            loadFileButton.Name = "loadFileButton";
            loadFileButton.Size = new Size(75, 23);
            loadFileButton.TabIndex = 8;
            loadFileButton.Text = "Load File";
            loadFileButton.UseVisualStyleBackColor = true;
            loadFileButton.Click += OnLoadFileClick;
            // 
            // button2
            // 
            button2.Location = new Point(746, 7);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 9;
            button2.Text = "Find";
            button2.UseVisualStyleBackColor = true;
            button2.Click += OnFindClick;
            // 
            // logTextBox
            // 
            logTextBox.Location = new Point(7, 444);
            logTextBox.MaxLength = 99999999;
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = ScrollBars.Horizontal;
            logTextBox.Size = new Size(897, 144);
            logTextBox.TabIndex = 10;
            logTextBox.WordWrap = false;
            // 
            // clearAllButton
            // 
            clearAllButton.Location = new Point(827, 7);
            clearAllButton.Name = "clearAllButton";
            clearAllButton.Size = new Size(75, 23);
            clearAllButton.TabIndex = 11;
            clearAllButton.Text = "Clear All";
            clearAllButton.UseVisualStyleBackColor = true;
            clearAllButton.Click += OnClearAllClick;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(912, 600);
            Controls.Add(clearAllButton);
            Controls.Add(logTextBox);
            Controls.Add(button2);
            Controls.Add(loadFileButton);
            Controls.Add(loadFolderButton);
            Controls.Add(exportAllButton);
            Controls.Add(panelHandle);
            Controls.Add(treeView);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "MainWindow";
            Text = "GenshinFilePlayer";
            Load += OnWindowLoad;
            panelHandle.ResumeLayout(false);
            panelHandle.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)songPosition).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Panel panelHandle;
        private Button playButton;
        private TextBox songData;
        private TrackBar songPosition;
        private Button button1;
        private Button pauseButton;
        private System.Windows.Forms.Button goToFileButton;
        private System.Windows.Forms.Button exportAllButton;
        private Button loadFolderButton;
        private Button loadFileButton;
        private Button button2;
        public TextBox logTextBox;
        private Button clearAllButton;
        public TreeView treeView;
    }
}

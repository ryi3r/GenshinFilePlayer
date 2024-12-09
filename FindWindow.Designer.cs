namespace GenshinFilePlayer
{
    partial class FindWindow
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
            findOnList = new CheckedListBox();
            optionLabel = new Label();
            whatToFindLabel = new Label();
            whatToFindBox = new TextBox();
            findButton = new Button();
            treeView = new TreeView();
            SuspendLayout();
            // 
            // findOnList
            // 
            findOnList.FormattingEnabled = true;
            findOnList.Items.AddRange(new object[] { "Case Sensitive", "Name", "SHA512 (Hash)" });
            findOnList.Location = new Point(12, 27);
            findOnList.Name = "findOnList";
            findOnList.Size = new Size(120, 58);
            findOnList.TabIndex = 0;
            // 
            // optionLabel
            // 
            optionLabel.AutoSize = true;
            optionLabel.Location = new Point(12, 9);
            optionLabel.Name = "optionLabel";
            optionLabel.Size = new Size(73, 15);
            optionLabel.TabIndex = 1;
            optionLabel.Text = "How to find:";
            // 
            // whatToFindLabel
            // 
            whatToFindLabel.AutoSize = true;
            whatToFindLabel.Location = new Point(12, 88);
            whatToFindLabel.Name = "whatToFindLabel";
            whatToFindLabel.Size = new Size(76, 15);
            whatToFindLabel.TabIndex = 2;
            whatToFindLabel.Text = "What to find:";
            // 
            // whatToFindBox
            // 
            whatToFindBox.Location = new Point(12, 106);
            whatToFindBox.Name = "whatToFindBox";
            whatToFindBox.ScrollBars = ScrollBars.Horizontal;
            whatToFindBox.Size = new Size(120, 23);
            whatToFindBox.TabIndex = 3;
            // 
            // findButton
            // 
            findButton.Location = new Point(57, 135);
            findButton.Name = "findButton";
            findButton.Size = new Size(75, 23);
            findButton.TabIndex = 4;
            findButton.Text = "Find";
            findButton.UseVisualStyleBackColor = true;
            findButton.Click += OnFindClick;
            // 
            // treeView
            // 
            treeView.Location = new Point(138, 12);
            treeView.Name = "treeView";
            treeView.Size = new Size(414, 426);
            treeView.TabIndex = 5;
            treeView.DoubleClick += this.OnTreeViewDoubleClick;
            // 
            // FindWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(564, 450);
            Controls.Add(treeView);
            Controls.Add(findButton);
            Controls.Add(whatToFindBox);
            Controls.Add(whatToFindLabel);
            Controls.Add(optionLabel);
            Controls.Add(findOnList);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "FindWindow";
            Text = "Find";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private CheckedListBox findOnList;
        private Label optionLabel;
        private Label whatToFindLabel;
        private TextBox whatToFindBox;
        private Button findButton;
        private TreeView treeView;
    }
}
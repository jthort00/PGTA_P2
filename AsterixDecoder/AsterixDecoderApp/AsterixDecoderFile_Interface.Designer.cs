namespace AsterixDecoderApp
{
    partial class AsterixDecoderFile_Interface
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
            drop_panel = new Panel();
            SuspendLayout();
            // 
            // drop_panel
            // 
            drop_panel.AllowDrop = true;
            drop_panel.Location = new Point(12, 12);
            drop_panel.Name = "drop_panel";
            drop_panel.Size = new Size(776, 426);
            drop_panel.TabIndex = 0;
            // 
            // AsterixDecoderFile_Interface
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(drop_panel);
            Name = "AsterixDecoderFile_Interface";
            Text = "AsterixDecoderFile_Interface";
            Load += AsterixDecoderFile_Interface_Load;
            ResumeLayout(false);
        }

        #endregion

        private Panel drop_panel;
    }
}
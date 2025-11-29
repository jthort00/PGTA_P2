namespace AsterixDecoderApp
{
    partial class AsterixDecoderTable_Interface
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AsterixDecoderTable_Interface));
            cat_dataGridView = new DataGridView();
            csv_button = new Button();
            close_button = new Button();
            CAT021_checkbox = new CheckBox();
            CAT048_checkbox = new CheckBox();
            combined_checkBox = new CheckBox();
            label1 = new Label();
            label2 = new Label();
            OnGround_checkBox = new CheckBox();
            transponderfijo_checkBox = new CheckBox();
            Purewhite_checkBox = new CheckBox();
            simulation_button = new Button();
            ((System.ComponentModel.ISupportInitialize)cat_dataGridView).BeginInit();
            SuspendLayout();
            // 
            // cat_dataGridView
            // 
            cat_dataGridView.AllowUserToAddRows = false;
            cat_dataGridView.AllowUserToDeleteRows = false;
            cat_dataGridView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            cat_dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            cat_dataGridView.Location = new Point(251, 0);
            cat_dataGridView.Name = "cat_dataGridView";
            cat_dataGridView.ReadOnly = true;
            cat_dataGridView.Size = new Size(1421, 669);
            cat_dataGridView.TabIndex = 0;
            // 
            // csv_button
            // 
            csv_button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            csv_button.Location = new Point(128, 615);
            csv_button.Name = "csv_button";
            csv_button.Size = new Size(110, 35);
            csv_button.TabIndex = 2;
            csv_button.Text = "Descargar CSV";
            csv_button.UseVisualStyleBackColor = true;
            csv_button.Click += csv_button_Click;
            // 
            // close_button
            // 
            close_button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            close_button.Location = new Point(12, 615);
            close_button.Name = "close_button";
            close_button.Size = new Size(110, 35);
            close_button.TabIndex = 9;
            close_button.Text = "Close";
            close_button.UseVisualStyleBackColor = true;
            close_button.Click += close_button_Click;
            // 
            // CAT021_checkbox
            // 
            CAT021_checkbox.AutoSize = true;
            CAT021_checkbox.Location = new Point(27, 36);
            CAT021_checkbox.Name = "CAT021_checkbox";
            CAT021_checkbox.Size = new Size(66, 19);
            CAT021_checkbox.TabIndex = 10;
            CAT021_checkbox.Text = "CAT021";
            CAT021_checkbox.UseVisualStyleBackColor = true;
            CAT021_checkbox.CheckedChanged += CAT021_checkbox_CheckedChanged;
            // 
            // CAT048_checkbox
            // 
            CAT048_checkbox.AutoSize = true;
            CAT048_checkbox.Location = new Point(27, 61);
            CAT048_checkbox.Name = "CAT048_checkbox";
            CAT048_checkbox.Size = new Size(66, 19);
            CAT048_checkbox.TabIndex = 11;
            CAT048_checkbox.Text = "CAT048";
            CAT048_checkbox.UseVisualStyleBackColor = true;
            CAT048_checkbox.CheckedChanged += CAT048_checkbox_CheckedChanged;
            // 
            // combined_checkBox
            // 
            combined_checkBox.AutoSize = true;
            combined_checkBox.Location = new Point(27, 86);
            combined_checkBox.Name = "combined_checkBox";
            combined_checkBox.Size = new Size(120, 19);
            combined_checkBox.TabIndex = 12;
            combined_checkBox.Text = "CAT021 + CAT048";
            combined_checkBox.UseVisualStyleBackColor = true;
            combined_checkBox.CheckedChanged += combined_checkBox_CheckedChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(101, 15);
            label1.TabIndex = 13;
            label1.Text = "Categorias Asterix";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 143);
            label2.Name = "label2";
            label2.Size = new Size(39, 15);
            label2.TabIndex = 14;
            label2.Text = "Filtros";
            // 
            // OnGround_checkBox
            // 
            OnGround_checkBox.AutoSize = true;
            OnGround_checkBox.Location = new Point(27, 225);
            OnGround_checkBox.Name = "OnGround_checkBox";
            OnGround_checkBox.Size = new Size(131, 19);
            OnGround_checkBox.TabIndex = 17;
            OnGround_checkBox.Text = "Eliminar On Ground";
            OnGround_checkBox.UseVisualStyleBackColor = true;
            OnGround_checkBox.CheckedChanged += OnGround_checkBox_CheckedChanged;
            // 
            // transponderfijo_checkBox
            // 
            transponderfijo_checkBox.AutoSize = true;
            transponderfijo_checkBox.Location = new Point(27, 200);
            transponderfijo_checkBox.Name = "transponderfijo_checkBox";
            transponderfijo_checkBox.Size = new Size(156, 19);
            transponderfijo_checkBox.TabIndex = 16;
            transponderfijo_checkBox.Text = "Eliminar transponder fijo";
            transponderfijo_checkBox.UseVisualStyleBackColor = true;
            transponderfijo_checkBox.CheckedChanged += transponderfijo_checkBox_CheckedChanged;
            // 
            // Purewhite_checkBox
            // 
            Purewhite_checkBox.AutoSize = true;
            Purewhite_checkBox.Location = new Point(27, 175);
            Purewhite_checkBox.Name = "Purewhite_checkBox";
            Purewhite_checkBox.Size = new Size(146, 19);
            Purewhite_checkBox.TabIndex = 15;
            Purewhite_checkBox.Text = "Eliminar blancos puros";
            Purewhite_checkBox.UseVisualStyleBackColor = true;
            Purewhite_checkBox.CheckedChanged += Purewhite_checkBox_CheckedChanged;
            // 
            // simulation_button
            // 
            simulation_button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            simulation_button.Location = new Point(12, 574);
            simulation_button.Name = "simulation_button";
            simulation_button.Size = new Size(226, 35);
            simulation_button.TabIndex = 25;
            simulation_button.Text = "Abrir simulación";
            simulation_button.UseVisualStyleBackColor = true;
            simulation_button.Click += simulation_button_Click;
            // 
            // AsterixDecoderTable_Interface
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1671, 669);
            Controls.Add(simulation_button);
            Controls.Add(OnGround_checkBox);
            Controls.Add(transponderfijo_checkBox);
            Controls.Add(Purewhite_checkBox);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(combined_checkBox);
            Controls.Add(CAT048_checkbox);
            Controls.Add(CAT021_checkbox);
            Controls.Add(close_button);
            Controls.Add(csv_button);
            Controls.Add(cat_dataGridView);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "AsterixDecoderTable_Interface";
            Text = "Asterix Decoder Table";
            Load += AsterixDecoderTable_Interface_Load;
            ((System.ComponentModel.ISupportInitialize)cat_dataGridView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView cat_dataGridView;
        private Button csv_button;
        private Button close_button;
        private CheckBox CAT021_checkbox;
        private CheckBox CAT048_checkbox;
        private CheckBox combined_checkBox;
        private Label label1;
        private Label label2;
        private CheckBox OnGround_checkBox;
        private CheckBox transponderfijo_checkBox;
        private CheckBox Purewhite_checkBox;
        private Button simulation_button;
    }
}
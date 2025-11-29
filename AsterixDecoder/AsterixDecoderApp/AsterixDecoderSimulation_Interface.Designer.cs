namespace AsterixDecoderApp
{
    partial class AsterixDecoderSimulation_Interface
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AsterixDecoderSimulation_Interface));
            map_panel = new Panel();
            avanzar_button = new Button();
            retroceder_button = new Button();
            auto_button = new Button();
            reset_button = new Button();
            Filter_button = new Button();
            csv_button = new Button();
            close_button = new Button();
            speed_trackBar = new TrackBar();
            ((System.ComponentModel.ISupportInitialize)speed_trackBar).BeginInit();
            SuspendLayout();
            // 
            // map_panel
            // 
            map_panel.Location = new Point(180, 0);
            map_panel.Name = "map_panel";
            map_panel.Size = new Size(1090, 695);
            map_panel.TabIndex = 0;
            map_panel.Paint += map_panel_Paint;
            // 
            // avanzar_button
            // 
            avanzar_button.Location = new Point(30, 27);
            avanzar_button.Name = "avanzar_button";
            avanzar_button.Size = new Size(110, 35);
            avanzar_button.TabIndex = 1;
            avanzar_button.Text = "Avanzar";
            avanzar_button.UseVisualStyleBackColor = true;
            avanzar_button.Click += avanzar_button_Click;
            // 
            // retroceder_button
            // 
            retroceder_button.Location = new Point(30, 68);
            retroceder_button.Name = "retroceder_button";
            retroceder_button.Size = new Size(110, 35);
            retroceder_button.TabIndex = 2;
            retroceder_button.Text = "Retroceder";
            retroceder_button.UseVisualStyleBackColor = true;
            retroceder_button.Click += retroceder_button_Click;
            // 
            // auto_button
            // 
            auto_button.Location = new Point(30, 109);
            auto_button.Name = "auto_button";
            auto_button.Size = new Size(110, 35);
            auto_button.TabIndex = 4;
            auto_button.Text = "Auto";
            auto_button.UseVisualStyleBackColor = true;
            auto_button.Click += auto_button_Click;
            // 
            // reset_button
            // 
            reset_button.Location = new Point(30, 150);
            reset_button.Name = "reset_button";
            reset_button.Size = new Size(110, 35);
            reset_button.TabIndex = 5;
            reset_button.Text = "Reset";
            reset_button.UseVisualStyleBackColor = true;
            reset_button.Click += reset_button_Click;
            // 
            // Filter_button
            // 
            Filter_button.Location = new Point(30, 566);
            Filter_button.Name = "Filter_button";
            Filter_button.Size = new Size(110, 35);
            Filter_button.TabIndex = 6;
            Filter_button.Text = "Filtrar datos";
            Filter_button.UseVisualStyleBackColor = true;
            Filter_button.Click += Filter_button_Click;
            // 
            // csv_button
            // 
            csv_button.Location = new Point(30, 607);
            csv_button.Name = "csv_button";
            csv_button.Size = new Size(110, 35);
            csv_button.TabIndex = 7;
            csv_button.Text = "Descargar CSV";
            csv_button.UseVisualStyleBackColor = true;
            csv_button.Click += csv_button_Click;
            // 
            // close_button
            // 
            close_button.Location = new Point(30, 648);
            close_button.Name = "close_button";
            close_button.Size = new Size(110, 35);
            close_button.TabIndex = 8;
            close_button.Text = "Cerrar";
            close_button.UseVisualStyleBackColor = true;
            close_button.Click += close_button_Click;
            // 
            // speed_trackBar
            // 
            speed_trackBar.BackColor = Color.Black;
            speed_trackBar.Location = new Point(30, 199);
            speed_trackBar.Name = "speed_trackBar";
            speed_trackBar.Size = new Size(110, 45);
            speed_trackBar.TabIndex = 9;
            speed_trackBar.TickStyle = TickStyle.Both;
            speed_trackBar.Scroll += speed_trackBar_Scroll;
            // 
            // AsterixDecoderSimulation_Interface
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1270, 695);
            Controls.Add(speed_trackBar);
            Controls.Add(close_button);
            Controls.Add(csv_button);
            Controls.Add(Filter_button);
            Controls.Add(reset_button);
            Controls.Add(auto_button);
            Controls.Add(retroceder_button);
            Controls.Add(avanzar_button);
            Controls.Add(map_panel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "AsterixDecoderSimulation_Interface";
            Text = "Asterix Decoder Simulation";
            Load += AsterixDecoderSimulation_Interface_Load;
            ((System.ComponentModel.ISupportInitialize)speed_trackBar).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel map_panel;
        private Button avanzar_button;
        private Button retroceder_button;
        private Button auto_button;
        private Button reset_button;
        private Button Filter_button;
        private Button csv_button;
        private Button close_button;
        private TrackBar speed_trackBar;
    }
}
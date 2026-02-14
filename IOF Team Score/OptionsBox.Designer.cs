using IOF_Team_Score.Model;

namespace IOF_Team_Score
{
    partial class OptionsBox
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
            ExportCSS = new CheckBox();
            OK = new Button();
            Cancel = new Button();
            EventTypeComboBox = new ComboBox();
            EventType = new GroupBox();
            EventType.SuspendLayout();
            SuspendLayout();
            // 
            // ExportCSS
            // 
            ExportCSS.AutoSize = true;
            ExportCSS.Location = new Point(12, 85);
            ExportCSS.Name = "ExportCSS";
            ExportCSS.Size = new Size(168, 19);
            ExportCSS.TabIndex = 0;
            ExportCSS.Text = "Export CSS with report files";
            ExportCSS.UseVisualStyleBackColor = true;
            // 
            // OK
            // 
            OK.Location = new Point(198, 119);
            OK.Name = "OK";
            OK.Size = new Size(75, 26);
            OK.TabIndex = 1;
            OK.Text = "OK";
            OK.UseVisualStyleBackColor = true;
            OK.Click += OK_Click;
            // 
            // Cancel
            // 
            Cancel.Location = new Point(287, 119);
            Cancel.Name = "Cancel";
            Cancel.Size = new Size(75, 26);
            Cancel.TabIndex = 2;
            Cancel.Text = "Cancel";
            Cancel.UseVisualStyleBackColor = true;
            Cancel.Click += Cancel_Click;
            // 
            // EventTypeComboBox
            // 
            EventTypeComboBox.DisplayMember = "EventType";
            EventTypeComboBox.FormattingEnabled = true;
            EventTypeComboBox.Items.AddRange(new object[] { "European Youth Orienteering Championships", "Junior World Orienteering Championships" });
            EventTypeComboBox.Location = new Point(15, 22);
            EventTypeComboBox.Name = "EventTypeComboBox";
            EventTypeComboBox.Size = new Size(321, 23);
            EventTypeComboBox.TabIndex = 3;
            EventTypeComboBox.ValueMember = "EventType";
            // 
            // EventType
            // 
            EventType.Controls.Add(EventTypeComboBox);
            EventType.Location = new Point(12, 14);
            EventType.Name = "EventType";
            EventType.Size = new Size(350, 65);
            EventType.TabIndex = 4;
            EventType.TabStop = false;
            EventType.Text = "Event Type";
            // 
            // OptionsBox
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(374, 157);
            Controls.Add(Cancel);
            Controls.Add(OK);
            Controls.Add(ExportCSS);
            Controls.Add(EventType);
            Name = "OptionsBox";
            Text = "Options";
            EventType.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private CheckBox ExportCSS;
        private Button OK;
        private Button Cancel;
        private ComboBox EventTypeComboBox;
        private GroupBox EventType;
    }
}
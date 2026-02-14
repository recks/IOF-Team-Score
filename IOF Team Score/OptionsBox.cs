using IOF_Team_Score.Services;
using IOF_Team_Score.Util;
using System.ComponentModel;

namespace IOF_Team_Score
{
    public partial class OptionsBox : Form
    {
        private Options _options;

        public OptionsBox(Options options)
        {
            _options = options;
            InitializeComponent();
            ExportCSS.Checked = _options.ExportCSS;
            EventTypeComboBox.SelectedItem = _options.EventType;
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            _options.ExportCSS = ExportCSS.Checked;
            _options.EventType = EventTypeComboBox.Text;
            OptionsService.Save(_options);
            Close();
        }

    }
}

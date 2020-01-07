using System;
using System.Windows.Forms;

namespace ExampleGUI
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private async void ButtonClient_Click(object sender, EventArgs e)
        {
            Hide();

            await using var client = new FormClient();
            client.ShowDialog(this);

            Close();
        }

        private async void ButtonServer_Click(object sender, EventArgs e)
        {
            Hide();

            await using var server = new FormServer();
            server.ShowDialog(this);

            Close();
        }
    }
}

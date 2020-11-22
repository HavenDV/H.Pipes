using System;
using System.Windows.Forms;

namespace WindowsFormsApp
{
    public partial class FormMain : Form
    {
        private const string DefaultPipeName = "named_pipe_test_server";

        public FormMain()
        {
            InitializeComponent();
        }

        private async void ButtonClient_Click(object sender, EventArgs e)
        {
            Hide();

            await using var client = new FormClient(DefaultPipeName);
            client.ShowDialog(this);

            Close();
        }

        private async void ButtonServer_Click(object sender, EventArgs e)
        {
            Hide();

            await using var server = new FormServer(DefaultPipeName);
            server.ShowDialog(this);

            Close();
        }
    }
}

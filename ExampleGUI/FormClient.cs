using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NamedPipeWrapper;

namespace ExampleGUI
{
    public partial class FormClient : Form
    {
        private readonly NamedPipeClient<string> _client = new NamedPipeClient<string>(Constants.PIPE_NAME);

        public FormClient()
        {
            InitializeComponent();
            Load += OnLoad;
        }

        private void OnLoad(object sender, EventArgs eventArgs)
        {
            _client.ServerMessage += OnServerMessage;
            _client.Disconnected += OnDisconnected;
            _client.Start();
        }

        private void OnServerMessage(NamedPipeConnection<string, string> connection, string message)
        {
            richTextBoxMessages.Invoke(new Action(delegate
                {
                    AddLine("<b>Server</b>: " + message);
                }));
        }

        private void OnDisconnected(NamedPipeConnection<string, string> connection)
        {
            richTextBoxMessages.Invoke(new Action(delegate
                {
                    AddLine("<b>Disconnected from server</b>");
                }));
        }

        private void AddLine(string html)
        {
            richTextBoxMessages.Invoke(new Action(delegate
                {
                    richTextBoxMessages.Text += Environment.NewLine + "<div>" + html + "</div>";
                }));
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxMessage.Text))
                return;

            _client.PushMessage(textBoxMessage.Text);
            textBoxMessage.Text = "";
        }
    }
}

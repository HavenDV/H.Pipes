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
    public partial class FormServer : Form
    {
        private readonly NamedPipeServer<string> _server = new NamedPipeServer<string>(Constants.PIPE_NAME);
        private readonly ISet<string> _clients = new HashSet<string>();

        public FormServer()
        {
            InitializeComponent();
            Load += OnLoad;
        }

        private void OnLoad(object sender, EventArgs eventArgs)
        {
            _server.ClientConnected += OnClientConnected;
            _server.ClientDisconnected += OnClientDisconnected;
            _server.ClientMessage += (client, message) => AddLine("<b>" + client.Name + "</b>: " + message);
            _server.Start();
        }

        private void OnClientConnected(NamedPipeConnection<string, string> connection)
        {
            _clients.Add(connection.Name);
            AddLine("<b>" + connection.Name + "</b> connected!");
            UpdateClientList();
            connection.PushMessage("Welcome!  You are now connected to the server.");
        }

        private void OnClientDisconnected(NamedPipeConnection<string, string> connection)
        {
            _clients.Remove(connection.Name);
            AddLine("<b>" + connection.Name + "</b> disconnected!");
            UpdateClientList();
        }

        private void AddLine(string html)
        {
            richTextBoxMessages.Invoke(new Action(delegate
                {
                    richTextBoxMessages.Text += Environment.NewLine + "<div>" + html + "</div>";
                }));
        }

        private void UpdateClientList()
        {
            listBoxClients.Invoke(new Action(UpdateClientListImpl));
        }

        private void UpdateClientListImpl()
        {
            listBoxClients.Items.Clear();
            foreach (var client in _clients)
            {
                listBoxClients.Items.Add(client);
            }
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxMessage.Text))
                return;

            _server.PushMessage(textBoxMessage.Text);
            textBoxMessage.Text = "";
        }
    }
}

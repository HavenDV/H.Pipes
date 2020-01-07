using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using NamedPipeWrapper;

namespace ExampleGUI
{
    public partial class FormClient : Form, IAsyncDisposable
    {
        private NamedPipeClient<string> Client { get; set; }

        public FormClient()
        {
            InitializeComponent();

            Load += OnLoad;
        }

        private async void OnLoad(object sender, EventArgs eventArgs)
        {
            Client = new NamedPipeClient<string>(Constants.PIPE_NAME);
            Client.MessageReceived += (o, args) => AddLine("MessageReceived: " + args.Message);
            Client.Disconnected += (o, args) => AddLine("Disconnected from server");
            Client.ExceptionOccurred += (o, args) => OnExceptionOccurred(args.Exception);

            try
            {
                AddLine("Client connecting...");

                await Client.ConnectAsync().ConfigureAwait(false);

                AddLine("Client is connected!");
            }
            catch (Exception exception)
            {
                OnExceptionOccurred(exception);
            }
        }

        private void OnExceptionOccurred(Exception exception)
        {
            AddLine($"Exception: {exception}");
        }

        private void AddLine(string text)
        {
            richTextBoxMessages.Invoke(new Action(delegate
            {
                richTextBoxMessages.Text += $@"{text}{Environment.NewLine}";
            }));
        }

        private async void ButtonSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxMessage.Text))
            {
                return;
            }

            try
            {
                if (!Client.IsConnected)
                {
                    AddLine("Client is not connected");
                    return;
                }

                await Client.WriteAsync(textBoxMessage.Text).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                OnExceptionOccurred(exception);
            }

            textBoxMessage.Invoke(new Action(delegate
            {
                textBoxMessage.Text = "";
            }));
        }

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync().ConfigureAwait(false);
        }
    }
}

using H.Pipes;

namespace WindowsFormsApp;

public partial class FormClient : Form, IAsyncDisposable
{
    private string PipeName { get; }
    private PipeClient<string> Client { get; }

    public FormClient(string pipeName)
    {
        PipeName = pipeName;

        InitializeComponent();

        Load += OnLoad;

        Client = new PipeClient<string>(PipeName);
        Client.MessageReceived += (o, args) => AddLine("MessageReceived: " + args.Message);
        Client.Disconnected += (o, args) => AddLine("Disconnected from server");
        Client.Connected += (o, args) => AddLine("Connected to server");
        Client.ExceptionOccurred += (o, args) => OnExceptionOccurred(args.Exception);
    }

    private async void OnLoad(object? sender, EventArgs eventArgs)
    {
        try
        {
            AddLine("Client connecting...");

            await Client.ConnectAsync().ConfigureAwait(false);
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

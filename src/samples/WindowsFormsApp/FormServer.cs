using H.Pipes;

namespace WindowsFormsApp;

public sealed partial class FormServer : Form, IAsyncDisposable
{
    private string PipeName { get; }
    private PipeServer<string> Server { get; }
    private ISet<string> Clients { get; } = new HashSet<string>();

    public FormServer(string pipeName)
    {
        PipeName = pipeName;

        InitializeComponent();

        Load += OnLoad;

        Server = new PipeServer<string>(PipeName);
        Server.ClientConnected += async (o, args) =>
        {
            Clients.Add(args.Connection.PipeName);
            UpdateClientList();

            AddLine($"{args.Connection.PipeName} connected!");

            try
            {
                await args.Connection.WriteAsync("Welcome! You are now connected to the server.").ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                OnExceptionOccurred(exception);
            }
        };
        Server.ClientDisconnected += (o, args) =>
        {
            Clients.Remove(args.Connection.PipeName);
            UpdateClientList();

            AddLine($"{args.Connection.PipeName} disconnected!");
        };
        Server.MessageReceived += (o, args) => AddLine($"{args.Connection.PipeName}: {args.Message}");
        Server.ExceptionOccurred += (o, args) => OnExceptionOccurred(args.Exception);
    }

    private async void OnLoad(object? sender, EventArgs eventArgs)
    {

        try
        {
            AddLine("Server starting...");

            await Server.StartAsync().ConfigureAwait(false);

            AddLine("Server is started!");
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

    private void UpdateClientList()
    {
        listBoxClients.Invoke(new Action(() =>
        {
            listBoxClients.Items.Clear();
            foreach (var client in Clients)
            {
                listBoxClients.Items.Add(client);
            }
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
            AddLine($"Sent to {Server.ConnectedClients.Count} clients");

            await Server.WriteAsync(textBoxMessage.Text).ConfigureAwait(false);
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
        await Server.DisposeAsync().ConfigureAwait(false);
    }
}

using System.Timers;
using EventGenerator;
using Timer = System.Timers.Timer;

// ReSharper disable UnusedMember.Global

namespace H.Pipes;

/// <summary>
/// Watches the directory "\\.\pipe\" and reports on new events
/// <![CDATA[!!! WARNING: Use it carefully, it is very slow !!!]]>
/// </summary>
[Event<string>("Created", PropertyNames = new[] { "Name" },
    Description = "When any pipe created.")]
[Event<string>("Deleted", PropertyNames = new[] { "Name" },
    Description = "When any pipe deleted.")]
[Event<Exception>("ExceptionOccurred", PropertyNames = new[] { "Exception" },
    Description = "When any exception is thrown.")]
public sealed partial class PipeWatcher : IDisposable
{
    #region Properties

    /// <summary>
    /// Returns <see langword="true"/> if <see cref="PipeWatcher"/> is active
    /// </summary>
    public bool IsStarted => Timer.Enabled;

    private Timer Timer { get; }

    private IReadOnlyCollection<string> LastPipes { get; set; } = new List<string>();

    #endregion

    #region Constructors

    /// <summary>
    /// Create new instance of watcher <br/>
    /// Default interval is <see langword="100 milliseconds"/>
    /// </summary>
    /// <param name="interval"></param>
    public PipeWatcher(TimeSpan? interval = default)
    {
        Timer = new Timer((interval ?? TimeSpan.FromMilliseconds(100)).TotalMilliseconds);
        Timer.Elapsed += OnElapsed;
    }

    #endregion

    #region Event Handlers

    private void OnElapsed(object? sender, ElapsedEventArgs args)
    {
        try
        {
            var pipes = GetActivePipes();

            foreach (var name in pipes.Except(LastPipes))
            {
                _ = OnCreated(name);
            }
            foreach (var name in LastPipes.Except(pipes))
            {
                _ = OnDeleted(name);
            }

            LastPipes = pipes;
        }
        catch (Exception exception)
        {
            _ = OnExceptionOccurred(exception);
        }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Starts watching
    /// </summary>
    public void Start()
    {
        LastPipes = GetActivePipes();
        Timer.Start();
    }

    /// <summary>
    /// Stops watching(without disposing) <br/>
    /// You can call <see cref="Start()"/> again if it is required
    /// </summary>
    public void Stop()
    {
        Timer.Stop();
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Dispose internal <see cref="Timer"/>
    /// </summary>
    public void Dispose()
    {
        Timer.Dispose();
    }

    #endregion

    #region Static methods

    /// <summary>
    /// Checks if a given pipe name exists
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool IsExists(string name)
    {
        return GetActivePipes().Contains(name);
    }

    /// <summary>
    /// Returns list of active pipes
    /// </summary>
    public static IReadOnlyCollection<string> GetActivePipes()
    {
        return Directory
            .EnumerateFiles(@"\\.\pipe\")
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            .Select(static path => path.Replace(@"\\.\pipe\", string.Empty, StringComparison.Ordinal))
#else
            .Select(static path => path.Replace(@"\\.\pipe\", string.Empty))
#endif
            .ToList();
    }

    /// <summary>
    /// Create new instance of watcher and start it<br/>
    /// Default interval is <see langword="100 milliseconds"/>
    /// </summary>
    /// <param name="interval"></param>
    /// <returns></returns>
    public static PipeWatcher CreateAndStart(TimeSpan? interval = default)
    {
        var watcher = new PipeWatcher(interval);
        watcher.Start();

        return watcher;
    }

#endregion
}

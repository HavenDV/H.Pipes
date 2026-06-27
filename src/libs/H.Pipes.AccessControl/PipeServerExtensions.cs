using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace H.Pipes.AccessControl;

/// <summary>
/// Adds AccessControl extensions methods for <see cref="PipeServer"/>.
/// </summary>
public static class PipeServerExtensions
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static Func<string, NamedPipeServerStream> CreatePipeStreamFunc(PipeSecurity pipeSecurity)
    {
        pipeSecurity = pipeSecurity ?? throw new ArgumentNullException(nameof(pipeSecurity));

        return pipeName =>
#if NET6_0_OR_GREATER
            NamedPipeServerStreamAcl.Create(
                pipeName: pipeName,
                direction: PipeDirection.InOut,
                maxNumberOfServerInstances: 1,
                transmissionMode: PipeTransmissionMode.Byte,
                options: PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                inBufferSize: 0,
                outBufferSize: 0,
                pipeSecurity: pipeSecurity);
#else
           NamedPipeServerStreamConstructors.New(
                pipeName: pipeName,
                direction: PipeDirection.InOut,
                maxNumberOfServerInstances: 1,
                transmissionMode: PipeTransmissionMode.Byte,
                options: PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                inBufferSize: 0,
                outBufferSize: 0,
                pipeSecurity: pipeSecurity);
#endif
    }

    /// <summary>
    /// Sets <see cref="PipeSecurity"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer"/> <br/>
    /// Overrides <see cref="PipeServer.CreatePipeStreamFunc"/> and <see cref="PipeServer.CreatePipeStreamForConnectionFunc"></see>
    /// </summary>
    /// <param name="server"></param>
    /// <param name="pipeSecurity"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void SetPipeSecurity(this IPipeServer server, PipeSecurity pipeSecurity)
    {
        server = server ?? throw new ArgumentNullException(nameof(server));

        server.CreatePipeStreamFunc = CreatePipeStreamFunc(pipeSecurity);
    }

    /// <summary>
    /// Sets <see cref="PipeSecurity"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer{T}"/> <br/>
    /// Overrides the server's pipe stream creation delegate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="server"></param>
    /// <param name="pipeSecurity"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void SetPipeSecurity<T>(this IPipeServer<T> server, PipeSecurity pipeSecurity)
    {
        server = server ?? throw new ArgumentNullException(nameof(server));

        server.CreatePipeStreamFunc = CreatePipeStreamFunc(pipeSecurity);
    }

    /// <summary>
    /// Sets <see cref="PipeSecurity"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="server"></param>
    /// <param name="pipeSecurity"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void SetPipeSecurity<T>(this PipeServer<T> server, PipeSecurity pipeSecurity)
    {
        SetPipeSecurity((IPipeServer)server, pipeSecurity);
    }

    /// <summary>
    /// Sets <see cref="PipeSecurity"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="SingleConnectionPipeServer{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="server"></param>
    /// <param name="pipeSecurity"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void SetPipeSecurity<T>(this SingleConnectionPipeServer<T> server, PipeSecurity pipeSecurity)
    {
        SetPipeSecurity((IPipeServer)server, pipeSecurity);
    }

    /// <summary>
    /// Adds <see cref="PipeAccessRule"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer"/>.
    /// </summary>
    /// <param name="server"></param>
    /// <param name="rules"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void AddAccessRules(this IPipeServer server, params PipeAccessRule[] rules)
    {
        server = server ?? throw new ArgumentNullException(nameof(server));
        rules = rules ?? throw new ArgumentNullException(nameof(rules));

        var pipeSecurity = new PipeSecurity();
        foreach (var rule in rules)
        {
            pipeSecurity.AddAccessRule(rule);
        }

        server.SetPipeSecurity(pipeSecurity);
    }

    /// <summary>
    /// Adds <see cref="PipeAccessRule"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="server"></param>
    /// <param name="rules"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void AddAccessRules<T>(this IPipeServer<T> server, params PipeAccessRule[] rules)
    {
        server = server ?? throw new ArgumentNullException(nameof(server));
        rules = rules ?? throw new ArgumentNullException(nameof(rules));

        var pipeSecurity = new PipeSecurity();
        foreach (var rule in rules)
        {
            pipeSecurity.AddAccessRule(rule);
        }

        server.SetPipeSecurity(pipeSecurity);
    }

    /// <summary>
    /// Adds <see cref="PipeAccessRule"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="server"></param>
    /// <param name="rules"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void AddAccessRules<T>(this PipeServer<T> server, params PipeAccessRule[] rules)
    {
        AddAccessRules((IPipeServer)server, rules);
    }

    /// <summary>
    /// Adds <see cref="PipeAccessRule"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="SingleConnectionPipeServer{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="server"></param>
    /// <param name="rules"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void AddAccessRules<T>(this SingleConnectionPipeServer<T> server, params PipeAccessRule[] rules)
    {
        AddAccessRules((IPipeServer)server, rules);
    }

    /// <summary>
    /// Adds <see cref="PipeAccessRule"/> that allow ReadWrite to BuiltinUsersSid.
    /// </summary>
    /// <param name="server"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void AllowUsersReadWrite(this IPipeServer server)
    {
        server = server ?? throw new ArgumentNullException(nameof(server));

        server.AddAccessRules(
            new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));
    }

    /// <summary>
    /// Adds <see cref="PipeAccessRule"/> that allow ReadWrite to BuiltinUsersSid.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="server"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void AllowUsersReadWrite<T>(this IPipeServer<T> server)
    {
        server = server ?? throw new ArgumentNullException(nameof(server));

        server.AddAccessRules(
            new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));
    }

    /// <summary>
    /// Adds <see cref="PipeAccessRule"/> that allow ReadWrite to BuiltinUsersSid.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="server"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void AllowUsersReadWrite<T>(this PipeServer<T> server)
    {
        AllowUsersReadWrite((IPipeServer)server);
    }

    /// <summary>
    /// Adds <see cref="PipeAccessRule"/> that allow ReadWrite to BuiltinUsersSid.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="server"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void AllowUsersReadWrite<T>(this SingleConnectionPipeServer<T> server)
    {
        AllowUsersReadWrite((IPipeServer)server);
    }
}

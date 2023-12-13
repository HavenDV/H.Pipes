using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace H.Pipes.AccessControl;

/// <summary>
/// Adds AccessControl extensions methods for <see cref="PipeServer{T}"/>
/// </summary>
public static class PipeServerExtensions
{
    /// <summary>
    /// Sets <see cref="PipeSecurity"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer{T}"/> <br/>
    /// Overrides <see cref="PipeServer{T}.CreatePipeStreamFunc"/>
    /// </summary>
    /// <param name="server"></param>
    /// <param name="pipeSecurity"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void SetPipeSecurity<T>(this IPipeServer<T> server, PipeSecurity pipeSecurity)
    {
        server = server ?? throw new ArgumentNullException(nameof(server));
        pipeSecurity = pipeSecurity ?? throw new ArgumentNullException(nameof(pipeSecurity));

        server.CreatePipeStreamFunc = pipeName => NamedPipeServerStreamConstructors.New(
            pipeName: pipeName,
            direction: PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous | PipeOptions.WriteThrough,
            inBufferSize: 0,
            outBufferSize: 0,
            pipeSecurity: pipeSecurity);
    }

    /// <summary>
    /// Adds <see cref="PipeAccessRule"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer{T}"/>
    /// </summary>
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
    /// Adds <see cref="PipeAccessRule"/> that allow ReadWrite to BuiltinUsersSid.
    /// </summary>
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
}

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
    /// Overrides <see cref="PipeServer.CreatePipeStreamFunc"/>
    /// </summary>
    /// <param name="server"></param>
    /// <param name="pipeSecurity"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#elif NETSTANDARD2_0_OR_GREATER || NET461_OR_GREATER
#else
#error Target Framework is not supported
#endif
    public static void SetPipeSecurity<T>(this IPipeServer<T> server, PipeSecurity pipeSecurity)
    {
        server = server ?? throw new ArgumentNullException(nameof(server));
        pipeSecurity = pipeSecurity ?? throw new ArgumentNullException(nameof(pipeSecurity));

#if NET461_OR_GREATER
        server.CreatePipeStreamFunc = pipeName => new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough, 0, 0, pipeSecurity);
#elif NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER
        server.CreatePipeStreamFunc = pipeName => NamedPipeServerStreamConstructors.New(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough, 0, 0, pipeSecurity);
#else
#error Target Framework is not supported
#endif
    }

    /// <summary>
    /// Adds <see cref="PipeAccessRule"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer{T}"/>
    /// </summary>
    /// <param name="server"></param>
    /// <param name="rules"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#elif NETSTANDARD2_0_OR_GREATER || NET461_OR_GREATER
#else
#error Target Framework is not supported
#endif
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
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#elif NETSTANDARD2_0_OR_GREATER || NET461_OR_GREATER
#else
#error Target Framework is not supported
#endif
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

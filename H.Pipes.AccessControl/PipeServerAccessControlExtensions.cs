using System;
using System.IO.Pipes;
using System.Security.AccessControl;
using H.Pipes.Internal;

namespace H.Pipes
{
    /// <summary>
    /// Adds AccessControl extensions methods for <see cref="PipeServer{T}"/>
    /// </summary>
    public static class PipeServerAccessControlExtensions
    {
        /// <summary>
        /// Adds <see cref="PipeAccessRule"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer{T}"/>
        /// </summary>
        /// <param name="server"></param>
        /// <param name="rules"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public static void AddAccessRules<T>(this PipeServer<T> server, params PipeAccessRule[] rules)
        {
            server = server ?? throw new ArgumentNullException(nameof(rules));
            rules = rules ?? throw new ArgumentNullException(nameof(rules));

            server.PipeStreamInitializeAction = stream =>
            {
                var control = stream.GetAccessControl();
                foreach (var rule in rules)
                {
                    control.AddAccessRule(rule);
                }

                stream.SetAccessControl(control);
            };
        }

        /// <summary>
        /// Sets <see cref="PipeSecurity"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer{T}"/>
        /// </summary>
        /// <param name="server"></param>
        /// <param name="pipeSecurity"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public static void SetPipeSecurity<T>(this PipeServer<T> server, PipeSecurity pipeSecurity)
        {
            server = server ?? throw new ArgumentNullException(nameof(server));
            pipeSecurity = pipeSecurity ?? throw new ArgumentNullException(nameof(pipeSecurity));

            server.CreatePipeStreamFunc = name =>
            {
                var handle = NativeNamedPipeServer.CreateNamedPipeServer(name, pipeSecurity.GetSecurityDescriptorSddlForm(AccessControlSections.Access));

                return new NamedPipeServerStream(PipeDirection.InOut, true, false, handle);
            };
        }
    }
}

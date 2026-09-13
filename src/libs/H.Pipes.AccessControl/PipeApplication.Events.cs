
#pragma warning disable CA1034 // Preserve the public API formerly emitted by EventGenerator.

namespace H.Pipes.AccessControl
{
    internal sealed class EventSubscription : global::System.IDisposable
    {
        private readonly global::System.Action action;

        public EventSubscription(global::System.Action action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            action();
        }
    }
}

#nullable enable

namespace H.Pipes.AccessControl
{
    public partial class PipeApplication
    {
        /// <summary>
        ///
        /// </summary>
        public class ArgumentsReceivedEventArgs : global::System.EventArgs
        {
            /// <summary>
            ///
            /// </summary>
            public global::System.Collections.Generic.IReadOnlyCollection<string> Arguments { get; }

            /// <summary>
            ///
            /// </summary>
            public ArgumentsReceivedEventArgs(global::System.Collections.Generic.IReadOnlyCollection<string> arguments)
            {
                Arguments = arguments;
            }

            /// <summary>
            ///
            /// </summary>
            public void Deconstruct(out global::System.Collections.Generic.IReadOnlyCollection<string> arguments)
            {
                arguments = Arguments;
            }

            /// <summary>
            ///
            /// </summary>
            public override string ToString()
            {
                return $"(Arguments={Arguments})";
            }
        }
    }
}

#nullable enable

namespace H.Pipes.AccessControl
{
    public partial class PipeApplication
    {
        /// <summary>
        ///
        /// </summary>
        public class ExceptionOccurredEventArgs : global::System.EventArgs
        {
            /// <summary>
            ///
            /// </summary>
            public global::System.Exception Exception { get; }

            /// <summary>
            ///
            /// </summary>
            public ExceptionOccurredEventArgs(global::System.Exception exception)
            {
                Exception = exception;
            }

            /// <summary>
            ///
            /// </summary>
            public void Deconstruct(out global::System.Exception exception)
            {
                exception = Exception;
            }

            /// <summary>
            ///
            /// </summary>
            public override string ToString()
            {
                return $"(Exception={Exception})";
            }
        }
    }
}

#nullable enable

namespace H.Pipes.AccessControl
{
    public partial class PipeApplication
    {
        /// <summary>
        /// Occurs when new arguments received.
        /// </summary>
        public event global::System.EventHandler<global::H.Pipes.AccessControl.PipeApplication.ArgumentsReceivedEventArgs>? ArgumentsReceived;

        /// <summary>
        /// A helper method to subscribe the ArgumentsReceived event.
        /// </summary>
        public global::System.IDisposable SubscribeToArgumentsReceived(global::System.EventHandler<global::H.Pipes.AccessControl.PipeApplication.ArgumentsReceivedEventArgs> handler)
        {
            ArgumentsReceived += handler;

            return new global::H.Pipes.AccessControl.EventSubscription(() => ArgumentsReceived -= handler);
        }

        /// <summary>
        /// A helper method to raise the ArgumentsReceived event.
        /// </summary>
        private global::H.Pipes.AccessControl.PipeApplication.ArgumentsReceivedEventArgs OnArgumentsReceived(global::H.Pipes.AccessControl.PipeApplication.ArgumentsReceivedEventArgs args)
        {
            ArgumentsReceived?.Invoke(this, args);

            return args;
        }

        /// <summary>
        /// A helper method to raise the ArgumentsReceived event.
        /// </summary>
        private global::H.Pipes.AccessControl.PipeApplication.ArgumentsReceivedEventArgs OnArgumentsReceived(
            global::System.Collections.Generic.IReadOnlyCollection<string> arguments)
        {
            var args = new global::H.Pipes.AccessControl.PipeApplication.ArgumentsReceivedEventArgs(arguments);
            ArgumentsReceived?.Invoke(this, args);

            return args;
        }
    }
}

#nullable enable

namespace H.Pipes.AccessControl
{
    public partial class PipeApplication
    {
        /// <summary>
        /// Occurs when new exception.
        /// </summary>
        public event global::System.EventHandler<global::H.Pipes.AccessControl.PipeApplication.ExceptionOccurredEventArgs>? ExceptionOccurred;

        /// <summary>
        /// A helper method to subscribe the ExceptionOccurred event.
        /// </summary>
        public global::System.IDisposable SubscribeToExceptionOccurred(global::System.EventHandler<global::H.Pipes.AccessControl.PipeApplication.ExceptionOccurredEventArgs> handler)
        {
            ExceptionOccurred += handler;

            return new global::H.Pipes.AccessControl.EventSubscription(() => ExceptionOccurred -= handler);
        }

        /// <summary>
        /// A helper method to raise the ExceptionOccurred event.
        /// </summary>
        private global::H.Pipes.AccessControl.PipeApplication.ExceptionOccurredEventArgs OnExceptionOccurred(global::H.Pipes.AccessControl.PipeApplication.ExceptionOccurredEventArgs args)
        {
            ExceptionOccurred?.Invoke(this, args);

            return args;
        }

        /// <summary>
        /// A helper method to raise the ExceptionOccurred event.
        /// </summary>
        private global::H.Pipes.AccessControl.PipeApplication.ExceptionOccurredEventArgs OnExceptionOccurred(
            global::System.Exception exception)
        {
            var args = new global::H.Pipes.AccessControl.PipeApplication.ExceptionOccurredEventArgs(exception);
            ExceptionOccurred?.Invoke(this, args);

            return args;
        }
    }
}

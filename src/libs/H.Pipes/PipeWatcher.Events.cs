
#pragma warning disable CA1034 // Preserve the public API formerly emitted by EventGenerator.

namespace H.Pipes
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

namespace H.Pipes
{
    public partial class PipeWatcher
    {
        /// <summary>
        ///
        /// </summary>
        public class CreatedEventArgs : global::System.EventArgs
        {
            /// <summary>
            ///
            /// </summary>
            public string Name { get; }

            /// <summary>
            ///
            /// </summary>
            public CreatedEventArgs(string name)
            {
                Name = name;
            }

            /// <summary>
            ///
            /// </summary>
            public void Deconstruct(out string name)
            {
                name = Name;
            }

            /// <summary>
            ///
            /// </summary>
            public override string ToString()
            {
                return $"(Name={Name})";
            }
        }
    }
}

#nullable enable

namespace H.Pipes
{
    public partial class PipeWatcher
    {
        /// <summary>
        ///
        /// </summary>
        public class DeletedEventArgs : global::System.EventArgs
        {
            /// <summary>
            ///
            /// </summary>
            public string Name { get; }

            /// <summary>
            ///
            /// </summary>
            public DeletedEventArgs(string name)
            {
                Name = name;
            }

            /// <summary>
            ///
            /// </summary>
            public void Deconstruct(out string name)
            {
                name = Name;
            }

            /// <summary>
            ///
            /// </summary>
            public override string ToString()
            {
                return $"(Name={Name})";
            }
        }
    }
}

#nullable enable

namespace H.Pipes
{
    public partial class PipeWatcher
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

namespace H.Pipes
{
    public partial class PipeWatcher
    {
        /// <summary>
        /// When any pipe created.
        /// </summary>
        public event global::System.EventHandler<global::H.Pipes.PipeWatcher.CreatedEventArgs>? Created;

        /// <summary>
        /// A helper method to subscribe the Created event.
        /// </summary>
        public global::System.IDisposable SubscribeToCreated(global::System.EventHandler<global::H.Pipes.PipeWatcher.CreatedEventArgs> handler)
        {
            Created += handler;

            return new global::H.Pipes.EventSubscription(() => Created -= handler);
        }

        /// <summary>
        /// A helper method to raise the Created event.
        /// </summary>
        private global::H.Pipes.PipeWatcher.CreatedEventArgs OnCreated(global::H.Pipes.PipeWatcher.CreatedEventArgs args)
        {
            Created?.Invoke(this, args);

            return args;
        }

        /// <summary>
        /// A helper method to raise the Created event.
        /// </summary>
        private global::H.Pipes.PipeWatcher.CreatedEventArgs OnCreated(
            string name)
        {
            var args = new global::H.Pipes.PipeWatcher.CreatedEventArgs(name);
            Created?.Invoke(this, args);

            return args;
        }
    }
}

#nullable enable

namespace H.Pipes
{
    public partial class PipeWatcher
    {
        /// <summary>
        /// When any pipe deleted.
        /// </summary>
        public event global::System.EventHandler<global::H.Pipes.PipeWatcher.DeletedEventArgs>? Deleted;

        /// <summary>
        /// A helper method to subscribe the Deleted event.
        /// </summary>
        public global::System.IDisposable SubscribeToDeleted(global::System.EventHandler<global::H.Pipes.PipeWatcher.DeletedEventArgs> handler)
        {
            Deleted += handler;

            return new global::H.Pipes.EventSubscription(() => Deleted -= handler);
        }

        /// <summary>
        /// A helper method to raise the Deleted event.
        /// </summary>
        private global::H.Pipes.PipeWatcher.DeletedEventArgs OnDeleted(global::H.Pipes.PipeWatcher.DeletedEventArgs args)
        {
            Deleted?.Invoke(this, args);

            return args;
        }

        /// <summary>
        /// A helper method to raise the Deleted event.
        /// </summary>
        private global::H.Pipes.PipeWatcher.DeletedEventArgs OnDeleted(
            string name)
        {
            var args = new global::H.Pipes.PipeWatcher.DeletedEventArgs(name);
            Deleted?.Invoke(this, args);

            return args;
        }
    }
}

#nullable enable

namespace H.Pipes
{
    public partial class PipeWatcher
    {
        /// <summary>
        /// When any exception is thrown.
        /// </summary>
        public event global::System.EventHandler<global::H.Pipes.PipeWatcher.ExceptionOccurredEventArgs>? ExceptionOccurred;

        /// <summary>
        /// A helper method to subscribe the ExceptionOccurred event.
        /// </summary>
        public global::System.IDisposable SubscribeToExceptionOccurred(global::System.EventHandler<global::H.Pipes.PipeWatcher.ExceptionOccurredEventArgs> handler)
        {
            ExceptionOccurred += handler;

            return new global::H.Pipes.EventSubscription(() => ExceptionOccurred -= handler);
        }

        /// <summary>
        /// A helper method to raise the ExceptionOccurred event.
        /// </summary>
        private global::H.Pipes.PipeWatcher.ExceptionOccurredEventArgs OnExceptionOccurred(global::H.Pipes.PipeWatcher.ExceptionOccurredEventArgs args)
        {
            ExceptionOccurred?.Invoke(this, args);

            return args;
        }

        /// <summary>
        /// A helper method to raise the ExceptionOccurred event.
        /// </summary>
        private global::H.Pipes.PipeWatcher.ExceptionOccurredEventArgs OnExceptionOccurred(
            global::System.Exception exception)
        {
            var args = new global::H.Pipes.PipeWatcher.ExceptionOccurredEventArgs(exception);
            ExceptionOccurred?.Invoke(this, args);

            return args;
        }
    }
}

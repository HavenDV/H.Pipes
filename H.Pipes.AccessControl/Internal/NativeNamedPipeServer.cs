using System;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace H.Pipes.Internal
{
    /// <summary>
    /// https://stackoverflow.com/questions/50711518/attempted-to-perform-an-unauthorized-operation-when-calling-namedpipeserverstr
    /// </summary>
    public static class NativeNamedPipeServer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="sddl"></param>
        /// <returns></returns>
        public static SafePipeHandle CreateNamedPipeServer(string pipeName, string sddl)
        {
            return NativeMethod.CreateNamedPipe(
                @"\\.\pipe\" + pipeName, // The unique pipe name.
                PipeOpenMode.PIPE_ACCESS_DUPLEX | PipeOpenMode.ASYNCHRONOUS,
                PipeMode.PIPE_TYPE_BYTE,
                1, // Max server instances
                1024 * 16, // Output buffer size
                1024 * 16, // Input buffer size
                NMPWAIT_USE_DEFAULT_WAIT, // Time-out interval
                CreateNativePipeSecurity(sddl) // Pipe security attributes
            );
        }

        /// <summary>
        /// The CreateNativePipeSecurity function creates and initializes a new 
        /// SECURITY_ATTRIBUTES object to allow Authenticated Users read and 
        /// write access to a pipe, and to allow the Administrators group full 
        /// access to the pipe.
        /// </summary>
        /// <returns>
        /// A SECURITY_ATTRIBUTES object that allows Authenticated Users read and 
        /// write access to a pipe, and allows the Administrators group full 
        /// access to the pipe.
        /// </returns>
        /// see https://msdn.microsoft.com/en-us/library/aa365600(VS.85).aspx
        private static SECURITY_ATTRIBUTES CreateNativePipeSecurity(string sddl)
        {
            if (!NativeMethod.ConvertStringSecurityDescriptorToSecurityDescriptor(
                sddl, 1, out var pSecurityDescriptor, IntPtr.Zero))
            {
                throw new Win32Exception();
            }

            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.nLength = Marshal.SizeOf(sa);
            sa.lpSecurityDescriptor = pSecurityDescriptor;
            sa.bInheritHandle = false;
            return sa;
        }


        #region Native API Signatures and Types

        /// <summary>
        /// Named Pipe Open Modes
        /// http://msdn.microsoft.com/en-us/library/aa365596.aspx
        /// </summary>
        [Flags]
        internal enum PipeOpenMode : uint
        {
            PIPE_ACCESS_INBOUND = 0x00000001, // Inbound pipe access.
            PIPE_ACCESS_OUTBOUND = 0x00000002, // Outbound pipe access.
            PIPE_ACCESS_DUPLEX = 0x00000003, // Duplex pipe access.

            // added from C# PipeOptions.cs
            WRITE_THROUGH = 0x80000000,
            ASYNCHRONOUS = 0x40000000,
            CURRENT_USER_ONLY = 0x20000000
        }

        /// <summary>
        /// Named Pipe Type, Read, and Wait Modes
        /// http://msdn.microsoft.com/en-us/library/aa365605.aspx
        /// </summary>
        [Flags]
        internal enum PipeMode : uint
        {
            // Type Mode
            PIPE_TYPE_BYTE = 0x00000000, // Byte pipe type.
            PIPE_TYPE_MESSAGE = 0x00000004, // Message pipe type.

            // Read Mode
            PIPE_READMODE_BYTE = 0x00000000, // Read mode of type Byte.
            PIPE_READMODE_MESSAGE = 0x00000002, // Read mode of type Message.

            // Wait Mode
            PIPE_WAIT = 0x00000000, // Pipe blocking mode.
            PIPE_NOWAIT = 0x00000001 // Pipe non-blocking mode.
        }

        /// <summary>
        /// Uses the default time-out specified in a call to the 
        /// CreateNamedPipe method.
        /// </summary>
        internal const uint NMPWAIT_USE_DEFAULT_WAIT = 0x00000000;


        /// <summary>
        /// The SECURITY_ATTRIBUTES structure contains the security descriptor for 
        /// an object and specifies whether the handle retrieved by specifying 
        /// this structure is inheritable. This structure provides security 
        /// settings for objects created by various functions, such as CreateFile, 
        /// CreateNamedPipe, CreateProcess, RegCreateKeyEx, or RegSaveKeyEx.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES
        {
            public int nLength;
            public SafeLocalMemHandle? lpSecurityDescriptor;
            public bool bInheritHandle;
        }


        /// <summary>
        /// Represents a wrapper class for a local memory pointer. 
        /// </summary>
        [SuppressUnmanagedCodeSecurity] //, HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)
        internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeLocalMemHandle() : base(true)
            {
            }

            public SafeLocalMemHandle(IntPtr preexistingHandle, bool ownsHandle)
                : base(ownsHandle)
            {
                base.SetHandle(preexistingHandle);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success),
             DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr LocalFree(IntPtr hMem);

            protected override bool ReleaseHandle()
            {
                return (LocalFree(base.handle) == IntPtr.Zero);
            }
        }


        /// <summary>
        /// The class exposes Windows APIs to be used in this code sample.
        /// </summary>
        [SuppressUnmanagedCodeSecurity]
        internal class NativeMethod
        {
            /// <summary>
            /// Creates an instance of a named pipe and returns a handle for 
            /// subsequent pipe operations.
            /// </summary>
            /// <param name="pipeName">Pipe name</param>
            /// <param name="openMode">Pipe open mode</param>
            /// <param name="pipeMode">Pipe-specific modes</param>
            /// <param name="maxInstances">Maximum number of instances</param>
            /// <param name="outBufferSize">Output buffer size</param>
            /// <param name="inBufferSize">Input buffer size</param>
            /// <param name="defaultTimeout">Time-out interval</param>
            /// <param name="securityAttributes">Security attributes</param>
            /// <returns>If the function succeeds, the return value is a handle 
            /// to the server end of a named pipe instance.</returns>
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern SafePipeHandle CreateNamedPipe(string pipeName,
                PipeOpenMode openMode, PipeMode pipeMode, int maxInstances,
                int outBufferSize, int inBufferSize, uint defaultTimeout,
                SECURITY_ATTRIBUTES securityAttributes);


            /// <summary>
            /// The ConvertStringSecurityDescriptorToSecurityDescriptor function 
            /// converts a string-format security descriptor into a valid, 
            /// functional security descriptor.
            /// </summary>
            /// <param name="sddlSecurityDescriptor">
            /// A string containing the string-format security descriptor (SDDL) 
            /// to convert.
            /// </param>
            /// <param name="sddlRevision">
            /// The revision level of the sddlSecurityDescriptor string. 
            /// Currently this value must be 1.
            /// </param>
            /// <param name="pSecurityDescriptor">
            /// A pointer to a variable that receives a pointer to the converted 
            /// security descriptor.
            /// </param>
            /// <param name="securityDescriptorSize">
            /// A pointer to a variable that receives the size, in bytes, of the 
            /// converted security descriptor. This parameter can be IntPtr.Zero.
            /// </param>
            /// <returns>
            /// If the function succeeds, the return value is true.
            /// </returns>
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
                string sddlSecurityDescriptor, int sddlRevision,
                out SafeLocalMemHandle pSecurityDescriptor,
                IntPtr securityDescriptorSize);
        }

        #endregion
    }
}

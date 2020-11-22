using System.Globalization;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace System.IO.Pipes
{
    /// <summary>
    /// Original .Net Framework <see cref="NamedPipeServerStream"/> constructors from decompiled code
    /// </summary>
    [SecurityCritical]
    public static class NamedPipeServerStreamConstructors
    {
        /// <summary>
        /// Create a new <see cref="NamedPipeServerStream"/>. All default parameters are copied from the original constructors.
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="direction"></param>
        /// <param name="maxNumberOfServerInstances"></param>
        /// <param name="transmissionMode"></param>
        /// <param name="options"></param>
        /// <param name="inBufferSize"></param>
        /// <param name="outBufferSize"></param>
        /// <param name="pipeSecurity"></param>
        /// <param name="inheritability"></param>
        /// <param name="additionalAccessRights"></param>
        /// <returns></returns>
        [SecurityCritical]
        public static NamedPipeServerStream New(
            string pipeName,
            PipeDirection direction = PipeDirection.InOut,
            int maxNumberOfServerInstances = 1,
            PipeTransmissionMode transmissionMode = PipeTransmissionMode.Byte,
            PipeOptions options = PipeOptions.None,
            int inBufferSize = 0,
            int outBufferSize = 0,
            PipeSecurity pipeSecurity = null,
            HandleInheritability inheritability = HandleInheritability.None,
            PipeAccessRights additionalAccessRights = 0)
        {
            switch (pipeName)
            {
                case "":
                    throw new ArgumentException(SR.GetString("Argument_NeedNonemptyPipeName"));
                case null:
                    throw new ArgumentNullException(nameof(pipeName));
                default:
                    if ((options & ~(PipeOptions.WriteThrough | PipeOptions.Asynchronous)) != PipeOptions.None)
                        throw new ArgumentOutOfRangeException(nameof(options), SR.GetString("ArgumentOutOfRange_OptionsInvalid"));
                    if (inBufferSize < 0)
                        throw new ArgumentOutOfRangeException(nameof(inBufferSize), SR.GetString("ArgumentOutOfRange_NeedNonNegNum"));
                    if ((maxNumberOfServerInstances < 1 || maxNumberOfServerInstances > 254) && maxNumberOfServerInstances != -1)
                        throw new ArgumentOutOfRangeException(nameof(maxNumberOfServerInstances), SR.GetString("ArgumentOutOfRange_MaxNumServerInstances"));
                    if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
                        throw new ArgumentOutOfRangeException(nameof(inheritability), SR.GetString("ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable"));
                    if ((additionalAccessRights & ~(PipeAccessRights.ChangePermissions | PipeAccessRights.TakeOwnership | PipeAccessRights.AccessSystemSecurity)) != (PipeAccessRights)0)
                        throw new ArgumentOutOfRangeException(nameof(additionalAccessRights), SR.GetString("ArgumentOutOfRange_AdditionalAccessLimited"));
                    if (Environment.OSVersion.Platform == PlatformID.Win32Windows)
                        throw new PlatformNotSupportedException(SR.GetString("PlatformNotSupported_NamedPipeServers"));
                    string fullPath = Path.GetFullPath("\\\\.\\pipe\\" + pipeName);
                    if (string.Compare(fullPath, "\\\\.\\pipe\\anonymous", StringComparison.OrdinalIgnoreCase) == 0)
                        throw new ArgumentOutOfRangeException(nameof(pipeName), SR.GetString("ArgumentOutOfRange_AnonymousReserved"));
                    object pinningHandle = (object)null;
                    SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(inheritability, pipeSecurity, out pinningHandle);
                    try
                    {
                        int openMode = (int)((PipeOptions)(direction | (maxNumberOfServerInstances == 1 ? (PipeDirection)524288 : (PipeDirection)0)) | options | (PipeOptions)additionalAccessRights);
                        int pipeMode = (int)transmissionMode << 2 | (int)transmissionMode << 1;
                        if (maxNumberOfServerInstances == -1)
                            maxNumberOfServerInstances = (int)byte.MaxValue;
#pragma warning disable CA2000 // Dispose objects before losing scope
                        SafePipeHandle namedPipe = CreateNamedPipe(fullPath, openMode, pipeMode, maxNumberOfServerInstances, outBufferSize, inBufferSize, 0, secAttrs);
#pragma warning restore CA2000 // Dispose objects before losing scope
                        if (namedPipe.IsInvalid)
                            WinIOError(Marshal.GetLastWin32Error(), string.Empty);

                        return new NamedPipeServerStream(direction, (uint)(options & PipeOptions.Asynchronous) > 0U, false, namedPipe);
                    }
                    finally
                    {
                        if (pinningHandle != null)
                            ((GCHandle)pinningHandle).Free();
                    }
            }
        }

        [SecurityCritical]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        internal static extern SafePipeHandle CreateNamedPipe(
            string pipeName,
            int openMode,
            int pipeMode,
            int maxInstances,
            int outBufferSize,
            int inBufferSize,
            int defaultTimeout,
            SECURITY_ATTRIBUTES securityAttributes);


        [SecurityCritical]
        internal static unsafe SECURITY_ATTRIBUTES GetSecAttrs(
            HandleInheritability inheritability,
            PipeSecurity pipeSecurity,
            out object pinningHandle)
        {
            pinningHandle = (object)null;
            SECURITY_ATTRIBUTES securityAttributes = (SECURITY_ATTRIBUTES)null;
            if ((inheritability & HandleInheritability.Inheritable) != HandleInheritability.None || pipeSecurity != null)
            {
                securityAttributes = new SECURITY_ATTRIBUTES();
                securityAttributes.nLength = Marshal.SizeOf((object)securityAttributes);
                if ((inheritability & HandleInheritability.Inheritable) != HandleInheritability.None)
                    securityAttributes.bInheritHandle = 1;
                if (pipeSecurity != null)
                {
                    byte[] descriptorBinaryForm = pipeSecurity.GetSecurityDescriptorBinaryForm();
                    pinningHandle = (object)GCHandle.Alloc((object)descriptorBinaryForm, GCHandleType.Pinned);
                    fixed (byte* numPtr = descriptorBinaryForm)
                        securityAttributes.pSecurityDescriptor = numPtr;
                }
            }
            return securityAttributes;
        }

        [SecurityCritical]
        internal static void WinIOError(int errorCode, string maybeFullPath)
        {
            bool isInvalidPath = errorCode == 123 || errorCode == 161;
            string displayablePath = GetDisplayablePath(maybeFullPath, isInvalidPath);
            switch (errorCode)
            {
                case 2:
                    if (displayablePath.Length == 0)
                        throw new FileNotFoundException(SR.GetString("IO_FileNotFound"));
                    throw new FileNotFoundException(string.Format((IFormatProvider)CultureInfo.CurrentCulture, SR.GetString("IO_FileNotFound_FileName"), new object[1]
                    {
            (object) displayablePath
                    }), displayablePath);
                case 3:
                    if (displayablePath.Length == 0)
                        throw new DirectoryNotFoundException(SR.GetString("IO_PathNotFound_NoPathName"));
                    throw new DirectoryNotFoundException(string.Format((IFormatProvider)CultureInfo.CurrentCulture, SR.GetString("IO_PathNotFound_Path"), new object[1]
                    {
            (object) displayablePath
                    }));
                case 5:
                    if (displayablePath.Length == 0)
                        throw new UnauthorizedAccessException(SR.GetString("UnauthorizedAccess_IODenied_NoPathName"));
                    throw new UnauthorizedAccessException(string.Format((IFormatProvider)CultureInfo.CurrentCulture, SR.GetString("UnauthorizedAccess_IODenied_Path"), new object[1]
                    {
            (object) displayablePath
                    }));
                case 15:
                    throw new DriveNotFoundException(string.Format((IFormatProvider)CultureInfo.CurrentCulture, SR.GetString("IO_DriveNotFound_Drive"), new object[1]
                    {
            (object) displayablePath
                    }));
                case 32:
                    if (displayablePath.Length == 0)
                        throw new IOException(SR.GetString("IO_IO_SharingViolation_NoFileName"), MakeHRFromErrorCode(errorCode));
                    throw new IOException(SR.GetString("IO_IO_SharingViolation_File", (object)displayablePath), MakeHRFromErrorCode(errorCode));
                case 80:
                    if (displayablePath.Length != 0)
                        throw new IOException(string.Format((IFormatProvider)CultureInfo.CurrentCulture, SR.GetString("IO_IO_FileExists_Name"), new object[1]
                        {
              (object) displayablePath
                        }), MakeHRFromErrorCode(errorCode));
                    break;
                case 87:
                    throw new IOException(GetMessage(errorCode), MakeHRFromErrorCode(errorCode));
                case 183:
                    if (displayablePath.Length != 0)
                        throw new IOException(SR.GetString("IO_IO_AlreadyExists_Name", (object)displayablePath), MakeHRFromErrorCode(errorCode));
                    break;
                case 206:
                    throw new PathTooLongException(SR.GetString("IO_PathTooLong"));
                case 995:
                    throw new OperationCanceledException();
            }
            throw new IOException(GetMessage(errorCode), MakeHRFromErrorCode(errorCode));
        }

        [SecuritySafeCritical]
        internal static string GetDisplayablePath(string path, bool isInvalidPath)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            bool flag1 = false;
            if (path.Length < 2)
                return path;
            if ((int)path[0] == (int)Path.DirectorySeparatorChar && (int)path[1] == (int)Path.DirectorySeparatorChar)
                flag1 = true;
            else if ((int)path[1] == (int)Path.VolumeSeparatorChar)
                flag1 = true;
            if (!flag1 && !isInvalidPath)
                return path;
            bool flag2 = false;
            try
            {
                if (!isInvalidPath)
                {
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, new string[1]
                    {
                        path
                    }).Demand();
                    flag2 = true;
                }
            }
            catch (SecurityException)
            {
            }
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }
            if (!flag2)
                path = (int)path[path.Length - 1] != (int)Path.DirectorySeparatorChar ? Path.GetFileName(path) : SR.GetString("IO_IO_NoPermissionToDirectoryName");
            return path;
        }

        internal static int MakeHRFromErrorCode(int errorCode)
        {
            return -2147024896 | errorCode;
        }

        [SecurityCritical]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int FormatMessage(
            int dwFlags,
            IntPtr lpSource,
            int dwMessageId,
            int dwLanguageId,
            StringBuilder lpBuffer,
            int nSize,
            IntPtr va_list_arguments);

        internal static readonly IntPtr NULL = IntPtr.Zero;

        [SecurityCritical]
        internal static string GetMessage(int errorCode)
        {
            StringBuilder lpBuffer = new StringBuilder(512);
            return FormatMessage(12800, NULL, errorCode, 0, lpBuffer, lpBuffer.Capacity, NULL) != 0 ? lpBuffer.ToString() : "UnknownError_Num " + (object)errorCode;
        }
    }
}

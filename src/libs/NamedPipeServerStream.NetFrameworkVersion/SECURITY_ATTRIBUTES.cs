using System.Runtime.InteropServices;
using System.Security;

namespace System.IO.Pipes
{
    [StructLayout(LayoutKind.Sequential)]
    internal class SECURITY_ATTRIBUTES
    {
        internal int nLength;
        [SecurityCritical]
        internal unsafe byte* pSecurityDescriptor;
        internal int bInheritHandle;
    }
}

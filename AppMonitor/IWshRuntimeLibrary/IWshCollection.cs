using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AppMonitor.IWshRuntimeLibrary
{
    [ComImport]
    [DefaultMember("Item")]
    [Guid("F935DC27-1CF0-11D0-ADB9-00C04FD58A0B")]
    [TypeIdentifier]
    public interface IWshCollection : IEnumerable
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [DispId(0)]
        [return: MarshalAs(UnmanagedType.Struct)]
        object Item([In][MarshalAs(UnmanagedType.Struct)] ref object Index);
    }
}

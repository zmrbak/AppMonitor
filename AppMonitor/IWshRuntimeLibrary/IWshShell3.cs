using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AppMonitor.IWshRuntimeLibrary
{
	[ComImport]
	[Guid("41904400-BE18-11D3-A28B-00104BD35090")]
	[TypeIdentifier]
	public interface IWshShell3 : IWshShell2
	{
		[DispId(100)]
		IWshCollection SpecialFolders
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			[DispId(100)]
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}

		void _VtblGap1_3();

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1002)]
		[return: MarshalAs(UnmanagedType.IDispatch)]
		object CreateShortcut([In][MarshalAs(UnmanagedType.BStr)] string PathLink);
	}
}

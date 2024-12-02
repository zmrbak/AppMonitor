using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AppMonitor.IWshRuntimeLibrary
{
    [ComImport]
    [Guid("41904400-BE18-11D3-A28B-00104BD35090")]
    [CoClass(typeof(object))]
    [TypeIdentifier]
    public interface WshShell : IWshShell3
    {
    }
}

using System;
using System.Runtime.InteropServices;

namespace AppMonitor.MouseKeyboardLibrary
{
    public class Utility
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ShowWindowAsync(IntPtr hwnd, int nCmdShow);
    }
}

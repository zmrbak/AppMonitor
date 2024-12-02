using AppMonitor.IWshRuntimeLibrary;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AppMonitor
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            //单进程运行
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);
            foreach (Process process in processes)
            {
                if (process.Id != current.Id)
                {
                    if (process.MainModule.FileName == current.MainModule.FileName)
                    {
                        return;
                    }
                }
            }

            try
            {
                //创建自启动快捷方式
                //%AppData%\Microsoft\Windows\Start Menu\Programs\Startup（当前用户的启动文件夹）
                string pathLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "AppMonitor.lnk");
                if (File.Exists(pathLink) == false)
                {
                    WshShell wshShell = (WshShell)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")));
                    IWshShortcut obj = (IWshShortcut)(dynamic)wshShell.CreateShortcut(pathLink);
                    obj.Description = "AppMonitor 监控某程序自动运行";
                    obj.TargetPath = Assembly.GetExecutingAssembly().Location;
                    obj.Save();
                }
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}

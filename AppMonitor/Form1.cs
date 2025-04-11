using AppMonitor.MouseKeyboardLibrary;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml;

namespace AppMonitor
{
    public partial class Form1 : Form
    {
        //设置定时器
        private System.Timers.Timer timer1;
        //配置文件
        private string AppDataPath;
        string appConfigJsonFile;
        //配置参数
        private AppConfig appConfig;
        //鼠标Hook
        private MouseHook mouseHook;
        //键盘Hook
        private KeyboardHook keyboardHook;
        //上次键盘鼠标输入时间
        private DateTime LastTime;
        //鼠标键盘状态，是否输入过？
        //默认动过
        private Boolean IsMouseKeyDirtied = true;
        //监视配置文件的改动
        private FileSystemWatcher watcher;
        //进程名称
        private string processName;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //加载配置文件
            LoadConfigFile();

            //启动鼠标HooK
            mouseHook = new MouseHook();
            mouseHook.MouseMove += new MouseEventHandler(KeyboardHook_Action);
            mouseHook.MouseDown += new MouseEventHandler(KeyboardHook_Action);
            mouseHook.MouseUp += new MouseEventHandler(KeyboardHook_Action);
            mouseHook.MouseWheel += new MouseEventHandler(KeyboardHook_Action);
            mouseHook.Start();

            //启动键盘HooK
            keyboardHook = new KeyboardHook();
            keyboardHook.KeyDown += new KeyEventHandler(KeyboardHook_Action);
            keyboardHook.KeyUp += new KeyEventHandler(KeyboardHook_Action);
            keyboardHook.KeyPress += new KeyPressEventHandler(KeyboardHook_Action);
            keyboardHook.Start();

            //启动定时器

            timer1 = new System.Timers.Timer();
            timer1.Interval = 1000;
            timer1.Enabled = true;
            timer1.Elapsed += new System.Timers.ElapsedEventHandler(Timer_TimesUp);
            timer1.Start();


            //当前时间
            LastTime = DateTime.Now;

            //检测当前配置文件
            watcher = new FileSystemWatcher();
            watcher.Path = AppDataPath;
            watcher.IncludeSubdirectories = false;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*.json";
            watcher.Changed += Watcher_Changed;
            watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            //重新加载配置文件
            LoadConfigFile();
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        private void LoadConfigFile()
        {
            //确定路径存在
            if (String.IsNullOrEmpty(AppDataPath))
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                AssemblyProductAttribute product = (AssemblyProductAttribute)assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];
                AppDataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Zmrbak", product.Product);

                //如果路径不存在，则创建路径，获取AccessToken
                if (Directory.Exists(AppDataPath) == false)
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                //配置文件
                appConfigJsonFile = Path.Combine(AppDataPath, "AppMonitor.Json");
            }


            //读取配置
            if (File.Exists(appConfigJsonFile))
            {
                //从配置文件中获取参数
                appConfig = new JavaScriptSerializer().Deserialize<AppConfig>(File.ReadAllText(appConfigJsonFile));

                // 设置任务栏自动隐藏
                if (appConfig.IsTaskbarAutoHide == true)
                {
                    TaskbarAutoHide taskbarAutoHide = new TaskbarAutoHide();
                    taskbarAutoHide.SetTaskbarState(TaskbarAutoHide.AppBarStates.AutoHide);
                }
            }
            else
            {
                //选择默认浏览器
                RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(@"http\shell\open\command\");
                string appRegistryKeyValue = registryKey.GetValue("").ToString().Trim();

                //第一个双引号，到第二个双引号之间的字符串
                int startIndex = appRegistryKeyValue.IndexOf("\"");
                string appString = appRegistryKeyValue.Substring(startIndex + 1, appRegistryKeyValue.IndexOf("\"", startIndex + 2) - 1);

                if (File.Exists(appString))
                {
                    appConfig = new AppConfig
                    {
                        Application = appString
                    };
                }
                else
                {
                    //默认配置文件
                    appConfig = new AppConfig
                    {
                        Application = @"请替换为要监视的应用程序的绝对路径",
                    };
                }
                //将默认配置写入磁盘
                File.WriteAllText(appConfigJsonFile, new JavaScriptSerializer().Serialize(appConfig));

                //调用记事本，打开配置文件，准备修改
                Process.Start("notepad.exe", appConfigJsonFile);

                Application.Exit();
            }
        }

        /// <summary>
        /// 遇到键盘鼠标输入，则重新计算时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void KeyboardHook_Action(object sender, EventArgs e)
        {
            LastTime = DateTime.Now;

            //鼠标或键盘动过了，设置标志
            IsMouseKeyDirtied = true;
        }

        /// <summary>
        /// 定时器更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_TimesUp(object sender, System.Timers.ElapsedEventArgs e)
        {
            //如果进程没有启动，则启动它
            if (IsProcessRunning() == false)
            {
                timer1.Stop();

                //启动进程
                StartProcess();

                //重启定时器
                timer1.Start();
                LastTime = DateTime.Now;

                //鼠标键盘，自从程序重启后未输入过
                IsMouseKeyDirtied = false;

                //窗口最小化
                this.Invoke((EventHandler)delegate { WindowState = FormWindowState.Minimized; Hide(); });

                return;
            }

            //已空闲时间(秒)
            int idleSeconds = (int)((DateTime.Now - LastTime).TotalSeconds);
            this.Invoke((EventHandler)delegate { this.label1.Text = "空闲时间：" + idleSeconds.ToString() + "/" + appConfig.IdleTime; });

            //未超时，不处理
            if (idleSeconds < appConfig.IdleTime) { return; }

            //鼠标键盘没动，则不执行
            if (IsMouseKeyDirtied == false) { return; }

            //鼠标键盘有输入过，执行下列操作
            //暂停定时器
            timer1.Stop();
            this.Invoke((EventHandler)delegate { this.label1.Text = "结束进程！"; });

            //结束指定进程
            ShutdownProcess();
            this.Invoke((EventHandler)delegate { this.label1.Text = "重启进程！"; });

            //重启定时器
            timer1.Start();
            LastTime = DateTime.Now;

            //窗口最小化
            this.Invoke((EventHandler)delegate { WindowState = FormWindowState.Minimized; Hide(); });
        }

        /// <summary>
        /// 指定进程是否已经启动
        /// </summary>
        /// <returns></returns>
        private bool IsProcessRunning()
        {
            var processes = Process.GetProcesses();
            foreach (var item in processes)
            {
                try
                {
                    if (item.MainModule.FileName == appConfig.Application)
                    {
                        if (item.MainWindowHandle != IntPtr.Zero)
                        {
                            return true;
                        }
                    }
                }
                catch { }
            }
            return false;
        }

        /// <summary>
        /// 结束进程
        /// </summary>
        private void ShutdownProcess()
        {
            if (appConfig.IdleTime == 0) return;

            if (processName != null)
            {
                var processes1 = Process.GetProcessesByName(processName);
                foreach (var item in processes1)
                {
                    if (item.MainWindowHandle != IntPtr.Zero)
                    {
                        try
                        {
                            item.CloseMainWindow();
                        }
                        catch { }
                        ;
                    }
                }
            }

            //二次清理
            var processes = Process.GetProcesses();
            foreach (var item in processes)
            {
                try
                {
                    if (item.MainModule.FileName == appConfig.Application)
                    {
                        if (item.MainWindowHandle == IntPtr.Zero)
                        {
                            try
                            {
                                item.Kill();
                            }
                            catch { }
                            ;
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 启动进程
        /// </summary>
        private void StartProcess()
        {
            Process process = new Process();
            process.StartInfo.FileName = appConfig.Application;
            process.StartInfo.Arguments = appConfig.StartParam;
            process.Start();

            if (processName == null)
            {
                processName = process.ProcessName;
            }

            int breakLoop = 0;
            while (true)
            {
                if (breakLoop++ > 10) break;
                Process[] processes = Process.GetProcessesByName(processName);
                foreach (var item in processes)
                {
                    if (item.MainWindowHandle != IntPtr.Zero)
                    {
                        //设置前置
                        Utility.SetForegroundWindow(item.MainWindowHandle);
                        //设置最大化
                        Utility.ShowWindowAsync(item.MainWindowHandle, 3);
                        return;
                    }
                }
                Thread.Sleep(200);
            }
            ;
        }

        /// <summary>
        /// 双击通知栏，恢复窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// 点击窗口后，窗口最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Hide();
        }

        /// <summary>
        /// 双击图标后，显示窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// 关闭程序时，释放HOOK
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                notifyIcon1.Visible = false;

                timer1.Stop();
                mouseHook.Stop();
                keyboardHook.Stop();

                //鼠标Hook
                mouseHook.MouseMove -= new MouseEventHandler(KeyboardHook_Action);
                mouseHook.MouseDown -= new MouseEventHandler(KeyboardHook_Action);
                mouseHook.MouseUp -= new MouseEventHandler(KeyboardHook_Action);
                mouseHook.MouseWheel -= new MouseEventHandler(KeyboardHook_Action);

                //键盘HooK
                keyboardHook.KeyDown -= new KeyEventHandler(KeyboardHook_Action);
                keyboardHook.KeyUp -= new KeyEventHandler(KeyboardHook_Action);
                keyboardHook.KeyPress -= new KeyPressEventHandler(KeyboardHook_Action);

                //定时器
                timer1.Elapsed -= new System.Timers.ElapsedEventHandler(Timer_TimesUp);
            }
            catch { }
        }
    }
}

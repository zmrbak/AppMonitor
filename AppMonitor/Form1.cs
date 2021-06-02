using AppMonitor.MouseKeyboardLibrary;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Timers;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AppMonitor
{
    public partial class Form1 : Form
    {
        //设置定时器
        System.Timers.Timer timer1;
        System.Timers.Timer timer2;
        Boolean AppStarting = false;

        //配置文件
        string AppDataPath;
        string appConfigJsonFile;

        //配置参数
        AppConfig appConfig;

        //鼠标Hook
        MouseHook mouseHook;

        //键盘Hook
        KeyboardHook keyboardHook;

        //上次键盘鼠标输入时间
        DateTime LastTime;

        //鼠标键盘状态，是否输入过？
        //默认动过
        Boolean IsMouseKeyDirtied = true;

        //监视配置文件的改动
        FileSystemWatcher watcher;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //只允许启动一个
            CheckSelfRunning();

            //检查快捷方式
            CheckAutoStart();

            //加载配置文件
            LoadConfigFile();

            //启动鼠标HooK
            mouseHook = new MouseHook();
            mouseHook.MouseMove += new MouseEventHandler(keyboardHook_Action);
            mouseHook.MouseDown += new MouseEventHandler(keyboardHook_Action);
            mouseHook.MouseUp += new MouseEventHandler(keyboardHook_Action);
            mouseHook.MouseWheel += new MouseEventHandler(keyboardHook_Action);
            mouseHook.Start();

            //启动键盘HooK
            keyboardHook = new KeyboardHook();
            keyboardHook.KeyDown += new KeyEventHandler(keyboardHook_Action);
            keyboardHook.KeyUp += new KeyEventHandler(keyboardHook_Action);
            keyboardHook.KeyPress += new KeyPressEventHandler(keyboardHook_Action);
            keyboardHook.Start();

            //启动定时器
            timer1 = new System.Timers.Timer();
            timer1.Interval = appConfig.CheckInterval;
            timer1.Enabled = true;
            timer1.Elapsed += new System.Timers.ElapsedEventHandler(Timer_TimesUp);
            timer1.Start();

            timer2 = new System.Timers.Timer();
            timer2.Interval = appConfig.MonitorInterval;
            timer2.Enabled = true;
            timer2.Elapsed += new System.Timers.ElapsedEventHandler(Timer_TimesUp2);
            timer2.Start();

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

        /// <summary>
        /// 定时检测进程是否启动，如果没启动，则启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_TimesUp2(object sender, ElapsedEventArgs e)
        {
            //如果进程未启动，则立即启动进程
            Process[] processes = Process.GetProcesses();
            Boolean appStarted = false;
            foreach (var item in processes)
            {
                try
                {
                    if (item.MainModule.FileName.ToString() == appConfig.Application)
                    {
                        appStarted = true;
                        break;
                    }
                }
                catch { }
            }

            if (appStarted == false)
            {
                //启动进程
                StartApplicatoin();
            }
        }

        /// <summary>
        /// 检查正在运行的副本，结束其他副本
        /// </summary>
        private void CheckSelfRunning()
        {
            Process[] processes = Process.GetProcesses();
            foreach (var item in processes)
            {
                try
                {
                    if (item.MainModule.FileName.ToString() == Application.ExecutablePath)
                    {
                        //排除自己
                        if (item.Id == Process.GetCurrentProcess().Id)
                        {
                            continue;
                        }

                        //结束其他进程
                        item.Kill();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 检查注册表，设置自动启动
        /// </summary>
        private void CheckAutoStart()
        {
            string appName = "AppMonitor";
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (registryKey.GetValueNames().Contains(appName))
            {
                if (registryKey.GetValue(appName).ToString() == Application.ExecutablePath)
                {
                    registryKey.Close();
                    return;
                }
            }

            //路径不对，删除此项,需要权限
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
            if (windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator) == false)
            {
                MessageBox.Show("首次执行，请用管理员权限运行！");

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = Application.ExecutablePath;

                //设置启动动作,确保以管理员身份运行
                startInfo.Verb = "runas";
                try
                {
                    Process.Start(startInfo);
                }
                catch { }

                //退出
                Application.Exit();
                return;
            }

            //以管理员权限运行，设置注册表后，程序退出！
            //创建注册表
            registryKey.SetValue(appName, Application.ExecutablePath);
            registryKey.Close();

            //退出
            MessageBox.Show("设置完毕，请双击重新启动程序！");
            Application.Exit();
            return;
        }

        /// <summary>
        /// 监控配置文件，一旦配置文件改变，则重新加载配置文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            //重新加载配置文件
            LoadConfigFile();

            //重置计时器参数
            timer1.Interval = appConfig.CheckInterval;
            timer2.Interval = appConfig.MonitorInterval;
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
                appConfigJsonFile = Path.Combine(AppDataPath, "AppConfig.Json");
            }


            //读取配置
            if (File.Exists(appConfigJsonFile))
            {
                //从配置文件中获取参数
                appConfig = new JavaScriptSerializer().Deserialize<AppConfig>(File.ReadAllText(appConfigJsonFile));
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
                        Application = appString,
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
            }
        }

        /// <summary>
        /// 遇到键盘鼠标输入，则重新计算时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void keyboardHook_Action(object sender, EventArgs e)
        {
            LastTime = DateTime.Now;

            //鼠标或键盘动过了，设置标志
            IsMouseKeyDirtied = true;
        }

        /// <summary>
        /// 定时器更新，监控指定进程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_TimesUp(object sender, System.Timers.ElapsedEventArgs e)
        {
            //已空闲时间(秒)
            int idleSeconds = (int)((DateTime.Now - LastTime).TotalSeconds);

            this.Invoke((EventHandler)delegate
            {
                this.label1.Text = "空闲时间：" + idleSeconds.ToString() + "/" + appConfig.IdleTime;
            });

            //未超时，不处理
            if (idleSeconds < appConfig.IdleTime)
            {
                return;
            }

            //鼠标键盘没动，则不执行
            if (IsMouseKeyDirtied == false)
            {
                return;
            }

            //鼠标键盘有输入过，执行下列操作
            //暂停定时器
            timer1.Stop();

            this.Invoke((EventHandler)delegate
            {
                this.label1.Text = "结束进程！";
            });

            //结束指定进程
            var processes = Process.GetProcesses();
            foreach (var item in processes)
            {
                try
                {
                    if (item.MainModule.FileName.ToString() == appConfig.Application)
                    {
                        item.Kill();
                    }
                }
                catch { }
            }

            this.Invoke((EventHandler)delegate
            {
                this.label1.Text = "重启进程！";
            });

            StartApplicatoin();
        }

        /// <summary>
        /// 启动应用程序
        /// </summary>
        private void StartApplicatoin()
        {
            //拒绝启动多个
            if (AppStarting == true)
            {
                return;
            }
            else
            {
                AppStarting = true;
            }

            //启动进程
            Process.Start(appConfig.Application);

            //重启定时器
            timer1.Start();
            timer2.Start();

            LastTime = DateTime.Now;

            //鼠标键盘，自从程序重启后未输入过
            IsMouseKeyDirtied = false;

            //窗口最小化
            this.Invoke((EventHandler)delegate
            {
                WindowState = FormWindowState.Minimized;
                Hide();
            });

            AppStarting = false;
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
            timer1?.Stop();
            mouseHook?.Stop();
            keyboardHook?.Stop();

            //鼠标Hook
            if (mouseHook == null) return;
            mouseHook.MouseMove -= new MouseEventHandler(keyboardHook_Action);
            mouseHook.MouseDown -= new MouseEventHandler(keyboardHook_Action);
            mouseHook.MouseUp -= new MouseEventHandler(keyboardHook_Action);
            mouseHook.MouseWheel -= new MouseEventHandler(keyboardHook_Action);

            //键盘HooK
            if (keyboardHook == null) return;
            keyboardHook.KeyDown -= new KeyEventHandler(keyboardHook_Action);
            keyboardHook.KeyUp -= new KeyEventHandler(keyboardHook_Action);
            keyboardHook.KeyPress -= new KeyPressEventHandler(keyboardHook_Action);

            //定时器
            timer1.Elapsed -= new System.Timers.ElapsedEventHandler(Timer_TimesUp);
        }
    }
}

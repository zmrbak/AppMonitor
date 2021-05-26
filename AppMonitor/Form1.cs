using AppMonitor.MouseKeyboardLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AppMonitor
{
    public partial class Form1 : Form
    {
        //设置定时器
        System.Timers.Timer timer1;

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
                //获取当前计算机的用户名
                WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
                string userName = windowsIdentity.Name.Substring(windowsIdentity.Name.LastIndexOf("\\") + 1);

                //默认配置文件
                appConfig = new AppConfig
                {
                    Application = @"C:\Users\" + userName + @"\AppData\Roaming\secoresdk\360se6\Application\360se.exe",
                    IdleTime = 120
                };

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
        /// 定时器更新
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
            Process[] processes = Process.GetProcesses();
            foreach (var item in processes)
            {
                try
                {
                    if (item.MainModule.FileName.ToString() == appConfig.Application)
                    {
                        item.Kill();
                    }
                }
                catch
                {
                }
            }

            this.Invoke((EventHandler)delegate
            {
                this.label1.Text = "重启进程！";
            });

            //启动进程
            Process.Start(appConfig.Application);

            //重启定时器
            timer1.Start();
            LastTime = DateTime.Now;

            //鼠标键盘，自从程序重启后未输入过
            IsMouseKeyDirtied = false;

            //窗口最小化
            this.Invoke((EventHandler)delegate
            {
                WindowState = FormWindowState.Minimized;
                Hide();
            });
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
            timer1.Stop();
            mouseHook.Stop();
            keyboardHook.Stop();

            //鼠标Hook
            mouseHook.MouseMove -= new MouseEventHandler(keyboardHook_Action);
            mouseHook.MouseDown -= new MouseEventHandler(keyboardHook_Action);
            mouseHook.MouseUp -= new MouseEventHandler(keyboardHook_Action);
            mouseHook.MouseWheel -= new MouseEventHandler(keyboardHook_Action);

            //键盘HooK
            keyboardHook.KeyDown -= new KeyEventHandler(keyboardHook_Action);
            keyboardHook.KeyUp -= new KeyEventHandler(keyboardHook_Action);
            keyboardHook.KeyPress -= new KeyPressEventHandler(keyboardHook_Action);

            //定时器
            timer1.Elapsed -= new System.Timers.ElapsedEventHandler(Timer_TimesUp);            
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppMonitor
{
    public class AppConfig
    {
        /// <summary>
        /// 被监视的应用程序的完全路径
        /// </summary>
        public string Application;

        /// <summary>
        /// 键盘鼠标计数周期（毫秒），CheckInterval秒触发一次
        /// </summary>
        public int CheckInterval = 1000;

        /// <summary>
        /// 键盘鼠标闲置计数次数
        /// </summary>
        public int IdleTime = 120;

        /// <summary>
        /// 检查应用程序是否存活的定时器周期（毫秒），MonitorInterval秒触发一次
        /// 未检测到，则启动应用程序
        /// </summary>
        public int MonitorInterval = 4800;
    }
}

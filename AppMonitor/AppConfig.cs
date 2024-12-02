namespace AppMonitor
{
    public class AppConfig
    {
        /// <summary>
        /// 监控的应用程序
        /// </summary>
        public string Application { set; get; }
        /// <summary>
        /// 启动参数
        /// </summary>
        public string StartParam { set; get; } = " --kiosk https://lib.cdut.edu.cn --edge-kiosk-type=fullscreen";
        /// <summary>
        /// 空闲时间
        /// </summary>        
        public int IdleTime { set; get; } = 120;
    }
}

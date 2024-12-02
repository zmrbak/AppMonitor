# AppMonitor
监控指定程序：用户键盘鼠标闲置一段时间后，重启指定应用程序，并将其前置。

# 用途
用于公共桌面，自动打开指定程序，程序被用户关闭后自动打开。

程序界面被用户修改后，指定闲置时间后，自动重启程序。

# 说明
软件启动时，将检查本用户名下的自启动目录，并创建自启动快捷方式。

软件启动时，检查本用户名下的配置文件，不存在则创建，创建后使用notepad打开。

软件默认打开 Edge浏览器，添加参数，使其全屏模式。

# 配置文件

## 启动快捷方式：

%AppData%\Microsoft\Windows\Start Menu\Programs\Startup\AppMonitor.lnk

## AppMonitor配置文件:

C:\Users\[当前登录用户名]\AppData\Local\Zmrbak\AppMonitor\AppMonitor.Json

该文件默认不存在，创建后使用notepad打开，也可后期按照需求手动修改。

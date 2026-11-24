# TinyKeys

一个白色小面板，用鼠标完成 B 站网页视频的常用快捷键操作。使用轻量的 WinForms / .NET Framework 4.8 实现，避免 WPF 的冷启动开销。

- **←**：左方向键（后退）
- **空格**：播放 / 暂停
- **→**：右方向键（前进）
- 鼠标滚轮停在右侧音量区：调节 Windows 系统音量
- 按住按钮 = 保持对应按键按下；松开鼠标才释放
- 右上角支持 **置顶（图钉）**、**最小化** 和 **关闭**

![界面](./docs/assets/ui-preview.png)

## 运行

从源码运行（需要 .NET SDK；项目目标为 .NET Framework 4.8）：

```powershell
dotnet run --project src\TinyKeys\TinyKeys.csproj
```

直接运行已构建的版本：

```powershell
dist\TinyKeys.exe
```

发布物是**单个 exe**，使用 Windows 自带的 .NET Framework 4.8（Windows 10 / 11 默认已安装）。不需要额外的 `.config` 或运行时文件；高 DPI 声明已内嵌在 exe 的 manifest 中。构建输出目录 `dist/` 不提交到仓库。

> v1.2.0 把界面从 WPF / .NET 8 改为 WinForms / .NET Framework 4.8。之前的冷启动日志显示约 8 秒主要耗在 WPF / .NET 8 的加载和 XAML 初始化上；WinForms 版本当前日志中的首帧时间约为 0.2 秒级。v1.2.1 起去掉了自动生成的 `TinyKeys.exe.config`，整个程序只有一个 exe。

## 启动诊断日志

每次启动会追加一份启动阶段日志到这里（v1.2.0 起记录 WinForms 阶段）：

```text
%LOCALAPPDATA%\TinyKeys\logs\startup.log
```

日志只包含时间戳、阶段名称、耗时、Windows/.NET 版本，不包含按键、窗口标题或浏览内容。如果之后仍然遇到冷启动变慢，把整个 `startup.log` 发出来即可定位耗时发生在：

- 进程启动到 `Program.Main`：CLR / WinForms 初始化和进程创建
- `MainForm ctor`：字体、按钮和自绘控件创建
- `OnHandleCreated` / `Window placement`：窗口句柄与初始位置
- `OnLoad` / `OnShown` / `first paint`：窗口显示和首帧绘制
- `Volume wheel hook installed`：全局鼠标钩子安装（已延后到首帧之后）

如果日志里的 `Process start` 离你双击的时间已经过了数秒，而第一行 `sinceProcess` 很小，说明延迟发生在 TinyKeys 进程创建之前（例如 Windows/安全软件扫描）。日志超过 512 KB 会自动轮换为 `startup.old.log`。

## 使用

1. 先点击一下 B 站视频页面，让浏览器成为最近使用的窗口。
2. 点击面板上的按钮；面板会短暂把浏览器切回前台并发送按键。
3. 按住左 / 右按钮会保持方向键按下，B 站会触发原生行为：左键快速回退，右键 2 倍速播放，松开鼠标后停止。
4. 移动鼠标到右侧音量区，上下滚动滚轮调节系统音量。
5. 拖动顶部标题区域移动面板；右上角图钉切换是否悬浮在最上层，`−` 最小化，`×` 退出。

默认不置顶；点图钉后按钮变粉色，面板会悬浮在其他窗口之上，再点一次取消。置顶状态只对当前运行有效，不写入配置。

如果 B 站窗口位于某块显示器，启动时面板会优先放到另一块显示器中央；否则放在鼠标所在显示器中央。

## 长按的实现

B 站的“长按右键 2 倍速”依赖的是按键保持状态，而不是连续点击。因此 TinyKeys 模拟的是物理键盘的完整按住过程：

- 鼠标按下时发送一次 `keydown`；
- 按住期间按 Windows 键盘重复延迟和速率继续发送 `keydown`，不发送 `keyup`；
- 松开鼠标时才发送一次 `keyup`。

这样 B 站会把整个按住过程识别为一次长按，而不是很多次短按。短按仍然是快速按下并释放。

**左键和右键共用同一套按住逻辑**，只有发送的虚拟键码不同（左键 `VK_LEFT`、右键 `VK_RIGHT`）。不存在左键一套、右键另一套的实现；B 站自己在收到按住的方向键后决定是快速回退还是 2 倍速。

参考资料：

- [B站新功能：键盘快捷操作！「短按快进」「长按倍速」](https://sj.qq.com/blog/1746605207000)
- [V2EX：B 站桌面端长按右键激活二倍速](https://global.v2ex.co/t/808104)
- [Golden-Right：按住键倍速、松开恢复的实现](https://github.com/SkyJinXX/Golden-Right)
- [B站快捷键说明：左/右键短按跳转，长按右键倍速、长按左键快速回退](https://blog.csdn.net/qq_37860634/article/details/124464965)

## 说明

- 只向浏览器窗口发送按键（Chrome、Edge、Firefox、Brave、Vivaldi、Opera、Chromium）。若最近的前台不是浏览器，底部会提示先点一下 B 站。
- 不读取网页、不注入脚本、不安装浏览器扩展、不联网。
- 音量是 Windows 系统音量，不是 B 站播放器内部音量。
- 仅支持已经获得焦点的 B 站视频页面；如果键盘焦点在地址栏或输入框，模拟按键会进入那个控件。
- WinForms 版本使用 GDI+ 自绘白色卡片和控件，不依赖 WPF；窗口只请求系统原生 `CS_DROPSHADOW`，不额外引入分层窗口，阴影是否显示由 Windows 决定。
- 发布目录只有一个 `TinyKeys.exe`，没有 `TinyKeys.exe.config`；之前的旧配置文件可以直接删除。

## 构建

```powershell
dotnet build TinyKeys.sln -c Release
dotnet publish src\TinyKeys\TinyKeys.csproj -c Release -o dist
```

设计说明见 [docs/DESIGN.md](docs/DESIGN.md)。

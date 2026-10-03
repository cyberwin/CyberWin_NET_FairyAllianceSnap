# 未来之窗・FairyAllianceSnap
![输入图片说明](dstScreenShot_2026-10-03_182534_585.jpg)
![输入图片说明](dst/ScreenShot_2026-10-03_182456_683.jpg)

> 
> CyberWin 系列 Windows 开源截图工具，轻量本地屏幕捕获组件，`.NET` 开发，可独立运行，也可集成至其他 CyberWin 应用。

Gitee 仓库：[https://gitee.com/cybersnow/CyberWin_NET_FairyAllianceSnap](https://gitee.com/cybersnow/CyberWin_NET_FairyAllianceSnap)

## 📖 项目介绍

**FairyAllianceSnap（未来之窗截图）**，属于 CyberWin 开源套件下的 Windows 屏幕截图模块。
基于 .NET 实现，专注 Windows 平台屏幕捕获；全部逻辑本地运行，不上传截图数据，注重隐私安全。
既可作为独立截图小工具直接使用，也可以作为程序库嵌入其他 .NET 桌面项目，快速给软件增加截图能力。

适用场景：办公截图、文档素材采集、程序内部截图能力扩展、桌面工具二次开发。

## ✨ 功能特性

- ✅ Windows 平台屏幕捕获，支持区域、窗口、全屏截图
- ✅ 截图输出至剪贴板 / 本地文件
- ✅ .NET 组件化设计，支持第三方项目调用集成
- ✅ 轻量，无多余后台服务，开箱即用
- ✅ 完全开源，代码透明，截图数据仅保存在本机
- ✅ 适配 Win10 / Win11 主流系统

## 🧱 软件架构

```
src/
└── CyberWin.NET.FairyAllianceSnap
    └── CyberWinPHP
        └── APP
            └── CyberWin_SnapApp.cs      # 截图主应用入口
```

- 主体：C# .NET 桌面库，`CyberWin_SnapApp.cs` 为截图核心业务类
- 运行环境：.NET Framework /.NET
- 输出：可编译独立 exe，也可编译类库供其他项目引用

## 📦 安装教程

### 方式 1：直接使用编译发布包

1. 进入本仓库 **发行版 (release)**
2. 下载最新 `win截图` 发行压缩包
3. 解压后直接运行 `FairyAllianceSnap.exe`

### 方式 2：源码编译

1. Clone 仓库

```
git clone https://gitee.com/cybersnow/CyberWin_NET_FairyAllianceSnap.git
```

2. 使用 Visual Studio 打开解决方案
3. 还原 .NET 依赖
4. 直接编译生成可执行程序

## 🚀 使用说明

### 独立程序使用

1. 运行 `FairyAllianceSnap.exe`
2. 唤起截图模式，框选需要捕获的屏幕区域
3. 确认截图，图片可复制到剪贴板，或保存到本地磁盘

### 作为组件集成到你的 .NET 项目

引用项目源码，调用核心类 `CyberWin_SnapApp`，即可在你的软件内部唤起截图能力。

```
// 伪代码示例
var snap = new CyberWin_SnapApp();
snap.StartCapture();
```

> 
> 具体 API 后续版本完善补充。

## ⚠️ 注意事项

1. 仅支持 Windows 系统；不支持 Linux / MacOS
2. 截图全部在本地处理，**不会自动上传任何图片**
3. 高 DPI 多显示器环境，建议开启系统应用 DPI 适配

## 🤝 参与贡献

欢迎提交 Issue、PR 参与改进本项目：

1. Fork 本仓库
2. 新建 `Feat_xxx` / `Fix_xxx` 分支
3. 修改代码，提交 commit
4. 提交 Pull Request，描述改动内容

## 📋 开发计划（Roadmap）

- 截图标注工具（箭头、文字、马赛克）
- 自定义全局快捷键
- OCR 文字识别扩展
- 长截图滚动捕获支持
- 完善对外调用 API 文档
/*
============================================
FairyAlliance Snap 软件说明
版本：NET Framework 4.5
兼容系统：Windows7 / Windows10 / Windows11
作者：东方仙盟
配置文件：FairyAllianceSnap.config.ini（放在exe同目录）
============================================
功能简介：
1. 托盘后台运行全局截图工具，支持全局快捷键，快捷键最多支持2个修饰键(Ctrl/Shift/Alt)+主键
2. 截图规则：
   默认保存目录：程序目录\FairyAllianceSnap\yyyy-MM（按月自动新建文件夹）
   可自定义保存目录，自定义目录优先级高于默认目录
   文件命名：snap_年月日时分秒_前台窗口标题.png，自动清理非法字符，支持外文系统不乱码
3. 标注工具（截图后弹出）：
   矩形框、圆角矩形、椭圆、铅笔自由手绘、文字标注
   支持：线条粗细选择、拾色器、字体/字号选择、撤销上一步
   标注窗口两个保存按钮：
      ✅【对勾】：自动保存到配置目录，直接归档
      💾【保存】：弹出窗口，自选路径另存图片
4. 设置项【快捷键模式直接保存】：
   勾选后，按下快捷键截图会直接保存图片，**不会弹出标注窗口，不能绘制标注**
   不勾选：快捷键截图同样弹出标注界面，可编辑标注
   【托盘菜单手动截图】永远弹出标注界面，不受这个选项控制
5. 热键冲突检测，若快捷键被占用会弹出提示
6. 配置持久化：所有设置保存至 FairyAllianceSnap.config.ini，重启程序自动加载
============================================
*/
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.IO;
 
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CyberWin.NET.FairyAllianceSnap.CyberwinPHP;
using CyberWin.NET.FairyAllianceSnap.CyberwinPHP.Cyber_Dialog;








namespace CyberWin.NET.FairyAllianceSnap.CyberWinPHP.APP
{
   
    public class CyberWin_SnapApp
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private string _customSavePath = string.Empty;

        // 热键配置
        private Keys _hotKeyMain = Keys.F8;
        private HotkeyModifiers _hotKeyMod = HotkeyModifiers.None;
        private readonly int _hotKeyId = 9001;
        private bool _hotKeyRegistered = false;
        private MessageWindow _msgWindow;

        // 快捷键直接保存（跳过标注）
        private bool _hotkeyDirectSave = false;

        [Flags]
        public enum HotkeyModifiers
        {
            None = 0,
            Alt = 0x0001,
            Ctrl = 0x0002,
            Shift = 0x0004
        }

        #region Win32 API
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);
        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);
        #endregion

        public CyberWin_SnapApp()
        {
            // 启动加载INI配置
            LoadConfig();

            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = SystemIcons.Application;
            _trayIcon.Text = "东方仙盟 FA Snap Tool";
            _trayIcon.Visible = true;

            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Start Capture / 开始截图", null, (s, e) => StartSelectCapture(false));
            _trayMenu.Items.Add("Settings / 设置", null, (s, e) => ShowSettingDialog());
            _trayMenu.Items.Add("Exit / 退出", null, (s, e) => ExitApp());
            _trayMenu.Items.Add("捐款", null, (s, e) => fa_东方仙盟捐款());
            _trayMenu.Items.Add("下载源码", null, (s, e) => fa_开源地址());
            _trayMenu.Items.Add("东方仙盟", null, (s, e) => fa_东方仙盟());
            _trayIcon.ContextMenuStrip = _trayMenu;

            _msgWindow = new MessageWindow();
            _msgWindow.HotKeyPressed += () => StartSelectCapture(_hotkeyDirectSave);
            ReRegisterHotkey();

            cyber_增加浏览器控制();
        }

        /// <summary>读取ini配置</summary>
        private void LoadConfig()
        {
            _customSavePath = CyberWin_IniHelper.Read("CustomSavePath");
            string modStr = CyberWin_IniHelper.Read("HotkeyMod", "0");
            _hotKeyMod = (HotkeyModifiers)int.Parse(modStr);
            string mainKeyStr = CyberWin_IniHelper.Read("HotkeyMain", "F8");
            Enum.TryParse(mainKeyStr, out _hotKeyMain);
            string directSaveStr = CyberWin_IniHelper.Read("HotkeyDirectSave", "false");
            bool.TryParse(directSaveStr, out _hotkeyDirectSave);
        }

        /// <summary>保存配置写入ini</summary>
        private void SaveConfig()
        {
            CyberWin_IniHelper.Write("CustomSavePath", _customSavePath);
            CyberWin_IniHelper.Write("HotkeyMod", ((int)_hotKeyMod).ToString());
            CyberWin_IniHelper.Write("HotkeyMain", _hotKeyMain.ToString());
            CyberWin_IniHelper.Write("HotkeyDirectSave", _hotkeyDirectSave.ToString().ToLower());
        }

        private bool ReRegisterHotkey()
        {
            if (_hotKeyRegistered)
            {
                UnregisterHotKey(_msgWindow.Handle, _hotKeyId);
                _hotKeyRegistered = false;
            }
            uint mod = (uint)_hotKeyMod;
            uint key = (uint)_hotKeyMain;
            bool ok = RegisterHotKey(_msgWindow.Handle, _hotKeyId, mod, key);
            if (ok)
                _hotKeyRegistered = true;
            else
                MessageBox.Show("Hotkey register failed, key already occupied.\n热键注册失败，快捷键已被占用。");
            return ok;
        }

        /// <summary>获取前台窗口标题，清洗非法字符</summary>
        private string GetActiveWindowTitle()
        {
            IntPtr hwnd = GetForegroundWindow();
            int len = GetWindowTextLength(hwnd);
            if (len <= 0) return "NoTitle";
            var sb = new System.Text.StringBuilder(len + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            string title = sb.ToString();
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
                title = title.Replace(c, '_');
            return title;
        }

        /// <summary>获取最终保存目录</summary>
        private string GetFinalSaveDirectory()
        {
            if (!string.IsNullOrWhiteSpace(_customSavePath))
            {
                string dir = _customSavePath.Trim();
                if (!dir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    dir += Path.DirectorySeparatorChar;
                return dir;
            }
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            string baseFolder = Path.Combine(exeDir, "FairyAllianceSnap");
            string monthFolder = DateTime.Now.ToString("yyyy-MM");
            string fullDir = Path.Combine(baseFolder, monthFolder);
            Directory.CreateDirectory(fullDir);
            return fullDir + Path.DirectorySeparatorChar;
        }

        private string BuildFileName()
        {
            string title = GetActiveWindowTitle();
            string timeStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"snap_{timeStr}_{title}.png";
            string saveDir = GetFinalSaveDirectory();
            Directory.CreateDirectory(saveDir);
            return Path.Combine(saveDir, fileName);
        }

        /// <param name="directSave">true=直接保存跳过标注；false=弹出标注窗口</param>
        private void StartSelectCapture(bool directSave)
        {
            var selectorForm = new SelectorForm();
            if (selectorForm.ShowDialog() == DialogResult.OK)
            {
                Rectangle rect = selectorForm.SelectedRect;
                //  using (Bitmap bmp = new Bitmap(rect.Width, rect.Height))
                Bitmap bmp = new Bitmap(rect.Width, rect.Height) ;
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size);
                }

                if (directSave)
                {
                    //快捷键直接保存，跳过标注
                    string fullFile = BuildFileName();
                    bmp.Save(fullFile, System.Drawing.Imaging.ImageFormat.Png);
                    bmp.Dispose();
                }
                else
                {
                    //弹出标注窗口
                    using (var annotateForm = new AnnotateForm(bmp))
                    {
                        var res = annotateForm.ShowDialog();
                        if (res == DialogResult.OK)
                        {
                            //对勾：自动归档保存
                            string fullFile = BuildFileName();
                            annotateForm.ResultBitmap.Save(fullFile, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        else if (res == DialogResult.Yes)
                        {
                            //另存为：用户自选路径
                            using (SaveFileDialog sfd = new SaveFileDialog())
                            {
                                sfd.Filter = "PNG图片(*.png)|*.png";
                                sfd.FileName = Path.GetFileName(BuildFileName());
                                if (sfd.ShowDialog() == DialogResult.OK)
                                {
                                    annotateForm.ResultBitmap.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                                }
                            }
                        }
                    }
                    bmp.Dispose();
                }
            }
        }

        private void ShowSettingDialog()
        {
            using (var frm = new Form())
            {
                frm.Text = "Settings - FairyAlliance Snap";
                frm.Size = new Size(600, 300);
                frm.StartPosition = FormStartPosition.CenterScreen;

                frm.ShowIcon = false;

                var lblPath = new Label
                {
                    Text = "Custom Save Path (Empty = use FairyAllianceSnap\\yyyy-MM):",
                    Location = new Point(20, 12),
                    AutoSize = true
                };
                var txtPath = new TextBox
                {
                    Text = _customSavePath,
                    Location = new Point(20, 35),
                    Width = 540
                };

                var lblHotkey = new Label
                {
                    Text = "Global Hotkey (Max 2 modifiers):",
                    Location = new Point(20, 70),
                    AutoSize = true
                };

                var chkCtrl = new CheckBox { Text = "Ctrl", Location = new Point(20, 95), Checked = (_hotKeyMod & HotkeyModifiers.Ctrl) != 0 };
                var chkShift = new CheckBox { Text = "Shift", Location = new Point(80, 95), Checked = (_hotKeyMod & HotkeyModifiers.Shift) != 0 };
                var chkAlt = new CheckBox { Text = "Alt", Location = new Point(140, 95), Checked = (_hotKeyMod & HotkeyModifiers.Alt) != 0 };

                chkCtrl.Width = 50;
                chkShift.Width = 50;
                chkAlt.Width = 50;

                //互斥：最多勾选2个修饰键
                Action checkMutual = () =>
                {
                    int count = 0;
                    if (chkCtrl.Checked) count++;
                    if (chkShift.Checked) count++;
                    if (chkAlt.Checked) count++;
                    if (count > 2)
                    {
                        MessageBox.Show("Cannot select more than 2 modifiers!\n最多只能选择2个修饰键");
                    }
                };
                chkCtrl.CheckedChanged += (s, e) => checkMutual();
                chkShift.CheckedChanged += (s, e) => checkMutual();
                chkAlt.CheckedChanged += (s, e) => checkMutual();

                var cboKey = new ComboBox { Location = new Point(200, 92), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
                cboKey.Items.AddRange(new object[] { Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11 });
                cboKey.SelectedItem = _hotKeyMain;

                //新增：快捷键直接保存选项 + 备注
                var chkHotDirectSave = new CheckBox
                {
                    Text = "快捷键模式直接保存",
                    Location = new Point(20, 130),
                    Checked = _hotkeyDirectSave
                };
                var lblTipDirect = new Label
                {
                    Text = "备注：开启后截图不会弹出标注窗口，无法进行标注绘制",
                    Location = new Point(40, 155),
                    ForeColor = Color.DarkRed,
                    AutoSize = true
                };

                var btnOk = new Button
                {
                    Text = "OK",
                    Location = new Point(20, 190),
                    Width = 100
                };
                btnOk.Click += (s, e) =>
                {
                    int cnt = 0;
                    HotkeyModifiers mod = HotkeyModifiers.None;
                    if (chkCtrl.Checked) { mod |= HotkeyModifiers.Ctrl; cnt++; }
                    if (chkShift.Checked) { mod |= HotkeyModifiers.Shift; cnt++; }
                    if (chkAlt.Checked) { mod |= HotkeyModifiers.Alt; cnt++; }
                    if (cnt > 2)
                    {
                        MessageBox.Show("Too many modifiers! Max 2.\n修饰键不能超过2个");
                        return;
                    }
                    _customSavePath = txtPath.Text.Trim();
                    _hotKeyMod = mod;
                    _hotKeyMain = (Keys)cboKey.SelectedItem;
                    _hotkeyDirectSave = chkHotDirectSave.Checked;

                    //保存写入ini
                    SaveConfig();
                    ReRegisterHotkey();
                    frm.DialogResult = DialogResult.OK;
                    frm.Close();
                };
                frm.Controls.AddRange(new Control[] { lblPath, txtPath, lblHotkey, chkCtrl, chkShift, chkAlt, cboKey, chkHotDirectSave, lblTipDirect, btnOk });
                frm.ShowDialog();
            }
        }

        private void ExitApp()
        {
            if (_hotKeyRegistered)
                UnregisterHotKey(_msgWindow.Handle, _hotKeyId);
            _trayIcon.Visible = false;
            Application.Exit();
        }
        private void fa_开源地址()
        {
            string openSourceUrl = "https://gitee.com/cybersnow/CyberWin_NET_FairyAllianceSnap"; //替换你的开源地址
            

            string mac = MacHelper.GetFirstPhysicalMac();
            //  CyberWin_JsStandardPlugV2005 cjsv2005 = new CyberWin_JsStandardPlugV2005();

            //   string DeviceUUid =cjsv2005.getDeviceUUid();
            //直接静态调用，不需要new对象
            string DeviceUUid = CyberinLicHelper.Get();// 

            
            donateUrl = donateUrl + client_snv2026;
            System.Diagnostics.Process.Start(openSourceUrl);
        }
        private void fa_东方仙盟捐款()
        {
            
            string donateUrl = "http://app.ynwlzc.net/app.php/appshow/QBQ?1=1"; //替换你的捐赠地址


            string mac = MacHelper.GetFirstPhysicalMac();
            //  CyberWin_JsStandardPlugV2005 cjsv2005 = new CyberWin_JsStandardPlugV2005();

            //   string DeviceUUid =cjsv2005.getDeviceUUid();
            //直接静态调用，不需要new对象
            string DeviceUUid = CyberinLicHelper.Get();// 

            // Public_Var.未来之窗系统sn = DeviceUUid;


            //  string client_snv2026 = "&software=东方CyberWinCleanToolWin&client_mac=" + mac + "&client_sn=" + DeviceUUid;// mac_md5;
            string client_snv2026 = "&software=东方CyberWinNetRepairWin&client_mac=" + mac + "&client_sn=" + DeviceUUid;// mac_md5;

            donateUrl = donateUrl + client_snv2026;
            System.Diagnostics.Process.Start(donateUrl);
        }

        private void fa_东方仙盟()
        {
            
            string donateUrl = "http://app.ynwlzc.net/"; //替换你的捐赠地址


            string mac = MacHelper.GetFirstPhysicalMac();
            //  CyberWin_JsStandardPlugV2005 cjsv2005 = new CyberWin_JsStandardPlugV2005();

            //   string DeviceUUid =cjsv2005.getDeviceUUid();
            //直接静态调用，不需要new对象
            string DeviceUUid = CyberinLicHelper.Get();// 

            // Public_Var.未来之窗系统sn = DeviceUUid;


            //  string client_snv2026 = "&software=东方CyberWinCleanToolWin&client_mac=" + mac + "&client_sn=" + DeviceUUid;// mac_md5;
            string client_snv2026 = "&software=东方CyberWinNetRepairWin&client_mac=" + mac + "&client_sn=" + DeviceUUid;// mac_md5;

           // donateUrl = donateUrl + client_snv2026;
            System.Diagnostics.Process.Start(donateUrl);
        }
        /// <summary>
        /// lic文件完整路径：程序根目录\CleanToolWin.lic
        /// </summary>
        private static readonly string LicFilePath = Path.Combine(Application.StartupPath, "CleanToolWin.lic");
        public void cyber_增加浏览器控制()
        {
            //加入内网控制
            //
            
            string 认证服务器 = "768B81FE198CF219675F733C25EF100C80038FBCF50A0D41";
            // string url = CyberWin.CSHARP.YNWLZC.WebFrame.CyberPHP.CyberSafe._Encrpt.DESEncrypt.Decrypt(认证服务器, "20225578");
            
            //  Cyber_TokenKey.CL_Token cl = new Cyber_TokenKey.CL_Token();
            //  string mac_md5 = cl.getLocalMacMd5(true);
            //  string mac = cl.getLocalMac();
            string mac = MacHelper.GetFirstPhysicalMac();
            //  CyberWin_JsStandardPlugV2005 cjsv2005 = new CyberWin_JsStandardPlugV2005();

            //   string DeviceUUid =cjsv2005.getDeviceUUid();
            //直接静态调用，不需要new对象
            string DeviceUUid = CyberinLicHelper.Get();// 

            // Public_Var.未来之窗系统sn = DeviceUUid;

            string client_sn = "&client_mac=" + mac + "&client_sn=" + DeviceUUid;// mac_md5;

            string client_snv2026 = "&software=东方CyberWinFairyAllianceSnap&client_mac=" + mac + "&client_sn=" + DeviceUUid;// mac_md5;

            url = url + client_snv2026;

            string 认证类型 = "";// get_SetConfig("authen_type");

            if (认证类型.Length > 1)
            {
                // 认证类型 = CyberWin.CSHARP.YNWLZC.WebFrame.CyberPHP.CyberSafe._Encrpt.DESEncrypt.Decrypt(认证类型, "20225578");
                //   string 认证服务器2 = get_SetConfig("authen_svr");
                if (认证类型.Equals("company") == true)
                {
                    // url = CyberWin.CSHARP.YNWLZC.WebFrame.CyberPHP.CyberSafe._Encrpt.DESEncrypt.Decrypt(认证服务器2, "20225578");
                    //  url = url + client_sn;
                }
            }
            else
            {
                认证类型 = "";
            }


            string 强制认证 = "";// get_SetConfig("authen_force");
            bool 强制认证B = false;
            if (强制认证.Equals("1") == true)
            {
                强制认证B = true;
            }

            强制认证B = false;

            //  string sn=cl.
            //  CyberWin.CSHARP.YNWLZC.WebFrame.CyberPHP.CyberWinPC.Helper.Logger.Log_Engine.write_logV2("system", "sn", mac);
            //   CyberWin.CSHARP.YNWLZC.WebFrame.CyberPHP.CyberWinPC.Helper.Logger.Log_Engine.write_logV2("system", "snmac_md5", mac_md5);

            //  MessageBox.Show(url);

            // string authen_ret = CyberWin.CSHARP.YNWLZC.WebFrame.CyberPHP.CyberWinNet.Http.CyberHttpDownload.OpenReadWithHttpsByInnerAgent(url, "", "UTF-8", "GET");
            string authen_ret = SimpleHttp.OpenReadWithHttpsByInnerAgent(url, "", "UTF-8", "GET");
            //MessageBox.Show(authen_ret);
            //   MessageBox.Show(authen_ret);
            if (authen_ret == "wlzc_fairyalliance_callhaltnotice")
            {
                 

            }

            if (强制认证B == true)
            {
                if (authen_ret == "wlzc_open")
                {

                }
                else
                {
                    //MessageBox.Show("授权已经到期，请联打开微信《优然数字生活服务》公众号，点击查看终端授权");
                    frm_expire fe = new frm_expire(DeviceUUid);
                    if (fe.ShowDialog() == DialogResult.OK)
                    {
                        Application.Exit();
                    }
                    else
                    {

                        Application.Exit();
                    }
                }
            }
            else
            {

                if (authen_ret == "wlzc_close")
                {
                    // MessageBox.Show("授权已经到期，请联打开微信《优然数字生活服务》公众号，点击查看终端授权");
                    frm_expire fe = new frm_expire(DeviceUUid);
                    if (fe.ShowDialog() == DialogResult.OK)
                    {
                        Application.Exit();
                    }
                    else
                    {

                        Application.Exit();
                    }

                    Application.Exit();
                }
            }

        }
    }

    /// <summary>接收全局热键消息的隐藏窗口</summary>
    public class MessageWindow : Form
    {
        public Action HotKeyPressed;
        public MessageWindow()
        {
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Visible = false;
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312) // WM_HOTKEY
            {
                HotKeyPressed?.Invoke();
            }
            base.WndProc(ref m);
        }
    }

    /// <summary>全屏选区截图窗口</summary>
    public class SelectorForm_天黑了 : Form
    {
        public Rectangle SelectedRect { get; private set; }
        private Point _startPos;
        private bool _isDragging;

        private readonly Color _maskKeyColor = Color.Magenta;

        public SelectorForm_天黑了()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            //   this.BackColor = Color.FromArgb(80, Color.Black);
            //  this.Opacity = 0.6;
            // 修复核心：移除Opacity，只用TransparencyKey，杜绝“控件不支持透明背景”异常
            //     this.TransparencyKey = _maskKeyColor;
            //   this.BackColor = _maskKeyColor;
            // 使用普通纯色背景，不带Alpha，不会报透明异常；在OnPaint绘制半黑遮罩
            // this.BackColor = Color.Black;
            this.TransparencyKey = _maskKeyColor;
            this.BackColor = _maskKeyColor;

            this.Cursor = Cursors.Cross;
            this.DoubleBuffered = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _startPos = e.Location;
                SelectedRect = Rectangle.Empty;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                SelectedRect = GetRect(_startPos, e.Location);
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _isDragging)
            {
                _isDragging = false;
                if (SelectedRect.Width > 5 && SelectedRect.Height > 5)
                    this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            // 绘制半透明黑色遮罩，ARGB在这里绘制，不是写在窗体BackColor上！！
            using (SolidBrush maskBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            {
                if (_isDragging && SelectedRect.Width > 0 && SelectedRect.Height > 0)
                {
                    // 挖空选区，选区内透过去看到桌面
                    Rectangle fullScreen = this.ClientRectangle;
                    Region fullRegion = new Region(fullScreen);
                    Region selectRegion = new Region(SelectedRect);
                    fullRegion.Exclude(selectRegion);
                    g.FillRegion(maskBrush, fullRegion);
                    fullRegion.Dispose();
                    selectRegion.Dispose();

                    using (Pen pen = new Pen(Color.Red, 2))
                    {
                        g.DrawRectangle(pen, SelectedRect);
                    }
                }
                else
                {
                    // 未拖动时全屏半黑遮罩
                    g.FillRectangle(maskBrush, this.ClientRectangle);
                }
            }
        }

        private Rectangle GetRect(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int w = Math.Abs(p2.X - p1.X);
            int h = Math.Abs(p2.Y - p1.Y);
            return new Rectangle(x, y, w, h);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            /*
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
            */
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    /// <summary>全屏选区截图窗口【无全屏遮罩 + 鼠标悬停窗口预览红框，轻量化】</summary>
    public class SelectorForm : Form
    {
        public Rectangle SelectedRect { get; private set; }
        private Point _startPos;
        private bool _isDragging;
        private Rectangle _hoverWindowRect;

        #region Win32 用于拾取鼠标下窗口
        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point pt);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        #endregion

        private readonly Color _maskKeyColor = Color.Magenta;
        public SelectorForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
       //     this.BackColor = Color.Transparent;
           
            this.BackColor = _maskKeyColor;// Color.Transparent;
                                           //  this.TransparencyKey = _maskKeyColor;
            this.Opacity = 0.4;
            this.Cursor = Cursors.Cross;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _startPos = e.Location;
                SelectedRect = Rectangle.Empty;
                this.Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                SelectedRect = GetRect(_startPos, e.Location);
            }
            else
            {
                // 拾取鼠标位置下的窗口
                Point screenPt = PointToScreen(e.Location);
                IntPtr hWnd = WindowFromPoint(screenPt);
                if (hWnd != IntPtr.Zero && GetWindowRect(hWnd, out RECT rc))
                {
                    _hoverWindowRect = new Rectangle(rc.Left, rc.Top, rc.Right - rc.Left, rc.Bottom - rc.Top);
                }
                else
                {
                    _hoverWindowRect = Rectangle.Empty;
                }
            }
            this.Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _isDragging)
            {
                _isDragging = false;
                if (SelectedRect.Width > 5 && SelectedRect.Height > 5)
                    this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 拖拽模式：绘制手动选区红框
            if (_isDragging && SelectedRect.Width > 0 && SelectedRect.Height > 0)
            {
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    g.DrawRectangle(pen, SelectedRect);
                }
            }
            else
            {
                // 未拖拽：绘制悬停窗口预览框
                if (!_hoverWindowRect.IsEmpty)
                {
                    // 把屏幕窗口坐标转换为本窗体客户区坐标
                    Rectangle clientRc = new Rectangle(
                        _hoverWindowRect.X - this.Left,
                        _hoverWindowRect.Y - this.Top,
                        _hoverWindowRect.Width,
                        _hoverWindowRect.Height);
                    using (Pen pen = new Pen(Color.Lime, 2))
                    {
                        pen.DashStyle = DashStyle.Dash;
                        g.DrawRectangle(pen, clientRc);
                    }
                }
            }
        }

        private Rectangle GetRect(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int w = Math.Abs(p2.X - p1.X);
            int h = Math.Abs(p2.Y - p1.Y);
            return new Rectangle(x, y, w, h);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }


    #region 标注画布窗口
    public enum AnnotateTool_天黑了
    {
        Rect,
        RoundedRect,
        Ellipse,
        Pencil,
        Text
    }
    public enum AnnotateTool
    {
        Rect,
        RoundedRect,
        Ellipse,
        Pencil,
        Text,
        Mosaic
    }


    public class AnnotateForm_天黑了 : Form
    {
        public Bitmap ResultBitmap { get; private set; }
        private Bitmap _originBmp;
        private Bitmap _workBmp;
        private Graphics _g;

        private AnnotateTool _currentTool = AnnotateTool.Rect;
        private Color _drawColor = Color.Red;
        private int _lineWidth = 2;
        private Font _textFont = new Font("Microsoft Sans Serif", 12);

        private Point _startPt;
        private bool _isDrawing;
        private List<Point> _pencilPath = new List<Point>();
        private Bitmap _undoBackup;

        private PictureBox _picBox;

        public AnnotateForm_天黑了(Bitmap srcBmp)
        {
            _originBmp = new Bitmap(srcBmp);
            _workBmp = new Bitmap(_originBmp);
            _g = Graphics.FromImage(_workBmp);
            _g.SmoothingMode = SmoothingMode.AntiAlias;
            _undoBackup = new Bitmap(_workBmp);

            this.ShowIcon = false;



            this.Text = "东方仙盟 开源 Annotate - FairyAlliance Snap";
            this.Size = new Size(Math.Min(_workBmp.Width + 100, 1200), Math.Min(_workBmp.Height + 160, 800));
            this.StartPosition = FormStartPosition.CenterScreen;

            //顶部工具栏
            var panelTool = new Panel { Dock = DockStyle.Top, Height = 60 };
            var btnRect = new Button { Text = "矩形", Location = new Point(10, 10), Width = 70 };
            btnRect.Click += (s, e) => _currentTool = AnnotateTool.Rect;
            var btnRoundRect = new Button { Text = "圆角矩形", Location = new Point(85, 10), Width = 70 };
            btnRoundRect.Click += (s, e) => _currentTool = AnnotateTool.RoundedRect;
            var btnEllipse = new Button { Text = "椭圆", Location = new Point(160, 10), Width = 70 };
            btnEllipse.Click += (s, e) => _currentTool = AnnotateTool.Ellipse;
            var btnPencil = new Button { Text = "铅笔", Location = new Point(235, 10), Width = 70 };
            btnPencil.Click += (s, e) => _currentTool = AnnotateTool.Pencil;
            var btnText = new Button { Text = "文字", Location = new Point(310, 10), Width = 70 };
            btnText.Click += (s, e) => _currentTool = AnnotateTool.Text;

            var cboWidth = new ComboBox { Location = new Point(390, 10), Width = 70, DropDownStyle = ComboBoxStyle.DropDownList };
            cboWidth.Items.AddRange(new object[] { 1, 2, 3, 5 });
            cboWidth.SelectedItem = 2;
            cboWidth.SelectedIndexChanged += (s, e) => _lineWidth = (int)cboWidth.SelectedItem;

            var btnColor = new Button { Text = "颜色", Location = new Point(470, 10), Width = 70 };
            btnColor.Click += (s, e) =>
            {
                using (var cd = new ColorDialog())
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                        _drawColor = cd.Color;
                }
            };

            var btnFont = new Button { Text = "字体", Location = new Point(545, 10), Width = 70 };
            btnFont.Click += (s, e) =>
            {
                using (var fd = new FontDialog())
                {
                    fd.Font = _textFont;
                    if (fd.ShowDialog() == DialogResult.OK)
                        _textFont = fd.Font;
                }
            };

            var btnUndo = new Button { Text = "撤销", Location = new Point(620, 10), Width = 70 };
            btnUndo.Click += (s, e) =>
            {
                var temp = new Bitmap(_undoBackup);
                _g.Dispose();
                _workBmp.Dispose();
                _workBmp = temp;
                _g = Graphics.FromImage(_workBmp);
                _g.SmoothingMode = SmoothingMode.AntiAlias;
                _picBox.Image = _workBmp;
            };

            //新增两个按钮：对勾自动保存、另存为
            var btnOkAutoSave = new Button { Text = "✓ 对勾", Location = new Point(695, 10), Width = 70 };
            btnOkAutoSave.Click += (s, e) =>
            {
                ResultBitmap = new Bitmap(_workBmp);
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            var btnSaveAs = new Button { Text = "💾保存", Location = new Point(770, 10), Width = 70 };
            btnSaveAs.Click += (s, e) =>
            {
                ResultBitmap = new Bitmap(_workBmp);
                this.DialogResult = DialogResult.Yes;
                this.Close();
            };

            var btnCancel = new Button { Text = "取消", Location = new Point(845, 10), Width = 70 };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            panelTool.Controls.AddRange(new Control[] { btnRect, btnRoundRect, btnEllipse, btnPencil, btnText, cboWidth, btnColor, btnFont, btnUndo, btnOkAutoSave, btnSaveAs, btnCancel });

            _picBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = _workBmp,
                SizeMode = PictureBoxSizeMode.AutoSize
            };
            _picBox.MouseDown += PicBox_MouseDown;
            _picBox.MouseMove += PicBox_MouseMove;
            _picBox.MouseUp += PicBox_MouseUp;

            this.Controls.Add(_picBox);
            this.Controls.Add(panelTool);
        }

        private void SaveUndo()
        {
            _undoBackup.Dispose();
            _undoBackup = new Bitmap(_workBmp);
        }

        //自制文字输入窗口，移除Microsoft.VisualBasic依赖
        private string ShowTextInputDialog(string title, string defaultText)
        {
            using (Form inputForm = new Form())
            {
                inputForm.Text = title;
                inputForm.Size = new Size(320, 140);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                TextBox txtInput = new TextBox { Text = defaultText, Location = new Point(20, 20), Width = 260 };
                Button btnConfirm = new Button { Text = "确定", Location = new Point(20, 60), Width = 100 };
                btnConfirm.Click += (s, e) => inputForm.DialogResult = DialogResult.OK;
                inputForm.Controls.AddRange(new Control[] { txtInput, btnConfirm });
                if (inputForm.ShowDialog() == DialogResult.OK)
                    return txtInput.Text.Trim();
                return null;
            }
        }

        private void PicBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _startPt = e.Location;
            _isDrawing = true;
            if (_currentTool == AnnotateTool.Pencil)
            {
                _pencilPath.Clear();
                _pencilPath.Add(e.Location);
            }
            if (_currentTool == AnnotateTool.Text)
            {
                SaveUndo();
                string input = ShowTextInputDialog("输入文字", "");
                if (!string.IsNullOrEmpty(input))
                {
                    using (var brush = new SolidBrush(_drawColor))
                    {
                        _g.DrawString(input, _textFont, brush, e.X, e.Y);
                    }
                    _picBox.Image = _workBmp;
                }
                _isDrawing = false;
            }
        }

        private void PicBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;
            if (_currentTool == AnnotateTool.Pencil)
            {
                _pencilPath.Add(e.Location);
                using (var pen = new Pen(_drawColor, _lineWidth))
                {
                    if (_pencilPath.Count >= 2)
                        _g.DrawLines(pen, _pencilPath.ToArray());
                }
                _picBox.Image = _workBmp;
            }
        }

        private void PicBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;
            _isDrawing = false;
            SaveUndo();
            Rectangle rect = GetRect(_startPt, e.Location);
            using (var pen = new Pen(_drawColor, _lineWidth))
            {
                switch (_currentTool)
                {
                    case AnnotateTool.Rect:
                        _g.DrawRectangle(pen, rect);
                        break;
                    case AnnotateTool.RoundedRect:
                        using (GraphicsPath gp = new GraphicsPath())
                        {
                            int r = Math.Min(rect.Width, rect.Height) / 6;
                            gp.AddArc(rect.X, rect.Y, r, r, 180, 90);
                            gp.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                            gp.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                            gp.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                            gp.CloseAllFigures();
                            _g.DrawPath(pen, gp);
                        }
                        break;
                    case AnnotateTool.Ellipse:
                        _g.DrawEllipse(pen, rect);
                        break;
                }
            }
            _picBox.Image = _workBmp;
        }

        private Rectangle GetRect(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int w = Math.Abs(p2.X - p1.X);
            int h = Math.Abs(p2.Y - p1.Y);
            return new Rectangle(x, y, w, h);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _g?.Dispose();
                _workBmp?.Dispose();
                _originBmp?.Dispose();
                _undoBackup?.Dispose();
                _textFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    #endregion
    public class AnnotateForm : Form
    {
        public Bitmap ResultBitmap { get; private set; }
        private Bitmap _originBmp;
        private Bitmap _workBmp;
        private Graphics _g;

        private AnnotateTool _currentTool = AnnotateTool.Rect;
        private Color _drawColor = Color.Red;
        private int _lineWidth = 2;
        private Font _textFont = new Font("Microsoft Sans Serif", 12);

        private Point _startPt;
        private bool _isDrawing;
        private List<Point> _pencilPath = new List<Point>();
        private Bitmap _undoBackup;

        private PictureBox _picBox;

     //   private Point _startPt;
        private Point _currentPt;
        //   private bool _isDrawing;
        // private List<Point> _pencilPath = new List<Point>();

        // ========== 撤销栈（支持多次撤销） ==========
        private List<Bitmap> _undoStack = new List<Bitmap>();
        private const int MaxUndoCount = 20; //最多保留20步，可以改

        public AnnotateForm(Bitmap srcBmp)
        {
            _originBmp = new Bitmap(srcBmp);
            _workBmp = new Bitmap(_originBmp);
            _g = Graphics.FromImage(_workBmp);
            _g.SmoothingMode = SmoothingMode.AntiAlias;
            _undoBackup = new Bitmap(_workBmp);

            //初始画面入撤销栈
            PushUndoSnapshot();

            this.ShowIcon = false;



            this.Text = "东方仙盟 开源 Annotate - FairyAlliance Snap";
            this.Size = new Size(Math.Min(_workBmp.Width + 100, 1200), Math.Min(_workBmp.Height + 160, 800));
            this.StartPosition = FormStartPosition.CenterScreen;

            //顶部工具栏
            var panelTool = new Panel { Dock = DockStyle.Top, Height = 60 };
            var btnRect = new Button { Text = "矩形", Location = new Point(10, 10), Width = 70 };
            btnRect.Click += (s, e) => _currentTool = AnnotateTool.Rect;
            var btnRoundRect = new Button { Text = "圆角矩形", Location = new Point(85, 10), Width = 70 };
            btnRoundRect.Click += (s, e) => _currentTool = AnnotateTool.RoundedRect;
            var btnEllipse = new Button { Text = "椭圆", Location = new Point(160, 10), Width = 70 };
            btnEllipse.Click += (s, e) => _currentTool = AnnotateTool.Ellipse;
            var btnPencil = new Button { Text = "铅笔", Location = new Point(235, 10), Width = 70 };
            btnPencil.Click += (s, e) => _currentTool = AnnotateTool.Pencil;
            var btnText = new Button { Text = "文字", Location = new Point(310, 10), Width = 70 };
            btnText.Click += (s, e) => _currentTool = AnnotateTool.Text;

            var btnMosaic = new Button { Text = "马赛克", Location = new Point(385, 10), Width = 70 };
            btnMosaic.Click += (s, e) => _currentTool = AnnotateTool.Mosaic;

            var cboWidth = new ComboBox { Location = new Point(460, 10), Width = 70, DropDownStyle = ComboBoxStyle.DropDownList };
            cboWidth.Items.AddRange(new object[] { 1, 2, 3, 5 });
            cboWidth.SelectedItem = 2;
            cboWidth.SelectedIndexChanged += (s, e) => _lineWidth = (int)cboWidth.SelectedItem;

            var btnColor = new Button { Text = "颜色", Location = new Point(540, 10), Width = 70 };
            btnColor.Click += (s, e) =>
            {
                using (var cd = new ColorDialog())
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                        _drawColor = cd.Color;
                }
            };

            var btnFont = new Button { Text = "字体", Location = new Point(615, 10), Width = 70 };
            btnFont.Click += (s, e) =>
            {
                using (var fd = new FontDialog())
                {
                    fd.Font = _textFont;
                    if (fd.ShowDialog() == DialogResult.OK)
                        _textFont = fd.Font;
                }
            };

            var btnUndo = new Button { Text = "撤销", Location = new Point(690, 10), Width = 70 };
            btnUndo.Click += (s, e) =>
            {
                var temp = new Bitmap(_undoBackup);
                _g.Dispose();
                _workBmp.Dispose();
                _workBmp = temp;
                _g = Graphics.FromImage(_workBmp);
                _g.SmoothingMode = SmoothingMode.AntiAlias;
                _picBox.Image = _workBmp;
            };

            //新增两个按钮：对勾自动保存、另存为
            var btnOkAutoSave = new Button { Text = "✓ 自动保存", Location = new Point(765, 10), Width = 70 };
            btnOkAutoSave.Click += (s, e) =>
            {
                ResultBitmap = new Bitmap(_workBmp);
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            var btnSaveAs = new Button { Text = "💾保存", Location = new Point(840, 10), Width = 70 };
            btnSaveAs.Click += (s, e) =>
            {
                ResultBitmap = new Bitmap(_workBmp);
                this.DialogResult = DialogResult.Yes;
                this.Close();
            };

            var btnCancel = new Button { Text = "取消", Location = new Point(915, 10), Width = 70 };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;



           // panelTool.Controls.AddRange(new Control[] { btnRect, btnRoundRect, btnEllipse, btnPencil, btnText, cboWidth, btnColor, btnFont, btnUndo, btnOkAutoSave, btnSaveAs, btnCancel });
            panelTool.Controls.AddRange(new Control[] { btnRect, btnRoundRect, btnEllipse, btnPencil, btnText, btnMosaic, cboWidth, btnColor, btnFont, btnUndo, btnOkAutoSave, btnSaveAs, btnCancel });
            _picBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = _workBmp,
                SizeMode = PictureBoxSizeMode.AutoSize
            };
            _picBox.MouseDown += PicBox_MouseDown;
            _picBox.MouseMove += PicBox_MouseMove;
            _picBox.MouseUp += PicBox_MouseUp;

            this.Controls.Add(_picBox);
            this.Controls.Add(panelTool);
        }

        private void ApplyMosaic(Rectangle rect, int blockSize = 10)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using (Bitmap temp = new Bitmap(_workBmp))
            {
                for (int y = rect.Top; y < rect.Bottom; y += blockSize)
                {
                    for (int x = rect.Left; x < rect.Right; x += blockSize)
                    {
                        int w = Math.Min(blockSize, rect.Right - x);
                        int h = Math.Min(blockSize, rect.Bottom - y);

                        Color avg = GetAverageColor(temp, new Rectangle(x, y, w, h));

                        using (SolidBrush brush = new SolidBrush(avg))
                        {
                            _g.FillRectangle(brush, x, y, w, h);
                        }
                    }
                }
            }
        }

        private Color GetAverageColor(Bitmap bmp, Rectangle rect)
        {
            long r = 0, g = 0, b = 0;
            int count = 0;

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    r += c.R;
                    g += c.G;
                    b += c.B;
                    count++;
                }
            }

            if (count == 0) return Color.Gray;

            return Color.FromArgb((int)(r / count), (int)(g / count), (int)(b / count));
        }


        // 将当前画面压入撤销栈
        private void PushUndoSnapshot()
        {
            _undoStack.Add(new Bitmap(_workBmp));
            if (_undoStack.Count > MaxUndoCount)
            {
                //超过上限，释放最旧的图片，防止内存暴涨
                var old = _undoStack[0];
                _undoStack.RemoveAt(0);
                old.Dispose();
            }
        }

        // 撤销操作
        private void UndoAction()
        {
            if (_undoStack.Count <= 1)
                return;

            //弹出当前，回退上一步
            var currentBmp = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            currentBmp.Dispose();

            _workBmp.Dispose();
            _workBmp = new Bitmap(_undoStack[_undoStack.Count - 1]);
            _g.Dispose();
            _g = Graphics.FromImage(_workBmp);
            _g.SmoothingMode = SmoothingMode.AntiAlias;

            _picBox.Image = _workBmp;
            _picBox.Invalidate();
        }

        private void SaveUndo()
        {
            _undoBackup.Dispose();
            _undoBackup = new Bitmap(_workBmp);
        }
        // PictureBox绘制：原图 + 拖拽临时预览橡皮筋
        private void PicBox_Paint(object sender, PaintEventArgs e)
        {
            if (!_isDrawing) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = GetRect(_startPt, _currentPt);
            using (Pen pen = new Pen(_drawColor, _lineWidth))
            {
                switch (_currentTool)
                {
                    case AnnotateTool.Rect:
                        g.DrawRectangle(pen, rect);
                        break;
                    case AnnotateTool.RoundedRect:
                        using (GraphicsPath gp = new GraphicsPath())
                        {
                            int r = Math.Min(rect.Width, rect.Height) / 6;
                            gp.AddArc(rect.X, rect.Y, r, r, 180, 90);
                            gp.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                            gp.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                            gp.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                            gp.CloseAllFigures();
                            g.DrawPath(pen, gp);
                        }
                        break;
                    case AnnotateTool.Ellipse:
                        g.DrawEllipse(pen, rect);
                        break;
                    case AnnotateTool.Pencil:
                        if (_pencilPath.Count >= 2)
                            g.DrawLines(pen, _pencilPath.ToArray());
                        break;
                }
            }
        }
        private void PicBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _startPt = e.Location;
            _currentPt = e.Location;
            _isDrawing = true;

            if (_currentTool == AnnotateTool.Pencil)
            {
                _pencilPath.Clear();
                _pencilPath.Add(e.Location);
            }
            if (_currentTool == AnnotateTool.Text)
            {
                PushUndoSnapshot(); //文字绘制前保存快照
                string input = ShowTextInputDialog("输入文字", "");
                if (!string.IsNullOrEmpty(input))
                {
                    using (var brush = new SolidBrush(_drawColor))
                    {
                        _g.DrawString(input, _textFont, brush, e.X, e.Y);
                    }
                    _picBox.Image = _workBmp;
                }
                _isDrawing = false;
            }
        }

        private void PicBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;
            _currentPt = e.Location;

            if (_currentTool == AnnotateTool.Pencil)
            {
                _pencilPath.Add(e.Location);
                //铅笔实时直接画在原图，铅笔不需要橡皮筋预览
                using (var pen = new Pen(_drawColor, _lineWidth))
                {
                    if (_pencilPath.Count >= 2)
                        _g.DrawLines(pen, _pencilPath.ToArray());
                }
                _picBox.Image = _workBmp;
            }
            else
            {
                //矩形/圆角矩形/椭圆：触发重绘，渲染橡皮筋预览
                _picBox.Invalidate();
            }
        }

        private void PicBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;
            _isDrawing = false;
            Rectangle rect = GetRect(_startPt, e.Location);

            // 鼠标松开，正式绘制到原图
            using (var pen = new Pen(_drawColor, _lineWidth))
            {
                switch (_currentTool)
                {
                    case AnnotateTool.Rect:
                        _g.DrawRectangle(pen, rect);
                        break;
                    case AnnotateTool.RoundedRect:
                        using (GraphicsPath gp = new GraphicsPath())
                        {
                            int r = Math.Min(rect.Width, rect.Height) / 6;
                            gp.AddArc(rect.X, rect.Y, r, r, 180, 90);
                            gp.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                            gp.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                            gp.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                            gp.CloseAllFigures();
                            _g.DrawPath(pen, gp);
                        }
                        break;
                    case AnnotateTool.Ellipse:
                        _g.DrawEllipse(pen, rect);
                        break;
                    case AnnotateTool.Mosaic:
                        ApplyMosaic(rect, 10);
                        break;
                }
            }

            //绘制完成，保存一步快照，加入撤销栈
            PushUndoSnapshot();
            _picBox.Image = _workBmp;
            _picBox.Invalidate();
        }

        //自制文字输入窗口，移除Microsoft.VisualBasic依赖
        private string ShowTextInputDialog(string title, string defaultText)
        {
            using (Form inputForm = new Form())
            {
                inputForm.Text = title;
                inputForm.Size = new Size(320, 140);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                TextBox txtInput = new TextBox { Text = defaultText, Location = new Point(20, 20), Width = 260 };
                Button btnConfirm = new Button { Text = "确定", Location = new Point(20, 60), Width = 100 };
                btnConfirm.Click += (s, e) => inputForm.DialogResult = DialogResult.OK;
                inputForm.Controls.AddRange(new Control[] { txtInput, btnConfirm });
                if (inputForm.ShowDialog() == DialogResult.OK)
                    return txtInput.Text.Trim();
                return null;
            }
        }

        private void PicBox_MouseDown天黑了(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _startPt = e.Location;
            _isDrawing = true;
            if (_currentTool == AnnotateTool.Pencil)
            {
                _pencilPath.Clear();
                _pencilPath.Add(e.Location);
            }
            if (_currentTool == AnnotateTool.Text)
            {
                SaveUndo();
                string input = ShowTextInputDialog("输入文字", "");
                if (!string.IsNullOrEmpty(input))
                {
                    using (var brush = new SolidBrush(_drawColor))
                    {
                        _g.DrawString(input, _textFont, brush, e.X, e.Y);
                    }
                    _picBox.Image = _workBmp;
                }
                _isDrawing = false;
            }
        }

        private void PicBox_MouseMove天黑了(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;
            if (_currentTool == AnnotateTool.Pencil)
            {
                _pencilPath.Add(e.Location);
                using (var pen = new Pen(_drawColor, _lineWidth))
                {
                    if (_pencilPath.Count >= 2)
                        _g.DrawLines(pen, _pencilPath.ToArray());
                }
                _picBox.Image = _workBmp;
            }
        }

        private void PicBox_MouseUp天黑了(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;
            _isDrawing = false;
            SaveUndo();
            Rectangle rect = GetRect(_startPt, e.Location);
            using (var pen = new Pen(_drawColor, _lineWidth))
            {
                switch (_currentTool)
                {
                    case AnnotateTool.Rect:
                      
                        _g.DrawRectangle(pen, rect);
                        break;
                    case AnnotateTool.RoundedRect:
                        using (GraphicsPath gp = new GraphicsPath())
                        {
                            int r = Math.Min(rect.Width, rect.Height) / 6;
                            gp.AddArc(rect.X, rect.Y, r, r, 180, 90);
                            gp.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                            gp.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                            gp.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                            gp.CloseAllFigures();
                            _g.DrawPath(pen, gp);
                        }
                        break;
                    case AnnotateTool.Ellipse:
                        _g.DrawEllipse(pen, rect);
                        break;
                    case AnnotateTool.Mosaic:
                        ApplyMosaic(rect, 10);
                        break;
                }
            }
            // _picBox.Image = _workBmp;
            PushUndoSnapshot();
            _picBox.Image = _workBmp;
            _picBox.Invalidate();
        }

        private Rectangle GetRect(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int w = Math.Abs(p2.X - p1.X);
            int h = Math.Abs(p2.Y - p1.Y);
            return new Rectangle(x, y, w, h);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _g?.Dispose();
                _workBmp?.Dispose();
                _originBmp?.Dispose();
                _undoBackup?.Dispose();
                _textFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

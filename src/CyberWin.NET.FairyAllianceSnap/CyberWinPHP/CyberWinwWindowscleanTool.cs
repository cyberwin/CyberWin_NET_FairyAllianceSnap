using CyberWin.NET.CleanToolWin.CyberwinPHP.Cyber_Dialog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Windows.Forms;
using static CyberWin.NET.CleanToolWin.CyberwinPHP.FrmMain;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CyberWin.NET.CleanToolWin.CyberwinPHP
{
   

   
        public partial class FrmMain : Form
        {
            private readonly List<FileItem> _allFileList = new List<FileItem>();
            private readonly string[] _allowExt = { ".tmp", ".temp", ".log", ".chk" };// ".iog"
            private const string ScanLogFile = "cyber_Scan_Log.txt";
            private const string CleanLogFile = "cyber_Clean_Log.txt";

        //类级别变量
        private bool _scanStopFlag = false;

        public FrmMain()
            {
              
            try
            {
                InitializeComponent();
                SetListViewDoubleBuffered(listView1);
                InitListView();
                cyber_增加浏览器控制();
                //在这里加上这一行！！
                bgWorkerScan.WorkerSupportsCancellation = true;
            }
            catch (Exception EX)
            {
                MessageBox.Show("系统错误" + EX.Message+EX.StackTrace+EX.InnerException);
            }
        }

            #region 初始化ListView
            void InitListView()
            {
                listView1.View = View.Details;
                listView1.FullRowSelect = true;
                listView1.CheckBoxes = true;
                listView1.Columns.Clear();
                listView1.Columns.Add("", 35);
                listView1.Columns.Add("大小", 90);
                listView1.Columns.Add("文件路径", 540);
                listView1.Columns.Add("类型", 80);
                listView1.ItemCheck += ListView1_ItemCheck;
            }

            //勾选改变，实时统计勾选数量、总大小
            private void ListView1_ItemCheck(object sender, ItemCheckEventArgs e)
            {
                //等状态真正变更后再统计，用BeginInvoke避免状态不同步
                this.BeginInvoke(new Action(() =>
                {
                    long selectTotalBytes = 0;
                    int selectCount = 0;
                    for (int i = 0; i < listView1.Items.Count; i++)
                    {
                        var item = listView1.Items[i];
                        if (item.Checked)
                        {
                            selectCount++;
                            var tag = item.Tag as FileItem;
                            if (tag != null) selectTotalBytes += tag.Size;
                        }
                    }
                    lblSelectInfo.Text = $"已勾选：{selectCount} 项，共 {GetHumanSize(selectTotalBytes)}";
                }));
            }

            //反射开启双缓冲，解决闪烁、大量列表卡顿
            void SetListViewDoubleBuffered(ListView lv)
            {
                var pi = lv.GetType().GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                pi?.SetValue(lv, true, null);
            }
            #endregion

            #region 扫描按钮
            private void btnScan_Click(object sender, EventArgs e)
            {
            _scanStopFlag = false;

            if (bgWorkerScan.IsBusy) return;

                listView1.Items.Clear();
                _allFileList.Clear();
                lblStatus.Text = "正在扫描……";
                lblSelectInfo.Text = "已勾选：0 项，共 0B";
                progressBar1.Value = 0;

                bgWorkerScan.RunWorkerAsync();
            }

            //后台扫描逻辑
            private void bgWorkerScan_DoWork单个temp扫描(object sender, DoWorkEventArgs e)
            {
                var bw = sender as BackgroundWorker;
                var scanDirs = new List<string>();

                //系统临时目录
                scanDirs.Add(Path.GetTempPath());
                var winTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                scanDirs.Add(winTemp);

                //所有本地磁盘根下Temp目录（仅一级，不递归子文件夹）
                foreach (var drv in DriveInfo.GetDrives())
                {
                    if (drv.DriveType == DriveType.Fixed)
                    {
                        try
                        {
                            string dTemp = Path.Combine(drv.RootDirectory.FullName, "Temp");
                            if (Directory.Exists(dTemp))
                                scanDirs.Add(dTemp);
                        }
                        catch { }
                    }
                }

                foreach (var dir in scanDirs)
                {
                    if (!Directory.Exists(dir)) continue;
                    try
                    {
                        var di = new DirectoryInfo(dir);
                        var files = di.GetFiles("*.*", SearchOption.TopDirectoryOnly);
                        foreach (var f in files)
                        {
                            string ext = f.Extension.ToLowerInvariant();
                            if (Array.IndexOf(_allowExt, ext) > -1)
                            {
                                _allFileList.Add(new FileItem(f.FullName, f.Length, ext));
                            }
                        }
                    }
                    catch { }
                }
            }

        //后台扫描逻辑
        private void bgWorkerScan_DoWork多目录(object sender, DoWorkEventArgs e)
        {
            var bw = sender as BackgroundWorker;
            var scanDirs = new List<string>();

            //系统临时目录
            scanDirs.Add(Path.GetTempPath());
            var winTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            scanDirs.Add(winTemp);

            //所有本地磁盘根下Temp目录（递归所有子文件夹）
            foreach (var drv in DriveInfo.GetDrives())
            {
                if (drv.DriveType == DriveType.Fixed) //所有本地硬盘 C/D/E...
                {
                    try
                    {
                        string dTemp = Path.Combine(drv.RootDirectory.FullName, "Temp");
                        if (Directory.Exists(dTemp))
                            scanDirs.Add(dTemp);
                    }
                    catch { }
                }
            }

            foreach (var dir in scanDirs)
            {
                if (!Directory.Exists(dir)) continue;
                //调用递归扫描函数
                ScanDirectoryRecursive(dir);
            }
        }

        //新增：递归遍历子目录，独立函数，内部捕获每一层目录的异常
        private void ScanDirectoryRecursive多目录(string dir)
        {
            try
            {
                // 读取当前目录文件
                var di = new DirectoryInfo(dir);
                var files = di.GetFiles("*.*", SearchOption.TopDirectoryOnly);
                foreach (var f in files)
                {
                    string ext = f.Extension.ToLowerInvariant();
                    if (Array.IndexOf(_allowExt, ext) > -1)
                    {
                        _allFileList.Add(new FileItem(f.FullName, f.Length, ext));
                    }
                }

                // 遍历子文件夹，递归进去
                var subDirs = di.GetDirectories("*.*", SearchOption.TopDirectoryOnly);
                foreach (var sub in subDirs)
                {
                    ScanDirectoryRecursive(sub.FullName);
                }
            }
            catch
            {
                // 权限不足/拒绝访问的目录直接跳过，不中断整体扫描
            }
        }

        
           private void btnstop_Click(object sender, EventArgs e)
        {
            //  if (bgWorkerScan.IsBusy)
            //   {
            //  bgWorkerScan.CancelAsync();
            //lblStatus.Text = "正在终止扫描...";
            // }
            _scanStopFlag = true;
            lblStatus.Text = "正在终止扫描...";
        }
            private void bgWorkerScan_DoWork(object sender, DoWorkEventArgs e)
        {
            var bw = sender as BackgroundWorker;
            var scanDirs = new List<string>();


            //1. CyberWin项目目录
            string cyberDir = @"D:\CyberWin\pro";
            if (Directory.Exists(cyberDir))
            {
                scanDirs.Add(cyberDir);
            }

            //2. 用户默认Temp
            string userTemp = Path.GetTempPath();
            if (Directory.Exists(userTemp))
            {
                scanDirs.Add(userTemp);
            }

            //3. Windows Temp
            string winTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            if (Directory.Exists(winTemp))
            {
                scanDirs.Add(winTemp);
            }

            // 取出所有本地固定硬盘 C D E...
            foreach (var drv in DriveInfo.GetDrives())
            {
                if (drv.DriveType == DriveType.Fixed)
                {
                    try
                    {
                        scanDirs.Add(drv.RootDirectory.FullName);
                    }
                    catch { }
                }
            }

            foreach (var rootPath in scanDirs)
            {
                if (bw.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                if (!Directory.Exists(rootPath)) continue;
                lblStatus.Text = "正在扫描……"+ rootPath;
                ScanDirectoryRecursive(rootPath);
            }
        }

        //递归遍历目录，每层独立捕获异常，权限不足直接跳过该文件夹
        private void ScanDirectoryRecursive(string dir)
        {
            if (_scanStopFlag) return;

            // 收到停止请求，直接退出
            if (bgWorkerScan.CancellationPending)
            {
                return;
            }
            try
            {
                lblStatus.Text = "正在扫描……" + dir;
                DirectoryInfo di = new DirectoryInfo(dir);
                //读取当前目录文件
                foreach (var f in di.GetFiles("*.*", SearchOption.TopDirectoryOnly))
                {
                    string ext = f.Extension.ToLowerInvariant();
                    if (Array.IndexOf(_allowExt, ext) > -1)
                    {
                        _allFileList.Add(new FileItem(f.FullName, f.Length, ext));
                        var fileItem = new FileItem(f.FullName, f.Length, ext);
                        //把当前文件抛到UI主线程
                        // bgWorkerScan.ReportProgress(0, fileItem);
                        //传统Invoke，切回主线程添加ListView行
                        if (listView1.InvokeRequired)
                        {
                            listView1.Invoke(new Action<FileItem>((fi) =>
                            {
                                var lvItem = new ListViewItem();
                                lvItem.SubItems.Add(GetHumanSize(fi.Size));
                                lvItem.SubItems.Add(fi.Path);
                                lvItem.SubItems.Add(fi.Ext);
                                lvItem.Tag = fi;
                                if (fi.Ext.Equals(".iog"))
                                {
                                    lvItem.Font = new System.Drawing.Font(lvItem.Font, System.Drawing.FontStyle.Bold);
                                }
                                listView1.Items.Add(lvItem);
                            }), fileItem);
                        }
                        else
                        {
                            //如果已经在主线程（备用分支）
                            var lvItem = new ListViewItem();
                            lvItem.SubItems.Add(GetHumanSize(fileItem.Size));
                            lvItem.SubItems.Add(fileItem.Path);
                            lvItem.SubItems.Add(fileItem.Ext);
                            lvItem.Tag = fileItem;
                            if (fileItem.Ext.Equals(".iog"))
                            {
                                lvItem.Font = new System.Drawing.Font(lvItem.Font, System.Drawing.FontStyle.Bold);
                            }
                            listView1.Items.Add(lvItem);
                        }

                    }
                }
                //递归子文件夹
                foreach (var subDir in di.GetDirectories("*.*", SearchOption.TopDirectoryOnly))
                {
                    ScanDirectoryRecursive(subDir.FullName);
                }
            }
            catch
            {
                //权限拒绝/系统保护目录直接跳过，扫描不中断
            }
        }
       // bgWorkerScan.WorkerSupportsCancellation = true;
        private void bgWorkerScan_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is FileItem f)
            {
                var lvItem = new ListViewItem();
                lvItem.SubItems.Add(GetHumanSize(f.Size));
                lvItem.SubItems.Add(f.Path);
                lvItem.SubItems.Add(f.Ext);
                lvItem.Tag = f;
                if (f.Ext.Equals(".iog"))
                {
                    lvItem.Font = new System.Drawing.Font(lvItem.Font, System.Drawing.FontStyle.Bold);
                }
                listView1.Items.Add(lvItem);
            }
        }

        private void bgWorkerScan_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                lblStatus.Text = "扫描已手动终止";
            }
            else if (e.Error != null)
            {
                lblStatus.Text = $"扫描异常：{e.Error.Message}";
            }
            else
            {
                long allTotalByte = 0;
                foreach (var f in _allFileList)
                {
                    allTotalByte += f.Size;
                }
                progressBar1.Value = 100;
                lblStatus.Text = $"扫描完成：总计 {_allFileList.Count} 个垃圾文件，总大小 {GetHumanSize(allTotalByte)}";
                WriteScanLog(_allFileList);
            }
        }

        //扫描完成，加载UI + 写扫描日志
        private void bgWorkerScan_RunWorkerCompleted作废(object sender, RunWorkerCompletedEventArgs e)
            {
                listView1.BeginUpdate();
                listView1.Items.Clear();
                long allTotalByte = 0;

                foreach (var f in _allFileList)
                {
                    allTotalByte += f.Size;
                    var lvItem = new ListViewItem();
                    lvItem.SubItems.Add(GetHumanSize(f.Size));
                    lvItem.SubItems.Add(f.Path);
                    lvItem.SubItems.Add(f.Ext);
                    lvItem.Tag = f;
                    if (f.Ext.Equals(".iog"))
                    {
                        lvItem.Font = new System.Drawing.Font(lvItem.Font, System.Drawing.FontStyle.Bold);
                    }
                    listView1.Items.Add(lvItem);
                }
                listView1.EndUpdate();
                progressBar1.Value = 100;
                lblStatus.Text = $"扫描完成：总计 {_allFileList.Count} 个垃圾文件，总大小 {GetHumanSize(allTotalByte)}";

                //写入扫描日志：详单 + 汇总
                WriteScanLog(_allFileList);
            }

            //扫描日志写入
            void WriteScanLog(List<FileItem> list)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("========================================");
                sb.AppendLine($"扫描时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"文件总数：{list.Count}");
                long total = 0;
                foreach (var fi in list) total += fi.Size;
                sb.AppendLine($"总大小：{GetHumanSize(total)}");
                sb.AppendLine("----------------------------------------");
                foreach (var fi in list)
                {
                    sb.AppendLine($"{GetHumanSize(fi.Size),-12} | {fi.Ext,-8} | {fi.Path}");
                }
                sb.AppendLine("========================================\r\n");
                File.AppendAllText(ScanLogFile, sb.ToString(), Encoding.UTF8);
            }
            #endregion

            #region 全选 / 反选 / 清空
            private void btnSelectAll_Click(object sender, EventArgs e)
            {
                foreach (ListViewItem it in listView1.Items) it.Checked = true;
            }

            private void btnInvertSel_Click(object sender, EventArgs e)
            {
                foreach (ListViewItem it in listView1.Items) it.Checked = !it.Checked;
            }

            private void btnClearSel_Click(object sender, EventArgs e)
            {
                foreach (ListViewItem it in listView1.Items) it.Checked = false;
            }
            #endregion

            #region 清理选中项
            private void btnCleanSelected_Click(object sender, EventArgs e)
            {
                List<FileItem> selectedList = new List<FileItem>();
                foreach (ListViewItem item in listView1.Items)
                {
                    if (item.Checked && item.Tag is FileItem fi)
                    {
                        selectedList.Add(fi);
                    }
                }
                if (selectedList.Count == 0)
                {
                    MessageBox.Show("请先勾选要清理的文件");
                    return;
                }

                var confirm = MessageBox.Show($"即将清理 {selectedList.Count} 个文件，确认继续？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                int successCnt = 0;
                int failCnt = 0;
                long successBytes = 0;
                StringBuilder cleanLogSb = new StringBuilder();
                cleanLogSb.AppendLine("========================================");
                cleanLogSb.AppendLine($"清理时间：{DateTime.Now:yyyy‑MM‑dd HH:mm:ss}");
                cleanLogSb.AppendLine($"本次勾选待清理数量：{selectedList.Count}");
                cleanLogSb.AppendLine("【待清理文件列表】");

                foreach (var fi in selectedList)
                {
                    cleanLogSb.AppendLine($"{fi.Ext,-8} | {fi.Path}");
                    try
                    {
                        if (File.Exists(fi.Path))
                        {
                            File.Delete(fi.Path);
                            successCnt++;
                            successBytes += fi.Size;
                            cleanLogSb.AppendLine($"   >> OK 删除成功");
                            //UI移除该项
                            for (int i = 0; i < listView1.Items.Count; i++)
                            {
                                var tag = listView1.Items[i].Tag as FileItem;
                                if (tag != null && tag.Path == fi.Path)
                                {
                                    listView1.Items.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            failCnt++;
                            cleanLogSb.AppendLine($"   >> FAIL 文件已不存在");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCnt++;
                        cleanLogSb.AppendLine($"   >> FAIL {ex.Message}");
                    }
                }

                cleanLogSb.AppendLine("----------------------------------------");
                cleanLogSb.AppendLine($"成功删除：{successCnt} 项，释放 {GetHumanSize(successBytes)}");
                cleanLogSb.AppendLine($"失败跳过：{failCnt} 项");
                cleanLogSb.AppendLine("========================================\r\n");
                File.AppendAllText(CleanLogFile, cleanLogSb.ToString(), Encoding.UTF8);

                MessageBox.Show($"处理完成\r\n成功：{successCnt} 项\r\n失败跳过：{failCnt} 项\r\n日志已输出到程序目录", "完成", MessageBoxButtons.OKCancel);
                //刷新勾选统计
                ListView1_ItemCheck(null, null);
            }
            #endregion

            #region 辅助工具
            public string GetHumanSize(long bytes)
            {
                if (bytes < 1024) return $"{bytes} B";
                if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.00} KB";
                if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):0.00} MB";
                return $"{bytes / (1024.0 * 1024 * 1024):0.00} GB";
            }

            public class FileItem
            {
                public string Path { get; set; }
                public long Size { get; set; }
                public string Ext { get; set; }
                public FileItem(string path, long size, string ext)
                {
                    Path = path; Size = size; Ext = ext;
                }
            }
            #endregion

            #region 窗体设计器部分
            private System.ComponentModel.IContainer components = null;
            protected override void Dispose(bool disposing)
            {
                if (disposing && components != null) components.Dispose();
                base.Dispose(disposing);
            }

            private Button btnScan;
        private Button btnstop;
        private Button btnSelectAll;
            private Button btnInvertSel;
            private Button btnClearSel;
            private Button btnCleanSelected;
            private ListView listView1;
            private ProgressBar progressBar1;
            private Label lblStatus;
            private Label lblSelectInfo;
            private BackgroundWorker bgWorkerScan;
        private System.Windows.Forms.LinkLabel linkLabelFooter;

        private void InitializeComponent()
            {
                this.btnScan = new Button();
            this.btnstop = new Button();
            this.btnSelectAll = new Button();
                this.btnInvertSel = new Button();
                this.btnClearSel = new Button();
                this.btnCleanSelected = new Button();
                this.listView1 = new ListView();
                this.progressBar1 = new ProgressBar();
                this.lblStatus = new Label();
                this.lblSelectInfo = new Label();
                this.bgWorkerScan = new BackgroundWorker();
                this.SuspendLayout();

                // btnScan
                this.btnScan.Location = new System.Drawing.Point(12, 12);
                this.btnScan.Size = new System.Drawing.Size(100, 30);
                this.btnScan.Text = "开始扫描";
                this.btnScan.UseVisualStyleBackColor = true;
                this.btnScan.Click += new EventHandler(this.btnScan_Click);

                // btnSelectAll
                this.btnSelectAll.Location = new System.Drawing.Point(118, 12);
                this.btnSelectAll.Size = new System.Drawing.Size(75, 30);
                this.btnSelectAll.Text = "全选";
                this.btnSelectAll.UseVisualStyleBackColor = true;
                this.btnSelectAll.Click += new EventHandler(this.btnSelectAll_Click);

                // btnInvertSel
                this.btnInvertSel.Location = new System.Drawing.Point(199, 12);
                this.btnInvertSel.Size = new System.Drawing.Size(75, 30);
                this.btnInvertSel.Text = "反选";
                this.btnInvertSel.UseVisualStyleBackColor = true;
                this.btnInvertSel.Click += new EventHandler(this.btnInvertSel_Click);

                // btnClearSel
                this.btnClearSel.Location = new System.Drawing.Point(280, 12);
                this.btnClearSel.Size = new System.Drawing.Size(75, 30);
                this.btnClearSel.Text = "清空选择";
                this.btnClearSel.UseVisualStyleBackColor = true;
                this.btnClearSel.Click += new EventHandler(this.btnClearSel_Click);


            this.btnstop.Location = new System.Drawing.Point(365, 12);
            this.btnstop.Size = new System.Drawing.Size(100, 30);
            this.btnstop.Text = "停止扫描";
            this.btnstop.UseVisualStyleBackColor = true;
            this.btnstop.Click += new EventHandler(this.btnstop_Click);

            // btnCleanSelected
            this.btnCleanSelected.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                this.btnCleanSelected.Location = new System.Drawing.Point(640, 12);
                this.btnCleanSelected.Size = new System.Drawing.Size(120, 30);
                this.btnCleanSelected.Text = "清理选中项";
                this.btnCleanSelected.UseVisualStyleBackColor = true;
                this.btnCleanSelected.Click += new EventHandler(this.btnCleanSelected_Click);

                // listView1
                this.listView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                this.listView1.Location = new System.Drawing.Point(12, 48);
                this.listView1.Size = new System.Drawing.Size(748, 340);

                // progressBar1
                this.progressBar1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                this.progressBar1.Location = new System.Drawing.Point(12, 394);
                this.progressBar1.Size = new System.Drawing.Size(748, 18);

                // lblSelectInfo
                this.lblSelectInfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                this.lblSelectInfo.AutoSize = true;
                this.lblSelectInfo.Location = new System.Drawing.Point(12, 418);
                this.lblSelectInfo.Text = "已勾选：0 项，共 0B";

                // lblStatus
                this.lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                this.lblStatus.AutoSize = true;
                this.lblStatus.Location = new System.Drawing.Point(400, 418);
                this.lblStatus.Text = "等待操作";

            // linkLabelFooter 底部开源+捐赠超链接
            // linkLabelFooter 底部开源+捐赠超链接
            this.linkLabelFooter = new System.Windows.Forms.LinkLabel();
            this.linkLabelFooter.Location = new System.Drawing.Point(24, 448);
            this.linkLabelFooter.Name = "linkLabelFooter";
            this.linkLabelFooter.Size = new System.Drawing.Size(740, 20);
            this.linkLabelFooter.TabIndex = 2;
            this.linkLabelFooter.Text = "本软件已开源获取源码        如果你觉得软件好，点击这里捐赠";
            this.linkLabelFooter.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelFooter_LinkClicked);
            // 移除旧的LinkArea写法，改用Add Link，.NET4.5.2原生支持
            this.linkLabelFooter.Links.Add(0, 7, "opensource");
            this.linkLabelFooter.Links.Add(22, 8, "donate");

            // bgWorkerScan
            this.bgWorkerScan.DoWork += bgWorkerScan_DoWork;
                this.bgWorkerScan.RunWorkerCompleted += bgWorkerScan_RunWorkerCompleted;

                // FrmMain
                this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
                this.AutoScaleMode = AutoScaleMode.Font;
                this.ClientSize = new System.Drawing.Size(772, 500);
                this.Controls.Add(this.btnScan);
                this.Controls.Add(this.btnSelectAll);
                this.Controls.Add(this.btnInvertSel);
                this.Controls.Add(this.btnClearSel);
                this.Controls.Add(this.btnCleanSelected);
                this.Controls.Add(this.listView1);
                this.Controls.Add(this.progressBar1);
                this.Controls.Add(this.lblSelectInfo);
                this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnstop);
            this.Controls.Add(this.linkLabelFooter);

            this.Name = "FrmMain";
                this.Text = "磁盘垃圾清理工具—未来之窗东方仙盟-(CyberWin-20225578-PPUH-O874-0C44OON20NON)";
                this.ResumeLayout(false);
                this.PerformLayout();
            // 窗口打开，在屏幕正中
              this.StartPosition = FormStartPosition.CenterScreen;
            //隐藏左上角图标
            this.ShowIcon = false;
            //科技淡蓝色背景
               this.BackColor = Color.FromArgb(229, 241, 251);
            this.bgWorkerScan.WorkerSupportsCancellation = true;
        }
        #endregion
       
        /// <summary>
        /// lic文件完整路径：程序根目录\CleanToolWin.lic
        /// </summary>
        private static readonly string LicFilePath = Path.Combine(Application.StartupPath, "CleanToolWin.lic");
        public void cyber_增加浏览器控制()
        {
            //加入内网控制
            //
            // string url = "http://51.onelink.ynwlzc.cn/CyberWinAPI.cwas/API_CyberWinWebBrowser/" + get_SetConfig("authen_appid");

            string 认证服务器 = "768B812A56693D7C683CE3CA99A515413105D743B59FC2E2D88C8B2AADFB2F67C95D68909A5C10A94BC89E68E7A06369C01E32FE198CF219675F733C25EF100C80038FBCF50A0D41";
            // string url = CyberWin.CSHARP.YNWLZC.WebFrame.CyberPHP.CyberSafe._Encrpt.DESEncrypt.Decrypt(认证服务器, "20225578");
            string url = "http://51.onelink.ynwlzc.net/o2o/cpdw_api/cwp_lic_center.cwas?u=1";

            //  Cyber_TokenKey.CL_Token cl = new Cyber_TokenKey.CL_Token();
            //  string mac_md5 = cl.getLocalMacMd5(true);
            //  string mac = cl.getLocalMac();
            string mac = MacHelper.GetFirstPhysicalMac();
            //  CyberWin_JsStandardPlugV2005 cjsv2005 = new CyberWin_JsStandardPlugV2005();

            //   string DeviceUUid =cjsv2005.getDeviceUUid();
            //直接静态调用，不需要new对象
            string DeviceUUid  = CyberinLicHelper.Get();// 

           // Public_Var.未来之窗系统sn = DeviceUUid;

            string client_sn = "&client_mac=" + mac + "&client_sn=" + DeviceUUid;// mac_md5;

            string client_snv2026 = "&software=东方CyberWinCleanToolWin&client_mac=" + mac + "&client_sn=" + DeviceUUid;// mac_md5;

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
               // cyber_open_signal("仙盟召唤", "http://app.ynwlzc.net/fairyalliance_callhaltnotice?client_sn" + DeviceUUid);

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
        private void linkLabelFooter_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

            string openSourceUrl = "https://gitee.com/cybersnow/CyberWinCleanToolWin"; //替换你的开源地址
            string donateUrl = "http://app.ynwlzc.net/app.php/appshow/BRXv?1=1"; //替换你的捐赠地址


            string mac = MacHelper.GetFirstPhysicalMac();
            //  CyberWin_JsStandardPlugV2005 cjsv2005 = new CyberWin_JsStandardPlugV2005();

            //   string DeviceUUid =cjsv2005.getDeviceUUid();
            //直接静态调用，不需要new对象
            string DeviceUUid = CyberinLicHelper.Get();// 

            // Public_Var.未来之窗系统sn = DeviceUUid;


              string client_snv2026 = "&software=东方CyberWinCleanToolWin&client_mac=" + mac + "&client_sn=" + DeviceUUid;// mac_md5;
        
            donateUrl = donateUrl + client_snv2026;

            int start = e.Link.Start;
            int len = e.Link.Length;

            if (start == 0 && len == 7)
            {
                //打开开源地址
                System.Diagnostics.Process.Start(openSourceUrl);
            }
            else if (start == 22 && len == 8)
            {
                //打开捐赠地址
                System.Diagnostics.Process.Start(donateUrl);

            }
        }
    }
    }


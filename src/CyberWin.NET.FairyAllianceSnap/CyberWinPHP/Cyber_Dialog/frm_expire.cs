using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CyberWin.NET.FairyAllianceSnap.CyberwinPHP.Cyber_Dialog
{
   

     
        /// <summary>
        /// 授权到期弹窗窗体，单文件完整class，可直接复制使用
        /// </summary>
        public class frm_expire : Form
        {
            private System.ComponentModel.IContainer components = null;
            private System.Windows.Forms.Button okButton;
            private System.Windows.Forms.PictureBox pictureBox1;
            private System.Windows.Forms.Label label1;
            private System.Windows.Forms.Label label2;
            private System.Windows.Forms.PictureBox pictureBox2;


            public frm_expire(string serialno)
            {
                InitializeComponent();

                string content = "https://51.onelink.ynwlzc.cn/o2o/cyberwin_xcxqr/index.php/ApplicationclientAPI/o2owlzcactive?client_uuid=" + serialno + "&software=cyberwinCleanToolWin";
                try
                {
                    string quurl = "http://51.onelink.ynwlzc.net/cyber_lib/qr/cyber_qrg.php?url=" + UrlEncode(content);
                    this.pictureBox1.Load(quurl);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("配置加载异常");
                }
            }

            public static string UrlEncode(string str)
            {
                StringBuilder sb = new StringBuilder();
                byte[] byStr = System.Text.Encoding.UTF8.GetBytes(str);
                for (int i = 0; i < byStr.Length; i++)
                {
                    sb.Append(@"%" + Convert.ToString(byStr[i], 16));
                }
                return (sb.ToString());
            }

            #region Assembly Attribute Accessors
            public string AssemblyTitle
            {
                get
                {
                    object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                    if (attributes.Length > 0)
                    {
                        AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                        if (titleAttribute.Title != "")
                        {
                            return titleAttribute.Title;
                        }
                    }
                    return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
                }
            }

            public string AssemblyVersion
            {
                get
                {
                    return Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
            }

            public string AssemblyDescription
            {
                get
                {
                    object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                    if (attributes.Length == 0)
                    {
                        return "";
                    }
                    return ((AssemblyDescriptionAttribute)attributes[0]).Description;
                }
            }

            public string AssemblyProduct
            {
                get
                {
                    object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                    if (attributes.Length == 0)
                    {
                        return "";
                    }
                    return ((AssemblyProductAttribute)attributes[0]).Product;
                }
            }

            public string AssemblyCopyright
            {
                get
                {
                    object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                    if (attributes.Length == 0)
                    {
                        return "";
                    }
                    return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
                }
            }

            public string AssemblyCompany
            {
                get
                {
                    object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                    if (attributes.Length == 0)
                    {
                        return "";
                    }
                    return ((AssemblyCompanyAttribute)attributes[0]).Company;
                }
            }
            #endregion


            private void logoPictureBox_Click(object sender, EventArgs e)
            {

            }

            private void okButton_Click(object sender, EventArgs e)
            {
                Application.Exit();
            }

            private void frm_expire_Load(object sender, EventArgs e)
            {

            }


            /// <summary>
            /// Clean up any resources being used.
            /// </summary>
            /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
            protected override void Dispose(bool disposing)
            {
                if (disposing && (components != null))
                {
                    components.Dispose();
                }
                base.Dispose(disposing);
            }

            #region Windows Form Designer generated code
            /// <summary>
            /// Required method for Designer support - do not modify
            /// the contents of this method with the code editor.
            /// </summary>
            private void InitializeComponent()
            {
                this.okButton = new System.Windows.Forms.Button();
                this.pictureBox1 = new System.Windows.Forms.PictureBox();
                this.label1 = new System.Windows.Forms.Label();
                this.label2 = new System.Windows.Forms.Label();
                this.pictureBox2 = new System.Windows.Forms.PictureBox();
                ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
                ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
                this.SuspendLayout();
                // 
                // okButton
                // 
                this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
                this.okButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                this.okButton.Location = new System.Drawing.Point(269, 295);
                this.okButton.Name = "okButton";
                this.okButton.Size = new System.Drawing.Size(76, 43);
                this.okButton.TabIndex = 24;
                this.okButton.Text = "知道了&OK";
                this.okButton.Click += new System.EventHandler(this.okButton_Click);
                // 
                // pictureBox1
                // 
              //  this.pictureBox1.Image = Properties.Resources.qr;
                this.pictureBox1.Location = new System.Drawing.Point(12, 55);
                this.pictureBox1.Name = "pictureBox1";
                this.pictureBox1.Size = new System.Drawing.Size(212, 204);
                this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
                this.pictureBox1.TabIndex = 25;
                this.pictureBox1.TabStop = false;
                // 
                // label1
                // 
                this.label1.AutoSize = true;
                this.label1.Font = new System.Drawing.Font("宋体", 15F);
                this.label1.Location = new System.Drawing.Point(30, 25);
                this.label1.Name = "label1";
                this.label1.Size = new System.Drawing.Size(229, 20);
                this.label1.TabIndex = 26;
                this.label1.Text = "授权已经到期，扫码激活";
                // 
                // label2
                // 
                this.label2.Font = new System.Drawing.Font("宋体", 15F);
                this.label2.Location = new System.Drawing.Point(36, 272);
                this.label2.Name = "label2";
                this.label2.Size = new System.Drawing.Size(614, 20);
                this.label2.TabIndex = 27;
                this.label2.Text = "请联打开微信《优然数字生活服务》公众号，点击查看终端授权";
                // 
                // pictureBox2
                // 
              //  this.pictureBox2.Image = Properties.Resources.qr;
                this.pictureBox2.Location = new System.Drawing.Point(359, 55);
                this.pictureBox2.Name = "pictureBox2";
                this.pictureBox2.Size = new System.Drawing.Size(212, 204);
                this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
                this.pictureBox2.TabIndex = 28;
                this.pictureBox2.TabStop = false;
                // 
                // frm_expire
                // 
                this.AcceptButton = this.okButton;
                this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                this.ClientSize = new System.Drawing.Size(662, 347);
                this.Controls.Add(this.pictureBox2);
                this.Controls.Add(this.label2);
                this.Controls.Add(this.label1);
                this.Controls.Add(this.pictureBox1);
                this.Controls.Add(this.okButton);
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.Name = "frm_expire";
                this.Padding = new System.Windows.Forms.Padding(9, 8, 9, 8);
                this.ShowIcon = false;
                this.ShowInTaskbar = false;
                this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                this.Text = "授权已经到期，请联打开微信《优然数字生活服务》公众号，点击查看终端授权";
                this.TopMost = true;
                this.Load += new System.EventHandler(this.frm_expire_Load);
                ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
                ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
                this.ResumeLayout(false);
                this.PerformLayout();
            }
            #endregion
        }
    }

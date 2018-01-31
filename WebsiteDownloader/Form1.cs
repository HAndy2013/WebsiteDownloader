using SharpHttp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebsiteDownloader
{
    public partial class Form1 : Form
    {
        Downloader _downloader = null;

        public Form1()
        {
            InitializeComponent();

            this.btnBeginDownload.Click += BtnBeginDownload_Click;
            this.btnStop.Click += BtnStop_Click;
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _downloader.Stop = true;
            btnBeginDownload.Enabled = true;
        }

        private async void BtnBeginDownload_Click(object sender, EventArgs e)
        {
            btnBeginDownload.Enabled = false;
            var url = txtBaseUrl.Text;
            var threadNum = txtMaxThreadNum.Text;
            var tn = 0;
            int.TryParse(threadNum, out tn);
            if (tn == 0) tn = 5;
            var client = new SharpHttpClient();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("URL 不能为空");
                btnBeginDownload.Enabled = true;
                return;
            }

            if (_downloader == null)
            {
                _downloader = new Downloader(client,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files"),
                    url);
            }
            _downloader.Stop = false;
            _downloader.DownloadImgs = false;
            await Task.Run(() => { _downloader.DownloadSite(ShowInfo); });

            btnBeginDownload.Enabled = true;
        }

        private void ShowInfo(RuntimeInfo info)
        {
            var msg = "";
            if (string.IsNullOrEmpty( info.ErrorMsg))
            {
                msg = string.Format("{0} downloaded url:{1}"
                    , info.ExeTime.ToString("yyyy-MM-dd HH:mm:ss"), info.Url);
            }
            else
            {
                msg = string.Format("{0} an error occored when downloading url:{1}, error message:{2}"
                    , info.ExeTime.ToString("yyyy-MM-dd HH:mm:ss"), info.Url, info.ErrorMsg);
            }
            var total = info.TotalNum;
            var cur = info.DownloadedNum;
            this.Invoke(new Action(() =>
            {
                txtInfo.Text += msg + Environment.NewLine;
                txtInfo.SelectionStart = txtInfo.Text.Length;

                progressBar1.Maximum = total;
                progressBar1.Value = cur;                
            }));
        }
        
    }
}

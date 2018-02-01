using SharpHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebsiteDownloader.Console
{
    class Program
    {
        static Downloader.Downloader _downloader;
        static void Main(string[] args)
        {
            if (args.Any())
            {
                var folderName = "";
                var url = "";                
                var al = args.ToList();
                if (al.Any(x => x == "-u"))
                {
                    url = al.ElementAt(al.IndexOf("-u") + 1);
                    folderName = url.Replace("http://", "")
                        .Replace("https://", "")
                        .Replace("/", "");

                    var client = new SharpHttpClient();
                    client.SetUserAgent(UserAgent.BAIDU_SPIDER);
                    if (_downloader == null)
                    {
                        _downloader = new Downloader.Downloader(client,
                                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName),
                                url);
                        
                        _downloader.DownloadSite();
                    }
                }          
            }            
        }
    }
}

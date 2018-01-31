using SharpHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebsiteDownloader
{
    public class Downloader
    {
        private SharpHttp.SharpHttpClient _client;
        private string _saveFolder;
        private List<string> _unDownloadUrls;
        private List<string> _downloadedUrls;
        private string _baseUrl;

        public bool DownloadImgs { get; set; } = true;
        public int ThreadNum { get; set; } = 5;
        public bool Stop { get; set; } = false;

        public Downloader(SharpHttp.SharpHttpClient client, string saveFolder ,string baseUrl)
        {
            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }
            _client = client;
            //_client.SetUserAgent(UserAgent.BAIDU_SPIDER);

            _saveFolder = saveFolder;
            _unDownloadUrls = new List<string>();
            _downloadedUrls = new List<string>();
            _baseUrl = baseUrl;
            _unDownloadUrls.Add(_baseUrl);
        }

        public void DownloadSite(Action<RuntimeInfo> action = null)
        {
            while (_unDownloadUrls.Count > 0 && !Stop)
            {
                var list = _unDownloadUrls.ToList();
                var total = 0;
                while (true && !Stop)
                {
                    if (total >= list.Count)
                    {
                        break;
                    }
                    var urls = list.Skip(total).Take(ThreadNum);
                    var ts = new Task[urls.Count()];
                    for (int i = 0; i < urls.Count(); i++)
                    {
                        var index = i;
                        ts[i] = DownloadPage(urls.ElementAt(index), action);
                    }
                    Task.WaitAll(ts);
                    total += urls.Count();
                }
            }
        }

        public async Task DownloadPage(string url, Action<RuntimeInfo> action = null)
        {
            try
            {
                var resp = await _client.GetAsync(url);
                SavePage(resp.ResponseRawString, resp.Charset, url);
                _downloadedUrls.Add(url);
                _unDownloadUrls.Remove(url);

                RuntimeInfo r = new RuntimeInfo
                {
                    DownloadedNum = _downloadedUrls.Count,
                    ExeTime = DateTime.Now,
                    TotalNum = _unDownloadUrls.Count + _downloadedUrls.Count,
                    Url = url,
                };
                if (action != null)
                {
                    action.Invoke(r);
                }

                var urls = GetUrls(resp.ResponseString, url).Where(x => x != string.Empty);
                foreach (var item in urls)
                {
                    if (!_downloadedUrls.Contains(item))
                    {
                        _unDownloadUrls.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                RuntimeInfo r = new RuntimeInfo
                {
                    DownloadedNum = _downloadedUrls.Count,
                    ErrorMsg = ex.Message,
                    ExeTime = DateTime.Now,
                    TotalNum = _unDownloadUrls.Count + _downloadedUrls.Count,
                    Url = url,
                };

                if (action != null)
                {
                    action.Invoke(r);
                }
            }
        }

        private void SavePage(string content,string charset, string url)
        {
            var uri = new Uri(url);
            var folder = string.Empty;
            var pageName = string.Empty;

            if (uri.Segments.Length <= 2)
            {
                folder = _saveFolder;
                pageName = uri.Segments.Last();
                if (uri.Segments.Length <= 1)
                {
                    pageName = "index.html";
                }
            }
            else
            {
                folder = Path.Combine(_saveFolder, string.Join("", uri.Segments.Skip(1).Take(uri.Segments.Length - 2)));
                pageName = uri.Segments.Last();
            }

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var fileName = Path.Combine(folder, pageName);

            var encoding = Encoding.GetEncoding(charset) ?? Encoding.UTF8;
            File.WriteAllText(fileName, content, encoding);
        }

        private List<string> GetUrls(string content, string url)
        {
            var doc = NSoup.NSoupClient.Parse(content);
            var aList = doc.Select("a").ToList()
                .Where(a => !string.IsNullOrEmpty(a.Attr("href"))&&!a.Attr("href").StartsWith("javascript"))
                .Select(x =>
                {
                    var href = x.Attr("href");
                    if (href.IndexOf("#") > 0)
                    {
                        return href.Substring(0, href.IndexOf("#"));
                    }
                    return href;
                }).Distinct();

            var scripts = doc.Select("script").ToList()
                .Where(x => !string.IsNullOrEmpty(x.Attr("src")) &&( x.Attr("src").StartsWith("/")|| x.Attr("src").StartsWith(".")))
                .Select(x => x.Attr("src")).Distinct();

            var csses = doc.Select("link").ToList()
                .Where(x =>
                    x.Attr("rel") == "stylesheet" &&
                    !string.IsNullOrEmpty(x.Attr("href")) &&
                    (x.Attr("href").StartsWith("/") || x.Attr("href").StartsWith(".")))
                .Select(x => x.Attr("href")).Distinct();

            IEnumerable<string> imgs = new List<string>();
            if (DownloadImgs)
            {
                imgs = doc.Select("img").ToList()
                    .Where(x => !string.IsNullOrEmpty(x.Attr("src")) &&
                    (x.Attr("src").StartsWith("/") || x.Attr("src").StartsWith(".")))
                    .Select(x => x.Attr("src")).Distinct();
            }

            var links = aList.Concat(scripts).Concat(csses);
            if (DownloadImgs)
            {
                links = links.Concat(imgs);
            }

            var list = new List<string>();
            foreach (var item in links)
            {
                var tempUrl = GetUrl(item, url);

                if (tempUrl != string.Empty)
                    list.Add(tempUrl);
            }
            return list;
        }

        private string GetUrl(string url,string pageUrl)
        {
            Uri baseuri;
            if (!Uri.TryCreate(pageUrl, UriKind.RelativeOrAbsolute, out baseuri))
            {
                return "";
            }
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                return "";
            }


            if (!(url.StartsWith("http://") || url.StartsWith("https://")))
            {
                if (url.StartsWith("."))
                {
                    var dotCount = url.IndexOf("/");
                    if (baseuri.Segments.Count() - dotCount > 0)
                    {
                        url = "/" + string.Join("", baseuri.Segments.Skip(baseuri.Segments.Count() - dotCount))
                            + Regex.Replace(url, "\\.+/", "");
                    }
                }
                if (url.StartsWith("/"))
                {
                    return string.Format("{0}://{1}{2}",
                        baseuri.Scheme, baseuri.Host, baseuri.IsDefaultPort ? "" : ":" + baseuri.Port)
                        + url;
                }
                else
                {
                    var pageUri = new Uri(pageUrl);
                    if (pageUri.AbsolutePath == "/")
                    {
                        return string.Format("{0}://{1}{2}/",
                            baseuri.Scheme, baseuri.Host, baseuri.IsDefaultPort ? "" : ":" + baseuri.Port)
                            + url;
                    }
                    else
                    {
                        return string.Join("", pageUri.Segments.Take(pageUri.Segments.Length - 1)) + url;
                    }
                }
            }

            var cururi = new Uri(url);
            var domain = new Uri(_baseUrl).Host;
            if (cururi.Host != domain)
            {
                return "";
            }
            return url;
        }
    }

    public class RuntimeInfo
    {
        public string Url { get; set; }

        public int TotalNum { get; set; }

        public int DownloadedNum { get; set; }

        public string ErrorMsg { get; set; }

        public DateTime ExeTime { get; set; }
    }
}

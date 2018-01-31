using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpHttp;

namespace WebsiteDownloader.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var client = new SharpHttpClient();
            var task = client.GetAsync("https://www.baidu.com");
            task.Wait();
        }
    }
}

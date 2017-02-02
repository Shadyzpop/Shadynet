using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shadynet.Http;
using Shadynet.Proxy;
using Shadynet.Threading;
using Shadynet.Other;
using System.Text.RegularExpressions;

namespace Shadynet.Other
{
    public class Spider : IDisposable
    {
        private Uri BaseAddress { set; get; } 

        public Spider(string baseAddress)
        {
            #region Check settings

            if (baseAddress == null)
            {
                throw new ArgumentNullException("baseAddress");
            }

            if (baseAddress.Length == 0)
            {
                throw ExceptionHelper.EmptyString("baseAddress");
            }

            #endregion

            if (!baseAddress.StartsWith("http"))
            {
                baseAddress = "http://" + baseAddress;
            }
            var uri = new Uri(baseAddress);
            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "baseAddress");
            }

            BaseAddress = uri;

        }

        #region Proxies Class

        public class Proxies
        {

            private string _proxy { set; get; }

            private Uri _judge { set; get; }

            public Proxies(string proxy = null)
            {
                _proxy = proxy;
            }

            public Proxies(string proxy, Uri judge)
            {
                _proxy = proxy;
                _judge = judge;
            }

            #region Methods(Open)
            
            public async Task<string[]> ScrapeProxies(string[] urls)
            {
                return await Task.Run(() =>
                {
                    List<string> data = new List<string>();
                    string pattern = @"\d{1,3}(\.\d{1,3}){3}:\d{1,5}";
                    try
                    {
                        using (HttpRequest req = new HttpRequest())
                        {
                            req.UserAgent = HttpHelper.ChromeUserAgent();
                            req.IgnoreProtocolErrors = true;
                            req.AllowAutoRedirect = true;
                            foreach (var url in urls)
                            {
                                var res = req.Get(url);
                                MatchCollection proxies = Regex.Matches(res.ToString(), pattern);
                                foreach (var proxy in proxies)
                                    data.Add(proxy.ToString());
                            }
                        }
                    }
                    catch { }
                    return data.ToArray();
                });
            }

            public async Task<string[,]> ProxyCheck(string proxy, string url, bool autoredirect, bool reconnect = false, int timeout = 0 , int ptype = 0)
            {
                return await Task.Run(() =>
                {
                    string[,] data = new string[1, 2];
                    try
                    {
                        using (HttpRequest req = new HttpRequest(url))
                        {
                            req.UserAgent = HttpHelper.ChromeUserAgent();
                            req.Cookies = new CookieCore(false);
                            req.AllowAutoRedirect = autoredirect;
                            req.Reconnect = reconnect;
                            req.SslCertificateValidatorCallback = HttpHelper.AcceptAllCertificationsCallback;
                            req.ConnectTimeout = timeout;

                            #region proxyset
                            switch (ptype)
                            {
                                case 0:
                                    req.Proxy = HttpProxyClient.Parse(proxy);
                                    break;
                                case 1:
                                    req.Proxy = Socks4ProxyClient.Parse(proxy);
                                    break;
                                case 2:
                                    req.Proxy = Socks4aProxyClient.Parse(proxy);
                                    break;
                                case 3:
                                    req.Proxy = Socks5ProxyClient.Parse(proxy);
                                    break;
                                default:
                                    break;
                            }
                            #endregion
                            var res = req.Get("/");
                            data[0, 0] = proxy;
                            data[0, 1] = res.ConnectionTime.ToString();
                            return data;
                        }
                    }
                    catch
                    {
                        return null;
                    }
                });
            }

            public async Task<bool> isProxyAnon(string proxy,ProxyType type,string judge = "http://proxyjudge.info")
            {
                return await Task<bool>.Run(async () =>
                {
                    try
                    {
                        using(HttpRequest req = new HttpRequest(judge))
                        {
                            req.SslCertificateValidatorCallback = HttpHelper.AcceptAllCertificationsCallback;
                            req.UserAgent = HttpHelper.ChromeUserAgent();
                            req.Proxy = ChainProxyClient.Parse(type, proxy);
                            req.ConnectTimeout = 1000;
                            req.IgnoreProtocolErrors = true;
                            var res = req.Get("/");
                            var content = res.ToString();
                            var myip = await this.Myip();
                            if (content != "" && !content.Contains(myip))
                            {
                                res.cLogger();
                                return true;
                            }
                            else
                                return false;
                        }
                    }
                    catch { return false; }
                });
            }

            public async Task<string> Myip()
            {
                return await Task<string>.Run(async () =>
                {
                    try
                    {
                        using(HttpRequest req = new HttpRequest("http://proxyjudge.info"))
                        {
                            req.UserAgent = HttpHelper.ChromeUserAgent();
                            var res = await req.GetAsync("/");
                            var data = Helper.Betweenstring(res.ToString(), "REMOTE_ADDR = ", "\n");
                            return data;
                        }
                    }
                    catch { return ""; }
                });
            }

            #endregion
        }

        #endregion

        #region Scrape Class

        public class Scrape
        {

            #region blogspot?
            public string[] BlogSpotUrls(string url)
            {
                List<string> uris = new List<string>();
                try
                {
                    using (HttpRequest req = new HttpRequest())
                    {
                        var res = req.Get(url);
                        var purl = Html.HTMLparse(res.ToString(), "href", "source", "blogger:blog:plusone", "g:plusone", 1, 1);
                        foreach (var a in purl)
                            uris.Add(a);
                    }
                }
                catch { }
                return uris.ToArray();
            }
            #endregion

            #region Working with images
            public string[] ImageScrape(string Source, string Element, string Imgtype = ".jpeg", string Origin = "")
            {
                #region Settings
                if (string.IsNullOrEmpty(Source) || string.IsNullOrEmpty(Element))
                    return null;

                bool finished = false;
                List<string> data = new List<string>();
                StringBuilder _source = new StringBuilder(Source);
                string typeorigin = string.Empty;
                if (Origin != "")
                    typeorigin = Origin;

                string ElemLeft = Element + "=\"";
                string imgRight = Imgtype + "\"";
                #endregion

                while (!finished)
                {
                    if (!string.IsNullOrEmpty(typeorigin))
                    {
                        if (_source.ToString().Contains(Element) || _source.ToString().Contains(Imgtype) || _source.ToString().Contains(typeorigin))
                        {
                            var imgi = Helper.Betweenstring(_source.ToString(), ElemLeft, imgRight);
                            Console.WriteLine(imgi);
                            data.Add(imgi);
                            int Leftindex = _source.ToString().IndexOf(ElemLeft);
                            if (Leftindex < 0)
                                Leftindex = 0;
                            int Rightindex = _source.ToString().IndexOf(imgRight);
                            _source.Remove(Leftindex, Rightindex);
                        }
                        else
                            finished = true;
                    }
                    else
                    {
                        if (_source.ToString().Contains(Element) || _source.ToString().Contains(Imgtype))
                        {
                            var imgi = Helper.Betweenstring(_source.ToString(), ElemLeft, imgRight);
                            Console.WriteLine(imgi);
                            data.Add(imgi);
                            int Leftindex = _source.ToString().IndexOf(ElemLeft);
                            if (Leftindex < 0)
                                Leftindex = 0;
                            int Rightindex = _source.ToString().IndexOf(imgRight);
                            _source.Remove(Leftindex, Rightindex);
                        }
                        else
                            finished = true;
                    }
                }
                return data.ToArray();
            }

            #endregion
            
        }

        #endregion

        public void Dispose()
        {
            BaseAddress = null;
        }
    }
}

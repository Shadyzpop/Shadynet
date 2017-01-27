using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shadynet.Http;
using Shadynet.Proxy;
using Shadynet.Threading;
using System.Text.RegularExpressions;

namespace Shadynet.Other
{
    public class Spider
    {
        #region Proxies Class

        public class Proxies
        {

            #region Methods(Open)

            public string[] ScrapeProxies(string[] urls)
            {
                List<string> data = new List<string>();
                string pattern = @"\d{1,3}(\.\d{1,3}){3}:\d{1,5}";

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
                return data.ToArray();
            }

            public string[,] ProxyCheck(string proxy, string url,int timeout, int ptype = 0)
            {
                string[,] data = new string[1,2];
                try
                {
                    using (HttpRequest req = new HttpRequest(url))
                    {
                        req.UserAgent = HttpHelper.ChromeUserAgent();
                        req.Cookies = new CookieCore(false);
                        req.AllowAutoRedirect = true;
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
            }

        }
        #endregion

        #endregion
    }
}

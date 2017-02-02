using System;
using Shadynet.Http;
using Shadynet.Proxy;
using Shadynet.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shadynet
{
    /// <summary>
    /// Represends a new forge of <see cref="Helper"/>, really useful.
    /// </summary>
    public class Helper
    {

        #region Methods(Open)

        /// <summary>
        /// Returns a string between two strings, starts from <paramref name="strStart"/> to <paramref name="strEnd"/> from <paramref name="strSource"/>
        /// </summary>
        /// <param name="strSource">The source string of the context.</param>
        /// <param name="strStart">The head start of the param</param>
        /// <param name="strEnd">The tail end of the param</param>
        /// <returns>The string between <paramref name="strStart"/> and <paramref name="strEnd"/> from <paramref name="strSource"/></returns>
        public static string Betweenstring(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                try
                {
                    Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                    End = strSource.IndexOf(strEnd, Start);
                    if (strSource.Substring(Start, End - Start).Length <= 0)
                        return "";
                    else
                        return strSource.Substring(Start, End - Start);
                }
                catch (Exception ez)
                {
                    return ez.ToString();
                }
            }
            else
            {
                return "";
            }
        }
        
        /// <summary>
        /// Returns a string from a url using 'Get' between two strings, starts from <paramref name="strStart"/> to <paramref name="strEnd"/> from <paramref name="strSource"/>
        /// </summary>
        /// <param name="URL">The Url Returning source string of the context.</param>
        /// <param name="strStart">The head start of the param</param>
        /// <param name="strEnd">The tail end of the param</param>
        /// <returns>The string between <paramref name="strStart"/> and <paramref name="strEnd"/> from <paramref name="strSource"/></returns>
        public static string BetweenUrl(string URL,string strStart,string strEnd)
        {
            string strSource = "";
            try
            {
                using (Http.HttpRequest rq = new Http.HttpRequest())
                {
                    rq.UserAgent = HttpHelper.ChromeUserAgent();
                    strSource = rq.Get(URL).ToString();
                }
                int Start, End;
                if (strSource.Contains(strStart) && strSource.Contains(strEnd))
                {
                    Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                    End = strSource.IndexOf(strEnd, Start);
                    return strSource.Substring(Start, End - Start);
                }
                else
                {
                    return "";
                }
            }
           catch(HttpException ex)
            {
                return ex.ToString();
            }
        }

        /// <summary>
        /// Returns a string from a url using 'Get' between two strings, starts from <paramref name="strStart"/> to <paramref name="strEnd"/> from <paramref name="strSource"/>, Asynchronously.
        /// </summary>
        /// <param name="URL">The Url Returning source string of the context.</param>
        /// <param name="strStart">The head start of the param</param>
        /// <param name="strEnd">The tail end of the param</param>
        /// <returns>The string between <paramref name="strStart"/> and <paramref name="strEnd"/> from <paramref name="strSource"/></returns>
        public static async Task<string> BetweenUrlAsync(string URL, string strStart, string strEnd)
        {
            return await Task.Run(() =>
            {
                return BetweenUrl(URL, strStart, strEnd);
            });
        }

        /// <summary>
        /// Returns raw cookie value from a url using 'Get'.
        /// </summary>
        /// <param name="URL">The Url to be specified.</param>
        /// <param name="Cookie">Given cookie in the context.</param>
        /// <returns>Raw cookie value from the given 'URL' specified in 'Cookie'</returns>
        public static string Cookie(string URL, string cookie)
        {
            try
            {
                using (HttpRequest rq = new HttpRequest())
                {
                    rq.UserAgent = HttpHelper.ChromeUserAgent();
                    string strSource = rq.Get(URL).Cookies.ToString() + ";";
                    string res = Betweenstring(strSource, cookie + "=", ";");
                    return res;
                }
            }
            catch (HttpException ex)
            {
                return ex.ToString();
            }
        }

        /// <summary>
        /// Returns raw cookie value from a url using 'Get'.
        /// </summary>                                                
        /// <param name="URL">The Url to be specified.</param>        
        /// <param name="Cookie">Given cookie in the context.</param> 
        /// <returns>Raw cookie value from the given 'URL' specified in 'Cookie'</returns>
        public static async Task<string> CookieAsync(string URL, string cookie)
        {
            return await Task.Run(() =>
            {
                return Cookie(URL, cookie);
            });
        }

        public static async Task<string> Htmlsource(string url)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (HttpRequest req = new HttpRequest(url))
                    {
                        req.UserAgent = HttpHelper.ChromeUserAgent();
                        req.AllowAutoRedirect = true;
                        req.IgnoreProtocolErrors = true;
                        var res = req.Get("/");
                        return res.ToString();
                    }
                }
                catch { return ""; }
            });
        }
        
        #endregion
       
    }
}

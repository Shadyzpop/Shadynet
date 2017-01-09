using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadynet.Miscs
{

    /// <summary>
    /// Represends a new forge of <see cref="GetInfo"/>, Really useful if you want to get Anything from a request.
    /// </summary>
    public static class GetInfo
    {
        #region string
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
        #endregion

        #region stringurl
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
                using (HttpRequest rq = new HttpRequest())
                {
                    rq.UserAgent = Http.ChromeUserAgent();
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
        #endregion

        #region cookie
        /// <summary>
        /// Returns raw cookie value from a url using 'Get'.
        /// </summary>
        /// <param name="URL">The Url to be specified.</param>
        /// <param name="Cookie">Given cookie in the context.</param>
        /// <returns>Raw cookie value from the given 'URL' specified in 'Cookie'</returns>
        public static string Cookie(string URL, string Cookie)
        {
            try
            {
                using (HttpRequest rq = new HttpRequest())
                {
                    rq.UserAgent = Http.ChromeUserAgent();
                    string strSource = rq.Get(URL).Cookies.ToString() + ";";
                    string res = Betweenstring(strSource, Cookie + "=", ";");
                    return res;
                }
            }
            catch (HttpException ex)
            {
                return ex.ToString();
            }
        }
        #endregion

    }
}

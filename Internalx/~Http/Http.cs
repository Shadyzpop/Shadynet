using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using Microsoft.Win32;
using System.Net.Security;

namespace Shadynet
{
    /// <summary>
    /// It is a static class, designed to aid in working with the HTTP-report.
    /// </summary>
    public static class Http
    {
        #region Constants (open)

        /// <summary>
        /// Indicates a new line in the HTTP-report.
        /// </summary>
        public const string NewLine = "\r\n";

        /// <summary>
        /// delegate method, which takes all the SSL certificates.
        /// </summary>
        public static readonly RemoteCertificateValidationCallback AcceptAllCertificationsCallback;

        #endregion


        #region Static fields (internal)

        internal static readonly Dictionary<HttpHeader, string> Headers = new Dictionary<HttpHeader, string>()
        {
            { HttpHeader.Accept, "Accept" },
            { HttpHeader.AcceptCharset, "Accept-Charset" },
            { HttpHeader.AcceptLanguage, "Accept-Language" },
            { HttpHeader.AcceptDatetime, "Accept-Datetime" },
            { HttpHeader.CacheControl, "Cache-Control" },
            { HttpHeader.ContentType, "Content-Type" },
            { HttpHeader.Date, "Date" },
            { HttpHeader.Expect, "Expect" },
            { HttpHeader.From, "From" },
            { HttpHeader.IfMatch, "If-Match" },
            { HttpHeader.IfModifiedSince, "If-Modified-Since" },
            { HttpHeader.IfNoneMatch, "If-None-Match" },
            { HttpHeader.IfRange, "If-Range" },
            { HttpHeader.IfUnmodifiedSince, "If-Unmodified-Since" },
            { HttpHeader.MaxForwards, "Max-Forwards" },
            { HttpHeader.Pragma, "Pragma" },
            { HttpHeader.Range, "Range" },
            { HttpHeader.Referer, "Referer" },
            { HttpHeader.Upgrade, "Upgrade" },
            { HttpHeader.UserAgent, "User-Agent" },
            { HttpHeader.Via, "Via" },
            { HttpHeader.Warning, "Warning" },
            { HttpHeader.DNT, "DNT" },
            { HttpHeader.AccessControlAllowOrigin, "Access-Control-Allow-Origin" },
            { HttpHeader.AcceptRanges, "Accept-Ranges" },
            { HttpHeader.Age, "Age" },
            { HttpHeader.Allow, "Allow" },
            { HttpHeader.ContentEncoding, "Content-Encoding" },
            { HttpHeader.ContentLanguage, "Content-Language" },
            { HttpHeader.ContentLength, "Content-Length" },
            { HttpHeader.ContentLocation, "Content-Location" },
            { HttpHeader.ContentMD5, "Content-MD5" },
            { HttpHeader.ContentDisposition, "Content-Disposition" },
            { HttpHeader.ContentRange, "Content-Range" },
            { HttpHeader.ETag, "ETag" },
            { HttpHeader.Expires, "Expires" },
            { HttpHeader.LastModified, "Last-Modified" },
            { HttpHeader.Link, "Link" },
            { HttpHeader.Location, "Location" },
            { HttpHeader.P3P, "P3P" },
            { HttpHeader.Refresh, "Refresh" },
            { HttpHeader.RetryAfter, "Retry-After" },
            { HttpHeader.Server, "Server" },
            { HttpHeader.TransferEncoding, "Transfer-Encoding" }
        };

        #endregion


        #region Static fields (closed)
        private static bool IsParse = false;
        [ThreadStatic] private static Random _rand;
        private static Random Rand
        {
            get
            {
                if (_rand == null)
                    _rand = new Random();
                return _rand;
            }
        }

        #endregion


        static Http()
        {
            AcceptAllCertificationsCallback = new RemoteCertificateValidationCallback(AcceptAllCertifications);
        }


        #region Static methods (open)
        /// <summary>
        /// Parses Raw postdata into a parameters to add as a request to the http client.
        /// </summary>
        /// <param name="postdata">Raw post parameters.</param>
        /// <param name="httpclient">Http client to add the parameters to.</param>
        public static void ParsePostData(string postdata,HttpRequest httpclient)
        {
            try
            {
                if (postdata.Contains("&"))
                {
                    string[] datastruct = postdata.Split('&');
                    foreach (var data in datastruct)
                    {
                        var key = data.Split('=')[0].Trim();
                        var value = data.Split('=')[1].Trim();
                        httpclient.AddParam(key, value);
                    }
                }
                else
                {
                    var key = postdata.Split('=')[0].Trim();
                    var value = postdata.Split('=')[1].Trim();
                    httpclient.AddParam(key, value);
                }
                IsParse = true;
            }
            catch {
                throw new ArgumentException("Invalid Postdata or Bad HttpClient Given.");
            }
        }
        /// <summary>
        /// A boolean that return the status of the ParsePostData, if succeded <see langword="True"/> else <see langword="False"/>.
        /// </summary>
        /// <param name="postdata">Raw post parameters.</param>
        /// <param name="httpclient">Http client to add the parameters to.</param>
        /// <returns></returns>
        public static bool TryParsePostData(string postdata,HttpRequest httpclient)
        {
            ParsePostData(postdata, httpclient);
            return IsParse;
        }
        /// <summary>
        /// It encodes a string for reliable HTTP-server transfer.
        /// </summary>
        /// <param name="str">String to be encoded.</param>
        /// <param name="encoding">Encoding used to convert the data into a sequence of bytes. If the parameter value is <see langword="null"/>, the value will be used <see cref="System.Text.Encoding.UTF8"/>.</param>
        /// <returns>The encoded string.</returns>
        public static string UrlEncode(string str, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            encoding = encoding ?? Encoding.UTF8;

            byte[] bytes = encoding.GetBytes(str);

            int spaceCount = 0;
            int otherCharCount = 0;

            #region counting characters

            for (int i = 0; i < bytes.Length; i++)
            {
                char c = (char)bytes[i];

                if (c == ' ')
                {
                    ++spaceCount;
                }
                else if (!IsUrlSafeChar(c))
                {
                    ++otherCharCount;
                }
            }

            #endregion

            // If the string does not present the characters to be encoded.
            if ((spaceCount == 0) && (otherCharCount == 0))
            {
                return str;
            }

            int bufferIndex = 0;
            byte[] buffer = new byte[bytes.Length + (otherCharCount * 2)];

            for (int i = 0; i < bytes.Length; i++)
            {
                char c = (char)bytes[i];

                if (IsUrlSafeChar(c))
                {
                    buffer[bufferIndex++] = bytes[i];
                }
                else if (c == ' ')
                {
                    buffer[bufferIndex++] = (byte)'+';
                }
                else
                {
                    buffer[bufferIndex++] = (byte)'%';
                    buffer[bufferIndex++] = (byte)IntToHex((bytes[i] >> 4) & 15);
                    buffer[bufferIndex++] = (byte)IntToHex(bytes[i] & 15);
                }
            }

            return Encoding.ASCII.GetString(buffer);
        }

        /// <summary>
        /// Converts parameters in the query string.
        /// </summary>
        /// <param name="parameters">Options.</param>
        /// <param name="dontEscape">Specifies whether to encode the values of the parameters needed.</param>
        /// <returns>string query.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="parameters"/> equally <see langword="null"/>.</exception>
        public static string ToQueryString(IEnumerable<KeyValuePair<string, string>> parameters, bool dontEscape)
        {
            #region Check settings

            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            #endregion

            var queryBuilder = new StringBuilder();

            foreach (var param in parameters)
            {
                if (string.IsNullOrEmpty(param.Key))
                {
                    continue;
                }

                queryBuilder.Append(param.Key);
                queryBuilder.Append('=');

                if (dontEscape)
                {
                    queryBuilder.Append(param.Value);
                }
                else
                {
                    queryBuilder.Append(
                        Uri.EscapeDataString(param.Value ?? string.Empty));
                }

                queryBuilder.Append('&');
            }

            if (queryBuilder.Length != 0)
            {
                //Remove the '&' at the end.
                queryBuilder.Remove(queryBuilder.Length - 1, 1);
            }

            return queryBuilder.ToString();
        }

        /// <summary>
        /// Converts parameters in POST-query string.
        /// </summary>
        /// <param name="parameters">Options.</param>
        /// <param name="dontEscape">Specifies whether to encode the values ​​of the parameters needed.</param>
        /// <param name="encoding">Encoding used to convert the query parameters. If parameter equals <see langword="null"/>, the value will be used <see cref="System.Text.Encoding.UTF8"/>.</param>
        /// <returns>string query.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="parameters"/> equally <see langword="null"/>.</exception>
        public static string ToPostQueryString(IEnumerable<KeyValuePair<string, string>> parameters, bool dontEscape, Encoding encoding = null)
        {
            #region Check settings

            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            #endregion

            var queryBuilder = new StringBuilder();

            foreach (var param in parameters)
            {
                if (string.IsNullOrEmpty(param.Key))
                {
                    continue;
                }

                queryBuilder.Append(param.Key);
                queryBuilder.Append('=');

                if (dontEscape)
                {
                    queryBuilder.Append(param.Value);
                }
                else
                {
                    queryBuilder.Append(
                        UrlEncode(param.Value ?? string.Empty, encoding));
                }

                queryBuilder.Append('&');
            }

            if (queryBuilder.Length != 0)
            {
                // Remove the '&' at the end.
                queryBuilder.Remove(queryBuilder.Length - 1, 1);
            }

            return queryBuilder.ToString();
        }

        /// <summary>
        /// Defines and returns a MIME-type based on file extension.
        /// </summary>
        /// <param name="extension">File extension.</param>
        /// <returns>MIME-type.</returns>
        public static string DetermineMediaType(string extension)
        {
            string mediaType = "application/octet-stream";

            try
            {
                using (var regKey = Registry.ClassesRoot.OpenSubKey(extension))
                {
                    if (regKey != null)
                    {
                        object keyValue = regKey.GetValue("Content Type");

                        if (keyValue != null)
                        {
                            mediaType = keyValue.ToString();
                        }
                    }
                }
            }
            #region Catches

            catch (IOException) { }
            catch (ObjectDisposedException) { }
            catch (UnauthorizedAccessException) { }
            catch (SecurityException) { }

            #endregion

            return mediaType;
        }

        #region User Agent

        /// <summary>
        /// Generates Random User-Agent of the IE browser.
        /// </summary>
        /// <returns>Random User-Agent of the IE browser.</returns>
        public static string IEUserAgent()
        {
            string windowsVersion = RandomWindowsVersion();

            string version = null;
            string mozillaVersion = null;
            string trident = null;
            string otherParams = null;

            #region Generate a random version

            if (windowsVersion.Contains("NT 5.1"))
            {
                version = "9.0";
                mozillaVersion = "5.0";
                trident = "5.0";
                otherParams = ".NET CLR 2.0.50727; .NET CLR 3.5.30729";
            }
            else if (windowsVersion.Contains("NT 6.0"))
            {
                version = "9.0";
                mozillaVersion = "5.0";
                trident = "5.0";
                otherParams = ".NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.5.30729";
            }
            else
            {
                switch (Rand.Next(3))
                {
                    case 0:
                        version = "10.0";
                        trident = "6.0";
                        mozillaVersion = "5.0";
                        break;

                    case 1:
                        version = "10.6";
                        trident = "6.0";
                        mozillaVersion = "5.0";
                        break;

                    case 2:
                        version = "11.0";
                        trident = "7.0";
                        mozillaVersion = "5.0";
                        break;
                }

                otherParams = ".NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; .NET4.0C; .NET4.0E";
            }

            #endregion

            return string.Format(
                "Mozilla/{0} (compatible; MSIE {1}; {2}; Trident/{3}; {4})",
                mozillaVersion, version, windowsVersion, trident, otherParams);
        }

        /// <summary>
        /// Generates Random User-Agent of the Opera browser.
        /// </summary>
        /// <returns>Random User-Agent of the Opera browser.</returns>
        public static string OperaUserAgent()
        {
            string version = null;
            string presto = null;

            #region Generate a random version

            switch (Rand.Next(4))
            {
                case 0:
                    version = "12.16";
                    presto = "2.12.388";
                    break;

                case 1:
                    version = "12.14";
                    presto = "2.12.388";
                    break;

                case 2:
                    version = "12.02";
                    presto = "2.10.289";
                    break;

                case 3:
                    version = "12.00";
                    presto = "2.10.181";
                    break;
            }

            #endregion

            return string.Format(
                "Opera/9.80 ({0}); U) Presto/{1} Version/{2}",
                RandomWindowsVersion(), presto, version);
        }

        /// <summary>
        /// Generates Random User-Agent of the browser Chrome.
        /// </summary>
        /// <returns>Random User-Agent of the browser Chrome.</returns>
        public static string ChromeUserAgent()
        {
            string version = null;
            string safari = null;

            #region Generate a random version

            switch (Rand.Next(5))
            {
                case 0:
                    version = "41.0.2228.0";
                    safari = "537.36";
                    break;

                case 1:
                    version = "41.0.2227.1";
                    safari = "537.36";
                    break;

                case 2:
                    version = "41.0.2224.3";
                    safari = "537.36";
                    break;

                case 3:
                    version = "41.0.2225.0";
                    safari = "537.36";
                    break;

                case 4:
                    version = "41.0.2226.0";
                    safari = "537.36";
                    break;
            }

            #endregion

            return string.Format(
                "Mozilla/5.0 ({0}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{1} Safari/{2}",
                RandomWindowsVersion(), version, safari);
        }

        /// <summary>
        /// Generates Random User-Agent of the browser Firefox.
        /// </summary>
        /// <returns>Random User-Agent of the browser Firefox.</returns>
        public static string FirefoxUserAgent()
        {
            string gecko = null;
            string version = null;

            #region Generate a random version

            switch (Rand.Next(5))
            {
                case 0:
                    version = "36.0";
                    gecko = "20100101";
                    break;

                case 1:
                    version = "33.0";
                    gecko = "20100101";
                    break;

                case 2:
                    version = "31.0";
                    gecko = "20100101";
                    break;

                case 3:
                    version = "29.0";
                    gecko = "20120101";
                    break;

                case 4:
                    version = "28.0";
                    gecko = "20100101";
                    break;
            }

            #endregion

            return string.Format(
                "Mozilla/5.0 ({0}; rv:{1}) Gecko/{2} Firefox/{1}",
                RandomWindowsVersion(), version, gecko);
        }

        /// <summary>
        /// Generates Random User-Agent from a mobile browser Opera.
        /// </summary>
        /// <returns>Random User-Agent from a mobile browser Opera.</returns>
        public static string OperaMiniUserAgent()
        {
            string os = null;
            string miniVersion = null;
            string version = null;
            string presto = null;

            #region Generate a random version

            switch (Rand.Next(3))
            {
                case 0:
                    os = "iOS";
                    miniVersion = "7.0.73345";
                    version = "11.62";
                    presto = "2.10.229";
                    break;

                case 1:
                    os = "J2ME/MIDP";
                    miniVersion = "7.1.23511";
                    version = "12.00";
                    presto = "2.10.181";
                    break;

                case 2:
                    os = "Android";
                    miniVersion = "7.5.54678";
                    version = "12.02";
                    presto = "2.10.289";
                    break;
            }

            #endregion

            return string.Format(
                "Opera/9.80 ({0}; Opera Mini/{1}/28.2555; U; ru) Presto/{2} Version/{3}",
                os, miniVersion, presto, version);
        }

        #endregion

        #endregion


        #region Static methods (closed)

        private static bool AcceptAllCertifications(object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate certification,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static bool IsUrlSafeChar(char c)
        {
            if ((((c >= 'a') && (c <= 'z')) ||
                ((c >= 'A') && (c <= 'Z'))) ||
                ((c >= '0') && (c <= '9')))
            {
                return true;
            }

            switch (c)
            {
                case '(':
                case ')':
                case '*':
                case '-':
                case '.':
                case '_':
                case '!':
                    return true;
            }

            return false;
        }

        private static char IntToHex(int i)
        {
            if (i <= 9)
            {
                return (char)(i + 48);
            }

            return (char)((i - 10) + 65);
        }

        private static string RandomWindowsVersion()
        {
            string windowsVersion = "Windows NT ";

            switch (Rand.Next(4))
            {
                case 0:
                    windowsVersion += "5.1"; // Windows XP
                    break;

                case 1:
                    windowsVersion += "6.0"; // Windows Vista
                    break;

                case 2:
                    windowsVersion += "6.1"; // Windows 7
                    break;

                case 3:
                    windowsVersion += "6.2"; // Windows 8
                    break;
            }

            if (Rand.NextDouble() < 0.2)
            {
                windowsVersion += "; WOW64"; // 64-bit version.
            }

            return windowsVersion;
        }

        #endregion
    }
}
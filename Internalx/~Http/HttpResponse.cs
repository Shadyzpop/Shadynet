using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Shadynet
{
    /// <summary>
    /// It represents the class HTTP to download a response from the HTTP-server.
    /// </summary>
    public sealed class HttpResponse
    {
        #region Classes (closed)

        // A wrapper for an array of bytes.
        // It specifies the actual number of bytes contained in the array.
        private sealed class BytesWraper
        {
            public int Length { get; set; }

            public byte[] Value { get; set; }
        }

        // This class is used to load the initial data.
        // But it is also used for downloading and message body, rather, it simply unloaded from the residue data obtained by the initial data loading.
        private sealed class ReceiverHelper
        {
            private const int InitialLineSize = 1000;


            #region Fields (closed)

            private Stream _stream;

            private byte[] _buffer;
            private int _bufferSize;

            private int _linePosition;
            private byte[] _lineBuffer = new byte[InitialLineSize];

            #endregion


            #region Properties (open)

            public bool HasData
            {
                get
                {
                    return (Length - Position) != 0;
                }
            }

            public int Length { get; private set; }

            public int Position { get; private set; }

            #endregion


            public ReceiverHelper(int bufferSize)
            {
                _bufferSize = bufferSize;
                _buffer = new byte[_bufferSize];
            }


            #region Methods (open)

            public void Init(Stream stream)
            {
                _stream = stream;
                _linePosition = 0;

                Length = 0;
                Position = 0;
            }

            public string ReadLine()
            {
                _linePosition = 0;

                while (true)
                {
                    if (Position == Length)
                    {
                        Position = 0;
                        Length = _stream.Read(_buffer, 0, _bufferSize);

                        if (Length == 0)
                        {
                            break;
                        }
                    }

                    byte b = _buffer[Position++];

                    _lineBuffer[_linePosition++] = b;

                    // If you read the character '\n'.
                    if (b == 10)
                    {
                        break;
                    }

                    // If you reach the maximum buffer size limit line.
                    if (_linePosition == _lineBuffer.Length)
                    {
                        // Increase the size of the buffer line twice.
                        byte[] newLineBuffer = new byte[_lineBuffer.Length * 2];

                        _lineBuffer.CopyTo(newLineBuffer, 0);
                        _lineBuffer = newLineBuffer;
                    }
                }

                return Encoding.ASCII.GetString(_lineBuffer, 0, _linePosition);
            }

            public int Read(byte[] buffer, int index, int length)
            {
                int curLength = Length - Position;

                if (curLength > length)
                {
                    curLength = length;
                }

                Array.Copy(_buffer, Position, buffer, index, curLength);

                Position += curLength;

                return curLength;
            }

            #endregion
        }

        // This class is used when loading the compressed data.
        // It allows you to determine the exact amount just some few bytes (compressed data).
        // This is necessary, as streams for reading compressed data report the number of bytes already converted data.
        private sealed class ZipWraperStream : Stream
        {
            #region Fields (closed)

            private Stream _baseStream;
            private ReceiverHelper _receiverHelper;

            #endregion


            #region Properties (open)

            public int BytesRead { get; private set; }

            public int TotalBytesRead { get; set; }

            public int LimitBytesRead { get; set; }

            #region overdetermined

            public override bool CanRead
            {
                get
                {
                    return _baseStream.CanRead;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return _baseStream.CanSeek;
                }
            }

            public override bool CanTimeout
            {
                get
                {
                    return _baseStream.CanTimeout;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return _baseStream.CanWrite;
                }
            }

            public override long Length
            {
                get
                {
                    return _baseStream.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return _baseStream.Position;
                }
                set
                {
                    _baseStream.Position = value;
                }
            }

            #endregion

            #endregion


            public ZipWraperStream(Stream baseStream, ReceiverHelper receiverHelper)
            {
                _baseStream = baseStream;
                _receiverHelper = receiverHelper;
            }


            #region Methods (open)

            public override void Flush()
            {
                _baseStream.Flush();
            }

            public override void SetLength(long value)
            {
                _baseStream.SetLength(value);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _baseStream.Seek(offset, origin);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                // If you set a limit on the number of bytes read.
                if (LimitBytesRead != 0)
                {
                    int length = LimitBytesRead - TotalBytesRead;

                    // If the limit is reached.
                    if (length == 0)
                    {
                        return 0;
                    }

                    if (length > buffer.Length)
                    {
                        length = buffer.Length;
                    }

                    if (_receiverHelper.HasData)
                    {
                        BytesRead = _receiverHelper.Read(buffer, offset, length);
                    }
                    else
                    {
                        BytesRead = _baseStream.Read(buffer, offset, length);
                    }
                }
                else
                {
                    if (_receiverHelper.HasData)
                    {
                        BytesRead = _receiverHelper.Read(buffer, offset, count);
                    }
                    else
                    {
                        BytesRead = _baseStream.Read(buffer, offset, count);
                    }
                }

                TotalBytesRead += BytesRead;

                return BytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _baseStream.Write(buffer, offset, count);
            }

            #endregion
        }

        #endregion


        #region Static fields (closed)

        private static readonly byte[] _openHtmlSignature = Encoding.ASCII.GetBytes("<html");
        private static readonly byte[] _closeHtmlSignature = Encoding.ASCII.GetBytes("</html>");

        private static readonly Regex _keepAliveTimeoutRegex = new Regex(
            @"timeout(|\s+)=(|\s+)(?<value>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _keepAliveMaxRegex = new Regex(
            @"max(|\s+)=(|\s+)(?<value>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _contentCharsetRegex = new Regex(
           @"charset(|\s+)=(|\s+)(?<value>[a-z,0-9,-]+)",
           RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion


        #region Fields (closed)

        private readonly HttpRequest _request;
        private ReceiverHelper _receiverHelper;

        private readonly Dictionary<string, string> _headers =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly CookieCore _rawCookies = new CookieCore();

        #endregion


        #region Properties (open)

        /// <summary>
        /// Gets a value indicating whether an error occurred while receiving a response from the HTTP-server.
        /// </summary>
        public bool HasError { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the body has been retrieved.
        /// </summary>
        public bool MessageBodyLoaded { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the successfully executed request (response code = 200 OK).
        /// </summary>
        public bool IsOK
        {
            get
            {
                return (StatusCode == HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the forwarding is available.
        /// </summary>
        public bool HasRedirect
        {
            get
            {
                int numStatusCode = (int)StatusCode;

                if (numStatusCode >= 300 && numStatusCode < 400)
                {
                    return true;
                }

                if (_headers.ContainsKey("Location"))
                {
                    return true;
                }

                if (_headers.ContainsKey("Redirect-Location"))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns the number of attempts to reconnect.
        /// </summary>
        public int ReconnectCount { get; internal set; }

        #region Basic data

        /// <summary>
        /// Returns the URI of the Internet resource that actually responded to the request.
        /// </summary>
        public Uri Address { get; private set; }

        /// <summary>
        /// Returns the HTTP-method used to obtain the answer.
        /// </summary>
        public HttpMethod Method { get; private set; }

        /// <summary>
        /// Returns the version of the HTTP protocol-used in the response.
        /// </summary>
        public Version ProtocolVersion { get; private set; }

        /// <summary>
        /// Returns response code status.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// Returns forwarding address.
        /// </summary>
        /// <returns>Forwarding address, or <see langword="null"/>.</returns>
        public Uri RedirectAddress { get; private set; }

        #endregion

        #region HTTP-headlines

        /// <summary>
        /// Returns the encoding of the body of the message.
        /// </summary>
        /// <value>Encoding the message body if appropriate response specified, otherwise the value specified in <see cref="Shadynet.HttpRequest"/>. If it is not specified, then the value <see cref="System.Text.Encoding.Default"/>.</value>
        public Encoding CharacterSet { get; private set; }

        /// <summary>
        /// It returns the length of the body of the message.
        /// </summary>
        /// <value>Length of the message body, if appropriate Response set, otherwise -1.</value>
        public int ContentLength { get; private set; }

        /// <summary>
        /// Returns the content type of the response.
        /// </summary>
        /// <value>Content Type response if the corresponding response set, otherwise an empty string.</value>
        public string ContentType { get; private set; }

        /// <summary>
        /// Returns the value of HTTP-header 'Location'.
        /// </summary>
        /// <returns>Header value if such specified otherwise empty string.</returns>
        public string Location
        {
            get
            {
                return this["Location"];
            }
        }

        /// <summary>
        /// Returns the cookies generated by the query, or set in <see cref="Shadynet.HttpRequest"/>.
        /// </summary>
        /// <remarks>If cookies are set to <see cref="Shadynet.HttpRequest"/> and the value of the <see cref="Shadynet.CookieDictionary.IsLocked"/> equally <see langword="true"/>, the new cookie will be created.</remarks>
        public CookieCore Cookies { get; private set; }

        /// <summary>
        /// It returns the time idling permanent connection in milliseconds.
        /// </summary>
        /// <value>The default - <see langword="null"/>.</value>
        public int? KeepAliveTimeout { get; private set; }

        /// <summary>
        /// It returns the maximum number of requests per connection.
        /// </summary>
        /// <value>The default - <see langword="null"/>.</value>
        public int? MaximumKeepAliveRequests { get; private set; }

        #endregion

        #endregion


        #region Indexers (open)

        /// <summary>
        /// Returns the value of HTTP-header.
        /// </summary>
        /// <param name="headerName">HTTP-header title.</param>
        /// <value>Value of HTTP-header, if it is set, otherwise an empty string.</value>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="headerName"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="headerName"/> is an empty string.</exception>
        public string this[string headerName]
        {
            get
            {
                #region Check parameter

                if (headerName == null)
                {
                    throw new ArgumentNullException("headerName");
                }

                if (headerName.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("headerName");
                }

                #endregion

                string value;

                if (!_headers.TryGetValue(headerName, out value))
                {
                    value = string.Empty;
                }

                return value;
            }
        }

        /// <summary>
        /// Returns the value of HTTP-header.
        /// </summary>
        /// <param name="header">HTTP-header.</param>
        /// <value>Value of HTTP-header, if it is set, otherwise an empty string.</value>
        public string this[HttpHeader header]
        {
            get
            {
                return this[Http.Headers[header]];
            }
        }

        #endregion


        internal HttpResponse(HttpRequest request)
        {
            _request = request;

            ContentLength = -1;
            ContentType = string.Empty;
        }


        #region Methods (open)
        /// <summary>
        /// Gets the content of an elemnt in the html source, for example: everything inside an html class. for example: a div element.
        /// </summary>
        /// <param name="classdata">Required Class name</param>
        /// <param name="ofaclass">Subclass that exist inside the attribute</param>
        /// <param name="ofaclassdata">the subclass data that exist inside the attribute</param>
        /// <param name="ofanelement">the HTML class of the attributes</param>
        /// <returns>the data inside the <see langword="classdata"/>.</returns>
        public string HTMLparse(string classdata, string ofaclass, string ofaclassdata,string ofanelement)
        {
            int i = 0;
            string source = this.ToString();
            while (true)
            {
                string elefirst = "<" + ofanelement;
                string eleres = GetInfo.Betweenstring(source, elefirst, "</" + ofanelement + ">");
                if (string.IsNullOrEmpty(eleres))
                {
                    return "Data doesnt exist";
                    break;
                }
                string ofaclassfirst = ofaclass + "=\"";
                string ofaclassres = GetInfo.Betweenstring(eleres, ofaclassfirst, "\"");
                if (string.IsNullOrEmpty(ofaclassres))
                {
                    source.Replace(elefirst + eleres + ">", "");
                }
                else if(ofaclassres != ofaclassdata)
                {
                    source.Replace(elefirst + eleres + ">", "");
                }
                else
                {
                    string classdatafirst = classdata + "=\"";
                    string classdatares = GetInfo.Betweenstring(eleres, classdatafirst, "\"");
                    if (string.IsNullOrEmpty(classdatares))
                    {
                        return "Data is empty.";
                        break;
                    }
                    else
                    {
                        return classdatares;
                        break;
                    }
                }
                i++;
                Console.WriteLine(i);
                Thread.Sleep(100);
            }
        }
        /// <summary>
        /// Gets the content of an elemnt in the html source, for example: everything inside an html class. for example: a div element.
        /// </summary>
        /// <param name="Element">the class</param>
        /// <param name="Data">specific content</param>
        /// <returns>the content aquaired from the input.</returns>
        public string HTMLparse(string Element,string Data = "")
        {
            string strone = "<" + Element;
            string strtwo = "</" + Element + ">";
            string res = this.Between(strone, strtwo);
            if (string.IsNullOrEmpty(Data))
            {
                return res;
            }
            string strd = Data + "=\"";
            string res2 = GetInfo.Betweenstring(res, strd, "\"");
            return res2;
        }
        /// <summary>
        /// Returns a string between two strings, starts from <paramref name="strStart"/> to <paramref name="strEnd"/> from the page body -> <see langword="this"/>
        /// </summary>
        /// <param name="strStart">The head start of the param</param>
        /// <param name="strEnd">The tail end of the param</param>
        /// <returns>The string between <paramref name="strStart"/> and <paramref name="strEnd"/> from the page body -> <see langword="this"/>
        public string Between(string strStart, string strEnd)
        {
            string strSource = this.ToString();
            int Start, End;
            if (strStart != string.Empty || strEnd != string.Empty || strSource != string.Empty)
            {

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
                        throw new InvalidOperationException("Cannot Parse Data.");
                    }
                }
                else
                {
                    return "";
                }
            }
            else
                return "";
        }

        /// <summary>
        /// Logs the important parts of the response and returns it out in the console.
        /// </summary>
        /// <param name="LoadBody">Wether to return the html body or not. Default Value=<see langword="false"/>.</param>
        public void cLogger(bool LoadBody = false)
        {
            string type = this.Method.ToString();
            Console.WriteLine("<------Log Started < "+type+"> ------>\r\n\r\n");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(string.Format("Status: {0} -> HTTP-{1} -> Request-{2} -> {3}", this.Address, this.ProtocolVersion, this.Method, this.StatusCode));
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\r\n< Headers >\r\n");
            var head = string.Join("\r\n", _headers);
            head = head.Replace('[', '-').Replace(',', ':').Replace(']', ' ');
            Console.WriteLine(head + "\r\n");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("< Cookies >\r\n");
            var cooki = string.Join("\r\n", _rawCookies);
            cooki = cooki.Replace("[", "").Replace(",", ":").Replace(";", "\n").Replace("]", "\r\n");
            Console.WriteLine(cooki + "\r\n");
            if (LoadBody)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("< HTML Body >");
                Console.WriteLine(this.ToString() + "\r\n");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /// <summary>
        /// Logs the important parts of the response and returns it out as a stringbuilder.
        /// </summary>
        /// <param name="log">stringbuilder instance.</param>
        /// <param name="LoadBody">Wether to return the html body or not. Default Value=<see langword="false"/>.-</param>
        public void Logger(out StringBuilder log ,bool LoadBody = false)
        {
            StringBuilder str = new StringBuilder();
            str.AppendLine("<------Log Started------>\r\n\r\n");
            str.AppendLine(string.Format("Status: {0} -> HTTP-{1} -> Request-{2} -> {3}", this.Address, this.ProtocolVersion, this.Method, this.StatusCode));
            str.AppendLine("\r\n< Headers >\r\n");
            var head = string.Join("\r\n", _headers);
            head = head.Replace('[', '-').Replace(',', ':').Replace(']', ' ');
            str.AppendLine(head + "\r\n");

            str.AppendLine("< Cookies > \r\n");
            var cooki = string.Join("\r\n", _rawCookies);
            cooki = cooki.Replace("[", "").Replace(",", ":").Replace(";", "\n").Replace("]", "\r\n");
            str.AppendLine(cooki + "\r\n");
            if (LoadBody)
            {
                str.AppendLine("< HTML Body >");
                str.AppendLine(this.ToString() + "\r\n");
            }
            log = str;
        }

        /// <summary>
        /// Loads the body of the message and returns it as an array of bytes.
        /// </summary>
        /// <returns>If the body of the message is missing, or it has already been downloaded, it will return an empty byte array.</returns>
        /// <exception cref="System.InvalidOperationException">Calling of the wrong answer.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public byte[] ToBytes()
        {
            #region Checking status

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return new byte[0];
            }

            var memoryStream = new MemoryStream(
                (ContentLength == -1) ? 0 : ContentLength);

            try
            {
                IEnumerable<BytesWraper> source = GetMessageBodySource();

                foreach (var bytes in source)
                {
                    memoryStream.Write(bytes.Value, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException || ex is InvalidOperationException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                }

                throw;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
            }

            MessageBodyLoaded = true;

            return memoryStream.ToArray();
        }

        /// <summary>
        /// Loads the body of the message and returns it as a string.
        /// </summary>
        /// <returns>If the body of the message is missing, or it has already been downloaded, then an empty string is returned.</returns>
        /// <exception cref="System.InvalidOperationException">Calling of the wrong answer.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        override public string ToString()
        {
            #region Checking status

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return string.Empty;
            }

            var memoryStream = new MemoryStream(
                (ContentLength == -1) ? 0 : ContentLength);

            try
            {
                IEnumerable<BytesWraper> source = GetMessageBodySource();

                foreach (var bytes in source)
                {
                    memoryStream.Write(bytes.Value, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException || ex is InvalidOperationException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                }

                throw;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
            }

            MessageBodyLoaded = true;

            string text = CharacterSet.GetString(
                memoryStream.GetBuffer(), 0, (int)memoryStream.Length);

            return text;
        }

        /// <summary>
        /// It loads the body of the message and stores it in a new file in the specified path.   If the file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The path to the file in which to keep the body of the message.</param>
        /// <exception cref="System.InvalidOperationException">Calling of the wrong answer.</exception>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="path"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="path"/> It is an empty string, contains only white space, or contains invalid characters.</exception>
        /// <exception cref="System.IO.PathTooLongException">The specified path, file name, or both exceed the maximum possible length of a certain system.   For example, on Windows platforms, the path length should not exceed 248 characters, and file names must not contain more than 260 characters.</exception>
        /// <exception cref="System.IO.FileNotFoundException">parameter <paramref name="path"/> points to a nonexistent file.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">parameter <paramref name="path"/> points to invalid path.</exception>
        /// <exception cref="System.IO.IOException">At the opening there was input or output error file.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the necessary permission.</exception>
        /// <exception cref="System.UnauthorizedAccessException">
        /// file reading operation is not supported on the current platform.
        /// -or-
        /// parameter <paramref name="path"/> specifies the directory.
        /// -or-
        /// The caller does not have permission.
        /// </exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public void ToFile(string path)
        {
            #region Checking status

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            #region Check settings

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return;
            }

            try
            {
                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    IEnumerable<BytesWraper> source = GetMessageBodySource();

                    foreach (var bytes in source)
                    {
                        fileStream.Write(bytes.Value, 0, bytes.Length);
                    }
                }
            }
            #region Catches

            catch (ArgumentException ex)
            {
                throw ExceptionHelper.WrongPath("path", ex);
            }
            catch (NotSupportedException ex)
            {
                throw ExceptionHelper.WrongPath("path", ex);
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException || ex is InvalidOperationException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                }

                throw;
            }

            #endregion

            if (ConnectionClosed())
            {
                _request.Dispose();
            }

            MessageBodyLoaded = true;
        }

        /// <summary>
        /// Loads the message body and returns it as a stream of bytes from memory.
        /// </summary>
        /// <returns>If the body of the message is missing, or it has already been downloaded, it will be returned <see langword="null"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Calling of the wrong answer.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public MemoryStream ToMemoryStream()
        {
            #region Checking status

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return null;
            }

            var memoryStream = new MemoryStream(
                (ContentLength == -1) ? 0 : ContentLength);

            try
            {
                IEnumerable<BytesWraper> source = GetMessageBodySource();

                foreach (var bytes in source)
                {
                    memoryStream.Write(bytes.Value, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException || ex is InvalidOperationException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                }

                throw;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
            }

            MessageBodyLoaded = true;
            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <summary>
        /// Skip message body. This method should be called, unless you want the message body.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Calling of the wrong answer.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public void None()
        {
            #region Checking status

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
            }
            else
            {
                try
                {
                    IEnumerable<BytesWraper> source = GetMessageBodySource();

                    foreach (var bytes in source) { }
                }
                catch (Exception ex)
                {
                    HasError = true;

                    if (ex is IOException || ex is InvalidOperationException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                    }

                    throw;
                }
            }

            MessageBodyLoaded = true;
        }

        #region Working with cookies

        /// <summary>
        /// It determines whether the specified cookie contained.
        /// </summary>
        /// <param name="name">The name of the cookie.</param>
        /// <returns>Value <see langword="true"/>, if these cookies contain, or value <see langword="false"/>.</returns>
        public bool ContainsCookie(string name)
        {
            if (Cookies == null)
            {
                return false;
            }

            return Cookies.ContainsKey(name);
        }

        /// <summary>
        /// It determines whether the raw value of the specified cookie contained.
        /// </summary>
        /// <param name="name">The name of the cookie.</param>
        /// <returns>Value <see langword="true"/>, if these cookies contain, or value <see langword="false"/>.</returns>
        /// <remarks>This cookie, which have been set in the current response.   These raw values ​​may be used to produce what some additional data.</remarks>
        public bool ContainsRawCookie(string name)
        {
            return _rawCookies.ContainsKey(name);
        }

        /// <summary>
        /// Returns the raw value of the cookie.
        /// </summary>
        /// <param name="name">The name of the cookie.</param>
        /// <returns>The value of the cookie, if it exists, otherwise an empty string.</returns>
        /// <remarks>This cookie, which have been set in the current response.   These raw values ​​may be used to produce what some additional data.</remarks>
        public string GetRawCookie(string name)
        {
            string value;

            if (!_rawCookies.TryGetValue(name, out value))
            {
                value = string.Empty;
            }

            return value;
        }

        /// <summary>
        /// Returns an enumerable collection of raw cookie values.
        /// </summary>
        /// <returns>Collection of raw cookie values.</returns>
        /// <remarks>This cookie, which have been set in the current response. These raw values ​​may be used to produce what some additional data.</remarks>
        public Dictionary<string, string>.Enumerator EnumerateRawCookies()
        {
            return _rawCookies.GetEnumerator();
        }

        #endregion

        #region Working with titles
        
        /// <summary>
        /// Specifies whether the HTTP-header contains the specified.
        /// </summary>
        /// <param name="headerName">HTTP-heading title.</param>
        /// <returns>Value <see langword="true"/>, if the specified HTTP-header contains, or value <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="headerName"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="headerName"/> It is an empty string.</exception>
        public bool ContainsHeader(string headerName)
        {
            #region Check settings

            if (headerName == null)
            {
                throw new ArgumentNullException("headerName");
            }

            if (headerName.Length == 0)
            {
                throw ExceptionHelper.EmptyString("headerName");
            }

            #endregion

            return _headers.ContainsKey(headerName);
        }

        /// <summary>
        /// Specifies whether the HTTP-header contains the specified.
        /// </summary>
        /// <param name="header">HTTP-header.</param>
        /// <returns>Value <see langword="true"/>, if the specified HTTP-header contains, or value <see langword="false"/>.</returns>
        public bool ContainsHeader(HttpHeader header)
        {
            return ContainsHeader(Http.Headers[header]);
        }

        /// <summary>
        /// Returns an enumerable collection of HTTP-header.
        /// </summary>
        /// <returns>A collection of HTTP-header.</returns>
        public Dictionary<string, string>.Enumerator EnumerateHeaders()
        {
            return _headers.GetEnumerator();
        }

        #endregion

        #endregion


        // Loading response and returns the size in bytes of the response.
        internal long LoadResponse(HttpMethod method)
        {
            Method = method;
            Address = _request.Address;

            HasError = false;
            MessageBodyLoaded = false;
            KeepAliveTimeout = null;
            MaximumKeepAliveRequests = null;

            _headers.Clear();
            _rawCookies.Clear();

            if (_request.Cookies != null && !_request.Cookies.IsLocked)
                Cookies = _request.Cookies;
            else
                Cookies = new CookieCore();

            if (_receiverHelper == null)
            {
                _receiverHelper = new ReceiverHelper(
                    _request.TcpClient.ReceiveBufferSize);
            }

            _receiverHelper.Init(_request.ClientStream);

            try
            {
                ReceiveStartingLine();
                ReceiveHeaders();

                RedirectAddress = GetLocation();
                CharacterSet = GetCharacterSet();
                ContentLength = GetContentLength();
                ContentType = GetContentType();

                KeepAliveTimeout = GetKeepAliveTimeout();
                MaximumKeepAliveRequests = GetKeepAliveMax();
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveResponse, ex);
                }

                throw;
            }

            // When the answer came without the body of the message.
            if (ContentLength == 0 ||
                Method == HttpMethod.HEAD ||
                StatusCode == HttpStatusCode.Continue ||
                StatusCode == HttpStatusCode.NoContent ||
                StatusCode == HttpStatusCode.NotModified)
            {
                MessageBodyLoaded = true;
            }

            long responseSize = _receiverHelper.Position;

            if (ContentLength > 0)
            {
                responseSize += ContentLength;
            }

            return responseSize;
        }


        #region Methods of (closed)

        #region Loading initial data

        private void ReceiveStartingLine()
        {
            string startingLine;

            while (true)
            {
                startingLine = _receiverHelper.ReadLine();

                if (startingLine.Length == 0)
                {
                    HttpException exception =
                        NewHttpException(Resources.HttpException_ReceivedEmptyResponse);

                    exception.EmptyMessageBody = true;

                    throw exception;
                }
                else if (startingLine == Http.NewLine)
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            string version = startingLine.Substring("HTTP/", " ");
            string statusCode = startingLine.Substring(" ", " ");

            if (statusCode.Length == 0)
            {
                // If the server does not return Reason Phrase
                statusCode = startingLine.Substring(" ", Http.NewLine);
            }

            if (version.Length == 0 || statusCode.Length == 0)
            {
                throw NewHttpException(Resources.HttpException_ReceivedEmptyResponse);
            }

            ProtocolVersion = Version.Parse(version);

            StatusCode = (HttpStatusCode)Enum.Parse(
                typeof(HttpStatusCode), statusCode);
        }

        private void SetCookie(string value)
        {
            if (value.Length == 0)
            {
                return;
            }

            // We are looking for a position, where it ends and begins the description of the cookie parameters.
            int endCookiePos = value.IndexOf(';');

            // We are looking for a position between the name and value of the cookie.
            int separatorPos = value.IndexOf('=');

            if (separatorPos == -1)
            {
                string message = string.Format(
                    Resources.HttpException_WrongCookie, value, Address.Host);

                throw NewHttpException(message);
            }

            string cookieValue;
            string cookieName = value.Substring(0, separatorPos);

            if (endCookiePos == -1)
            {
                cookieValue = value.Substring(separatorPos + 1);
            }
            else
            {
                cookieValue = value.Substring(separatorPos + 1,
                    (endCookiePos - separatorPos) - 1);

                #region We get the time that a cookie will be valid

                int expiresPos = value.IndexOf("expires=");

                if (expiresPos != -1)
                {
                    string expiresStr;
                    int endExpiresPos = value.IndexOf(';', expiresPos);

                    expiresPos += 8;

                    if (endExpiresPos == -1)
                    {
                        expiresStr = value.Substring(expiresPos);
                    }
                    else
                    {
                        expiresStr = value.Substring(expiresPos, endExpiresPos - expiresPos);
                    }

                    DateTime expires;

                    // If the time came cookies, then remove it.
                    if (DateTime.TryParse(expiresStr, out expires) &&
                        expires < DateTime.Now)
                    {
                        Cookies.Remove(cookieName);
                    }
                }

                #endregion
            }

            // If the cookie you want to delete.
            if (cookieValue.Length == 0 ||
                cookieValue.Equals("deleted", StringComparison.OrdinalIgnoreCase))
            {
                Cookies.Remove(cookieName);
            }
            else
            {
                Cookies[cookieName] = cookieValue;
            }

            _rawCookies[cookieName] = value;
        }

        private void ReceiveHeaders()
        {
            while (true)
            {
                string header = _receiverHelper.ReadLine();

                // If you reach the end of the headers.
                if (header == Http.NewLine)
                    return;

                // We are looking for a position between the name and header value.
                int separatorPos = header.IndexOf(':');

                if (separatorPos == -1)
                {
                    string message = string.Format(
                        Resources.HttpException_WrongHeader, header, Address.Host);

                    throw NewHttpException(message);
                }

                string headerName = header.Substring(0, separatorPos);
                string headerValue = header.Substring(separatorPos + 1).Trim(' ', '\t', '\r', '\n');

                if (headerName.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                {
                    SetCookie(headerValue);
                }
                else
                {
                    _headers[headerName] = headerValue;
                }
            }
        }

        #endregion

        #region Loading message body

        private IEnumerable<BytesWraper> GetMessageBodySource()
        {
            if (_headers.ContainsKey("Content-Encoding"))
            {
                return GetMessageBodySourceZip();
            }

            return GetMessageBodySourceStd();
        }

        // Download normal data.
        private IEnumerable<BytesWraper> GetMessageBodySourceStd()
        {
            if (_headers.ContainsKey("Transfer-Encoding"))
            {
                return ReceiveMessageBodyChunked();
            }

            if (ContentLength != -1)
            {
                return ReceiveMessageBody(ContentLength);
            }

            return ReceiveMessageBody(_request.ClientStream);
        }

        // Download the compressed data.
        private IEnumerable<BytesWraper> GetMessageBodySourceZip()
        {
            if (_headers.ContainsKey("Transfer-Encoding"))
            {
                return ReceiveMessageBodyChunkedZip();
            }

            if (ContentLength != -1)
            {
                return ReceiveMessageBodyZip(ContentLength);
            }

            var streamWrapper = new ZipWraperStream(
                _request.ClientStream, _receiverHelper);

            return ReceiveMessageBody(GetZipStream(streamWrapper));
        }

        // Loading message body of unknown length.
        private IEnumerable<BytesWraper> ReceiveMessageBody(Stream stream)
        {
            var bytesWraper = new BytesWraper();

            int bufferSize = _request.TcpClient.ReceiveBufferSize;
            byte[] buffer = new byte[bufferSize];

            bytesWraper.Value = buffer;

            int begBytesRead = 0;

            // Read the initial data from the message body.
            if (stream is GZipStream || stream is DeflateStream)
            {
                begBytesRead = stream.Read(buffer, 0, bufferSize);
            }
            else
            {
                if (_receiverHelper.HasData)
                {
                    begBytesRead = _receiverHelper.Read(buffer, 0, bufferSize);
                }

                if (begBytesRead < bufferSize)
                {
                    begBytesRead += stream.Read(buffer, begBytesRead, bufferSize - begBytesRead);
                }
            }

            // Return the initial data.
            bytesWraper.Length = begBytesRead;
            yield return bytesWraper;

            // Check whether there is an opening '<html' tag.
            // If so, then read the data as long as the closure does not meet tech'</html>'.
            bool isHtml = FindSignature(buffer, begBytesRead, _openHtmlSignature);

            if (isHtml)
            {
                bool found = FindSignature(buffer, begBytesRead, _closeHtmlSignature);

                // Check whether there is a closing tag of the initial data.
                if (found)
                {
                    yield break;
                }
            }

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, bufferSize);

                // If the body of the message is HTML.
                if (isHtml)
                {
                    if (bytesRead == 0)
                    {
                        WaitData();

                        continue;
                    }

                    bool found = FindSignature(buffer, bytesRead, _closeHtmlSignature);

                    if (found)
                    {
                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;

                        yield break;
                    }
                }
                else if (bytesRead == 0)
                {
                    yield break;
                }

                bytesWraper.Length = bytesRead;
                yield return bytesWraper;
            }
        }

        // Loading message body of known length.
        private IEnumerable<BytesWraper> ReceiveMessageBody(int contentLength)
        {
            Stream stream = _request.ClientStream;
            var bytesWraper = new BytesWraper();

            int bufferSize = _request.TcpClient.ReceiveBufferSize;
            byte[] buffer = new byte[bufferSize];

            bytesWraper.Value = buffer;

            int totalBytesRead = 0;

            while (totalBytesRead != contentLength)
            {
                int bytesRead;

                if (_receiverHelper.HasData)
                {
                    bytesRead = _receiverHelper.Read(buffer, 0, bufferSize);
                }
                else
                {
                    bytesRead = stream.Read(buffer, 0, bufferSize);
                }

                if (bytesRead == 0)
                {
                    WaitData();
                }
                else
                {
                    totalBytesRead += bytesRead;

                    bytesWraper.Length = bytesRead;
                    yield return bytesWraper;
                }
            }
        }

        // Loading parts of the body of the message.
        private IEnumerable<BytesWraper> ReceiveMessageBodyChunked()
        {
            Stream stream = _request.ClientStream;
            var bytesWraper = new BytesWraper();

            int bufferSize = _request.TcpClient.ReceiveBufferSize;
            byte[] buffer = new byte[bufferSize];

            bytesWraper.Value = buffer;

            while (true)
            {
                string line = _receiverHelper.ReadLine();

                // If you reach the end of the block.
                if (line == Http.NewLine)
                    continue;

                line = line.Trim(' ', '\r', '\n');

                // If you reach the end of the message body.
                if (line == string.Empty)
                    yield break;

                int blockLength;
                int totalBytesRead = 0;

                #region Asking block length

                try
                {
                    blockLength = Convert.ToInt32(line, 16);
                }
                catch (Exception ex)
                {
                    if (ex is FormatException || ex is OverflowException)
                    {
                        throw NewHttpException(string.Format(
                            Resources.HttpException_WrongChunkedBlockLength, line), ex);
                    }

                    throw;
                }

                #endregion

                // If you reach the end of the message body.
                if (blockLength == 0)
                    yield break;

                while (totalBytesRead != blockLength)
                {
                    int length = blockLength - totalBytesRead;

                    if (length > bufferSize)
                    {
                        length = bufferSize;
                    }

                    int bytesRead;

                    if (_receiverHelper.HasData)
                    {
                        bytesRead = _receiverHelper.Read(buffer, 0, length);
                    }
                    else
                    {
                        bytesRead = stream.Read(buffer, 0, length);
                    }

                    if (bytesRead == 0)
                    {
                        WaitData();
                    }
                    else
                    {
                        totalBytesRead += bytesRead;

                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;
                    }
                }
            }
        }

        private IEnumerable<BytesWraper> ReceiveMessageBodyZip(int contentLength)
        {
            var bytesWraper = new BytesWraper();
            var streamWrapper = new ZipWraperStream(
                _request.ClientStream, _receiverHelper);

            using (Stream stream = GetZipStream(streamWrapper))
            {
                int bufferSize = _request.TcpClient.ReceiveBufferSize;
                byte[] buffer = new byte[bufferSize];

                bytesWraper.Value = buffer;

                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, bufferSize);

                    if (bytesRead == 0)
                    {
                        if (streamWrapper.TotalBytesRead == contentLength)
                        {
                            yield break;
                        }
                        else
                        {
                            WaitData();

                            continue;
                        }
                    }

                    bytesWraper.Length = bytesRead;
                    yield return bytesWraper;
                }
            }
        }

        private IEnumerable<BytesWraper> ReceiveMessageBodyChunkedZip()
        {
            var bytesWraper = new BytesWraper();
            var streamWrapper = new ZipWraperStream
                (_request.ClientStream, _receiverHelper);

            using (Stream stream = GetZipStream(streamWrapper))
            {
                int bufferSize = _request.TcpClient.ReceiveBufferSize;
                byte[] buffer = new byte[bufferSize];

                bytesWraper.Value = buffer;

                while (true)
                {
                    string line = _receiverHelper.ReadLine();

                    // If you reach the end of the block.
                    if (line == Http.NewLine)
                        continue;

                    line = line.Trim(' ', '\r', '\n');

                    // If you reach the end of the message body.
                    if (line == string.Empty)
                        yield break;

                    int blockLength;

                    #region Asking block length

                    try
                    {
                        blockLength = Convert.ToInt32(line, 16);
                    }
                    catch (Exception ex)
                    {
                        if (ex is FormatException || ex is OverflowException)
                        {
                            throw NewHttpException(string.Format(
                                Resources.HttpException_WrongChunkedBlockLength, line), ex);
                        }

                        throw;
                    }

                    #endregion

                    // If you reach the end of the message body.
                    if (blockLength == 0)
                        yield break;

                    streamWrapper.TotalBytesRead = 0;
                    streamWrapper.LimitBytesRead = blockLength;

                    while (true)
                    {
                        int bytesRead = stream.Read(buffer, 0, bufferSize);

                        if (bytesRead == 0)
                        {
                            if (streamWrapper.TotalBytesRead == blockLength)
                            {
                                break;
                            }
                            else
                            {
                                WaitData();

                                continue;
                            }
                        }

                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;
                    }
                }
            }
        }

        #endregion

        #region Getting HTTP-header values

        private bool ConnectionClosed()
        {
            if (_headers.ContainsKey("Connection") &&
                _headers["Connection"].Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (_headers.ContainsKey("Proxy-Connection") &&
                _headers["Proxy-Connection"].Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private int? GetKeepAliveTimeout()
        {
            if (!_headers.ContainsKey("Keep-Alive"))
                return null;

            var header = _headers["Keep-Alive"];
            var match = _keepAliveTimeoutRegex.Match(header);

            if (match.Success)
                return int.Parse(match.Groups["value"].Value) * 1000; // In milliseconds.

            return null;
        }

        private int? GetKeepAliveMax()
        {
            if (!_headers.ContainsKey("Keep-Alive"))
                return null;

            var header = _headers["Keep-Alive"];
            var match = _keepAliveMaxRegex.Match(header);

            if (match.Success)
                return int.Parse(match.Groups["value"].Value);

            return null;
        }

        private Uri GetLocation()
        {
            string location;

            if (!_headers.TryGetValue("Location", out location))
                _headers.TryGetValue("Redirect-Location", out location);

            if (string.IsNullOrEmpty(location))
                return null;

            Uri redirectAddress;
            var baseAddress = _request.Address;
            Uri.TryCreate(baseAddress, location, out redirectAddress);

            return redirectAddress;
        }

        private Encoding GetCharacterSet()
        {
            if (!_headers.ContainsKey("Content-Type"))
                return _request.CharacterSet ?? Encoding.Default;

            var header = _headers["Content-Type"];
            var match = _contentCharsetRegex.Match(header);

            if (!match.Success)
                return _request.CharacterSet ?? Encoding.Default;

            var charset = match.Groups["value"];

            try
            {
                return Encoding.GetEncoding(charset.Value);
            }
            catch (ArgumentException ex)
            {
                return _request.CharacterSet ?? Encoding.Default;
            }
        }

        private int GetContentLength()
        {
            if (_headers.ContainsKey("Content-Length"))
            {
                int contentLength;
                int.TryParse(_headers["Content-Length"], out contentLength);
                return contentLength;
            }

            return -1;
        }

        private string GetContentType()
        {
            if (_headers.ContainsKey("Content-Type"))
            {
                string contentType = _headers["Content-Type"];

                // We are looking for a position, where it ends the description of the type of content and begins the description of its parameters.
                int endTypePos = contentType.IndexOf(';');
                if (endTypePos != -1)
                    contentType = contentType.Substring(0, endTypePos);
  
                return contentType;
            }

            return string.Empty;
        }

        #endregion

        private void WaitData()
        {
            int sleepTime = 0;
            int delay = (_request.TcpClient.ReceiveTimeout < 10) ?
                10 : _request.TcpClient.ReceiveTimeout;

            while (!_request.ClientNetworkStream.DataAvailable)
            {
                if (sleepTime >= delay)
                {
                    throw NewHttpException(Resources.HttpException_WaitDataTimeout);
                }

                sleepTime += 10;
                Thread.Sleep(10);
            }
        }

        private Stream GetZipStream(Stream stream)
        {
            string contentEncoding = _headers["Content-Encoding"].ToLower();

            switch (contentEncoding)
            {
                case "gzip":
                    return new GZipStream(stream, CompressionMode.Decompress, true);

                case "deflate":
                    return new DeflateStream(stream, CompressionMode.Decompress, true);

                default:
                    throw new InvalidOperationException(string.Format(
                        Resources.InvalidOperationException_NotSupportedEncodingFormat, contentEncoding));
            }
        }

        private bool FindSignature(byte[] source, int sourceLength, byte[] signature)
        {
            int length = (sourceLength - signature.Length) + 1;

            for (int sourceIndex = 0; sourceIndex < length; ++sourceIndex)
            {
                for (int signatureIndex = 0; signatureIndex < signature.Length; ++signatureIndex)
                {
                    byte sourceByte = source[signatureIndex + sourceIndex];
                    char sourceChar = (char)sourceByte;

                    if (char.IsLetter(sourceChar))
                    {
                        sourceChar = char.ToLower(sourceChar);
                    }

                    sourceByte = (byte)sourceChar;

                    if (sourceByte != signature[signatureIndex])
                    {
                        break;
                    }
                    else if (signatureIndex == (signature.Length - 1))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private HttpException NewHttpException(string message, Exception innerException = null)
        {
            return new HttpException(string.Format(message, Address.Host),
                HttpExceptionStatus.ReceiveFailure, HttpStatusCode.None, innerException);
        }

        #endregion
    }
}

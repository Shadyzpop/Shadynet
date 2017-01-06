using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading;

namespace Shadynet
{
    /// <summary>
    /// It represents a class that is designed to send HTTP-server requests.
    /// </summary>
    public class HttpRequest : IDisposable
    {
        // Used to determine how many bytes sent / read.
        private sealed class HttpWraperStream : Stream
        {
            #region Fields (closed)

            private Stream _baseStream;
            private int _sendBufferSize;

            #endregion


            #region Properties (open)

            public Action<int> BytesReadCallback { get; set; }

            public Action<int> BytesWriteCallback { get; set; }

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


            public HttpWraperStream(Stream baseStream, int sendBufferSize)
            {
                _baseStream = baseStream;
                _sendBufferSize = sendBufferSize;
            }


            #region Methods (open)

            public override void Flush() { }

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
                int bytesRead = _baseStream.Read(buffer, offset, count);

                if (BytesReadCallback != null)
                {
                    BytesReadCallback(bytesRead);
                }

                return bytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (BytesWriteCallback == null)
                {
                    _baseStream.Write(buffer, offset, count);
                }
                else
                {
                    int index = 0;

                    while (count > 0)
                    {
                        int bytesWrite = 0;

                        if (count >= _sendBufferSize)
                        {
                            bytesWrite = _sendBufferSize;
                            _baseStream.Write(buffer, index, bytesWrite);

                            index += _sendBufferSize;
                            count -= _sendBufferSize;
                        }
                        else
                        {
                            bytesWrite = count;
                            _baseStream.Write(buffer, index, bytesWrite);

                            count = 0;
                        }

                        BytesWriteCallback(bytesWrite);
                    }
                }
            }

            #endregion
        }


        /// <summary>
        /// HTTP-protocol version used in the request.
        /// </summary>
        public static readonly Version ProtocolVersion = new Version(1, 1);


        #region Static fields (closed)

        // Titles that can only be set by a special property / method.
        private static readonly List<string> _closedHeaders = new List<string>()
        {
            "Accept-Encoding",
            "Content-Length",
            "Content-Type",
            "Connection",
            "Proxy-Connection",
            "Host"
        };

        #endregion


        #region Static properties (open)

        /// <summary>
        /// Gets or sets a value indicating whether to use a proxy client Internet Explorer'a, if there is no direct connection to the Internet and do not specify the proxy client need.
        /// </summary>
        /// <value>default value — <see langword="false"/>.</value>
        public static bool UseIeProxy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to disable client proxy for local addresses.
        /// </summary>
        /// <value>default value — <see langword="false"/>.</value>
        public static bool DisableProxyForLocalAddress { get; set; }

        /// <summary>
        /// Gets or sets the global proxy client.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        public static ProxyClient GlobalProxy { get; set; }

        #endregion


        #region Fields (closed)

        private HttpResponse _response;

        private TcpClient _connection;
        private Stream _connectionCommonStream;
        private NetworkStream _connectionNetworkStream;

        private ProxyClient _currentProxy;

        private int _redirectionCount = 0;
        private int _maximumAutomaticRedirections = 5;

        private int _connectTimeout = 60 * 1000;
        private int _readWriteTimeout = 60 * 1000;

        private DateTime _whenConnectionIdle;
        private int _keepAliveTimeout = 30 * 1000;
        private int _maximumKeepAliveRequests = 100;
        private int _keepAliveRequestCount;
        private bool _keepAliveReconnected;

        private int _reconnectLimit = 3;
        private int _reconnectDelay = 100;
        private int _reconnectCount;

        private HttpMethod _method;
        private HttpContent _content; // The body of the request.

        private readonly Dictionary<string, string> _permanentHeaders =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Temporary data that are set through special techniques.  
        // Removed after the first request.
        private RequestParams _temporaryParams;
        private RequestParams _temporaryUrlParams;
        private Dictionary<string, string> _temporaryHeaders;
        private MultipartContent _temporaryMultipartContent;

        // Number of bytes sent and received.
        // Used to UploadProgressChanged DownloadProgressChanged and events.
        private long _bytesSent;
        private long _totalBytesSent;
        private long _bytesReceived;
        private long _totalBytesReceived;
        private bool _canReportBytesReceived;

        private EventHandler<UploadProgressChangedEventArgs> _uploadProgressChangedHandler;
        private EventHandler<DownloadProgressChangedEventArgs> _downloadProgressChangedHandler;


        #endregion


        #region Events (open)

        /// <summary>
        /// It occurs each time advancing progress unloading the message body data.
        /// </summary>
        public event EventHandler<UploadProgressChangedEventArgs> UploadProgressChanged
        {
            add
            {
                _uploadProgressChangedHandler += value;
            }
            remove
            {
                _uploadProgressChangedHandler -= value;
            }
        }

        /// <summary>
        /// Occurs each time advancing progress retrieve the message body data.
        /// </summary>
        public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged
        {
            add
            {
                _downloadProgressChangedHandler += value;
            }
            remove
            {
                _downloadProgressChangedHandler -= value;
            }
        }

        #endregion


        #region Properties (open)

        /// <summary>
        /// Gets or sets the URL of the Internet resource that is used when the query is specified relative address.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        public Uri BaseAddress { get; set; }

        /// <summary>
        /// Returns the URI of the Internet resource that actually responds to the request.
        /// </summary>
        public Uri Address { get; private set; }

        /// <summary>
        /// Gets the last response from the HTTP-server obtained by the instance.
        /// </summary>
        public HttpResponse Response
        {
            get
            {
                return _response;
            }
        }

        /// <summary>
        /// Gets or sets the proxy client.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        public ProxyClient Proxy { get; set; }

        /// <summary>
        /// Gets or sets the delegate method called when an SSL certificate validation is used to authenticate.
        /// </summary>
        /// <value>default value — <see langword="null"/>. When set to the default, use the method that takes all SSL certificates.</value>
        public RemoteCertificateValidationCallback SslCertificateValidatorCallback;

        #region Поведение

        /// <summary>
        /// Gets or sets a value indicating whether the request should follow redirection responses.
        /// </summary>
        /// <value>default value — <see langword="true"/>.</value>
        public bool AllowAutoRedirect { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of consecutive redirects.
        /// </summary>
        /// <value>default value - 5.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">The parameter value is less than 1.</exception>
        public int MaximumAutomaticRedirections
        {
            get
            {
                return _maximumAutomaticRedirections;
            }
            set
            {
                #region Check parameter

                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess("MaximumAutomaticRedirections", 1);
                }

                #endregion

                _maximumAutomaticRedirections = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait time in milliseconds when connecting to the HTTP-server.
        /// </summary>
        /// <value>Default value - 60,000, which is equal to one minute.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">The parameter value is less than 0.</exception>
        public int ConnectTimeout
        {
            get
            {
                return _connectTimeout;
            }
            set
            {
                #region Check parameter

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("ConnectTimeout", 0);
                }

                #endregion

                _connectTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait time in milliseconds when writing to the stream or reading from it.
        /// </summary>
        /// <value>Default value - 60,000, which is equal to one minute.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">The parameter value is less than 0.</exception>
        public int ReadWriteTimeout
        {
            get
            {
                return _readWriteTimeout;
            }
            set
            {
                #region Check parameter

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("ReadWriteTimeout", 0);
                }

                #endregion

                _readWriteTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore protocol errors and does not need to throw exceptions.
        /// </summary>
        /// <value>default value — <see langword="false"/>.</value>
        /// <remarks>If you set <see langword="true"/>, in the case of an incorrect response with status code 4xx or 5xx, will not be an exception.   You can check the response status code using the properties <see cref="HttpResponse.StatusCode"/>.</remarks>
        public bool IgnoreProtocolErrors { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether you want to establish a persistent connection to the Internet resource.
        /// </summary>
        /// <value>default value - <see langword="true"/>.</value>
        /// <remarks>If the value is <see langword="true"/>, it goes further heading 'Connection: Keep-Alive', otherwise sent a header 'Connection: Close'. If the connection uses an HTTP-proxy, instead of the title - 'Connection', set a title - 'Proxy-Connection'. If the server restarts his permanent connection, <see cref="HttpResponse"/> It tries to connect again, but this only works if the connection goes directly to the HTTP-server or a HTTP-Proxy.</remarks>
        public bool KeepAlive { get; set; }

        /// <summary>
        /// Gets or sets the idling time permanent connection in milliseconds, which is used by default.
        /// </summary>
        /// <value>default value - 30,000, which is equal to 30 seconds.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">The parameter value is less than 0.</exception>
        /// <remarks>If time is up, it will create a new connection.   If the server returns its value timeout <see cref="HttpResponse.KeepAliveTimeout"/>, then it it will be used.</remarks>
        public int KeepAliveTimeout
        {
            get
            {
                return _keepAliveTimeout;
            }
            set
            {
                #region Check parameter

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("KeepAliveTimeout", 0);
                }

                #endregion

                _keepAliveTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of requests per connection, which is used by default.
        /// </summary>
        /// <value>default value - 100.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">The parameter value is less than 1.</exception>
        /// <remarks>If the number of requests exceeds the maximum, it will create a new connection.   If your server returns the maximum of number of queries <see cref="HttpResponse.MaximumKeepAliveRequests"/>, then it it will be used.</remarks>
        public int MaximumKeepAliveRequests
        {
            get
            {
                return _maximumKeepAliveRequests;
            }
            set
            {
                #region Check parameter

                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess("MaximumKeepAliveRequests", 1);
                }

                #endregion

                _maximumKeepAliveRequests = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to try to reconnect through the n-millisecond is necessary if the error occurred during a connection or send / load data.
        /// </summary>
        /// <value>default value - <see langword="false"/>.</value>
        public bool Reconnect { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of attempts to reconnect.
        /// </summary>
        /// <value>default value - 3.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">The parameter value is less than 1.</exception>
        public int ReconnectLimit
        {
            get
            {
                return _reconnectLimit;
            }
            set
            {
                #region Check parameter

                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess("ReconnectLimit", 1);
                }

                #endregion

                _reconnectLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the delay in milliseconds, that occurs before reconnection perform.
        /// </summary>
        /// <value>default value - 100 milliseconds.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">The value is less than 0.</exception>
        public int ReconnectDelay
        {
            get
            {
                return _reconnectDelay;
            }
            set
            {
                #region Check parameter

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("ReconnectDelay", 0);
                }

                #endregion

                _reconnectDelay = value;
            }
        }

        #endregion

        #region HTTP-headers

        /// <summary>
        /// The language used by the current query.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        /// <remarks>If the language is set, an additional header is sent 'Accept-Language' with the name of the language.</remarks>
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// Gets or sets the Encoding used for the conversion of incoming and outgoing data.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        /// <remarks>If encoding is set, an additional header is sent to 'Accept-Charset' with the name of this encoding, but only if this header is not set directly. the response encoding is determined automatically, but if it can not be determined, the value will be used for this property. If the value of this property is not specified, the value will be used <see cref="System.Text.Encoding.Default"/>.</remarks>
        public Encoding CharacterSet { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to encode the contents of the response needed.   It is used primarily for data compression.
        /// </summary>
        /// <value>default value - <see langword="true"/>.</value>
        /// <remarks>If the value is <see langword="true"/>, it goes further heading 'Accept-Encoding: gzip, deflate'.</remarks>
        public bool EnableEncodingContent { get; set; }

        /// <summary>
        /// Gets or sets the user name for basic authentication on the HTTP-server.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        /// <remarks>If set, then further sent to header 'Authorization'.</remarks>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password for basic authentication on the HTTP-server.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        /// <remarks>If set, then further sent to header 'Authorization'.</remarks>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the value of HTTP-heading 'User-Agent'.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        public string UserAgent
        {
            get
            {
                return this["User-Agent"];
            }
            set
            {
                this["User-Agent"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of HTTP-heading 'Referer'.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        public string Referer
        {
            get
            {
                return this["Referer"];
            }
            set
            {
                this["Referer"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of HTTP-heading 'Authorization'.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        public string Authorization
        {
            get
            {
                return this["Authorization"];
            }
            set
            {
                this["Authorization"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the cookie associated with the request.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        /// <remarks>Cookies can change the response from the HTTP-server.   To prevent this, you need to set the property <see cref="Shadynet.CookieDictionary.IsLocked"/> equal <see langword="true"/>.</remarks>
        public CookieCore Cookies { get; set; }

        #endregion

        #endregion


        #region Properties (internal)

        internal TcpClient TcpClient
        {
            get
            {
                return _connection;
            }
        }

        internal Stream ClientStream
        {
            get
            {
                return _connectionCommonStream;
            }
        }

        internal NetworkStream ClientNetworkStream
        {
            get
            {
                return _connectionNetworkStream;
            }
        }

        #endregion


        private MultipartContent AddedMultipartData
        {
            get
            {
                if (_temporaryMultipartContent == null)
                {
                    _temporaryMultipartContent = new MultipartContent();
                }

                return _temporaryMultipartContent;
            }
        }


        #region Indexers (open)

        /// <summary>
        /// Gets or sets the value of HTTP-heading.
        /// </summary>
        /// <param name="headerName">HTTP-heading name.</param>
        /// <value>HTTP-header value, if specified, or an empty string.   If you set <see langword="null"/> or an empty string, the HTTP-header will be deleted from the list.</value>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="headerName"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">
        /// parameter <paramref name="headerName"/> is an empty string.
        /// -or-
        /// Installation HTTP-header value which should be set by using a special features / Method.
        /// </exception>
        /// <remarks>List HTTP-header that is to be set only by the special properties / methods:
        /// <list type="table">
        ///     <item>
        ///        <description>Accept-Encoding</description>
        ///     </item>
        ///     <item>
        ///        <description>Content-Length</description>
        ///     </item>
        ///     <item>
        ///         <description>Content-Type</description>
        ///     </item>
        ///     <item>
        ///        <description>Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Proxy-Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Host</description>
        ///     </item>
        /// </list>
        /// </remarks>
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

                if (!_permanentHeaders.TryGetValue(headerName, out value))
                {
                    value = string.Empty;
                }

                return value;
            }
            set
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

                if (IsClosedHeader(headerName))
                {
                    throw new ArgumentException(string.Format(
                        Resources.ArgumentException_HttpRequest_SetNotAvailableHeader, headerName), "headerName");
                }

                #endregion

                if (string.IsNullOrEmpty(value))
                {
                    _permanentHeaders.Remove(headerName);
                }
                else
                {
                    _permanentHeaders[headerName] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the value of HTTP-heading.
        /// </summary>
        /// <param name="header">HTTP-header.</param>
        /// <value>HTTP-header value, if specified, or an empty string.   If you set  <see langword="null"/> or an empty string, the HTTP-header will be deleted from the list.</value>
        /// <exception cref="System.ArgumentException">Installation HTTP-header value which should be set by using a special features / Method.</exception>
        /// <remarks>List HTTP-header that is to be set only by the special properties / methods:
        /// <list type="table">
        ///     <item>
        ///        <description>Accept-Encoding</description>
        ///     </item>
        ///     <item>
        ///        <description>Content-Length</description>
        ///     </item>
        ///     <item>
        ///         <description>Content-Type</description>
        ///     </item>
        ///     <item>
        ///        <description>Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Proxy-Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Host</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public string this[HttpHeader header]
        {
            get
            {
                return this[Http.Headers[header]];
            }
            set
            {
                this[Http.Headers[header]] = value;
            }
        }

        #endregion


        #region Constructors (open)

        /// <summary>
        /// Initializes a new instance of the class <see cref="HttpRequest"/>.
        /// </summary>
        public HttpRequest()
        {
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the class <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="baseAddress">Address of the Internet resource that is used if the query is specified relative address.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="baseAddress"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">
        /// parameter <paramref name="baseAddress"/> is an empty string.
        /// -or-
        /// parameter <paramref name="baseAddress"/> It is not an absolute URI.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="baseAddress"/> It is not absolute URI.</exception>
        public HttpRequest(string baseAddress)
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

            Init();
        }

        /// <summary>
        /// Initializes a new instance of the class <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="baseAddress">Address of the Internet resource that is used if the query is specified relative address.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="baseAddress"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="baseAddress"/> It is not an absolute URI.</exception>
        public HttpRequest(Uri baseAddress)
        {
            #region Check settings

            if (baseAddress == null)
            {
                throw new ArgumentNullException("baseAddress");
            }

            if (!baseAddress.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "baseAddress");
            }

            #endregion

            BaseAddress = baseAddress;

            Init();
        }

        #endregion


        #region Methods (open)

        #region Get

        /// <summary>
        /// It sends a GET-request to the HTTP-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="urlParams">Parameters URL-addresses, or value <see langword="null"/>.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="address"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="address"/> is an empty string.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Get(string address, RequestParams urlParams = null)
        {
            if (urlParams != null)
            {
                _temporaryUrlParams = urlParams;
            }

            return Raw(HttpMethod.GET, address);
        }

        /// <summary>
        /// It sends a GET-request to the HTTP-server.
        /// </summary>
        /// <param name="address">Address of the Internet resource.</param>
        /// <param name="urlParams">Parameters URL-addresses, or value <see langword="null"/>.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="address"/> equally <see langword="null"/>.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Get(Uri address, RequestParams urlParams = null)
        {
            if (urlParams != null)
            {
                _temporaryUrlParams = urlParams;
            }

            return Raw(HttpMethod.GET, address);
        }

        #endregion

        #region Post

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="address"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="address"/> is an empty string.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(string address)
        {
            return Raw(HttpMethod.POST, address);
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="address"/> equally <see langword="null"/>.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(Uri address)
        {
            return Raw(HttpMethod.POST, address);
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="reqParams">Request parameters sent to the HTTP-server.</param>
        /// <param name="dontEscape">Specifies whether to encode the request parameters need.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="reqParams"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="address"/> is an empty string.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(string address, RequestParams reqParams, bool dontEscape = false)
        {
            #region Check settings

            if (reqParams == null)
            {
                throw new ArgumentNullException("reqParams");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FormUrlEncodedContent(reqParams, dontEscape, CharacterSet));
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="reqParams">Request parameters sent to the HTTP-server.</param>
        /// <param name="dontEscape">Specifies whether to encode the request parameters need.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="reqParams"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(Uri address, RequestParams reqParams, bool dontEscape = false)
        {
            #region Check settings

            if (reqParams == null)
            {
                throw new ArgumentNullException("reqParams");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FormUrlEncodedContent(reqParams, dontEscape, CharacterSet));
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="str">The string sent by the HTTP-server.</param>
        /// <param name="contentType">Type of data to be sent.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="str"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="contentType"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// parameter <paramref name="address"/> is an empty string.
        /// -or-
        /// parameter <paramref name="str"/> is an empty string.
        /// -or
        /// parameter <paramref name="contentType"/> is an empty string.
        /// </exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(string address, string str, string contentType)
        {
            #region Check settings

            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (str.Length == 0)
            {
                throw new ArgumentNullException("str");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StringContent(str)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="str">The string sent by the HTTP-server.</param>
        /// <param name="contentType">Type of data to be sent.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="str"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="contentType"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// parameter <paramref name="str"/> is an empty string.
        /// -or-
        /// parameter <paramref name="contentType"/> is an empty string.
        /// </exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(Uri address, string str, string contentType)
        {
            #region Check settings

            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (str.Length == 0)
            {
                throw new ArgumentNullException("str");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StringContent(str)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="bytes">An array of bytes sent to the HTTP-server.</param>
        /// <param name="contentType">Type of data to be sent.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="bytes"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="contentType"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// parameter <paramref name="address"/> is an empty string.
        /// -or-
        /// parameter <paramref name="contentType"/> is an empty string.
        /// </exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(string address, byte[] bytes, string contentType = "application/octet-stream")
        {
            #region Check settings

            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new BytesContent(bytes)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="bytes">An array of bytes sent to the HTTP-server.</param>
        /// <param name="contentType">Type of data to be sent.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="bytes"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="contentType"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="contentType"/> is an empty string.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(Uri address, byte[] bytes, string contentType = "application/octet-stream")
        {
            #region Check settings

            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new BytesContent(bytes)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="stream">The data stream sent HTTP-server.</param>
        /// <param name="contentType">Type of data to be sent.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="stream"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="contentType"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// parameter <paramref name="address"/> is an empty string.
        /// -or-
        /// parameter <paramref name="contentType"/> is an empty string.
        /// </exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(string address, Stream stream, string contentType = "application/octet-stream")
        {
            #region Check settings

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StreamContent(stream)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="stream">The data stream sent HTTP-server.</param>
        /// <param name="contentType">Type of data to be sent.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="stream"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="contentType"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="contentType"/> is an empty string.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(Uri address, Stream stream, string contentType = "application/octet-stream")
        {
            #region Check settings

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StreamContent(stream)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="path">Path to the file from which the data will be sent to the HTTP-server.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="path"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// parameter <paramref name="address"/> is an empty string.
        /// -or-
        /// parameter <paramref name="path"/> is an empty string.
        /// </exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(string address, string path)
        {
            #region Check settings

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentNullException("path");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FileContent(path));
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="path">Path to the file from which the data will be sent to the HTTP-server.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="path"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="path"/> is an empty string.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(Uri address, string path)
        {
            #region Check settings

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentNullException("path");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FileContent(path));
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="content">Content that is sent to the HTTP-server.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="content"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="address"/> is an empty string.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(string address, HttpContent content)
        {
            #region Check settings

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            #endregion

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// Sends the HTTP POST-search-server.
        /// </summary>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="content">Content that is sent to the HTTP-server.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="address"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="content"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Post(Uri address, HttpContent content)
        {
            #region Check settings

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            #endregion

            return Raw(HttpMethod.POST, address, content);
        }
        #endregion

        #region Raw

        /// <summary>
        /// It sends a HTTP-server inquiry.
        /// </summary>
        /// <param name="method">HTTP-request method.</param>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="content">Content that is sent to the HTTP-server, or value <see langword="null"/>.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="address"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="address"/> is an empty string.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Raw(HttpMethod method, string address, HttpContent content = null)
        {
            #region Check settings

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length == 0)
            {
                throw ExceptionHelper.EmptyString("address");
            }

            #endregion

            var uri = new Uri(address, UriKind.RelativeOrAbsolute);
            return Raw(method, uri, content);
        }

        /// <summary>
        /// It sends a HTTP-server inquiry.
        /// </summary>
        /// <param name="method">HTTP-request method.</param>
        /// <param name="address">Address Internet resource.</param>
        /// <param name="content">Content that is sent to the HTTP-server, or value <see langword="null"/>.</param>
        /// <returns>The object is designed to download a response from the HTTP-server.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="address"/> equally <see langword="null"/>.</exception>
        /// <exception cref="Shadynet.HttpException">Error when working with the HTTP-report.</exception>
        public HttpResponse Raw(HttpMethod method, Uri address, HttpContent content = null)
        {
            #region Check settings

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            #endregion

            if (!address.IsAbsoluteUri)
                address = GetRequestAddress(BaseAddress, address);

            if (_temporaryUrlParams != null)
            {
                var uriBuilder = new UriBuilder(address);
                uriBuilder.Query = Http.ToQueryString(_temporaryUrlParams, true);

                address = uriBuilder.Uri;
            }

            if (content == null)
            {
                if (_temporaryParams != null)
                {
                    content = new FormUrlEncodedContent(_temporaryParams, false, CharacterSet);
                }
                else if (_temporaryMultipartContent != null)
                {
                    content = _temporaryMultipartContent;
                }
            }

            try
            {
                return Request(method, address, content);
            }
            finally
            {
                if (content != null)
                    content.Dispose();

                ClearRequestData();
            }
        }

        #endregion

        #region Adding temporal data query

        /// <summary>
        /// Adds the temporary parameter is the URL-address.
        /// </summary>
        /// <param name="name">parameter name.</param>
        /// <param name="value">parameter, or value <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="name"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="name"/> is an empty string.</exception>
        /// <remarks>This parameter will be erased after the first request.</remarks>
        public HttpRequest AddUrlParam(string name, object value = null)
        {
            #region Check settings

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            #endregion

            if (_temporaryUrlParams == null)
            {
                _temporaryUrlParams = new RequestParams();
            }

            _temporaryUrlParams[name] = value;

            return this;
        }

        /// <summary>
        /// Parses Raw Paremeters and adds them into the <see langword="RequestParams"/> container to be used in the request.
        /// </summary>
        /// <param name="postdata">Raw post parameters.</param>
        public HttpRequest ParsePostData(string postdata)
        {
            #region Check settings

            if (postdata == null)
            {
                throw new ArgumentNullException("postdata");
            }

            if (postdata.Length == 0)
            {
                throw ExceptionHelper.EmptyString("postdata");
            }

            if (!postdata.Contains('='))
            {
                throw new ArgumentException("postdata");
            }
            #endregion

            if (_temporaryParams == null)
            {
                _temporaryParams = new RequestParams();
            }

            try
            {
                if (postdata.Contains("&"))
                {
                    string[] datastruct = postdata.Split('&');
                    foreach (var data in datastruct)
                    {
                        var key = data.Split('=')[0].Trim();
                        var value = data.Split('=')[1].Trim();
                        _temporaryParams[key] = value;
                    }
                    return this;
                }
                else
                {
                    var key = postdata.Split('=')[0].Trim();
                    var value = postdata.Split('=')[1].Trim();
                    _temporaryParams[key] = value;
                    return this;
                }
            }
            catch
            {
                throw new ArgumentException("Invalid Parameters.");
            }
        }

        /// <summary>
        /// Adds the temporary parameter query.
        /// </summary>
        /// <param name="name">parameter name.</param>
        /// <param name="value">parameter, or value <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="name"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="name"/> is an empty string.</exception>
        /// <remarks>This parameter will be erased after the first request.</remarks>
        public HttpRequest AddParam(string name, object value = null)
        {
            #region Check settings

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            #endregion

            if (_temporaryParams == null)
            {
                _temporaryParams = new RequestParams();
            }

            _temporaryParams[name] = value;

            return this;
        }

        /// <summary>
        /// Adds element temporary Multipart / form data.
        /// </summary>
        /// <param name="name">Element name.</param>
        /// <param name="value">value member, or value <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="name"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="name"/> is an empty string.</exception>
        /// <remarks>This item will be erased after the first request.</remarks>
        public HttpRequest AddField(string name, object value = null)
        {
            return AddField(name, value, CharacterSet ?? Encoding.UTF8);
        }

        /// <summary>
        /// Adds element temporary Multipart / form data.
        /// </summary>
        /// <param name="name">Element name.</param>
        /// <param name="value">value member, or value <see langword="null"/>.</param>
        /// <param name="encoding">Encoding used to convert the value into a sequence of bytes.</param>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="name"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="encoding"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="name"/> is an empty string.</exception>
        /// <remarks>This item will be erased after the first request.</remarks>
        public HttpRequest AddField(string name, object value, Encoding encoding)
        {
            #region Check settings

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            #endregion

            string contentValue = (value == null ? string.Empty : value.ToString());

            AddedMultipartData.Add(new StringContent(contentValue, encoding), name);

            return this;
        }

        /// <summary>
        /// Adds element temporary Multipart / form data.
        /// </summary>
        /// <param name="name">Element name.</param>
        /// <param name="value">value member.</param>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="name"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="value"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="name"/> is an empty string.</exception>
        /// <remarks>This item will be erased after the first request.</remarks>
        public HttpRequest AddField(string name, byte[] value)
        {
            #region Check settings

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            AddedMultipartData.Add(new BytesContent(value), name);

            return this;
        }

        /// <summary>
        /// Adds element temporary Multipart / form data representing the file.
        /// </summary>
        /// <param name="name">Element name.</param>
        /// <param name="fileName">The name of the transmitted file.</param>
        /// <param name="value">data file.</param>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="name"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="fileName"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="value"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="name"/> is an empty string.</exception>
        /// <remarks>This item will be erased after the first request.</remarks>
        public HttpRequest AddFile(string name, string fileName, byte[] value)
        {
            #region Check settings

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            AddedMultipartData.Add(new BytesContent(value), name, fileName);

            return this;
        }

        /// <summary>
        /// Adds element temporary Multipart / form data representing the file.
        /// </summary>
        /// <param name="name">Element name.</param>
        /// <param name="fileName">The name of the transmitted file.</param>
        /// <param name="contentType">MIME-type content.</param>
        /// <param name="value">data file.</param>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="name"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="fileName"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="contentType"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="value"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="name"/> is an empty string.</exception>
        /// <remarks>This item will be erased after the first request.</remarks>
        public HttpRequest AddFile(string name, string fileName, string contentType, byte[] value)
        {
            #region Check settings

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            AddedMultipartData.Add(new BytesContent(value), name, fileName, contentType);

            return this;
        }

        /// <summary>
        /// Adds element temporary Multipart / form data representing the file.
        /// </summary>
        /// <param name="name">Element name.</param>
        /// <param name="fileName">The name of the transmitted file.</param>
        /// <param name="stream">The flow of data file.</param>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="name"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="fileName"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="stream"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="name"/> is an empty string.</exception>
        /// <remarks>This item will be erased after the first request.</remarks>
        public HttpRequest AddFile(string name, string fileName, Stream stream)
        {
            #region Check settings

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            #endregion

            AddedMultipartData.Add(new StreamContent(stream), name, fileName);

            return this;
        }

        /// <summary>
        /// Adds element temporary Multipart / form data representing the file.
        /// </summary>
        /// <param name="name">Element name.</param>
        /// <param name="fileName">The name of the transmitted file.</param>
        /// <param name="path">The path to the loaded file.</param>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="name"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="fileName"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="path"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// parameter <paramref name="name"/> is an empty string.
        /// -or-
        /// parameter <paramref name="path"/> is an empty string.
        /// </exception>
        /// <remarks>This item will be erased after the first request.</remarks>
        public HttpRequest AddFile(string name, string fileName, string path)
        {
            #region Check settings

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw ExceptionHelper.EmptyString("path");
            }

            #endregion

            AddedMultipartData.Add(new FileContent(path), name, fileName);

            return this;
        }

        /// <summary>
        /// Adds element temporary Multipart / form data representing the file.
        /// </summary>
        /// <param name="name">Element name.</param>
        /// <param name="path">The path to the loaded file.</param>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="name"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="path"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// parameter <paramref name="name"/> is an empty string.
        /// -or-
        /// parameter <paramref name="path"/> is an empty string.
        /// </exception>
        /// <remarks>This item will be erased after the first request.</remarks>
        public HttpRequest AddFile(string name, string path)
        {
            #region Check settings

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw ExceptionHelper.EmptyString("path");
            }

            #endregion

            AddedMultipartData.Add(new FileContent(path),
                name, Path.GetFileName(path));

            return this;
        }

        /// <summary>
        /// Adds a temporary HTTP-request header.   This heading covers the header set by the indexer.
        /// </summary>
        /// <param name="name">HTTP-heading Name.</param>
        /// <param name="value">value HTTP-header.</param>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="name"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="value"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// parameter <paramref name="name"/> is an empty string.
        /// -or-
        /// parameter <paramref name="value"/> is an empty string.
        /// -or-
        /// Installation HTTP-header value which should be set by using a special features / method.
        /// </exception>
        /// <remarks>This HTTP-header will be erased after the first request.</remarks>
        public HttpRequest AddHeader(string name, string value)
        {
            #region Check settings

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.Length == 0)
            {
                throw ExceptionHelper.EmptyString("value");
            }

            if (IsClosedHeader(name))
            {
                throw new ArgumentException(string.Format(
                    Resources.ArgumentException_HttpRequest_SetNotAvailableHeader, name), "name");
            }

            #endregion

            if (_temporaryHeaders == null)
            {
                _temporaryHeaders = new Dictionary<string, string>();
            }

            _temporaryHeaders[name] = value;

            return this;
        }

        /// <summary>
        /// Adds a temporary HTTP-request header.   This heading covers the header set by the indexer.
        /// </summary>
        /// <param name="header">HTTP-header.</param>
        /// <param name="value">value HTTP-header.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="value"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">
        /// parameter <paramref name="value"/> is an empty string.
        /// -or-
        /// Installation HTTP-header value which should be set by using a special features / method.
        /// </exception>
        /// <remarks>This HTTP-header will be erased after the first request.</remarks>
        public HttpRequest AddHeader(HttpHeader header, string value)
        {
            AddHeader(Http.Headers[header], value);

            return this;
        }

        #endregion

        /// <summary>
        /// It closes the connection to the HTTP-server.
        /// </summary>
        /// <remarks>Calling this method equallysilen method call <see cref="Dispose"/>.</remarks>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Releases all resources used by the current instance of the class <see cref="HttpRequest"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// It determines whether the specified cookie contained.
        /// </summary>
        /// <param name="name">The name of the cookie.</param>
        /// <returns>value <see langword="true"/>, if these cookies contain, or value <see langword="false"/>.</returns>
        public bool ContainsCookie(string name)
        {
            if (Cookies == null)
                return false;

            return Cookies.ContainsKey(name);
        }

        #region Working with headers

        /// <summary>
        /// Determines whether the specified HTTP-header.
        /// </summary>
        /// <param name="headerName">Name HTTP-header.</param>
        /// <returns>value <see langword="true"/>, if the HTTP-header contains the specified, otherwise the value<see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="headerName"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="headerName"/> is an empty string.</exception>
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

            return _permanentHeaders.ContainsKey(headerName);
        }

        /// <summary>
        /// Determines whether the specified HTTP-header.
        /// </summary>
        /// <param name="header">HTTP-header.</param>
        /// <returns>value <see langword="true"/>, if the HTTP-header contains the specified, otherwise the value <see langword="false"/>.</returns>
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
            return _permanentHeaders.GetEnumerator();
        }

        /// <summary>
        /// It clears all HTTP-headers.
        /// </summary>
        public void ClearAllHeaders()
        {
            _permanentHeaders.Clear();
        }

        #endregion

        #endregion


        #region Methods (protected)

        /// Releases the unmanaged (and if necessary controlled) resources used <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="disposing">value <see langword="true"/> frees managed and unmanaged resources; value <see langword="false"/> It allows the release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _connection != null)
            {
                _connection.Close();
                _connection = null;
                _connectionCommonStream = null;
                _connectionNetworkStream = null;

                _keepAliveRequestCount = 0;
            }
        }

        /// <summary>
        /// It raises an event <see cref="UploadProgressChanged"/>.
        /// </summary>
        /// <param name="e">event Arguments.</param>
        protected virtual void OnUploadProgressChanged(UploadProgressChangedEventArgs e)
        {
            EventHandler<UploadProgressChangedEventArgs> eventHandler = _uploadProgressChangedHandler;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        /// <summary>
        /// It raises an event <see cref="DownloadProgressChanged"/>.
        /// </summary>
        /// <param name="e">event Arguments.</param>
        protected virtual void OnDownloadProgressChanged(DownloadProgressChangedEventArgs e)
        {
            EventHandler<DownloadProgressChangedEventArgs> eventHandler = _downloadProgressChangedHandler;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        #endregion


        #region Methods of (closed)

        private void Init()
        {
            KeepAlive = true;
            AllowAutoRedirect = true;
            EnableEncodingContent = true;

            _response = new HttpResponse(this);
        }

        private Uri GetRequestAddress(Uri baseAddress, Uri address)
        {
            var requestAddress = address;

            if (baseAddress == null)
            {
                var uriBuilder = new UriBuilder(address.OriginalString);
                requestAddress = uriBuilder.Uri;
            }
            else
            {
                Uri.TryCreate(baseAddress, address, out requestAddress);
            }

            return requestAddress;
        }

        #region Sending request

        private HttpResponse Request(HttpMethod method, Uri address, HttpContent content)
        {
            _method = method;
            _content = content;

            CloseConnectionIfNeeded();

            var previousAddress = Address;
            Address = address;

            var createdNewConnection = false;
            try
            {
                createdNewConnection = TryCreateConnectionOrUseExisting(address, previousAddress);
            }
            catch (HttpException ex)
            {
                if (CanReconnect())
                    return ReconnectAfterFail();

                throw;
            }

            if (createdNewConnection)
                _keepAliveRequestCount = 1;
            else
                _keepAliveRequestCount++;

            #region Sending request

            try
            {
                SendRequestData(method);
            }
            catch (SecurityException ex)
            {
                throw NewHttpException(Resources.HttpException_FailedSendRequest, ex, HttpExceptionStatus.SendFailure);
            }
            catch (IOException ex)
            {
                if (CanReconnect())
                    return ReconnectAfterFail();

                throw NewHttpException(Resources.HttpException_FailedSendRequest, ex, HttpExceptionStatus.SendFailure);
            }

            #endregion

            #region Loading response headers

            try
            {
                ReceiveResponseHeaders(method);
            }
            catch (HttpException ex)
            {
                if (CanReconnect())
                    return ReconnectAfterFail();

                // If the server is interrupted permanent connection returned an empty response, then try to connect again.
                // He could break the connection because it has the maximum number of queries came or downtime.
                if (KeepAlive && !_keepAliveReconnected && !createdNewConnection && ex.EmptyMessageBody)
                    return KeepAliveReconect();

                throw;
            }

            #endregion

            _response.ReconnectCount = _reconnectCount;

            _reconnectCount = 0;
            _keepAliveReconnected = false;
            _whenConnectionIdle = DateTime.Now;

            if (!IgnoreProtocolErrors)
                CheckStatusCode(_response.StatusCode);

            #region call forwarding

            if (AllowAutoRedirect && _response.HasRedirect)
            {
                if (++_redirectionCount > _maximumAutomaticRedirections)
                    throw NewHttpException(Resources.HttpException_LimitRedirections);

                ClearRequestData();
                return Request(HttpMethod.GET, _response.RedirectAddress, null);
            }

            _redirectionCount = 0;

            #endregion

            return _response;
        }

        private void CloseConnectionIfNeeded()
        {
            var hasConnection = (_connection != null);

            if (hasConnection && !_response.HasError &&
                !_response.MessageBodyLoaded)
            {
                try
                {
                    _response.None();
                }
                catch (HttpException)
                {
                    Dispose();
                }
            }
        }

        private bool TryCreateConnectionOrUseExisting(Uri address, Uri previousAddress)
        {
            ProxyClient proxy = GetProxy();

            var hasConnection = (_connection != null);
            var proxyChanged = (_currentProxy != proxy);

            var addressChanged =
                (previousAddress == null) ||
                (previousAddress.Port != address.Port) ||
                (previousAddress.Host != address.Host) ||
                (previousAddress.Scheme != address.Scheme);

            // If you want to create a new connection.
            if (!hasConnection || proxyChanged ||
                addressChanged || _response.HasError ||
                KeepAliveLimitIsReached())
            {
                _currentProxy = proxy;

                Dispose();
                CreateConnection(address);
                return true;
            }

            return false;
        }

        private bool KeepAliveLimitIsReached()
        {
            if (!KeepAlive)
                return false;

            var maximumKeepAliveRequests =
                _response.MaximumKeepAliveRequests ?? _maximumKeepAliveRequests;

            if (_keepAliveRequestCount >= maximumKeepAliveRequests)
                return true;

            var keepAliveTimeout =
                _response.KeepAliveTimeout ?? _keepAliveTimeout;

            var timeLimit = _whenConnectionIdle.AddMilliseconds(keepAliveTimeout);
            if (timeLimit < DateTime.Now)
                return true;

            return false;
        }

        private void SendRequestData(HttpMethod method)
        {
            var contentLength = 0L;
            var contentType = string.Empty;

            if (CanContainsRequestBody(method) && (_content != null))
            {
                contentType = _content.ContentType;
                contentLength = _content.CalculateContentLength();
            }

            var startingLine = GenerateStartingLine(method);
            var headers = GenerateHeaders(method, contentLength, contentType);

            var startingLineBytes = Encoding.ASCII.GetBytes(startingLine);
            var headersBytes = Encoding.ASCII.GetBytes(headers);

            _bytesSent = 0;
            _totalBytesSent = startingLineBytes.Length + headersBytes.Length + contentLength;

            _connectionCommonStream.Write(startingLineBytes, 0, startingLineBytes.Length);
            _connectionCommonStream.Write(headersBytes, 0, headersBytes.Length);

            var hasRequestBody = (_content != null) && (contentLength > 0);

            // Sends a request to the body if it is not present.
            if (hasRequestBody)
                _content.WriteTo(_connectionCommonStream);
        }

        private void ReceiveResponseHeaders(HttpMethod method)
        {
            _canReportBytesReceived = false;

            _bytesReceived = 0;
            _totalBytesReceived = _response.LoadResponse(method);

            _canReportBytesReceived = true;
        }

        private bool CanReconnect()
        {
            return Reconnect && (_reconnectCount < _reconnectLimit);
        }

        private HttpResponse ReconnectAfterFail()
        {
            Dispose();
            Thread.Sleep(_reconnectDelay);

            _reconnectCount++;
            return Request(_method, Address, _content);
        }

        private HttpResponse KeepAliveReconect()
        {
            Dispose();
            _keepAliveReconnected = true;
            return Request(_method, Address, _content);
        }

        private void CheckStatusCode(HttpStatusCode statusCode)
        {
            var statusCodeNum = (int)statusCode;

            if ((statusCodeNum >= 400) && (statusCodeNum < 500))
            {
                throw new HttpException(string.Format(
                    Resources.HttpException_ClientError, statusCodeNum),
                    HttpExceptionStatus.ProtocolError, _response.StatusCode);
            }

            if (statusCodeNum >= 500)
            {
                throw new HttpException(string.Format(
                    Resources.HttpException_SeverError, statusCodeNum),
                    HttpExceptionStatus.ProtocolError, _response.StatusCode);
            }
        }

        private bool CanContainsRequestBody(HttpMethod method)
        {
            return
                (method == HttpMethod.PUT) ||
                (method == HttpMethod.POST) ||
                (method == HttpMethod.DELETE);
        }

        #endregion

        #region Creating a connection

        private ProxyClient GetProxy()
        {
            if (DisableProxyForLocalAddress)
            {
                try
                {
                    var checkIp = IPAddress.Parse("127.0.0.1");
                    IPAddress[] ips = Dns.GetHostAddresses(Address.Host);

                    foreach (var ip in ips)
                    {
                        if (ip.Equals(checkIp))
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is ArgumentException)
                    {
                        throw NewHttpException(
                            Resources.HttpException_FailedGetHostAddresses, ex);
                    }

                    throw;
                }
            }

            ProxyClient proxy = Proxy ?? GlobalProxy;

            if (proxy == null && UseIeProxy && !WinInet.InternetConnected)
            {
                proxy = WinInet.IEProxy;
            }

            return proxy;
        }

        private TcpClient CreateTcpConnection(string host, int port)
        {
            TcpClient tcpClient;

            if (_currentProxy == null)
            {
                #region Creating a connection

                tcpClient = new TcpClient();

                Exception connectException = null;
                var connectDoneEvent = new ManualResetEventSlim();

                try
                {
                    tcpClient.BeginConnect(host, port, new AsyncCallback(
                        (ar) =>
                        {
                            try
                            {
                                tcpClient.EndConnect(ar);
                            }
                            catch (Exception ex)
                            {
                                connectException = ex;
                            }

                            connectDoneEvent.Set();
                        }), tcpClient
                    );
                }
                #region Catches

                catch (Exception ex)
                {
                    tcpClient.Close();

                    if (ex is SocketException || ex is SecurityException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedConnect, ex, HttpExceptionStatus.ConnectFailure);
                    }

                    throw;
                }

                #endregion

                if (!connectDoneEvent.Wait(_connectTimeout))
                {
                    tcpClient.Close();
                    throw NewHttpException(Resources.HttpException_ConnectTimeout, null, HttpExceptionStatus.ConnectFailure);
                }

                if (connectException != null)
                {
                    tcpClient.Close();

                    if (connectException is SocketException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedConnect, connectException, HttpExceptionStatus.ConnectFailure);
                    }

                    throw connectException;
                }

                if (!tcpClient.Connected)
                {
                    tcpClient.Close();
                    throw NewHttpException(Resources.HttpException_FailedConnect, null, HttpExceptionStatus.ConnectFailure);
                }

                #endregion

                _response.ConnectionTimeout = _readWriteTimeout;
                tcpClient.SendTimeout = _readWriteTimeout;
                tcpClient.ReceiveTimeout = _readWriteTimeout;
            }
            else
            {
                try
                {
                    tcpClient = _currentProxy.CreateConnection(host, port);
                }
                catch (ProxyException ex)
                {
                    throw NewHttpException(Resources.HttpException_FailedConnect, ex, HttpExceptionStatus.ConnectFailure);
                }
            }

            return tcpClient;
        }

        private void CreateConnection(Uri address)
        {
            _connection = CreateTcpConnection(address.Host, address.Port);
            _connectionNetworkStream = _connection.GetStream();

            // If you want a secure connection.
            if (address.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    SslStream sslStream;

                    if (SslCertificateValidatorCallback == null)
                    {
                        sslStream = new SslStream(_connectionNetworkStream, false, Http.AcceptAllCertificationsCallback);
                    }
                    else
                    {
                        sslStream = new SslStream(_connectionNetworkStream, false, SslCertificateValidatorCallback);
                    }

                    sslStream.AuthenticateAsClient(address.Host);
                    _connectionCommonStream = sslStream;
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is AuthenticationException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedSslConnect, ex, HttpExceptionStatus.ConnectFailure);
                    }

                    throw;
                }
            }
            else
            {
                _connectionCommonStream = _connectionNetworkStream;
            }

            if (_uploadProgressChangedHandler != null ||
                _downloadProgressChangedHandler != null)
            {
                var httpWraperStream = new HttpWraperStream(
                    _connectionCommonStream, _connection.SendBufferSize);

                if (_uploadProgressChangedHandler != null)
                {
                    httpWraperStream.BytesWriteCallback = ReportBytesSent;
                }

                if (_downloadProgressChangedHandler != null)
                {
                    httpWraperStream.BytesReadCallback = ReportBytesReceived;
                }

                _connectionCommonStream = httpWraperStream;
            }
        }

        #endregion

        #region Formation of the data request

        private string GenerateStartingLine(HttpMethod method)
        {
            string query;

            if (_currentProxy != null &&
                (_currentProxy.Type == ProxyType.Http || _currentProxy.Type == ProxyType.Chain))
            {
                query = Address.AbsoluteUri;
            }
            else
            {
                query = Address.PathAndQuery;
            }

            return string.Format("{0} {1} HTTP/{2}\r\n",
                method, query, ProtocolVersion);
        }

        // There are 3 types of headers that can overlap the other. Here is the order of their installation:
        // - Headers, which are set through the special properties or automatically
        // - Headers, which are set by the indexer
        // - Time-headers that are specified through method AddHeader
        private string GenerateHeaders(HttpMethod method, long contentLength = 0, string contentType = null)
        {
            var headers = GenerateCommonHeaders(method, contentLength, contentType);

            MergeHeaders(headers, _permanentHeaders);

            if (_temporaryHeaders != null && _temporaryHeaders.Count > 0)
                MergeHeaders(headers, _temporaryHeaders);

            if (Cookies != null && Cookies.Count != 0 && !headers.ContainsKey("Cookie"))
                headers["Cookie"] = Cookies.ToString();

            return ToHeadersString(headers);
        }

        private Dictionary<string, string> GenerateCommonHeaders(HttpMethod method, long contentLength = 0, string contentType = null)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            #region Host

            if (Address.IsDefaultPort)
                headers["Host"] = Address.Host;
            else
                headers["Host"] = string.Format("{0}:{1}", Address.Host, Address.Port);

            #endregion

            #region Connection and Authorization

            HttpProxyClient httpProxy = null;

            if (_currentProxy != null && _currentProxy.Type == ProxyType.Http)
            {
                httpProxy = _currentProxy as HttpProxyClient;
            }
            else if (_currentProxy != null && _currentProxy.Type == ProxyType.Chain)
            {
                httpProxy = FindHttpProxyInChain(_currentProxy as ChainProxyClient);
            }

            if (httpProxy != null)
            {
                if (KeepAlive)
                    headers["Proxy-Connection"] = "keep-alive";
                else
                    headers["Proxy-Connection"] = "close";

                if (!string.IsNullOrEmpty(httpProxy.Username) ||
                    !string.IsNullOrEmpty(httpProxy.Password))
                {
                    headers["Proxy-Authorization"] = GetProxyAuthorizationHeader(httpProxy);
                }
            }
            else
            {
                if (KeepAlive)
                    headers["Connection"] = "keep-alive";
                else
                    headers["Connection"] = "close";
            }

            if (!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password))
            {
                headers["Authorization"] = GetAuthorizationHeader();
            }

            #endregion

            #region Content

            if (EnableEncodingContent)
                headers["Accept-Encoding"] = "gzip,deflate";

            if (Culture != null)
                headers["Accept-Language"] = GetLanguageHeader();

            if (CharacterSet != null)
                headers["Accept-Charset"] = GetCharsetHeader();

            if (CanContainsRequestBody(method))
            {
                if (contentLength > 0)
                {
                    headers["Content-Type"] = contentType;
                }

                headers["Content-Length"] = contentLength.ToString();
            }

            #endregion

            return headers;
        }

        #region Working with headers

        private string GetAuthorizationHeader()
        {
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                string.Format("{0}:{1}", Username, Password)));

            return string.Format("Basic {0}", data);
        }

        private string GetProxyAuthorizationHeader(HttpProxyClient httpProxy)
        {
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                string.Format("{0}:{1}", httpProxy.Username, httpProxy.Password)));

            return string.Format("Basic {0}", data);
        }

        private string GetLanguageHeader()
        {
            string cultureName;

            if (Culture != null)
                cultureName = Culture.Name;
            else
                cultureName = CultureInfo.CurrentCulture.Name;

            if (cultureName.StartsWith("en"))
                return cultureName;

            return string.Format("{0},{1};q=0.8,en-US;q=0.6,en;q=0.4",
                cultureName, cultureName.Substring(0, 2));
        }

        private string GetCharsetHeader()
        {
            if (CharacterSet == Encoding.UTF8)
            {
                return "utf-8;q=0.7,*;q=0.3";
            }

            string charsetName;

            if (CharacterSet == null)
            {
                charsetName = Encoding.Default.WebName;
            }
            else
            {
                charsetName = CharacterSet.WebName;
            }

            return string.Format("{0},utf-8;q=0.7,*;q=0.3", charsetName);
        }

        private void MergeHeaders(Dictionary<string, string> destination, Dictionary<string, string> source)
        {
            foreach (var sourceItem in source)
            {
                destination[sourceItem.Key] = sourceItem.Value;
            }
        }

        #endregion

        private HttpProxyClient FindHttpProxyInChain(ChainProxyClient chainProxy)
        {
            HttpProxyClient foundProxy = null;

            // HTTP-Proxy are looking in all the proxy chains. 
            // The priority to find a proxy that requires authentication.
            foreach (var proxy in chainProxy.Proxies)
            {
                if (proxy.Type == ProxyType.Http)
                {
                    foundProxy = proxy as HttpProxyClient;

                    if (!string.IsNullOrEmpty(foundProxy.Username) ||
                        !string.IsNullOrEmpty(foundProxy.Password))
                    {
                        return foundProxy;
                    }
                }
                else if (proxy.Type == ProxyType.Chain)
                {
                    HttpProxyClient foundDeepProxy =
                        FindHttpProxyInChain(proxy as ChainProxyClient);

                    if (foundDeepProxy != null &&
                        (!string.IsNullOrEmpty(foundDeepProxy.Username) ||
                        !string.IsNullOrEmpty(foundDeepProxy.Password)))
                    {
                        return foundDeepProxy;
                    }
                }
            }

            return foundProxy;
        }

        private string ToHeadersString(Dictionary<string, string> headers)
        {
            var headersBuilder = new StringBuilder();
            foreach (var header in headers)
            {
                headersBuilder.AppendFormat("{0}: {1}\r\n", header.Key, header.Value);
            }

            headersBuilder.AppendLine();
            return headersBuilder.ToString();
        }

        #endregion

        // Reports how many bytes were sent to the HTTP-server.
        private void ReportBytesSent(int bytesSent)
        {
            _bytesSent += bytesSent;

            OnUploadProgressChanged(
                new UploadProgressChangedEventArgs(_bytesSent, _totalBytesSent));
        }

        // Reports how many bytes were received from the HTTP-server.
        private void ReportBytesReceived(int bytesReceived)
        {
            _bytesReceived += bytesReceived;

            if (_canReportBytesReceived)
            {
                OnDownloadProgressChanged(
                    new DownloadProgressChangedEventArgs(_bytesReceived, _totalBytesReceived));
            }
        }

        // Checks to see if you can set this header.
        private bool IsClosedHeader(string name)
        {
            return _closedHeaders.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        private void ClearRequestData()
        {
            _content = null;

            _temporaryUrlParams = null;
            _temporaryParams = null;
            _temporaryMultipartContent = null;
            _temporaryHeaders = null;
        }

        private HttpException NewHttpException(string message,
            Exception innerException = null, HttpExceptionStatus status = HttpExceptionStatus.Other)
        {
            return new HttpException(string.Format(message, Address.Host), status, HttpStatusCode.None, innerException);
        }

        #endregion
    }
}

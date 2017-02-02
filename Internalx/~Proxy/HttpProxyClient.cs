using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Shadynet.Http;

namespace Shadynet.Proxy
{
    /// <summary>
    /// It is the client for the HTTP proxy server.
    /// </summary>
    public class HttpProxyClient : ProxyClient
    {
        #region Constants (closed)

        private const int BufferSize = 50;
        private const int DefaultPort = 8080;
        private const string NewLine = "\r\n";
        #endregion


        #region Constructors (open)

        /// <summary>
        /// Initializes a new instance of the class <see cref="HttpProxyClient"/>.
        /// </summary>
        public HttpProxyClient()
            : this(null) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="HttpProxyClient"/> specify proxy server host, and sets the port to be - 8080.
        /// </summary>
        /// <param name="host">Proxy Host.</param>
        public HttpProxyClient(string host)
            : this(host, DefaultPort) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="HttpProxyClient"/> specified data proxy server.
        /// </summary>
        /// <param name="host">Proxy Host.</param>
        /// <param name="port">Proxy Port.</param>
        public HttpProxyClient(string host, int port)
            : this(host, port, string.Empty, string.Empty) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="HttpProxyClient"/> specified data proxy server.
        /// </summary>
        /// <param name="host">Proxy Host.</param>
        /// <param name="port">Proxy Port.</param>
        /// <param name="username">Username for authentication on the proxy server.</param>
        /// <param name="password">Password for authentication on the proxy server.</param>
        public HttpProxyClient(string host, int port, string username, string password)
            : base(ProxyType.Http, host, port, username, password) { }

        #endregion


        #region Static methods (open)

        /// <summary>
        /// Converts a string to an instance <see cref="HttpProxyClient"/>.
        /// </summary>
        /// <param name="proxyAddress">String type - host:port:username:password. The last three are optional.</param>
        /// <returns>An instance <see cref="HttpProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="proxyAddress"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="proxyAddress"/> It is an empty string.</exception>
        /// <exception cref="System.FormatException">port format is wrong.</exception>
        public static HttpProxyClient Parse(string proxyAddress)
        {
            return ProxyClient.Parse(ProxyType.Http, proxyAddress) as HttpProxyClient;
        }

        /// <summary>
        /// Converts a string to an instance <see cref="HttpProxyClient"/>. Gets a value indicating whether the conversion was successful.
        /// </summary>
        /// <param name="proxyAddress">String type - host:port:username:password.   The last three are optional.</param>
        /// <param name="result">If the conversion is successful, it contains an instance <see cref="HttpProxyClient"/>, otherwise <see langword="null"/>.</param>
        /// <returns>Value <see langword="true"/>, if the parameter <paramref name="proxyAddress"/> convertes successfully, otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string proxyAddress, out HttpProxyClient result)
        {
            ProxyClient proxy;

            if (ProxyClient.TryParse(ProxyType.Http, proxyAddress, out proxy))
            {
                result = proxy as HttpProxyClient;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        #endregion


        #region Methods (open)

        /// <summary>
        /// It creates a connection to the server via a proxy server.
        /// </summary>
        /// <param name="destinationHost">Host server with which to connect through a proxy server.</param>
        /// <param name="destinationPort">Server port to which you want to communicate through a proxy server.</param>
        /// <param name="tcpClient">The connection through which to work, or value<see langword="null"/>.</param>
        /// <returns>The connection to the server via a proxy server.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// property value <see cref="Host"/> equally <see langword="null"/> or It has zero length.
        /// -or-
        /// property value <see cref="Port"/> less than 1 or greater than 65535.
        /// -or-
        /// property value <see cref="Username"/> It is longer than 255 characters.
        /// -or-
        /// property value <see cref="Password"/> It is longer than 255 characters.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="destinationHost"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="destinationHost"/> It is an empty string.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">parameter <paramref name="destinationPort"/> less than 1 or greater than 65535.</exception>
        /// <exception cref="Shadynet.Proxy.ProxyException">Failed to work with a proxy server.</exception>
        /// <remarks>If the server port 80 is uneven, use the method to connect 'CONNECT'.</remarks>
        public override TcpClient CreateConnection(string destinationHost, int destinationPort, TcpClient tcpClient = null)
        {
            CheckState();

            #region Check settings

            if (destinationHost == null)
            {
                throw new ArgumentNullException("destinationHost");
            }

            if (destinationHost.Length == 0)
            {
                throw ExceptionHelper.EmptyString("destinationHost");
            }

            if (!ExceptionHelper.ValidateTcpPort(destinationPort))
            {
                throw ExceptionHelper.WrongTcpPort("destinationPort");
            }

            #endregion

            TcpClient curTcpClient = tcpClient;

            if (curTcpClient == null)
            {
                curTcpClient = CreateConnectionToProxy();
            }

            if (destinationPort != 80)
            {
                HttpStatusCode statusCode = HttpStatusCode.OK;

                try
                {
                    NetworkStream nStream = curTcpClient.GetStream();

                    SendConnectionCommand(nStream, destinationHost, destinationPort);
                    statusCode = ReceiveResponse(nStream);
                }
                catch (Exception ex)
                {
                    curTcpClient.Close();

                    if (ex is IOException || ex is SocketException)
                    {
                        throw NewProxyException(Resources.ProxyException_Error, ex);
                    }

                    throw;
                }

                if (statusCode != HttpStatusCode.OK)
                {
                    curTcpClient.Close();

                    throw new ProxyException(string.Format(
                        Resources.ProxyException_ReceivedWrongStatusCode, statusCode, ToString()), this);
                }
            }

            return curTcpClient;
        }

        #endregion


        #region Methods of (closed)

        private string GenerateAuthorizationHeader()
        {
            if (!string.IsNullOrEmpty(_username) || !string.IsNullOrEmpty(_password))
            {
                string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                    string.Format("{0}:{1}", _username, _password)));

                return string.Format("Proxy-Authorization: Basic {0}\r\n", data);
            }

            return string.Empty;
        }

        private void SendConnectionCommand(NetworkStream nStream, string destinationHost, int destinationPort)
        {
            var commandBuilder = new StringBuilder();

            commandBuilder.AppendFormat("CONNECT {0}:{1} HTTP/1.1\r\n", destinationHost, destinationPort);
            commandBuilder.AppendFormat(GenerateAuthorizationHeader());
            commandBuilder.AppendLine();

            byte[] buffer = Encoding.ASCII.GetBytes(commandBuilder.ToString());

            nStream.Write(buffer, 0, buffer.Length);
        }

        private HttpStatusCode ReceiveResponse(NetworkStream nStream)
        {
            byte[] buffer = new byte[BufferSize];
            var responseBuilder = new StringBuilder();

            WaitData(nStream);

            do
            {
                int bytesRead = nStream.Read(buffer, 0, BufferSize);
                responseBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            } while (nStream.DataAvailable);

            string response = responseBuilder.ToString();

            if (response.Length == 0)
            {
                throw NewProxyException(Resources.ProxyException_ReceivedEmptyResponse);
            }

            // Select the status bar.   Example: HTTP/1.1 200 OK\r\n
            string strStatus = response.Substring(" ", NewLine);

            int simPos = strStatus.IndexOf(' ');

            if (simPos == -1)
            {
                throw NewProxyException(Resources.ProxyException_ReceivedWrongResponse);
            }

            string statusLine = strStatus.Substring(0, simPos);

            if (statusLine.Length == 0)
            {
                throw NewProxyException(Resources.ProxyException_ReceivedWrongResponse);
            }

            HttpStatusCode statusCode = (HttpStatusCode)Enum.Parse(
                typeof(HttpStatusCode), statusLine);

            return statusCode;
        }

        private void WaitData(NetworkStream nStream)
        {
            int sleepTime = 0;
            int delay = (nStream.ReadTimeout < 10) ?
                10 : nStream.ReadTimeout;

            while (!nStream.DataAvailable)
            {
                if (sleepTime >= delay)
                {
                    throw NewProxyException(Resources.ProxyException_WaitDataTimeout);
                }

                sleepTime += 10;
                Thread.Sleep(10);
            }
        }

        #endregion
    }
}
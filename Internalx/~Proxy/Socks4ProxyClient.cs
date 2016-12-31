using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Shadynet
{
    /// <summary>
    /// Represents the client proxy Socks4.
    /// </summary>
    public class Socks4ProxyClient : ProxyClient
    {
        #region Constants (protected)

        internal protected const int DefaultPort = 1080;

        internal protected const byte VersionNumber = 4;
        internal protected const byte CommandConnect = 0x01;
        internal protected const byte CommandBind = 0x02;
        internal protected const byte CommandReplyRequestGranted = 0x5a;
        internal protected const byte CommandReplyRequestRejectedOrFailed = 0x5b;
        internal protected const byte CommandReplyRequestRejectedCannotConnectToIdentd = 0x5c;
        internal protected const byte CommandReplyRequestRejectedDifferentIdentd = 0x5d;

        #endregion


        #region Constructors (open)

        /// <summary>
        /// Initializes a new instance of the class <see cref="Socks4ProxyClient"/>.
        /// </summary>
        public Socks4ProxyClient()
            : this(null) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="Socks4ProxyClient"/> specify proxy server host, and sets the port to be - 1080.
        /// </summary>
        /// <param name="host">Proxy Host.</param>
        public Socks4ProxyClient(string host)
            : this(host, DefaultPort) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="Socks4ProxyClient"/> specified data proxy server.
        /// </summary>
        /// <param name="host">Proxy Host.</param>
        /// <param name="port">Proxy Port.</param>
        public Socks4ProxyClient(string host, int port)
            : this(host, port, string.Empty) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="Socks4ProxyClient"/> specified data proxy server.
        /// </summary>
        /// <param name="host">Proxy Host.</param>
        /// <param name="port">Proxy Port.</param>
        /// <param name="username">Username for authentication on the proxy server.</param>
        public Socks4ProxyClient(string host, int port, string username)
            : base(ProxyType.Socks4, host, port, username, null) { }

        #endregion


        #region Static methods (closed)

        /// <summary>
        /// Converts a string to an instance <see cref="Socks4ProxyClient"/>.
        /// </summary>
        /// <param name="proxyAddress">String type - host:port:username:password.   The last three are optional.</param>
        /// <returns>An instance <see cref="Socks4ProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="proxyAddress"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="proxyAddress"/> is an empty string.</exception>
        /// <exception cref="System.FormatException">port format is wrong.</exception>
        public static Socks4ProxyClient Parse(string proxyAddress)
        {
            return ProxyClient.Parse(ProxyType.Socks4, proxyAddress) as Socks4ProxyClient;
        }

        /// <summary>
        /// Converts a string to an instance <see cref="Socks4ProxyClient"/>. Gets a value indicating whether the conversion was successful.
        /// </summary>
        /// <param name="proxyAddress">String type - host:port:username:password.   The last three are optional.</param>
        /// <param name="result">If the conversion is successful, it contains an instance <see cref="Socks4ProxyClient"/>, otherwise <see langword="null"/>.</param>
        /// <returns>Value<see langword="true"/>, if the parameter <paramref name="proxyAddress"/> converted successfully, otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string proxyAddress, out Socks4ProxyClient result)
        {
            ProxyClient proxy;

            if (ProxyClient.TryParse(ProxyType.Socks4, proxyAddress, out proxy))
            {
                result = proxy as Socks4ProxyClient;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        #endregion


        /// <summary>
        /// It creates a connection to the server via a proxy server.
        /// </summary>
        /// <param name="destinationHost">Host server with which to connect through a proxy server.</param>
        /// <param name="destinationPort">Server port to which you want to communicate through a proxy server.</param>
        /// <param name="tcpClient">The connection through which to work, or value <see langword="null"/>.</param>
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
        /// <exception cref="System.ArgumentException">parameter <paramref name="destinationHost"/> is an empty string.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">parameter <paramref name="destinationPort"/> less than 1 or greater than 65535.</exception>
        /// <exception cref="Shadynet.ProxyException">Failed to work with a proxy server.</exception>
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

            try
            {
                SendCommand(curTcpClient.GetStream(), CommandConnect, destinationHost, destinationPort);
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

            return curTcpClient;
        }


        #region Methods (internal protected)

        internal protected virtual void SendCommand(NetworkStream nStream, byte command, string destinationHost, int destinationPort)
        {
            byte[] dstPort = GetIPAddressBytes(destinationHost);
            byte[] dstIp = GetPortBytes(destinationPort);

            byte[] userId = string.IsNullOrEmpty(_username) ?
                new byte[0] : Encoding.ASCII.GetBytes(_username);

            // +----+----+----+----+----+----+----+----+----+----+....+----+
            // | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL|
            // +----+----+----+----+----+----+----+----+----+----+....+----+
            //    1    1      2              4           variable       1
            byte[] request = new byte[9 + userId.Length];

            request[0] = VersionNumber;
            request[1] = command;
            dstIp.CopyTo(request, 2);
            dstPort.CopyTo(request, 4);
            userId.CopyTo(request, 8);
            request[8 + userId.Length] = 0x00;

            nStream.Write(request, 0, request.Length);

            // +----+----+----+----+----+----+----+----+
            // | VN | CD | DSTPORT |      DSTIP        |
            // +----+----+----+----+----+----+----+----+
            //   1    1       2              4
            byte[] response = new byte[8];

            nStream.Read(response, 0, response.Length);

            byte reply = response[1];
            
            if (reply != CommandReplyRequestGranted)
            {
                HandleCommandError(reply);
            }
        }

        internal protected byte[] GetIPAddressBytes(string destinationHost)
        {
            IPAddress ipAddr = null;

            if (!IPAddress.TryParse(destinationHost, out ipAddr))
            {
                try
                {
                    IPAddress[] ips = Dns.GetHostAddresses(destinationHost);

                    if (ips.Length > 0)
                    {
                        ipAddr = ips[0];
                    }
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is ArgumentException)
                    {
                        throw new ProxyException(string.Format(
                            Resources.ProxyException_FailedGetHostAddresses, destinationHost), this, ex);
                    }

                    throw;
                }
            }

            return ipAddr.GetAddressBytes();
        }

        internal protected byte[] GetPortBytes(int port)
        {
            byte[] array = new byte[2];

            array[0] = (byte)(port / 256);
            array[1] = (byte)(port % 256);

            return array;
        }

        internal protected void HandleCommandError(byte command)
        {
            string errorMessage;

            switch (command)
            {
                case CommandReplyRequestRejectedOrFailed:
                    errorMessage = Resources.Socks4_CommandReplyRequestRejectedOrFailed;
                    break;

                case CommandReplyRequestRejectedCannotConnectToIdentd:
                    errorMessage = Resources.Socks4_CommandReplyRequestRejectedCannotConnectToIdentd;
                    break;

                case CommandReplyRequestRejectedDifferentIdentd:
                    errorMessage = Resources.Socks4_CommandReplyRequestRejectedDifferentIdentd;
                    break;

                default:
                    errorMessage = Resources.Socks_UnknownError;
                    break;
            }

            string exceptionMsg = string.Format(
                Resources.ProxyException_CommandError, errorMessage, ToString());

            throw new ProxyException(exceptionMsg, this);
        }

        #endregion
    }
}
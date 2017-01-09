using System.Net.Sockets;
using System.Text;

namespace Shadynet.Proxy
{
    /// <summary>
    /// Represents the client proxy Socks4a.
    /// </summary>
    public class Socks4aProxyClient : Socks4ProxyClient 
    {
        #region Constructors (open)

        /// <summary>
        /// Initializes a new instance of the class <see cref="Socks4aProxyClient"/>.
        /// </summary>
        public Socks4aProxyClient()
            : this(null) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="Socks4aProxyClient"/> specify proxy server host, and sets the port equal - 1080.
        /// </summary>
        /// <param name="host">Proxy Host.</param>
        public Socks4aProxyClient(string host)
            : this(host, DefaultPort) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="Socks4aProxyClient"/> specified data proxy server.
        /// </summary>
        /// <param name="host">Proxy Host.</param>
        /// <param name="port">Proxy Port.</param>
        public Socks4aProxyClient(string host, int port)
            : this(host, port, string.Empty) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="Socks4aProxyClient"/> specified data proxy server.
        /// </summary>
        /// <param name="host">Proxy Host.</param>
        /// <param name="port">Proxy Port.</param>
        /// <param name="username">Username for authentication on the proxy server.</param>
        public Socks4aProxyClient(string host, int port, string username)
            : base(host, port, username)
        {
            _type = ProxyType.Socks4a;
        }

        #endregion


        #region Methods (open)

        /// <summary>
        /// Converts a string to an instance <see cref="Socks4aProxyClient"/>.
        /// </summary>
        /// <param name="proxyAddress">String type - host: port: username: password.   The last three are optional.</param>
        /// <returns>An instance <see cref="Socks4aProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="proxyAddress"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="proxyAddress"/> is an empty string.</exception>
        /// <exception cref="System.FormatException">port format is wrong.</exception>
        public static Socks4aProxyClient Parse(string proxyAddress)
        {
            return ProxyClient.Parse(ProxyType.Socks4a, proxyAddress) as Socks4aProxyClient;
        }

        /// <summary>
        /// Converts a string to an instance <see cref="Socks4aProxyClient"/>. Gets a value indicating whether the conversion was successfully.
        /// </summary>
        /// <param name="proxyAddress">String type - host:port:username:password.   The last three are optional.</param>
        /// <param name="result">If the conversion is successful, it contains an instance <see cref="Socks4aProxyClient"/>, otherwise <see langword="null"/>.</param>
        /// <returns>Value <see langword="true"/>, if the parameter <paramref name="proxyAddress"/> It converted successfully, otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string proxyAddress, out Socks4aProxyClient result)
        {
            ProxyClient proxy;

            if (ProxyClient.TryParse(ProxyType.Socks4a, proxyAddress, out proxy))
            {
                result = proxy as Socks4aProxyClient;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        #endregion


        internal protected override void SendCommand(NetworkStream nStream, byte command, string destinationHost, int destinationPort)
        {
            byte[] dstPort = GetPortBytes(destinationPort);
            byte[] dstIp = { 0, 0, 0, 1 };

            byte[] userId = string.IsNullOrEmpty(_username) ?
                new byte[0] : Encoding.ASCII.GetBytes(_username);

            byte[] dstAddr = ASCIIEncoding.ASCII.GetBytes(destinationHost);

            // +----+----+----+----+----+----+----+----+----+----+....+----+----+----+....+----+
            // | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL| DSTADDR      |NULL|
            // +----+----+----+----+----+----+----+----+----+----+....+----+----+----+....+----+
            //    1    1      2              4           variable       1    variable        1 
            byte[] request = new byte[10 + userId.Length + dstAddr.Length];

            request[0] = VersionNumber;
            request[1] = command;
            dstPort.CopyTo(request, 2);
            dstIp.CopyTo(request, 4);
            userId.CopyTo(request, 8);
            request[8 + userId.Length] = 0x00;
            dstAddr.CopyTo(request, 9 + userId.Length);
            request[9 + userId.Length + dstAddr.Length] = 0x00;

            nStream.Write(request, 0, request.Length);

            // +----+----+----+----+----+----+----+----+
            // | VN | CD | DSTPORT |      DSTIP        |
            // +----+----+----+----+----+----+----+----+
            //    1    1      2              4
            byte[] response = new byte[8];

            nStream.Read(response, 0, 8);

            byte reply = response[1];

            // If the request is not made.
            if (reply != CommandReplyRequestGranted)
            {
                HandleCommandError(reply);
            }
        }
    }
}
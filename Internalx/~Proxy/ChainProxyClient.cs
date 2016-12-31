using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Shadynet
{
    /// <summary>
    /// It is a chain of different proxy servers.
    /// </summary>
    public class ChainProxyClient : ProxyClient
    {
        #region Static fields (closed)

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


        #region Fields (closed)

        private List<ProxyClient> _proxies = new List<ProxyClient>();

        #endregion


        #region Properties (open)

        /// <summary>
        /// Gets or sets a value indicating whether the list should be mixed chain of proxy servers, before you create a new connection.
        /// </summary>
        public bool EnableShuffle { get; set; }

        /// <summary>
        /// Returns a list of proxies chain.
        /// </summary>
        public List<ProxyClient> Proxies
        {
            get
            {
                return _proxies;
            }
        }

        #region overdetermined

        /// <summary>
        /// This feature is not supported.
        /// </summary>
        /// <exception cref="System.NotSupportedException">In any use of this property.</exception>
        override public string Host
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// This feature is not supported.
        /// </summary>
        /// <exception cref="System.NotSupportedException">In any use of this property.</exception>
        override public int Port
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// This feature is not supported.
        /// </summary>
        /// <exception cref="System.NotSupportedException">In any use of this property.</exception>
        override public string Username
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// This feature is not supported.
        /// </summary>
        /// <exception cref="System.NotSupportedException">In any use of this property.</exception>
        override public string Password
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// This feature is not supported.
        /// </summary>
        /// <exception cref="System.NotSupportedException">In any use of this property.</exception>
        override public int ConnectTimeout
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// This feature is not supported.
        /// </summary>
        /// <exception cref="System.NotSupportedException">In any use of this property.</exception>
        override public int ReadWriteTimeout
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #endregion


        /// <summary>
        /// Initializes a new instance of the class <see cref="ChainProxyClient"/>.
        /// </summary>
        /// <param name="enableShuffle">Specifies whether to mix the list of proxies chain, before creating a new connection.</param>
        public ChainProxyClient(bool enableShuffle = false)
            : base(ProxyType.Chain)
        {
            EnableShuffle = enableShuffle;
        }


        #region Methods (open)

        /// <summary>
        /// It creates a connection to the server through a chain of proxy servers.
        /// </summary>
        /// <param name="destinationHost">Host server with which to connect through a proxy server.</param>
        /// <param name="destinationPort">Server port to which you want to communicate through a proxy server.</param>
        /// <param name="tcpClient">The connection through which to work, or value <see langword="null"/>.</param>
        /// <returns>The connection to the server through a chain of proxy servers.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Number of proxy servers is 0.
        /// -or-
        /// property value <see cref="Host"/> equally <see langword="null"/> or is 0 length.
        /// -or-
        /// property value <see cref="Port"/> less than 1 or greater than 65535.
        /// -or-
        /// property value <see cref="Username"/> is longer than 255 characters.
        /// -or-
        /// property value <see cref="Password"/> is longer than 255 characters.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="destinationHost"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="destinationHost"/> is an empty string.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">parameter <paramref name="destinationPort"/> less than 1 or greater than 65535.</exception>
        /// <exception cref="xNet.Net.ProxyException">Failed to work with a proxy server.</exception>
        public override TcpClient CreateConnection(string destinationHost, int destinationPort, TcpClient tcpClient = null)
        {
            #region Checking status

            if (_proxies.Count == 0)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ChainProxyClient_NotProxies);
            }

            #endregion

            List<ProxyClient> proxies;

            if (EnableShuffle)
            {
                proxies = _proxies.ToList();

                // Mix proxy.
                for (int i = 0; i < proxies.Count; i++)
                {
                    int randI = Rand.Next(proxies.Count);

                    ProxyClient proxy = proxies[i];
                    proxies[i] = proxies[randI];
                    proxies[randI] = proxy;
                }
            }
            else
            {
                proxies = _proxies;
            }

            int length = proxies.Count - 1;
            TcpClient curTcpClient = tcpClient;

            for (int i = 0; i < length; i++)
            {
                curTcpClient = proxies[i].CreateConnection(
                    proxies[i + 1].Host, proxies[i + 1].Port, curTcpClient);
            }

            curTcpClient = proxies[length].CreateConnection(
                destinationHost, destinationPort, curTcpClient);

            return curTcpClient;
        }

        /// <summary>
        /// It generates a list of strings of the form - host:port represents the address of the proxy server.
        /// </summary>
        /// <returns>The list of lines of the form - host:port represents the address of the proxy server.</returns>
        public override string ToString()
        {
            var strBuilder = new StringBuilder();

            foreach (var proxy in _proxies)
            {
                strBuilder.AppendLine(proxy.ToString());
            }

            return strBuilder.ToString();
        }

        /// <summary>
        /// It generates a list of strings of the form - host:port:username:password. The last two parameters are added if they are set.
        /// </summary>
        /// <returns>The list of lines of the form - host:port:username:password.</returns>
        public virtual string ToExtendedString()
        {
            var strBuilder = new StringBuilder();

            foreach (var proxy in _proxies)
            {
                strBuilder.AppendLine(proxy.ToExtendedString());
            }

            return strBuilder.ToString();
        }

        #region Adding proxies

        /// <summary>
        /// Adds a new chain of proxy client.
        /// </summary>
        /// <param name="proxy">The added proxy client.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="proxy"/> equally <see langword="null"/>.</exception>
        public void AddProxy(ProxyClient proxy)
        {
            #region Check settings

            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            #endregion

            _proxies.Add(proxy);
        }

        /// <summary>
        /// Adds a new chain of the client HTTP-Proxy.
        /// </summary>
        /// <param name="proxyAddress">String type - host:port:username:password. The last three are optional.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="proxyAddress"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="proxyAddress"/> is an empty string.</exception>
        /// <exception cref="System.FormatException">port format is wrong.</exception>
        public void AddHttpProxy(string proxyAddress)
        {
            _proxies.Add(HttpProxyClient.Parse(proxyAddress));
        }

        /// <summary>
        /// Adds a chain of new customer Socks4-proxy.
        /// </summary>
        /// <param name="proxyAddress">String type - host:port:username:password. The last three are optional.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="proxyAddress"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="proxyAddress"/> is an empty string.</exception>
        /// <exception cref="System.FormatException">port format is wrong.</exception>
        public void AddSocks4Proxy(string proxyAddress)
        {
            _proxies.Add(Socks4ProxyClient.Parse(proxyAddress));
        }

        /// <summary>
        /// Adds a new chain Socks4a-proxy client.
        /// </summary>
        /// <param name="proxyAddress">String type - host:port:username:password. The last three are optional.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="proxyAddress"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="proxyAddress"/> It is an empty string.</exception>
        /// <exception cref="System.FormatException">port format is wrong.</exception>
        public void AddSocks4aProxy(string proxyAddress)
        {
            _proxies.Add(Socks4aProxyClient.Parse(proxyAddress));
        }

        /// <summary>
        /// Adds a chain of new customer-Socks5 proxy.
        /// </summary>
        /// <param name="proxyAddress">String type - host:port:username:password. The last three are optional.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="proxyAddress"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="proxyAddress"/> It is an empty string.</exception>
        /// <exception cref="System.FormatException">port format is wrong.</exception>
        public void AddSocks5Proxy(string proxyAddress)
        {
            _proxies.Add(Socks5ProxyClient.Parse(proxyAddress));
        }

        #endregion

        #endregion
    }
}

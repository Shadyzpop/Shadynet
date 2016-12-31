using System;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;

namespace Shadynet
{
    /// <summary>
    /// It represents the base class implementation to work with a proxy server.
    /// </summary>
    public abstract class ProxyClient : IEquatable<ProxyClient>
    {
        #region Fields (protected)

        /// <summary>Proxy Type.</summary>
        protected ProxyType _type;

        /// <summary>Proxy Host.</summary>
        protected string _host;
        /// <summary>Proxy Port.</summary>
        protected int _port = 1;
        /// <summary>Username for authentication on the proxy server.</summary>
        protected string _username;
        /// <summary>Password for authentication on the proxy server.</summary>
        protected string _password;

        /// <summary>Waiting time in milliseconds when connecting to the proxy server.</summary>
        protected int _connectTimeout = 60000;
        /// <summary>Waiting time in milliseconds when writing to the stream or reading from it.</summary>
        protected int _readWriteTimeout = 60000;

        #endregion


        #region Properties (open)

        /// <summary>
        /// Returns the type of proxy server.
        /// </summary>
        public virtual ProxyType Type
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// Gets or sets the host proxy.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        /// <exception cref="System.ArgumentNullException">The value is equal to <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">The parameter value is an empty string.</exception>
        public virtual string Host
        {
            get
            {
                return _host;
            }
            set
            {
                #region Check parameter

                if (value == null)
                {
                    throw new ArgumentNullException("Host");
                }

                if (value.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("Host");
                }

                #endregion

                _host = value;
            }
        }

        /// <summary>
        /// Gets or sets the proxy server port.
        /// </summary>
        /// <value>default value — 1.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">The parameter value is less than 1 or greater than 65535.</exception>
        public virtual int Port
        {
            get
            {
                return _port;
            }
            set
            {
                #region Check parameter

                if (!ExceptionHelper.ValidateTcpPort(value))
                {
                    throw ExceptionHelper.WrongTcpPort("Port");
                }

                #endregion

                _port = value;
            }
        }

        /// <summary>
        /// Gets or sets the user name for authentication on the proxy server.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">The value is longer than 255 characters.</exception>
        public virtual string Username
        {
            get
            {
                return _username;
            }
            set
            {
                #region Check parameter

                if (value != null && value.Length > 255)
                {
                    throw new ArgumentOutOfRangeException("Username", string.Format(
                        Resources.ArgumentOutOfRangeException_StringLengthCanNotBeMore, 255));
                }

                #endregion

                _username = value;
            }
        }

        /// <summary>
        /// Gets or sets the password for authentication on the proxy server.
        /// </summary>
        /// <value>default value — <see langword="null"/>.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">The value is longer than 255 characters.</exception>
        public virtual string Password
        {
            get
            {
                return _password;
            }
            set
            {
                #region Check parameter

                if (value != null && value.Length > 255)
                {
                    throw new ArgumentOutOfRangeException("Password", string.Format(
                        Resources.ArgumentOutOfRangeException_StringLengthCanNotBeMore, 255));
                }

                #endregion

                _password = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait time in milliseconds when connecting to the proxy server.
        /// </summary>
        /// <value>Default value - 60,000, which is equal to one minute.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">The parameter value is less than 0.</exception>
        public virtual int ConnectTimeout
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
        public virtual int ReadWriteTimeout
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

        #endregion


        #region Constructors (protected)

        /// <summary>
        /// Initializes a new instance of the class <see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">Proxy Type.</param>
        internal protected ProxyClient(ProxyType proxyType)
        {
            _type = proxyType;
        }

        /// <summary>
        /// Initializes a new instance of the class <see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">Proxy Type.</param>
        /// <param name="address">Proxy Host.</param>
        /// <param name="port">Proxy Port.</param>
        internal protected ProxyClient(ProxyType proxyType, string address, int port)
        {
            _type = proxyType;
            _host = address;
            _port = port;
        }

        /// <summary>
        /// Initializes a new instance of the class <see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">Proxy Type.</param>
        /// <param name="address">Proxy Host.</param>
        /// <param name="port">Proxy Port.</param>
        /// <param name="username">Username for authentication on the proxy server.</param>
        /// <param name="password">Password for authentication on the proxy server.</param>
        internal protected ProxyClient(ProxyType proxyType, string address, int port, string username, string password)
        {
            _type = proxyType;
            _host = address;
            _port = port;
            _username = username;
            _password = password;
        }

        #endregion


        #region Static methods (open)

        /// <summary>
        /// Converts a string to a class client proxy instance inherited from <see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">Proxy Type.</param>
        /// <param name="proxyAddress">String type - host:port:username:password.   The last three are optional.</param>
        /// <returns>An instance of a client proxy, inherited from <see cref="ProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="proxyAddress"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="proxyAddress"/> It is an empty string.</exception>
        /// <exception cref="System.FormatException">port format is wrong.</exception>
        /// <exception cref="System.InvalidOperationException">Received an unsupported type of proxy server.</exception>
        public static ProxyClient Parse(ProxyType proxyType, string proxyAddress)
        {
            #region Check settings

            if (proxyAddress == null)
            {
                throw new ArgumentNullException("proxyAddress");
            }

            if (proxyAddress.Length == 0)
            {
                throw ExceptionHelper.EmptyString("proxyAddress");
            }

            #endregion

            string[] values = proxyAddress.Split(':');

            int port = 0;
            string host = values[0];

            if (values.Length >= 2)
            {
                #region Getting the port

                try
                {
                    port = int.Parse(values[1]);
                }
                catch (Exception ex)
                {
                    if (ex is FormatException || ex is OverflowException)
                    {
                        throw new FormatException(
                            Resources.InvalidOperationException_ProxyClient_WrongPort, ex);
                    }

                    throw;
                }

                if (!ExceptionHelper.ValidateTcpPort(port))
                {
                    throw new FormatException(
                        Resources.InvalidOperationException_ProxyClient_WrongPort);
                }

                #endregion
            }

            string username = null;
            string password = null;

            if (values.Length >= 3)
            {
                username = values[2];
            }

            if (values.Length >= 4)
            {
                password = values[3];
            }

            return ProxyHelper.CreateProxyClient(proxyType, host, port, username, password);
        }

        /// <summary>
        /// Converts a string to a class client proxy instance inherited from <see cref="ProxyClient"/>. Gets a value indicating whether the conversion was successfully.
        /// </summary>
        /// <param name="proxyType">Proxy Type.</param>
        /// <param name="proxyAddress">String type - host:port:username:password.   The last three are optional.</param>
        /// <param name="result">If the conversion is successful, it contains an instance of the proxy client, inherited from <see cref="ProxyClient"/>, otherwise <see langword="null"/>.</param>
        /// <returns>Value <see langword="true"/>, if the parameter <paramref name="proxyAddress"/> converted successfully, otherwise <see langword="false"/>.</returns>
        public static bool TryParse(ProxyType proxyType, string proxyAddress, out ProxyClient result)
        {
            result = null;

            #region Check settings

            if (string.IsNullOrEmpty(proxyAddress))
            {
                return false;
            }

            #endregion

            string[] values = proxyAddress.Split(':');

            int port = 0;
            string host = values[0];

            if (values.Length >= 2)
            {
                if (!int.TryParse(values[1], out port) || !ExceptionHelper.ValidateTcpPort(port))
                {
                    return false;
                }
            }

            string username = null;
            string password = null;

            if (values.Length >= 3)
            {
                username = values[2];
            }

            if (values.Length >= 4)
            {
                password = values[3];
            }

            try
            {
                result = ProxyHelper.CreateProxyClient(proxyType, host, port, username, password);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            return true;
        }

        #endregion


        /// <summary>
        /// It creates a connection to the server via a proxy server.
        /// </summary>
        /// <param name="destinationHost">destination host with which to connect through a proxy server.</param>
        /// <param name="destinationPort">The port of destination to which you want to communicate through a proxy server.</param>
        /// <param name="tcpClient">The connection through which to work, or value <see langword="null"/>.</param>
        /// <returns>The connection to the proxy server.</returns>
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
        public abstract TcpClient CreateConnection(string destinationHost, int destinationPort, TcpClient tcpClient = null);


        #region Methods (open)

        /// <summary>
        /// Generates a string of the form - host:port represents the address of the proxy server.
        /// </summary>
        /// <returns>type String - host:port represents the address of the proxy server.</returns>
        public override string ToString()
        {
            return string.Format("{0}:{1}", _host, _port);
        }

        /// <summary>
        /// Generates a string of the form - host:port:username:password.   The last two parameters are added if they are set.
        /// </summary>
        /// <returns>String type - host:port:username:password.</returns>
        public virtual string ToExtendedString()
        {
            var strBuilder = new StringBuilder();

            strBuilder.AppendFormat("{0}:{1}", _host, _port);

            if (!string.IsNullOrEmpty(_username))
            {
                strBuilder.AppendFormat(":{0}", _username);

                if (!string.IsNullOrEmpty(_password))
                {
                    strBuilder.AppendFormat(":{0}", _password);
                }
            }

            return strBuilder.ToString();
        }

        /// <summary>
        /// Returns the hash code for this proxy client.
        /// </summary>
        /// <returns>The hash code of a 32-bit signed integer.</returns>
        public override int GetHashCode()
        {
            if (string.IsNullOrEmpty(_host))
            {
                return 0;
            }

            return (_host.GetHashCode() ^ _port);
        }

        /// <summary>
        /// Determines whether two proxy client are equal.
        /// </summary>
        /// <param name="proxy">Proxy client for comparison with the instance.</param>
        /// <returns>Value <see langword="true"/>, if two proxy client are equal, otherwise the value <see langword="false"/>.</returns>
        public bool Equals(ProxyClient proxy)
        {
            if (proxy == null || _host == null)
            {
                return false;
            }

            if (_host.Equals(proxy._host,
                StringComparison.OrdinalIgnoreCase) && _port == proxy._port)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether two proxy client are equal.
        /// </summary>
        /// <param name="obj">Proxy client to compare with this instance.</param>
        /// <returns>Value <see langword="true"/>, if two proxy client are equal, otherwise the value <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            var proxy = obj as ProxyClient;

            if (proxy == null)
            {
                return false;
            }

            return Equals(proxy);
        }

        #endregion


        #region Methods (protected)

        /// <summary>
        /// It creates a connection to a proxy server.
        /// </summary>
        /// <returns>The connection to the proxy server.</returns>
        /// <exception cref="Shadynet.ProxyException">Failed to work with a proxy server.</exception>
        protected TcpClient CreateConnectionToProxy()
        {
            TcpClient tcpClient = null;

            #region Creating a connection

            tcpClient = new TcpClient();
            Exception connectException = null;
            ManualResetEventSlim connectDoneEvent = new ManualResetEventSlim();

            try
            {
                tcpClient.BeginConnect(_host, _port, new AsyncCallback(
                    (ar) =>
                    {
                        if (tcpClient.Client != null)
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
                        }
                    }), tcpClient
                );
            }
            #region Catches

            catch (Exception ex)
            {
                tcpClient.Close();

                if (ex is SocketException || ex is SecurityException)
                {
                    throw NewProxyException(Resources.ProxyException_FailedConnect, ex);
                }

                throw;
            }

            #endregion

            if (!connectDoneEvent.Wait(_connectTimeout))
            {
                tcpClient.Close();
                throw NewProxyException(Resources.ProxyException_ConnectTimeout);
            }

            if (connectException != null)
            {
                tcpClient.Close();

                if (connectException is SocketException)
                {
                    throw NewProxyException(Resources.ProxyException_FailedConnect, connectException);
                }
                else
                {
                    throw connectException;
                }
            }

            if (!tcpClient.Connected)
            {
                tcpClient.Close();
                throw NewProxyException(Resources.ProxyException_FailedConnect);
            }

            #endregion

            tcpClient.SendTimeout = _readWriteTimeout;
            tcpClient.ReceiveTimeout = _readWriteTimeout;

            return tcpClient;
        }

        /// <summary>
        /// Checks various parameters of the proxy client to the wrong value.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">property value <see cref="Host"/> equally <see langword="null"/> or has zero length.</exception>
        /// <exception cref="System.InvalidOperationException">property value <see cref="Port"/> less than 1 or greater than 65535.</exception>
        /// <exception cref="System.InvalidOperationException">property value <see cref="Username"/> is longer than 255 characters.</exception>
        /// <exception cref="System.InvalidOperationException">property value <see cref="Password"/> is longer than 255 characters.</exception>
        protected void CheckState()
        {
            if (string.IsNullOrEmpty(_host))
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongHost);
            }

            if (!ExceptionHelper.ValidateTcpPort(_port))
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongPort);
            }

            if (_username != null && _username.Length > 255)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongUsername);
            }

            if (_password != null && _password.Length > 255)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongPassword);
            }
        }

        /// <summary>
        /// Constructs a proxy exceptions.
        /// </summary>
        /// <param name="message">The error message explaining the reason for the exception.</param>
        /// <param name="innerException">The exception that caused the current exception, or value <see langword="null"/>.</param>
        /// <returns>An exception object proxy.</returns>
        protected ProxyException NewProxyException(
            string message, Exception innerException = null)
        {
            return new ProxyException(string.Format(
                message, ToString()), this, innerException);
        }

        #endregion
    }
}
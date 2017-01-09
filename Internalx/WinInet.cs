using System;
using System.IO;
using System.Security;
using Microsoft.Win32;
using Shadynet.Proxy;

namespace Shadynet
{
    /// <summary>
    /// It represents a class to interact with the network settings of the Windows operating system.
    /// </summary>
    public static class WinInet
    {
        private const string PathToInternetOptions = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";


        #region Static properties (open)

        /// <summary>
        /// Gets a value indicating whether the Internet connection is established.
        /// </summary>
        public static bool InternetConnected
        {
            get
            {
                SafeNativeMethods.InternetConnectionState state = 0;
                return SafeNativeMethods.InternetGetConnectedState(ref state, 0);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the connection to the Internet via a modem installed.
        /// </summary>
        public static bool InternetThroughModem
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_MODEM);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the connection to the Internet via a local area network is installed.
        /// </summary>
        public static bool InternetThroughLan
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_LAN);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the connection to the Internet via a proxy server installed.
        /// </summary>
        public static bool InternetThroughProxy
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_PROXY);
            }
        }

        /// <summary>
        /// Gets a value indicating whether a proxy server in Internet Explorer is used.
        /// </summary>
        public static bool IEProxyEnable
        {
            get
            {
                try
                {
                    return GetIEProxyEnable();
                }
                catch (IOException) { return false; }
                catch (SecurityException) { return false; }
                catch (ObjectDisposedException) { return false; }
                catch (UnauthorizedAccessException) { return false; }
            }
            set
            {
                try
                {
                    SetIEProxyEnable(value);
                }
                catch (IOException) { }
                catch (SecurityException) { }
                catch (ObjectDisposedException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        /// <summary>
        /// Gets or sets the proxy server of Internet Explorer.
        /// </summary>
        /// <value>If the proxy server of Internet Explorer is not set or is wrong, it will be returned <see langword="null"/>. If you set <see langword="null"/>, the proxy server of Internet Explorer will be erased.</value>
        public static HttpProxyClient IEProxy
        {
            get
            {
                string proxy;

                try
                {
                    proxy = GetIEProxy();
                }
                catch (IOException) { return null; }
                catch (SecurityException) { return null; }
                catch (ObjectDisposedException) { return null; }
                catch (UnauthorizedAccessException) { return null; }

                HttpProxyClient ieProxy;
                HttpProxyClient.TryParse(proxy, out ieProxy);

                return ieProxy;
            }
            set
            {
                try
                {
                    if (value != null)
                    {
                        SetIEProxy(value.ToString());
                    }
                    else
                    {
                        SetIEProxy(string.Empty);
                    }
                }
                catch (SecurityException) { }
                catch (ObjectDisposedException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        #endregion


        #region Static methods (open)

        /// <summary>
        /// Gets a value indicating whether a proxy server in Internet Explorer is used.   The value is taken from the register.
        /// </summary>
        /// <returns>A value indicating whether a proxy server in Internet Explorer is used.</returns>
        /// <exception cref="System.Security.SecurityException">The user has no permissions to read the registry key.</exception>
        /// <exception cref="System.ObjectDisposedException">An object <see cref="Microsoft.Win32.RegistryKey"/>, for which this method is called, is closed (access to closed sections is not possible).</exception>
        /// <exception cref="System.UnauthorizedAccessException">The user lacks necessary permissions to the registry.</exception>
        /// <exception cref="System.IO.IOException">Section <see cref="Microsoft.Win32.RegistryKey"/>, contains a predetermined value, it has been marked for deletion.</exception>
        public static bool GetIEProxyEnable()
        {
            using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(PathToInternetOptions))
            {
                object value = regKey.GetValue("ProxyEnable");

                if (value == null)
                {
                    return false;
                }
                else
                {
                    return ((int)value == 0) ? false : true;
                }
            }
        }

        /// <summary>
        /// It sets a value indicating whether a proxy server in Internet Explorer is used.   The value is specified in the registry.
        /// </summary>
        /// <param name="enabled">Specifies whether to use a proxy server in Internet Explorer.</param>
        /// <exception cref="System.Security.SecurityException">The user has no permissions to create or open the registry key.</exception>
        /// <exception cref="System.ObjectDisposedException">An object <see cref="Microsoft.Win32.RegistryKey"/>, for which this method is called, is closed (access to closed sections is not possible).</exception>
        /// <exception cref="System.UnauthorizedAccessException">Writing to object <see cref="Microsoft.Win32.RegistryKey"/> not possible, for example, it can not be opened as a branch, writable, or the user does not have appropriate access rights.</exception>
        public static void SetIEProxyEnable(bool enabled)
        {
            using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(PathToInternetOptions))
            {
                regKey.SetValue("ProxyEnable", (enabled) ? 1 : 0);
            }
        }

        /// <summary>
        /// Returns the value of Internet Explorer's proxy server.   The value is taken from the register.
        /// </summary>
        /// <returns>The value of Internet Explorer's proxy server, or an empty string.</returns>
        /// <exception cref="System.Security.SecurityException">The user has no permissions to read the registry key.</exception>
        /// <exception cref="System.ObjectDisposedException">An object <see cref="Microsoft.Win32.RegistryKey"/>, for which this method is called, is closed (access to closed sections is not possible).</exception>
        /// <exception cref="System.UnauthorizedAccessException">The user lacks necessary permissions to the registry.</exception>
        /// <exception cref="System.IO.IOException">Section <see cref="Microsoft.Win32.RegistryKey"/>, contains a predetermined value, it has been marked for deletion.</exception>
        public static string GetIEProxy()
        {
            using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(PathToInternetOptions))
            {
                return (regKey.GetValue("ProxyServer") as string) ?? string.Empty;
            }
        }

        /// <summary>
        /// It sets the value of the Internet Explorer proxy server.   The value is specified in the registry.
        /// </summary>
        /// <param name="host">Proxy Host.</param>
        /// <param name="port">Proxy Port.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="host"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="host"/> is an empty string.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">parameter <paramref name="port"/> less than 1 or greater than 65535.</exception>
        /// <exception cref="System.Security.SecurityException">The user has no permissions to create or open the registry key.</exception>
        /// <exception cref="System.ObjectDisposedException">An object <see cref="Microsoft.Win32.RegistryKey"/>, for which this method is called, is closed (access to closed sections is not possible).</exception>
        /// <exception cref="System.UnauthorizedAccessException">Writing to object <see cref="Microsoft.Win32.RegistryKey"/> not possible, for example, it can not be opened as a branch, writable, or the user does not have appropriate access rights.</exception>
        public static void SetIEProxy(string host, int port)
        {
            #region Check settings

            if (host == null)
            {
                throw new ArgumentNullException("host");
            }

            if (host.Length == 0)
            {
                throw ExceptionHelper.EmptyString("host");
            }

            if (!ExceptionHelper.ValidateTcpPort(port))
            {
                throw ExceptionHelper.WrongTcpPort("port");
            }

            #endregion

            SetIEProxy(host + ":" + port.ToString());
        }

        /// <summary>
        /// It sets the value of the Internet Explorer proxy server.   The value is specified in the registry.
        /// </summary>
        /// <param name="hostAndPort">Host and proxy port in format - host: port or just a host.</param>
        /// <exception cref="System.Security.SecurityException">The user has no permissions to create or open the registry key.</exception>
        /// <exception cref="System.ObjectDisposedException">An object <see cref="Microsoft.Win32.RegistryKey"/>, for which this method is called, is closed (access to closed sections is not possible).</exception>
        /// <exception cref="System.UnauthorizedAccessException">Writing to object <see cref="Microsoft.Win32.RegistryKey"/> is not possible, for example, it can not be opened as a section writable or not the user has the necessary access rights.</exception>
        public static void SetIEProxy(string hostAndPort)
        {
            using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(PathToInternetOptions))
            {
                regKey.SetValue("ProxyServer", hostAndPort ?? string.Empty);
            }
        }

        #endregion


        private static bool EqualConnectedState(SafeNativeMethods.InternetConnectionState expected)
        {
            SafeNativeMethods.InternetConnectionState state = 0;
            SafeNativeMethods.InternetGetConnectedState(ref state, 0);

            return (state & expected) != 0;
        }
    }
}
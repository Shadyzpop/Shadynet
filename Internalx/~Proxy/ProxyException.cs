using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Shadynet
{
    /// <summary>
    /// The exception that is thrown when an error occurs when using a proxy.
    /// </summary>
    [Serializable]
    public sealed class ProxyException : NetworkEx
    {
        /// <summary>
        /// Returns the proxy client, where the error occurred.
        /// </summary>
        public ProxyClient ProxyClient { get; private set; }


        #region Constructors (open)

        /// <summary>
        /// Initializes a new instance of the class <see cref="ProxyException"/>.
        /// </summary>
        public ProxyException() : this(Resources.ProxyException_Default) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="ProxyException"/> specified error message.
        /// </summary>
        /// <param name="message">The error message explaining the reason for the exception.</param>
        /// <param name="innerException">The exception that caused the current exception, or value <see langword="null"/>.</param>
        public ProxyException(string message, Exception innerException = null)
            : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="xNet.Net.ProxyException"/> specified error message and a proxy client.
        /// </summary>
        /// <param name="message">The error message explaining the reason for the exception.</param>
        /// <param name="proxyClient">Proxy client, in which the error occurred.</param>
        /// <param name="innerException">The exception that caused the current exception, or value <see langword="null"/>.</param>
        public ProxyException(string message, ProxyClient proxyClient, Exception innerException = null)
            : base(message, innerException)
        {
            ProxyClient = proxyClient;
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the class <see cref="ProxyException"/> given copies <see cref="SerializationInfo"/> and <see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">An instance <see cref="SerializationInfo"/>, which contains the information required to serialize the new instance of the class <see cref="ProxyException"/>.</param>
        /// <param name="streamingContext">An instance <see cref="StreamingContext"/>, containing the source of the serialized stream associated with the new instance of the class <see cref="ProxyException"/>.</param>
        protected ProxyException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }
    }
}
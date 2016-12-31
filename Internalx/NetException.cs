using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Shadynet
{
    /// <summary>
    /// The exception that is thrown when an error occurs in the network.
    /// </summary>
    [Serializable]
    public class NetworkEx : Exception
    {
        #region Constructors (open)

        /// <summary>
        /// Initializes a new instance of the class <see cref="NetworkEx"/>.
        /// </summary>
        public NetworkEx() : this(Resources.NetworkEx_Default) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="NetworkEx"/> specified error message.
        /// </summary>
        /// <param name="message">The error message explaining the reason for the exception.</param>
        /// <param name="innerException">The exception that caused the current exception, or value <see langword="null"/>.</param>
        public NetworkEx(string message, Exception innerException = null)
            : base(message, innerException) { }

        #endregion


        /// <summary>
        /// Initializes a new instance of the class <see cref="NetworkEx"/> given copies <see cref="SerializationInfo"/> and <see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">An instance <see cref="SerializationInfo"/>, which contains the information required to serialize the new instance of the class <see cref="NetworkEx"/>.</param>
        /// <param name="streamingContext">An instance <see cref="StreamingContext"/>, containing the source of the serialized stream associated with the new instance of the class <see cref="NetworkEx"/>.</param>
        protected NetworkEx(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }
    }
}
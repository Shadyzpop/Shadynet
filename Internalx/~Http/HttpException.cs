using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Shadynet.Http
{
    /// <summary>
    /// The exception that is thrown when an error occurs when using HTTP-protocol.
    /// </summary>
    [Serializable]
    public sealed class HttpException : NetworkEx
    {
        #region Properties (open)

        /// <summary>
        /// Returns the state of exception.
        /// </summary>
        public HttpExceptionStatus Status { get; internal set; }

        /// <summary>
        /// Returns the response status code of the HTTP-server.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; private set; }

        #endregion


        internal bool EmptyMessageBody { get; set; }


        #region Constructors (open)

        /// <summary>
        /// Initializes a new instance of the class <see cref="HttpException"/>.
        /// </summary>
        public HttpException() : this(Resources.HttpException_Default) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="HttpException"/> specified error message.
        /// </summary>
        /// <param name="message">СThe message about the error with an explanation of the reasons for exclusion.</param>
        /// <param name="innerException">The exception that caused the current exception, or value<see langword="null"/>.</param>
        public HttpException(string message, Exception innerException = null)
            : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="HttpException"/> specified error message and the response status code.
        /// </summary>
        /// <param name="message">СThe message about the error with an explanation of the reasons for exclusion.</param>
        /// <param name="statusCode">The status code for a response from the HTTP-server.</param>
        /// <param name="innerException">The exception that caused the current exception, or value<see langword="null"/>.</param>
        public HttpException(string message, HttpExceptionStatus status,
            HttpStatusCode httpStatusCode = HttpStatusCode.None, Exception innerException = null)
            : base(message, innerException)
        {
            Status = status;
            HttpStatusCode = httpStatusCode;
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the class <see cref="HttpException"/> given copies <see cref="SerializationInfo"/> and <see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">An instance <see cref="SerializationInfo"/>, which contains the information required to serialize the new instance of the class <see cref="HttpException"/>.</param>
        /// <param name="streamingContext">An instance <see cref="StreamingContext"/>, containing the source of the serialized stream associated with the new instance of the class <see cref="HttpException"/>.</param>
        protected HttpException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
            if (serializationInfo != null)
            {
                Status = (HttpExceptionStatus)serializationInfo.GetInt32("Status");
                HttpStatusCode = (HttpStatusCode)serializationInfo.GetInt32("HttpStatusCode");
            }
        }


        /// <summary>
        /// Fills instance <see cref="SerializationInfo"/> the data needed to serialize the exception <see cref="HttpException"/>.
        /// </summary>
        /// <param name="serializationInfo">Data serialization, <see cref="SerializationInfo"/>, to be used.</param>
        /// <param name="streamingContext">Data serialization, <see cref="StreamingContext"/>, to be used.</param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);

            if (serializationInfo != null)
            {
                serializationInfo.AddValue("Status", (int)Status);
                serializationInfo.AddValue("HttpStatusCode", (int)HttpStatusCode);
            }
        }
    }
}
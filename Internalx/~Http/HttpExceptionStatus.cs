
namespace Shadynet
{
    /// <summary>
    /// It defines the state of the class <see cref="HttpException"/>.
    /// </summary>
    public enum HttpExceptionStatus
    {
        /// <summary>
        /// There was another error.
        /// </summary>
        Other,
        /// <summary>
        /// The answer received from the server was complete but indicated an error on the protocol level. For example, the server returns a 404 error or Not Found ("found").
        /// </summary>
        ProtocolError,
        /// <summary>
        /// Could not connect to the HTTP-server.
        /// </summary>
        ConnectFailure,
        /// <summary>
        /// Failed to send a request HTTP-server.
        /// </summary>
        SendFailure,
        /// <summary>
        /// Failed to load the response from the HTTP-server.
        /// </summary>
        ReceiveFailure
    }
}
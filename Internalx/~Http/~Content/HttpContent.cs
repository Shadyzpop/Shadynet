using System.IO;

namespace Shadynet
{
    /// <summary>
    /// It is the body of the request. Available from immediately after sending.
    /// </summary>
    public abstract class HttpContent
    {
        /// <summary>MIME-type content.</summary>
        protected string _contentType = string.Empty;


        /// <summary>
        /// Gets or sets the MIME content-type.
        /// </summary>
        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                _contentType = value ?? string.Empty;
            }
        }


        #region Methods (open)

        /// <summary>
        /// Calculates and returns the request body the length in bytes.
        /// </summary>
        /// <returns>request body length in bytes.</returns>
        public abstract long CalculateContentLength();

        /// <summary>
        /// Writes the body of the request data stream.
        /// </summary>
        /// <param name="stream">The stream where the body request data will be recorded.</param>
        public abstract void WriteTo(Stream stream);

        /// <summary>
        /// Releases all resources used by the current instance of the class <see cref="HttpContent"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion


        /// <summary>
        /// Releases the unmanaged (and if necessary controlled) resources used <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="disposing">Value <see langword="true"/> frees both managed and unmanaged resources;   value<see langword="false"/> It allows the release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing) { }
    }
}
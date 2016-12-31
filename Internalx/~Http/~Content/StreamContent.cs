using System;
using System.IO;

namespace Shadynet
{
    /// <summary>
    /// It represents a body of the request stream.
    /// </summary>
    public class StreamContent : HttpContent
    {
        #region Fields (protected by electromagnetic radiation)

        /// <summary>The contents of the request body.</summary>
        protected Stream _content;
        /// <summary>The buffer size in bytes for the flow.</summary>
        protected int _bufferSize;
        /// <summary>Byte position from which to start reading from the data stream.</summary>
        protected long _initialStreamPosition;

        #endregion


        /// <summary>
        /// Initializes a new instance of the class <see cref="StreamContent"/>.
        /// </summary>
        /// <param name="content">The contents of the request body.</param>
        /// <param name="bufferSize">The buffer size in bytes for the flow.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="content"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Flow <paramref name="content"/> It does not support reading or move positions.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"> parameter <paramref name="bufferSize"/> equals 1.</exception>
        /// <remarks>The default content type - 'application/octet-stream'.</remarks>
        public StreamContent(Stream content, int bufferSize = 32768)
        {
            #region Check settings

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (!content.CanRead || !content.CanSeek)
            {
                throw new ArgumentException(Resources.ArgumentException_CanNotReadOrSeek, "content");
            }

            if (bufferSize < 1)
            {
                throw ExceptionHelper.CanNotBeLess("bufferSize", 1);
            }

            #endregion

            _content = content;
            _bufferSize = bufferSize;
            _initialStreamPosition = _content.Position;

            _contentType = "application/octet-stream";
        }


        /// <summary>
        /// Initializes a new instance of the class <see cref="StreamContent"/>.
        /// </summary>
        protected StreamContent() { }


        #region Methods (open)

        /// <summary>
        /// Calculates and returns the request body the length in bytes.
        /// </summary>
        /// <returns>content length in bytes.</returns>
        /// <exception cref="System.ObjectDisposedException">The current instance has already been deleted.</exception>
        public override long CalculateContentLength()
        {
            ThrowIfDisposed();

            return _content.Length;
        }

        /// <summary>
        /// Writes the request body data Flow.
        /// </summary>
        /// <param name="stream">Flow, request body which data will be written.</param>
        /// <exception cref="System.ObjectDisposedException">The current instance has already been deleted.</exception>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="stream"/> equally <see langword="null"/>.</exception>
        public override void WriteTo(Stream stream)
        {
            ThrowIfDisposed();

            #region Check settings

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            #endregion

            _content.Position = _initialStreamPosition;

            var buffer = new byte[_bufferSize];

            while (true)
            {
                int bytesRead = _content.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    break;
                }

                stream.Write(buffer, 0, bytesRead);
            }
        }

        #endregion


        /// <summary>
        /// Releases the unmanaged (and if necessary controlled) resources used <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="disposing">Value <see langword="true"/> frees managed and unmanaged resources; Value <see langword="false"/> It allows the release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _content != null)
            {
                _content.Dispose();
                _content = null;
            }
        }


        private void ThrowIfDisposed()
        {
            if (_content == null)
            {
                throw new ObjectDisposedException("StreamContent");
            }
        }
    }
}
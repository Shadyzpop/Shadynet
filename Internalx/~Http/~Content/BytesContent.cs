using System;
using System.IO;

namespace Shadynet.Http
{
    /// <summary>
    /// It represents the body of the request in the form of bytes.
    /// </summary>
    public class BytesContent : HttpContent
    {
        #region Fields (protected)

        /// <summary>The contents of the request body.</summary>
        protected byte[] _content;
        /// <summary>The byte offset at the request body content.</summary>
        protected int _offset;
        /// <summary>The number of bytes sent to the content.</summary>
        protected int _count;

        #endregion


        #region Constructors (open)

        /// <summary>
        /// Initializes a new instance of the class <see cref="BytesContent"/>.
        /// </summary>
        /// <param name="content">The contents of the request body.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="content"/> equally <see langword="null"/>.</exception>
        /// <remarks>The default content type - 'application/octet-stream'.</remarks>
        public BytesContent(byte[] content)
            : this(content, 0, content.Length) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="BytesContent"/>.
        /// </summary>
        /// <param name="content">The contents of the request body.</param>
        /// <param name="offset">Offset in bytes for content.</param>
        /// <param name="count">The number of bytes sent from the content.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="content"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// parameter <paramref name="offset"/> is less than 0.
        /// -or-
        /// parameter <paramref name="offset"/> more content length.
        /// -or-
        /// parameter <paramref name="count"/> is less than 0.
        /// -or-
        /// parameter <paramref name="count"/> more (content length - offset).</exception>
        /// <remarks>The default content type - 'application/octet-stream'.</remarks>
        public BytesContent(byte[] content, int offset, int count)
        {
            #region Check settings

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (offset < 0)
            {
                throw ExceptionHelper.CanNotBeLess("offset", 0);
            }

            if (offset > content.Length)
            {
                throw ExceptionHelper.CanNotBeGreater("offset", content.Length);
            }

            if (count < 0)
            {
                throw ExceptionHelper.CanNotBeLess("count", 0);
            }

            if (count > (content.Length - offset))
            {
                throw ExceptionHelper.CanNotBeGreater("count", content.Length - offset);
            }

            #endregion

            _content = content;
            _offset = offset;
            _count = count;

            _contentType = "application/octet-stream";
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the class <see cref="BytesContent"/>.
        /// </summary>
        protected BytesContent() { }


        #region Methods (open)

        /// <summary>
        /// Calculates and returns the request body the length in bytes.
        /// </summary>
        /// <returns>Request body length in bytes.</returns>
        public override long CalculateContentLength()
        {
            return _content.LongLength;
        }

        /// <summary>
        /// Writes the body of the request data stream.
        /// </summary>
        /// <param name="stream">Flow request body which data will be written.</param>
        public override void WriteTo(Stream stream)
        {
            stream.Write(_content, _offset, _count);
        }

        #endregion
    }
}
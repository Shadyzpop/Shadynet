using System;
using System.Text;

namespace Shadynet
{
    /// <summary>
    /// It represents a body of the request line.
    /// </summary>
    public class StringContent : BytesContent
    {
        #region Constructors (open)

        /// <summary>
        /// Initializes a new instance of the class <see cref="StringContent"/>.
        /// </summary>
        /// <param name="content">content content.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="content"/> equally <see langword="null"/>.</exception>
        /// <remarks>The default content type - 'text/plain'.</remarks>
        public StringContent(string content)
            : this(content, Encoding.UTF8) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="StringContent"/>.
        /// </summary>
        /// <param name="content">content content.</param>
        /// <param name="encoding">Encoding used to convert the data into a sequence of bytes.</param>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="content"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="encoding"/> equally <see langword="null"/>.
        /// </exception>
        /// <remarks>The default content type - 'text/plain'.</remarks>
        public StringContent(string content, Encoding encoding)
        {
            #region Check settings

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            #endregion

            _content = encoding.GetBytes(content);
            _offset = 0;
            _count = _content.Length;

            _contentType = "text/plain";
        }

        #endregion
    }
}
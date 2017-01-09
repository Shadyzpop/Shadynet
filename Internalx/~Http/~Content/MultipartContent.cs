using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Shadynet.Http
{
    /// <summary>
    /// It represents the body of the request in the form of composite contents.
    /// </summary>
    public class MultipartContent : HttpContent, IEnumerable<HttpContent>
    {
        private sealed class Element
        {
            #region Fields (open)

            public string Name;
            public string FileName;

            public HttpContent Content;

            #endregion


            public bool IsFieldFile()
            {
                return FileName != null;
            }
        }


        #region Constants (closed)

        private const int FieldTemplateSize = 43;
        private const int FieldFileTemplateSize = 72;
        private const string FieldTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n";
        private const string FieldFileTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

        #endregion


        #region Static fields (closed)

        [ThreadStatic] private static Random _rand;
        private static Random Rand
        {
            get
            {
                if (_rand == null)
                    _rand = new Random();
                return _rand;
            }
        }

        #endregion


        #region Fields (closed)

        private string _boundary;
        private List<Element> _elements = new List<Element>();

        #endregion


        #region Кonstruktory (open)

        /// <summary>
        /// Initializes a new instance of the class <see cref="MultipartContent"/>.
        /// </summary>
        public MultipartContent()
            : this("----------------" + GetRandomString(16)) { }

        /// <summary>
        /// Initializes a new instance of the class <see cref="MultipartContent"/>.
        /// </summary>
        /// <param name="boundary">The boundary separating the components of the content.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="boundary"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="boundary"/> It is an empty string.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">parameter <paramref name="boundary"/> It is longer than 70 characters.</exception>
        public MultipartContent(string boundary)
        {
            #region Check settings

            if (boundary == null)
            {
                throw new ArgumentNullException("boundary");
            }

            if (boundary.Length == 0)
            {
                throw ExceptionHelper.EmptyString("boundary");
            }

            if (boundary.Length > 70)
            {
                throw ExceptionHelper.CanNotBeGreater("boundary", 70);
            }

            #endregion

            _boundary = boundary;

            _contentType = string.Format("multipart/form-data; boundary={0}", _boundary);
        }

        #endregion


        #region Methods (open)

        /// <summary>
        /// It adds a new element of the composite body of the request content.
        /// </summary>
        /// <param name="content">The value of the element.</param>
        /// <param name="name">Element name.</param>
        /// <exception cref="System.ObjectDisposedException">The current instance has been removed.</exception>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="content"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="name"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="name"/> It is an empty string.</exception>
        public void Add(HttpContent content, string name)
        {
            #region Check settings

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            #endregion

            var element = new Element()
            {
                Name = name,
                Content = content
            };

            _elements.Add(element);
        }

        /// <summary>
        /// It adds a new element of the composite body of the request content.
        /// </summary>
        /// <param name="content">The value of the element.</param>
        /// <param name="name">Element name.</param>
        /// <param name="fileName">element Filename.</param>
        /// <exception cref="System.ObjectDisposedException">The current instance has been removed.</exception>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="content"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="name"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="fileName"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="name"/> It is an empty string.</exception>
        public void Add(HttpContent content, string name, string fileName)
        {
            #region Check settings

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            #endregion

            content.ContentType = HttpHelper.DetermineMediaType(
                Path.GetExtension(fileName));

            var element = new Element()
            {
                Name = name,
                FileName = fileName,
                Content = content
            };

            _elements.Add(element);
        }

        /// <summary>
        /// It adds a new element of the composite body of the request content.
        /// </summary>
        /// <param name="content">The value of the element.</param>
        /// <param name="name">Element name.</param>
        /// <param name="fileName">element Filename.</param>
        /// <param name="contentType">MIME-тип контента.</param>
        /// <exception cref="System.ObjectDisposedException">The current instance has been removed.</exception>
        /// <exception cref="System.ArgumentNullException">
        /// parameter <paramref name="content"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="name"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="fileName"/> equally <see langword="null"/>.
        /// -or-
        /// parameter <paramref name="contentType"/> equally <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="name"/> It is an empty string.</exception>
        public void Add(HttpContent content, string name, string fileName, string contentType)
        {
            #region Check settings

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            content.ContentType = contentType;

            var element = new Element()
            {
                Name = name,
                FileName = fileName,
                Content = content
            };

            _elements.Add(element);
        }

        /// <summary>
        /// Calculates and returns the request body the length in bytes.
        /// </summary>
        /// <returns>Request body length in bytes.</returns>
        /// <exception cref="System.ObjectDisposedException">The current instance has been removed.</exception>
        public override long CalculateContentLength()
        {
            ThrowIfDisposed();

            long length = 0;

            foreach (var element in _elements)
            {
                length += element.Content.CalculateContentLength();

                if (element.IsFieldFile())
                {
                    length += FieldFileTemplateSize;
                    length += element.Name.Length;
                    length += element.FileName.Length;
                    length += element.Content.ContentType.Length;
                }
                else
                {
                    length += FieldTemplateSize;
                    length += element.Name.Length;
                }

                // 2 (--) + x (boundary) + 2 (\r\n) ...bound... + 2 (\r\n).
                length += _boundary.Length + 6;
            }

            // 2 (--) + x (boundary) + 2 (--) + 2 (\r\n).
            length += _boundary.Length + 6;

            return length;
        }

        /// <summary>
        /// Writes the body of the request data stream.
        /// </summary>
        /// <param name="stream">Flow request body which data will be written.</param>
        /// <exception cref="System.ObjectDisposedException">The current instance has been removed.</exception>
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

            byte[] newLineBytes = Encoding.ASCII.GetBytes("\r\n");
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("--" + _boundary + "\r\n");

            foreach (var element in _elements)
            {
                stream.Write(boundaryBytes, 0, boundaryBytes.Length);

                string field;

                if (element.IsFieldFile())
                {
                    field = string.Format(
                        FieldFileTemplate, element.Name, element.FileName, element.Content.ContentType);
                }
                else
                {
                    field = string.Format(
                        FieldTemplate, element.Name);
                }

                byte[] fieldBytes = Encoding.ASCII.GetBytes(field);
                stream.Write(fieldBytes, 0, fieldBytes.Length);

                element.Content.WriteTo(stream);
                stream.Write(newLineBytes, 0, newLineBytes.Length);
            }

            boundaryBytes = Encoding.ASCII.GetBytes("--" + _boundary + "--\r\n");
            stream.Write(boundaryBytes, 0, boundaryBytes.Length);
        }

        /// <summary>
        /// Returns an enumerator elements of the composite content.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ObjectDisposedException">The current instance has been removed.</exception>
        public IEnumerator<HttpContent> GetEnumerator()
        {
            ThrowIfDisposed();

            return _elements.Select(e => e.Content).GetEnumerator();
        }

        #endregion


        /// <summary>
        /// Releases the unmanaged (and if necessary controlled) resources used <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="disposing">Value <see langword="true"/> frees both managed and unmanaged resources; value <see langword="false"/> It allows the release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _elements != null)
            {
                foreach (var element in _elements)
                {
                    element.Content.Dispose();
                }

                _elements = null;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            ThrowIfDisposed();

            return GetEnumerator();
        }


        #region Methods of (closed)

        public static string GetRandomString(int length)
        {
            var strBuilder = new StringBuilder(length);

            for (int i = 0; i < length; ++i)
            {
                switch (Rand.Next(3))
                {
                    case 0:
                        strBuilder.Append((char)Rand.Next(48, 58));
                        break;

                    case 1:
                        strBuilder.Append((char)Rand.Next(97, 123));
                        break;

                    case 2:
                        strBuilder.Append((char)Rand.Next(65, 91));
                        break;
                }
            }

            return strBuilder.ToString();
        }

        private void ThrowIfDisposed()
        {
            if (_elements == null)
            {
                throw new ObjectDisposedException("MultipartContent");
            }
        }

        #endregion
    }
}
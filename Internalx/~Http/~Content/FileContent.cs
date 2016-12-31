using System;
using System.IO;

namespace Shadynet
{
    /// <summary>
    /// It represents the body of the request as a data stream from a particular file.
    /// </summary>
    public class FileContent : StreamContent
    {
        /// <summary>
        /// Initializes a new instance of the class <see cref="FileContent"/> and opens the file stream.
        /// </summary>
        /// <param name="pathToContent">The path to the file, which will be the contents of the request body.</param>
        /// <param name="bufferSize">The buffer size in bytes for the flow.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="pathToContent"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="pathToContent"/> is an empty string.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"> parameter <paramref name="bufferSize"/> is less than 1.</exception>
        /// <exception cref="System.IO.PathTooLongException">The specified path, file name, or both exceed the maximum possible length of a certain system.   For example, on Windows platforms, the path length should not exceed 248 characters, and file names must not contain more than 260 characters.</exception>
        /// <exception cref="System.IO.FileNotFoundException">parameter <paramref name="pathToContent"/> points to a nonexistent file.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">parameter <paramref name="pathToContent"/> points to invalid path.</exception>
        /// <exception cref="System.IO.IOException">IO error when dealing with file.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have permission.</exception>
        /// <exception cref="System.UnauthorizedAccessException">
        /// file reading operation is not supported on the current platform.
        /// -or-
        /// parameter <paramref name="pathToContent"/> specifies the directory.
        /// -or-
        /// The caller does not have permission.
        /// </exception>
        /// <remarks>The content type is determined automatically based on the file extension.</remarks>
        public FileContent(string pathToContent, int bufferSize = 32768)
        {
            #region Check settings

            if (pathToContent == null)
            {
                throw new ArgumentNullException("pathToContent");
            }

            if (pathToContent.Length == 0)
            {
                throw ExceptionHelper.EmptyString("pathToContent");
            }

            if (bufferSize < 1)
            {
                throw ExceptionHelper.CanNotBeLess("bufferSize", 1);
            }

            #endregion

            _content = new FileStream(pathToContent, FileMode.Open, FileAccess.Read);
            _bufferSize = bufferSize;
            _initialStreamPosition = 0;

            _contentType = Http.DetermineMediaType(
                Path.GetExtension(pathToContent));
        }
    }
}
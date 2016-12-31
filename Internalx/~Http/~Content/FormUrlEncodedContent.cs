using System;
using System.Collections.Generic;
using System.Text;

namespace Shadynet
{
    /// <summary>
    /// It represents the body of the request as a query parameter.
    /// </summary>
    public class FormUrlEncodedContent : BytesContent
    {
        /// <summary>
        /// Initializes a new instance of the class <see cref="FormUrlEncodedContent"/>.
        /// </summary>
        /// <param name="content">The contents of the request body as a query parameter.</param>
        /// <param name="dontEscape">Specifies whether to encode the values ​​of the parameters needed.</param>
        /// <param name="encoding">Encoding used to convert the query parameters.   If the parameter value is <see langword="null"/>, the value will be used <see cref="System.Text.Encoding.UTF8"/>.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="content"/> equally <see langword="null"/>.</exception>
        /// <remarks>The default content type - 'application/x-www-form-urlencoded'.</remarks>
        public FormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> content, bool dontEscape = false, Encoding encoding = null)
        {
            #region Check settings

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            #endregion

            string queryString = Http.ToPostQueryString(content, dontEscape, encoding);

            _content = Encoding.ASCII.GetBytes(queryString);
            _offset = 0;
            _count = _content.Length;

            _contentType = "application/x-www-form-urlencoded";
        }
    }
}
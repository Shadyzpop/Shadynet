using System;
using System.Collections.Generic;

namespace Shadynet
{
    /// <summary>
    /// It represents a collection of strings representing the query parameters.
    /// </summary>
    public class RequestParams : List<KeyValuePair<string,string>>
    {
        /// <summary>
        /// Sets new parameter query.
        /// </summary>
        /// <param name="paramName">The name of the request parameter.</param>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="paramName"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="paramName"/> It is an empty string.</exception>
        public object this[string paramName]
        {
            set
            {
                #region Check parameter

                if (paramName == null)
                {
                    throw new ArgumentNullException("paramName");
                }

                if (paramName.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("paramName");
                }

                #endregion

                string str = (value == null ? string.Empty : value.ToString());

                Add(new KeyValuePair<string, string>(paramName, str));
            }
        }
    }
}
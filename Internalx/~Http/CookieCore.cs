using System;
using System.Collections.Generic;
using System.Text;

namespace Shadynet
{
    /// <summary>
    /// It represents a collection of HTTP-cookies.
    /// </summary>
    public class CookieCore : Dictionary<string, string>
    {
        /// <summary>
        /// Gets or sets a value indicating whether cookies are closed for editing
        /// </summary>
        /// <value>default value — <see langword="false"/>.</value>
        public bool IsLocked { get; set; }


        /// <summary>
        /// Initializes a new instance of the class <see cref="CookieCore"/>.
        /// </summary>
        /// <param name="isLocked">It indicates whether cookies are closed for editing.</param>
        public CookieCore(bool isLocked = false) : base(StringComparer.OrdinalIgnoreCase)
        {
            IsLocked = isLocked;
        }


        /// <summary>
        /// Returns a string consisting of the names and cookie values.
        /// </summary>
        /// <returns>A string consisting of the names and values ​​of the cookies.</returns>
        override public string ToString()
        {
            var strBuilder = new StringBuilder();        

            foreach (var cookie in this)
            {
                strBuilder.AppendFormat("{0}={1}; ", cookie.Key, cookie.Value);
            }

            if (strBuilder.Length > 0)
            {
                strBuilder.Remove(strBuilder.Length - 2, 2);
            }

            return strBuilder.ToString();
        }
    }
}
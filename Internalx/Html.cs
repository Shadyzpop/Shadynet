using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Shadynet.Http
{
    /// <summary>
    /// It is a static class, designed to aid in working with HTML and other text data.
    /// </summary>
    public static class Html
    {
        #region Static fields (closed)

        private static readonly Dictionary<string, string> _htmlMnemonics = new Dictionary<string, string>()
        {
            { "apos", "'" },
            { "quot", "\"" },
            { "amp", "&" },
            { "lt", "<" },
            { "gt", ">" }
        };

        #endregion


        #region Static methods (open)
        /// <summary>
        /// Converts Html to plain text.
        /// </summary>
        /// <param name="Html"></param>
        /// <returns>Plain text of the html</returns>
        public static string HtmlToPlainText(string Html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";
            const string stripFormatting = @"<[^>]*(>|$)";
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = Html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }

        /// <summary>
        /// Gets the content of a class in the html source, for example: HTMLparse("href" the class that holds the data we want, "id" exist within the same element, "Submit" the data of the id, "Button" the element that hold the data , 2 type of the data, true -return the retries to console);
        /// </summary>
        /// <param name="HtmlBody">Response of the current request</param>
        /// <param name="ClassDataReturn">Required Class name</param>
        /// <param name="CoClassName">Subclass that exist inside the attribute</param>
        /// <param name="CoClassData">the subclass data that exist inside the attribute</param>
        /// <param name="Element">the HTML class of the attributes</param>
        /// <param name="ElementType">the type of ending in the Element [0-2] -> { 0 = div/> | 1 = /> | 2= > }</param>
        /// <param name="ReturnRetries">Return int represents the retried amount to get the classdata.</param>
        /// <returns>the data inside the <see langword="ClassDataReturn"/>.</returns>
        public static string HTMLparse(string HtmlBody, string ClassDataReturn, string CoClassName, string CoClassData, string Element, int ElementType = 0, bool ReturnRetries = false)
        {
            #region Settings
            int i = 0;
            bool found = false;
            string data = string.Empty;
            StringBuilder source = new StringBuilder();
            source.Append(HtmlBody);
            #endregion

            #region Check parameter
            if (string.IsNullOrEmpty(HtmlBody))
                throw new ArgumentNullException("HtmlBody");

            if (!source.ToString().Contains(ClassDataReturn))
                throw new ArgumentException("ClassDataReturn");

            if (!source.ToString().Contains(CoClassName))
                throw new ArgumentException("CoClassName");

            if (!source.ToString().Contains(CoClassData))
                throw new ArgumentException("CoClassData");

            if (!source.ToString().Contains(CoClassData))
                throw new ArgumentException("CoClassData");

            if (!source.ToString().Contains(Element))
                throw new ArgumentException("Element");
            #endregion

            while (!found)
            {
                #region CheckElement
                if (!source.ToString().Contains(Element))
                    throw new Exception("Not Found");

                string ElementResponse = string.Empty;
                string ElementLeft = "<" + Element;
                string ElementRight = string.Empty;
                switch (ElementType)
                {
                    case 0:
                        ElementRight = "</" + Element + ">";
                        break;
                    case 1:
                        ElementRight = "/>";
                        break;
                    case 2:
                        ElementRight = ">";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("ElementType");
                        break;
                }

                ElementResponse = Helper.Betweenstring(source.ToString(), ElementLeft, ElementRight);

                if (string.IsNullOrEmpty(ElementResponse))
                    throw new Exception("Not Found");
                #endregion

                #region ClassesCheckandSets
                if (!ElementResponse.Contains(ClassDataReturn) || !ElementResponse.Contains(CoClassName) || !ElementResponse.Contains(CoClassData))
                {
                    int Leftindex = source.ToString().IndexOf(ElementLeft);
                    int Rightindex = source.ToString().IndexOf(ElementRight);
                    if (i != -1)
                    {
                        source.Remove(Leftindex, Rightindex);
                    }
                    goto End;
                }
                string ClassDataLeft = ClassDataReturn + "=\"";
                string CoClassLeft = CoClassName + "=\"";
                string ClassRight = "\"";
                string CoClassResponse = Helper.Betweenstring(ElementResponse, CoClassLeft, ClassRight);

                #endregion

                #region MainCheck

                if (CoClassResponse == CoClassData)
                {
                    data = Helper.Betweenstring(ElementResponse, ClassDataLeft, ClassRight);
                    found = true;
                }
                else
                    throw new Exception("Not Found");

                #endregion
                End:
                i++;
                Thread.Sleep(50);
            }
            if (ReturnRetries)
                Console.WriteLine(i);
            return data;
        }
        
        /// <summary>
        /// Replaces in a string HTML-entities to represent their characters.
        /// </summary>
        /// <param name="str">The line in which the replacement will be made.</param>
        /// <returns>A string replaced with HTML-entities.</returns>
        /// <remarks>Replace only with the following mnemonics: apos, quot, amp, lt and gt. And all kinds of codes.</remarks>
        public static string ReplaceEntities(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            var regex = new Regex(@"(\&(?<text>\w{1,4})\;)|(\&#(?<code>\w{1,4})\;)", RegexOptions.Compiled);

            string result = regex.Replace(str, match =>
            {
                if (match.Groups["text"].Success)
                {
                    string value;

                    if (_htmlMnemonics.TryGetValue(match.Groups["text"].Value, out value))
                    {
                        return value;
                    }
                }
                else if (match.Groups["code"].Success)
                {
                    int code = int.Parse(match.Groups["code"].Value);
                    return ((char)code).ToString();
                }

                return match.Value;
            });

            return result;
        }

        /// <summary>
        /// Replaces in Unicode-line entities to represent their characters.
        /// </summary>
        /// <param name="str">The line in which the replacement will be made.</param>
        /// <returns>A string replaced with Unicode-entities.</returns>
        /// <remarks>Unicode-entities are of the form: \u2320 and \U044F</remarks>
        public static string ReplaceUnicode(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            var regex = new Regex(@"\\u(?<code>[0-9a-f]{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            string result = regex.Replace(str, match =>
            {
                int code = int.Parse(match.Groups["code"].Value, NumberStyles.HexNumber);

                return ((char)code).ToString();
            });

            return result;
        }

        #region Working with strings

        /// <summary>
        /// Retrieves a substring from a string. The substring begins at the end positions of the substring <paramref name="left"/> and the end of the line. The search starts at a predetermined position.
        /// </summary>
        /// <param name="str">The string, which will search for the substring.</param>
        /// <param name="left">СThrok, which is located to the left of the desired substring.</param>
        /// <param name="startIndex">The position from which to start searching for a substring. Starts From 0.</param>
        /// <param name="comparsion">One of the enumeration values ​​that specifies the search rules.</param>
        /// <returns>The matched substring, otherwise an empty string.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="left"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="left"/> is an empty string.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// parameter <paramref name="startIndex"/> less Than 0.
        /// -or-
        /// parameter <paramref name="startIndex"/> equal to or greater than the string length <paramref name="str"/>.
        /// </exception>
        public static string Substring(this string str, string left,
            int startIndex, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            #region Check settings

            if (left == null)
            {
                throw new ArgumentNullException("left");
            }

            if (left.Length == 0)
            {
                throw ExceptionHelper.EmptyString("left");
            }

            if (startIndex < 0)
            {
                throw ExceptionHelper.CanNotBeLess("startIndex", 0);
            }

            if (startIndex >= str.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex",
                    Resources.ArgumentOutOfRangeException_StringHelper_MoreLengthString);
            }

            #endregion

            // We are looking for top of the left position of the substring.
            int leftPosBegin = str.IndexOf(left, startIndex, comparsion);

            if (leftPosBegin == -1)
            {
                return string.Empty;
            }

            // We calculate the position of the left end of the string.
            int leftPosEnd = leftPosBegin + left.Length;

            // We calculate the length of the substring.
            int length = str.Length - leftPosEnd;

            return str.Substring(leftPosEnd, length);
        }

        /// <summary>
        /// Retrieves a substring from a string.   The substring begins at the end positions of the substring <paramref name="left"/> and the end of the line.
        /// </summary>
        /// <param name="str">The string, which will search for the substring.</param>
        /// <param name="left">The line to the left of the desired substring.</param>
        /// <param name="comparsion">One of the enumeration values ​​that specifies the search rules.</param>
        /// <returns>The matched substring, otherwise an empty string.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="left"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="left"/> It is an empty string.</exception>
        public static string Substring(this string str,
            string left, StringComparison comparsion = StringComparison.Ordinal)
        {
            return Substring(str, left, 0, comparsion);
        }

        /// <summary>
        /// Retrieves a substring from a string. The substring is sought between two given lines, starting at the specified position.
        /// </summary>
        /// <param name="str">The string, which will search for the substring.</param>
        /// <param name="left">The line to the left of the desired substring.</param>
        /// <param name="right">The line to the right of the desired substring.</param>
        /// <param name="startIndex">The position from which to start searching for a substring. starts at 0.</param>
        /// <param name="comparsion">One of the enumeration values ​​that specifies the search rules.</param>
        /// <returns>The matched substring, otherwise an empty string.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="left"/> or <paramref name="right"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="left"/> or <paramref name="right"/> is an empty string.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// parameter <paramref name="startIndex"/> less than 0.
        /// -or-
        /// parameter <paramref name="startIndex"/> equal to or greater than the string length <paramref name="str"/>.
        /// </exception>
        public static string Substring(this string str, string left, string right,
            int startIndex, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            #region Check settings

            if (left == null)
            {
                throw new ArgumentNullException("left");
            }

            if (left.Length == 0)
            {
                throw ExceptionHelper.EmptyString("left");
            }

            if (right == null)
            {
                throw new ArgumentNullException("right");
            }

            if (right.Length == 0)
            {
                throw ExceptionHelper.EmptyString("right");
            }

            if (startIndex < 0)
            {
                throw ExceptionHelper.CanNotBeLess("startIndex", 0);
            }

            if (startIndex >= str.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex",
                    Resources.ArgumentOutOfRangeException_StringHelper_MoreLengthString);
            }

            #endregion

            // We are looking for top of the left position of the substring.
            int leftPosBegin = str.IndexOf(left, startIndex, comparsion);

            if (leftPosBegin == -1)
            {
                return string.Empty;
            }

            // We calculate the position of the left end of the string.
            int leftPosEnd = leftPosBegin + left.Length;

            // We are looking for the right start position of the substring.
            int rightPos = str.IndexOf(right, leftPosEnd, comparsion);

            if (rightPos == -1)
            {
                return string.Empty;
            }

            // We calculate the length of the substring.
            int length = rightPos - leftPosEnd;

            return str.Substring(leftPosEnd, length);
        }

        /// <summary>
        /// Retrieves a substring from a string. The substring is sought between two given lines.
        /// </summary>
        /// <param name="str">The string, which will search for the substring.</param>
        /// <param name="left">The line to the left of the desired substring.</param>
        /// <param name="right">The line to the right of the desired substring.</param>
        /// <param name="comparsion">One of the enumeration values ​​that specifies the search rules.</param>
        /// <returns>The matched substring, otherwise an empty string.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="left"/> or <paramref name="right"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="left"/> or <paramref name="right"/> It is an empty string.</exception>
        public static string Substring(this string str, string left, string right,
            StringComparison comparsion = StringComparison.Ordinal)
        {
            return str.Substring(left, right, 0, comparsion);
        }

        /// <summary>
        /// Retrieves the last substring from a string. The substring begins at the end positions of the substring<paramref name="left"/> and the end of the line. The search starts at a predetermined position.
        /// </summary>
        /// <param name="str">The string, which will search for the last substring.</param>
        /// <param name="left">The line to the left of the desired substring.</param>
        /// <param name="startIndex">The position from which to start searching for a substring. Countdown 0.</param>
        /// <param name="comparsion">One of the enumeration values ​​that specifies the search rules.</param>
        /// <returns>The matched substring, otherwise an empty string.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="left"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="left"/> is an empty string.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// parameter <paramref name="startIndex"/> less than 0.
        /// -or-
        /// parameter <paramref name="startIndex"/> equal to or greater than the string length <paramref name="str"/>.
        /// </exception>
        public static string LastSubstring(this string str, string left,
            int startIndex, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            #region Check settings

            if (left == null)
            {
                throw new ArgumentNullException("left");
            }

            if (left.Length == 0)
            {
                throw ExceptionHelper.EmptyString("left");
            }

            if (startIndex < 0)
            {
                throw ExceptionHelper.CanNotBeLess("startIndex", 0);
            }

            if (startIndex >= str.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex",
                    Resources.ArgumentOutOfRangeException_StringHelper_MoreLengthString);
            }

            #endregion

            // We are looking for top of the left position of the substring.
            int leftPosBegin = str.LastIndexOf(left, startIndex, comparsion);

            if (leftPosBegin == -1)
            {
                return string.Empty;
            }

            // We calculate the position of the left end of the string.
            int leftPosEnd = leftPosBegin + left.Length;

            // We calculate the length of the substring.
            int length = str.Length - leftPosEnd;

            return str.Substring(leftPosEnd, length);
        }

        /// <summary>
        /// Retrieves the last substring from a string. The substring begins at the end positions of the substring <paramref name="left"/> and the end of the line.
        /// </summary>
        /// <param name="str">The string, which will search for the last substring.</param>
        /// <param name="left">The line to the left of the desired substring.</param>
        /// <param name="comparsion">One of the enumeration values ​​that specifies the search rules.</param>
        /// <returns>The matched substring, otherwise an empty string.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="left"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="left"/> is an empty string.</exception>
        public static string LastSubstring(this string str,
            string left, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            return LastSubstring(str, left, str.Length - 1, comparsion);
        }

        /// <summary>
        /// Retrieves the last substring from a string. The substring is sought between two given lines, starting at the specified position.
        /// </summary>
        /// <param name="str">The string, which will search for the last substring.</param>
        /// <param name="left">The line to the left of the desired substring.</param>
        /// <param name="right">The line to the right of the desired substring.</param>
        /// <param name="startIndex">The position from which to start searching for a substring. Countdown 0.</param>
        /// <param name="comparsion">One of the enumeration values ​​that specifies the search rules.</param>
        /// <returns>The matched substring, otherwise an empty string.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="left"/> or <paramref name="right"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="left"/> or <paramref name="right"/> It is an empty string.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// parameter <paramref name="startIndex"/> less than 0.
        /// -or-
        /// parameter <paramref name="startIndex"/> equal to or greater than the string length <paramref name="str"/>.
        /// </exception>
        public static string LastSubstring(this string str, string left, string right,
            int startIndex, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            #region Check settings

            if (left == null)
            {
                throw new ArgumentNullException("left");
            }

            if (left.Length == 0)
            {
                throw ExceptionHelper.EmptyString("left");
            }

            if (right == null)
            {
                throw new ArgumentNullException("right");
            }

            if (right.Length == 0)
            {
                throw ExceptionHelper.EmptyString("right");
            }

            if (startIndex < 0)
            {
                throw ExceptionHelper.CanNotBeLess("startIndex", 0);
            }

            if (startIndex >= str.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex",
                    Resources.ArgumentOutOfRangeException_StringHelper_MoreLengthString);
            }

            #endregion

            // We are looking for top of the left position of the substring.
            int leftPosBegin = str.LastIndexOf(left, startIndex, comparsion);

            if (leftPosBegin == -1)
            {
                return string.Empty;
            }

            // We calculate the position of the left end of the string.
            int leftPosEnd = leftPosBegin + left.Length;

            // We are looking for the right start position of the substring.
            int rightPos = str.IndexOf(right, leftPosEnd, comparsion);

            if (rightPos == -1)
            {
                if (leftPosBegin == 0)
                {
                    return string.Empty;
                }
                else
                {
                    return LastSubstring(str, left, right, leftPosBegin - 1, comparsion);
                }
            }

            // We calculate the length of the substring.
            int length = rightPos - leftPosEnd;

            return str.Substring(leftPosEnd, length);
        }

        /// <summary>
        /// Retrieves the last substring from a string.   The substring is sought between two given lines.
        /// </summary>
        /// <param name="str">The string, which will search for the last substring.</param>
        /// <param name="left">The line to the left of the desired substring.</param>
        /// <param name="right">The line to the right of the desired substring.</param>
        /// <param name="comparsion">One of the enumeration values ​​that specifies the search rules.</param>
        /// <returns>The matched substring, otherwise an empty string.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="left"/> or <paramref name="right"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="left"/> or <paramref name="right"/> It is an empty string.</exception>
        public static string LastSubstring(this string str, string left, string right,
            StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            return str.LastSubstring(left, right, str.Length - 1, comparsion);
        }

        /// <summary>
        /// Retrieves a substring from a string. The substring is sought between two given lines, starting at the specified position.
        /// </summary>
        /// <param name="str">The string, which will search for substring.</param>
        /// <param name="left">The line to the left of the desired substring.</param>
        /// <param name="right">The line to the right of the desired substring.</param>
        /// <param name="startIndex">The position, which begins substring search. Countdown 0.</param>
        /// <param name="comparsion">One of the enumeration values ​​that specifies the search rules.</param>
        /// <returns>The matched substring, otherwise an empty string array.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="left"/> or <paramref name="right"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="left"/> or <paramref name="right"/> is an empty string.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// parameter <paramref name="startIndex"/> less than 0.
        /// -or-
        /// parameter <paramref name="startIndex"/> equal to or greater than the string length <paramref name="str"/>.
        /// </exception>
        public static string[] Substrings(this string str, string left, string right,
            int startIndex, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new string[0];
            }

            #region Check settings

            if (left == null)
            {
                throw new ArgumentNullException("left");
            }

            if (left.Length == 0)
            {
                throw ExceptionHelper.EmptyString("left");
            }

            if (right == null)
            {
                throw new ArgumentNullException("right");
            }

            if (right.Length == 0)
            {
                throw ExceptionHelper.EmptyString("right");
            }

            if (startIndex < 0)
            {
                throw ExceptionHelper.CanNotBeLess("startIndex", 0);
            }

            if (startIndex >= str.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex",
                    Resources.ArgumentOutOfRangeException_StringHelper_MoreLengthString);
            }

            #endregion

            int currentStartIndex = startIndex;
            List<string> strings = new List<string>();

            while (true)
            {
                // We are looking for top of the left position of the substring.
                int leftPosBegin = str.IndexOf(left, currentStartIndex, comparsion);

                if (leftPosBegin == -1)
                {
                    break;
                }

                // We calculate the position of the left end of the string.
                int leftPosEnd = leftPosBegin + left.Length;

                // We are looking for the right position of the beginning of the line.
                int rightPos = str.IndexOf(right, leftPosEnd, comparsion);

                if (rightPos == -1)
                {
                    break;
                }

                // We calculate the length of the substring.
                int length = rightPos - leftPosEnd;

                strings.Add(str.Substring(leftPosEnd, length));

                // We calculate the position of the right end of the string.
                currentStartIndex = rightPos + right.Length;
            }

            return strings.ToArray();
        }

        /// <summary>
        /// Retrieves a substring from a string.   The substring is sought between two given lines.
        /// </summary>
        /// <param name="str">The string, which will search for substrings.</param>
        /// <param name="left">The line to the left of the desired substring.</param>
        /// <param name="right">The line to the right of the desired substring.</param>
        /// <param name="comparsion">One of the enumeration values ​​that specifies the search rules.</param>
        /// <returns>The matched substring, otherwise an empty string array.</returns>
        /// <exception cref="System.ArgumentNullException">parameter <paramref name="left"/> or <paramref name="right"/> equally <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">parameter <paramref name="left"/> or <paramref name="right"/> It is an empty string.</exception>
        public static string[] Substrings(this string str, string left, string right,
            StringComparison comparsion = StringComparison.Ordinal)
        {
            return str.Substrings(left, right, 0, comparsion);
        }

        #endregion

        #endregion
    }
}

using System;
using System.Security.Cryptography;
using System.Text;

namespace Shadynet.Security.Cryptography
{
    class Cryptography
    {
        public static class CryptographyHelper
        {
            /// <summary>
            /// Hashes byte data into md5 hash
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            public static string GetMd5Hash(byte[] data)
            {
                if (data == null)
                {
                    throw new ArgumentNullException("data");
                }
                if (data.Length == 0)
                {
                    return string.Empty;
                }
                string result;
                using (HashAlgorithm hashAlgorithm = new MD5CryptoServiceProvider())
                {
                    StringBuilder stringBuilder = new StringBuilder(32);
                    byte[] array = hashAlgorithm.ComputeHash(data);
                    for (int i = 0; i < array.Length; i++)
                    {
                        stringBuilder.Append(array[i].ToString("x2"));
                    }
                    result = stringBuilder.ToString();
                }
                return result;
            }
            /// <summary>
            /// Hashes a string of data into md5 hash.
            /// </summary>
            /// <param name="data"></param>
            /// <param name="encoding"></param>
            /// <returns></returns>
            public static string GetMd5Hash(string data, Encoding encoding = null)
            {
                if (string.IsNullOrEmpty(data))
                {
                    return string.Empty;
                }
                encoding = (encoding ?? Encoding.Default);
                return CryptographyHelper.GetMd5Hash(encoding.GetBytes(data));
            }
        }
    }
}

using System;

namespace Shadynet.Http
{
    /// <summary>
    /// It represents the data for the event, informing about the data upload progress.
    /// </summary>
    public sealed class UploadProgressChangedEventArgs : EventArgs
    {
        #region Properties (open)

        /// <summary>
        /// Returns the number of bytes sent.
        /// </summary>
        public long BytesSent { get; private set; }

        /// <summary>
        /// Returns the total number of bytes sent.
        /// </summary>
        public long TotalBytesToSend { get; private set; }

        /// <summary>
        /// Returns the percentage of bytes sent.
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                return ((double)BytesSent / (double)TotalBytesToSend) * 100.0;
            }
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the class <see cref="UploadProgressChangedEventArgs"/>.
        /// </summary>
        /// <param name="bytesSent">Number of bytes sent.</param>
        /// <param name="totalBytesToSend">The total number of bytes sent.</param>
        public UploadProgressChangedEventArgs(long bytesSent, long totalBytesToSend)
        {
            BytesSent = bytesSent;
            TotalBytesToSend = totalBytesToSend;
        }
    }
}
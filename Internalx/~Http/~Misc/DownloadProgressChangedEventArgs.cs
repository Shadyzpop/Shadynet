using System;

namespace Shadynet
{
    /// <summary>
    /// Represents the data for the event, reports on the progress of data download.
    /// </summary>
    public sealed class DownloadProgressChangedEventArgs : EventArgs
    {
        #region Properties (open)

        /// <summary>
        /// Returns the number of bytes received.
        /// </summary>
        public long BytesReceived { get; private set; }

        /// <summary>
        /// Returns the total number of bytes received.
        /// </summary>
        /// <value>If the total number of bytes received is unknown, then the value -1.</value>
        public long TotalBytesToReceive { get; private set; }

        /// <summary>
        /// Returns the percentage of bytes received.
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                return ((double)BytesReceived / (double)TotalBytesToReceive) * 100.0;
            }
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the class <see cref="DownloadProgressChangedEventArgs"/>.
        /// </summary>
        /// <param name="bytesReceived">Number of bytes received.</param>
        /// <param name="totalBytesToReceive">The total number of bytes received.</param>
        public DownloadProgressChangedEventArgs(long bytesReceived, long totalBytesToReceive)
        {
            BytesReceived = bytesReceived;
            TotalBytesToReceive = totalBytesToReceive;
        }
    }
}
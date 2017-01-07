using System;

namespace Shadynet.Threading
{
    public sealed class MultiThreadingRepeatEventArgs : EventArgs
    {
        public ulong RepeatCount
        {
            get;
            private set;
        }

        public MultiThreadingRepeatEventArgs(ulong repeatCount)
        {
            this.RepeatCount = repeatCount;
        }
    }
}

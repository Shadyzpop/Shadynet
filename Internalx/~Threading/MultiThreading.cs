using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace Shadynet.Threading
{
    public class MultiThreading : IDisposable
    {
        private struct ForParams
        {
            public int Begin;

            public int End;

            public Action<int> Action;
        }

        private struct ForEachParams<T>
        {
            public IEnumerator<T> Source;

            public Action<T> Action;
        }

        private struct ForEachListParams<T>
        {
            public int Begin;

            public int End;

            public IList<T> List;

            public Action<T> Action;
        }

        private bool _disposed;

        private ulong _repeatCount;

        private Barrier _barrierForReps;

        private int _threadCount;

        private int _currentThreadCount;

        private bool _endEnumerator;

        private bool _enableInfiniteRepeat;

        private bool _notImplementedReset;

        private bool _canceling;

        private readonly ReaderWriterLockSlim _lockForCanceling = new ReaderWriterLockSlim();

        private object _lockForEndThread = new object();

        private AsyncOperation _asyncOperation;

        private SendOrPostCallback _callbackEndWork;

        private EventHandler<EventArgs> _beginningWorkHandler;

        private EventHandler<EventArgs> _workCompletedAsyncEvent;

        private EventHandler<MultiThreadingRepeatEventArgs> _repeatCompletedHandler;

        private AsyncEvent<MultiThreadingProgressEventArgs> _progressChangedAsyncEvent;

        private AsyncEvent<EventArgs> _cancelingWorkAsyncEvent;

        public event EventHandler<EventArgs> BeginningWork
        {
            add
            {
                this._beginningWorkHandler = (EventHandler<EventArgs>)Delegate.Combine(this._beginningWorkHandler, value);
            }
            remove
            {
                this._beginningWorkHandler = (EventHandler<EventArgs>)Delegate.Remove(this._beginningWorkHandler, value);
            }
        }

        public event EventHandler<EventArgs> WorkCompleted
        {
            add
            {
                this._workCompletedAsyncEvent = (EventHandler<EventArgs>)Delegate.Combine(this._workCompletedAsyncEvent, value);
            }
            remove
            {
                this._workCompletedAsyncEvent = (EventHandler<EventArgs>)Delegate.Remove(this._workCompletedAsyncEvent, value);
            }
        }

        public event EventHandler<MultiThreadingRepeatEventArgs> RepeatCompleted
        {
            add
            {
                this._repeatCompletedHandler = (EventHandler<MultiThreadingRepeatEventArgs>)Delegate.Combine(this._repeatCompletedHandler, value);
            }
            remove
            {
                this._repeatCompletedHandler = (EventHandler<MultiThreadingRepeatEventArgs>)Delegate.Remove(this._repeatCompletedHandler, value);
            }
        }

        public event EventHandler<MultiThreadingProgressEventArgs> ProgressChanged
        {
            add
            {
                AsyncEvent<MultiThreadingProgressEventArgs> expr_06 = this._progressChangedAsyncEvent;
                expr_06.EventHandler = (EventHandler<MultiThreadingProgressEventArgs>)Delegate.Combine(expr_06.EventHandler, value);
            }
            remove
            {
                AsyncEvent<MultiThreadingProgressEventArgs> expr_06 = this._progressChangedAsyncEvent;
                expr_06.EventHandler = (EventHandler<MultiThreadingProgressEventArgs>)Delegate.Remove(expr_06.EventHandler, value);
            }
        }

        public event EventHandler<EventArgs> CancelingWork
        {
            add
            {
                AsyncEvent<EventArgs> expr_06 = this._cancelingWorkAsyncEvent;
                expr_06.EventHandler = (EventHandler<EventArgs>)Delegate.Combine(expr_06.EventHandler, value);
            }
            remove
            {
                AsyncEvent<EventArgs> expr_06 = this._cancelingWorkAsyncEvent;
                expr_06.EventHandler = (EventHandler<EventArgs>)Delegate.Remove(expr_06.EventHandler, value);
            }
        }

        public bool Working
        {
            get;
            private set;
        }

        public bool Canceling
        {
            get
            {
                this._lockForCanceling.EnterReadLock();
                bool canceling;
                try
                {
                    canceling = this._canceling;
                }
                finally
                {
                    this._lockForCanceling.ExitReadLock();
                }
                return canceling;
            }
        }

        public bool EnableInfiniteRepeat
        {
            get
            {
                return this._enableInfiniteRepeat;
            }
            set
            {
                if (this.Working)
                {
                    throw new InvalidOperationException("Value");
                }
                this._enableInfiniteRepeat = value;
            }
        }

        public int ThreadCount
        {
            get
            {
                return this._threadCount;
            }
            set
            {
                if (this.Working)
                {
                    throw new InvalidOperationException("Value");
                }
                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess<int>("ThreadsCount", 1);
                }
                this._threadCount = value;
            }
        }

        protected AsyncOperation AsyncOperation
        {
            get
            {
                return this._asyncOperation;
            }
        }

        public MultiThreading(int threadCount = 1)
        {
            if (threadCount < 1)
            {
                throw ExceptionHelper.CanNotBeLess<int>("threadCount", 1);
            }
            this._threadCount = threadCount;
            this._callbackEndWork = new SendOrPostCallback(this.EndWorkCallback);
            this._cancelingWorkAsyncEvent = new AsyncEvent<EventArgs>(new Action<EventArgs>(this.OnCancelingWork));
            this._progressChangedAsyncEvent = new AsyncEvent<MultiThreadingProgressEventArgs>(new Action<MultiThreadingProgressEventArgs>(this.OnProgressChanged));
        }

        public virtual void Run(Action action)
        {
            this.ThrowIfDisposed();
            if (this.Working)
            {
                throw new InvalidOperationException("action");
            }
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            this.InitBeforeRun(this._threadCount, true);
            try
            {
                for (int i = 0; i < this._threadCount; i++)
                {
                    this.StartThread(new Action<object>(this.Thread), action);
                }
            }
            catch (Exception)
            {
                this.EndWork();
                throw;
            }
        }

        public virtual void RunFor(int fromInclusive, int toExclusive, Action<int> action)
        {
            this.ThrowIfDisposed();
            if (this.Working)
            {
                throw new InvalidOperationException("RunFor");
            }
            if (fromInclusive < 0)
            {
                throw ExceptionHelper.CanNotBeLess<int>("fromInclusive", 0);
            }
            if (fromInclusive > toExclusive)
            {
                throw new ArgumentOutOfRangeException("fromInclusive", Resources.ArgumentException_MultiThreading_BegIndexRangeMoreEndIndex);
            }
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            int num = toExclusive - fromInclusive;
            if (num == 0)
            {
                return;
            }
            int num2 = this._threadCount;
            if (num2 > num)
            {
                num2 = num;
            }
            this.InitBeforeRun(num2, true);
            int num3 = 0;
            int[] array = this.CalculateThreadsIterations(num, num2);
            try
            {
                for (int i = 0; i < array.Length; i++)
                {
                    MultiThreading.ForParams forParams;
                    forParams.Action = action;
                    forParams.Begin = num3 + fromInclusive;
                    forParams.End = num3 + array[i] + fromInclusive;
                    this.StartThread(new Action<object>(this.ForInThread), forParams);
                    num3 += array[i];
                }
            }
            catch (Exception)
            {
                this.EndWork();
                throw;
            }
        }

        public virtual void RunForEach<T>(IEnumerable<T> source, Action<T> action)
        {
            this.ThrowIfDisposed();
            if (this.Working)
            {
                throw new InvalidOperationException("RunForEach");
            }
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            if (source is IList<T>)
            {
                this.RunForEachList<T>(source, action);
                return;
            }
            this.RunForEachOther<T>(source, action);
        }

        public void ReportProgress(object value = null)
        {
            this.ThrowIfDisposed();
            this._progressChangedAsyncEvent.Post(this._asyncOperation, this, new MultiThreadingProgressEventArgs(value));
        }

        public void ReportProgressSync(object value = null)
        {
            this.ThrowIfDisposed();
            this.OnProgressChanged(new MultiThreadingProgressEventArgs(value));
        }

        public virtual void Cancel()
        {
            this.ThrowIfDisposed();
            this._lockForCanceling.EnterWriteLock();
            try
            {
                if (!this._canceling)
                {
                    this._canceling = true;
                    this._cancelingWorkAsyncEvent.Post(this._asyncOperation, this, EventArgs.Empty);
                }
            }
            finally
            {
                this._lockForCanceling.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !this._disposed)
            {
                this._disposed = true;
                this._lockForCanceling.Dispose();
            }
        }

        protected virtual void OnBeginningWork(EventArgs e)
        {
            EventHandler<EventArgs> beginningWorkHandler = this._beginningWorkHandler;
            if (beginningWorkHandler != null)
            {
                beginningWorkHandler(this, e);
            }
        }

        protected virtual void OnWorkCompleted(EventArgs e)
        {
            EventHandler<EventArgs> workCompletedAsyncEvent = this._workCompletedAsyncEvent;
            if (workCompletedAsyncEvent != null)
            {
                workCompletedAsyncEvent(this, e);
            }
        }

        protected virtual void OnRepeatCompleted(MultiThreadingRepeatEventArgs e)
        {
            EventHandler<MultiThreadingRepeatEventArgs> repeatCompletedHandler = this._repeatCompletedHandler;
            if (repeatCompletedHandler != null)
            {
                repeatCompletedHandler(this, e);
            }
        }

        protected virtual void OnProgressChanged(MultiThreadingProgressEventArgs e)
        {
            this._progressChangedAsyncEvent.On(this, e);
        }

        protected virtual void OnCancelingWork(EventArgs e)
        {
            this._cancelingWorkAsyncEvent.On(this, e);
        }

        private void InitBeforeRun(int threadCount, bool needCreateBarrierForReps = true)
        {
            this._repeatCount = 0uL;
            this._notImplementedReset = false;
            this._currentThreadCount = threadCount;
            if (needCreateBarrierForReps)
            {
                this._barrierForReps = new Barrier(threadCount, delegate (Barrier b)
                {
                    if (!this.Canceling)
                    {
                        this.OnRepeatCompleted(new MultiThreadingRepeatEventArgs(this._repeatCount += 1uL));
                    }
                });
            }
            this._canceling = false;
            this._asyncOperation = AsyncOperationManager.CreateOperation(new object());
            this.Working = true;
            this.OnBeginningWork(EventArgs.Empty);
        }

        private bool EndThread()
        {
            lock (this._lockForEndThread)
            {
                this._currentThreadCount--;
                if (this._currentThreadCount == 0)
                {
                    this._asyncOperation.PostOperationCompleted(this._callbackEndWork, new EventArgs());
                    return true;
                }
            }
            return false;
        }

        private void EndWork()
        {
            this.Working = false;
            if (this._barrierForReps != null)
            {
                this._barrierForReps.Dispose();
                this._barrierForReps = null;
            }
            this._asyncOperation = null;
        }

        private void EndWorkCallback(object param)
        {
            this.EndWork();
            this.OnWorkCompleted(param as EventArgs);
        }

        private int[] CalculateThreadsIterations(int iterationCount, int threadsCount)
        {
            int[] array = new int[threadsCount];
            int num = iterationCount / threadsCount;
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = num;
            }
            int num2 = 0;
            int num3 = iterationCount - threadsCount * num;
            for (int j = 0; j < num3; j++)
            {
                array[num2]++;
                if (++num2 == array.Length)
                {
                    num2 = 0;
                }
            }
            return array;
        }

        private void StartThread(Action<object> body, object param)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(body.Invoke))
            {
                IsBackground = true
            };
            thread.Start(param);
        }

        private void RunForEachList<T>(IEnumerable<T> source, Action<T> action)
        {
            IList<T> list = source as IList<T>;
            int count = list.Count;
            if (count == 0)
            {
                return;
            }
            int num = this._threadCount;
            if (num > count)
            {
                num = count;
            }
            this.InitBeforeRun(num, true);
            int num2 = 0;
            int[] array = this.CalculateThreadsIterations(count, num);
            try
            {
                for (int i = 0; i < array.Length; i++)
                {
                    MultiThreading.ForEachListParams<T> forEachListParams;
                    forEachListParams.Action = action;
                    forEachListParams.List = list;
                    forEachListParams.Begin = num2;
                    forEachListParams.End = num2 + array[i];
                    this.StartThread(new Action<object>(this.ForEachListInThread<T>), forEachListParams);
                    num2 += array[i];
                }
            }
            catch (Exception)
            {
                this.EndWork();
                throw;
            }
        }

        private void RunForEachOther<T>(IEnumerable<T> source, Action<T> action)
        {
            this._endEnumerator = false;
            this.InitBeforeRun(this._threadCount, false);
            MultiThreading.ForEachParams<T> forEachParams;
            forEachParams.Action = action;
            forEachParams.Source = source.GetEnumerator();
            try
            {
                for (int i = 0; i < this._threadCount; i++)
                {
                    this.StartThread(new Action<object>(this.ForEachInThread<T>), forEachParams);
                }
            }
            catch (Exception)
            {
                this.EndWork();
                throw;
            }
        }

        private void Thread(object param)
        {
            Action action = param as Action;
            try
            {
                while (!this.Canceling)
                {
                    action();
                    if (!this._enableInfiniteRepeat)
                    {
                        break;
                    }
                    this._barrierForReps.SignalAndWait();
                }
            }
            catch (Exception)
            {
                this.Cancel();
                if (this._enableInfiniteRepeat)
                {
                    this._barrierForReps.RemoveParticipant();
                }
                throw;
            }
            finally
            {
                this.EndThread();
            }
        }

        private void ForInThread(object param)
        {
            MultiThreading.ForParams forParams = (MultiThreading.ForParams)param;
            try
            {
                do
                {
                    int num = forParams.Begin;
                    while (num < forParams.End && !this.Canceling)
                    {
                        forParams.Action(num);
                        num++;
                    }
                    if (!this._enableInfiniteRepeat)
                    {
                        break;
                    }
                    this._barrierForReps.SignalAndWait();
                }
                while (!this.Canceling);
            }
            catch (Exception)
            {
                this.Cancel();
                if (this._enableInfiniteRepeat)
                {
                    this._barrierForReps.RemoveParticipant();
                }
                throw;
            }
            finally
            {
                this.EndThread();
            }
        }

        private void ForEachListInThread<T>(object param)
        {
            MultiThreading.ForEachListParams<T> forEachListParams = (MultiThreading.ForEachListParams<T>)param;
            IList<T> list = forEachListParams.List;
            try
            {
                do
                {
                    int num = forEachListParams.Begin;
                    while (num < forEachListParams.End && !this.Canceling)
                    {
                        forEachListParams.Action(list[num]);
                        num++;
                    }
                    if (!this._enableInfiniteRepeat)
                    {
                        break;
                    }
                    this._barrierForReps.SignalAndWait();
                }
                while (!this.Canceling);
            }
            catch (Exception)
            {
                this.Cancel();
                if (this._enableInfiniteRepeat)
                {
                    this._barrierForReps.RemoveParticipant();
                }
                throw;
            }
            finally
            {
                this.EndThread();
            }
        }

        private void ForEachInThread<T>(object param)
        {
            MultiThreading.ForEachParams<T> forEachParams = (MultiThreading.ForEachParams<T>)param;
            try
            {
                while (!this.Canceling)
                {
                    T current;
                    lock (forEachParams.Source)
                    {
                        if (this.Canceling)
                        {
                            break;
                        }
                        if (!forEachParams.Source.MoveNext())
                        {
                            if (this._enableInfiniteRepeat && !this._notImplementedReset)
                            {
                                try
                                {
                                    forEachParams.Source.Reset();
                                }
                                catch (NotImplementedException)
                                {
                                    this._notImplementedReset = true;
                                    break;
                                }
                                this.OnRepeatCompleted(new MultiThreadingRepeatEventArgs(this._repeatCount += 1uL));
                                continue;
                            }
                            break;
                        }
                        else
                        {
                            current = forEachParams.Source.Current;
                        }
                    }
                    forEachParams.Action(current);
                }
            }
            catch (Exception)
            {
                this.Cancel();
            }
            finally
            {
                bool flag2 = this.EndThread();
                if (flag2)
                {
                    forEachParams.Source.Dispose();
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException("MultiThreading<TProgress>");
            }
        }
    }
}

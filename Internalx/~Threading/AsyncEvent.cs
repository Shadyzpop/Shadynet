using System;
using System.ComponentModel;
using System.Threading;

namespace Shadynet.Threading
{
    public class AsyncEvent<TEventArgs> where TEventArgs : EventArgs
    {
        private readonly Action<TEventArgs> _onEvent;

        private readonly SendOrPostCallback _callbackOnEvent;

        public EventHandler<TEventArgs> EventHandler
        {
            get;
            set;
        }

        public AsyncEvent(Action<TEventArgs> onEvent)
        {
            if (onEvent == null)
            {
                throw new ArgumentNullException("onEvent");
            }
            this._onEvent = onEvent;
            this._callbackOnEvent = new SendOrPostCallback(this.OnCallback);
        }

        public void On(object sender, TEventArgs eventArgs)
        {
            EventHandler<TEventArgs> eventHandler = this.EventHandler;
            if (eventHandler != null)
            {
                eventHandler(sender, eventArgs);
            }
        }

        public void Post(AsyncOperation asyncOperation, object sender, TEventArgs eventArgs)
        {
            if (asyncOperation == null)
            {
                this.On(sender, eventArgs);
                return;
            }
            asyncOperation.Post(this._callbackOnEvent, eventArgs);
        }

        public void PostOperationCompleted(AsyncOperation asyncOperation, object sender, TEventArgs eventArgs)
        {
            if (asyncOperation == null)
            {
                this.On(sender, eventArgs);
                return;
            }
            asyncOperation.PostOperationCompleted(this._callbackOnEvent, eventArgs);
        }

        private void OnCallback(object param)
        {
            this._onEvent(param as TEventArgs);
        }
    }
}

using System;

namespace dotnet_sockets
{
    public class EventArgs<T> : EventArgs
    {
        public EventArgs(T v)
        {
            Value = v;
        }
        public T Value { get; private set; }
    }
}

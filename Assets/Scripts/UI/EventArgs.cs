using System;

namespace Game.UI
{
    public class EventArgs<T> : EventArgs 
    {
        public EventArgs(T value) => Value = value;
        public T Value { get; }

        public static implicit operator EventArgs<T>(T value) => new(value);
        public static implicit operator T(EventArgs<T> args) =>
            args is null ? default! : args.Value;
    }
}
using System;

public class WatchableProperty<T, U>
{
    public U Descriptor;
    private T _value;
    public T Value
    {
        get => _value;
        set
        {
            if (_value.Equals(value))
                return;

            _value = value;
            Changed?.Invoke(Descriptor);
        }
    }

    public Action<U> Changed;

    public WatchableProperty(U descriptor, T value = default)
    {
        _value = value;
        Descriptor = descriptor;
    }
}

public class WatchableProperty<T>
{
    private T _value;
    public T Value
    {
        get => _value;
        set
        {
            if (_value.Equals(value))
                return;

            _value = value;
            Changed?.Invoke();
        }
    }

    public Action Changed;

    public WatchableProperty(T value)
    {
        _value = value;
    }
}

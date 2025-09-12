using FluxFramework.Core;

/// <summary>
/// A generic, reusable event class for publishing simple data values
/// through the Flux EventBus.
/// </summary>
/// <typeparam name="T">The type of the data value this event carries.</typeparam>
public class GenericDataEvent<T> : FluxEventBase
{
    /// <summary>
    /// The data value carried by this event.
    /// </summary>
    public T Value { get; }

    public GenericDataEvent(T value)
    {
        Value = value;
    }
}
namespace NsqMon.Common.Events.Ux
{
    /// <summary>OnPropertyChangedDelegate</summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PropertyChangedEventHandler&lt;T&gt;"/> instance containing the event data.</param>
    public delegate void PropertyChangedEventHandler<T>(object sender, PropertyChangedEventArgs<T> e);

    /// <summary>EnhancedPropertyChangedEventArgs</summary>
    public class PropertyChangedEventArgs<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangedEventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public PropertyChangedEventArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>Gets the old value.</summary>
        public T OldValue { get; private set; }

        /// <summary>Gets the new value.</summary>
        public T NewValue { get; private set; }
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using NsqMon.Common.Events.Ux;

namespace NsqMon.Common.Mvvm
{
    /// <summary>
    /// ModelBase
    /// </summary>
    public class ModelBase : INotifyPropertyChanged
    {
        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Dictionary<string, object> _propertyValues = new Dictionary<string, object>();

        private readonly Dictionary<string, List<object>> _onPropertyChangedEventHandlers
                                                                    = new Dictionary<string, List<object>>();
        private readonly object _onPropertyChangedEventHandlersLocker = new object();

        /// <summary>Raises the <see cref="PropertyChanged"/> event.</summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void AddPropertyChangedHandler<T>(string propertyName, PropertyChangedEventHandler<T> handler)
        {
            lock (_onPropertyChangedEventHandlersLocker)
            {
                List<object> handlers;
                if (!_onPropertyChangedEventHandlers.TryGetValue(propertyName, out handlers))
                {
                    handlers = new List<object>();
                    _onPropertyChangedEventHandlers.Add(propertyName, handlers);
                }
                handlers.Add(handler);
            }
        }

        /// <summary>Gets the specified property value.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>The property value.</returns>
        protected T Get<T>(string propertyName)
        {
            object value;
            if (_propertyValues.TryGetValue(propertyName, out value))
                return (T)value;
            else
                return default(T);
        }

        /// <summary>Sets the specified property value.</summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        protected void Set<T>(string propertyName, T value)
        {
            bool keyExists;
            T oldValue;
            object oldValueObject;
            if (_propertyValues.TryGetValue(propertyName, out oldValueObject))
            {
                keyExists = true;
                oldValue = (T)oldValueObject;
            }
            else
            {
                keyExists = false;
                oldValue = default(T);
            }

            bool hasChanged = false;
            if (value != null)
            {
                if (!value.Equals(oldValue))
                    hasChanged = true;
            }
            else if (oldValue != null)
            {
                hasChanged = true;
            }

            if (hasChanged)
            {
                if (keyExists)
                    _propertyValues[propertyName] = value;
                else
                    _propertyValues.Add(propertyName, value);

                RaisePropertyChanged(propertyName);

                List<object> handlers;
                lock (_onPropertyChangedEventHandlersLocker)
                {
                    _onPropertyChangedEventHandlers.TryGetValue(propertyName, out handlers);
                }
                if (handlers != null)
                {
                    foreach (var objHandler in handlers)
                    {
                        var handler = (PropertyChangedEventHandler<T>)objHandler;
                        handler(this, new PropertyChangedEventArgs<T>(oldValue, value));
                    }
                }
            }
        }
    }
}

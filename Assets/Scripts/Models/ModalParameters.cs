using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.Models
{
    public class ModalParameters
    {
        private Dictionary<string, object> parameters = new Dictionary<string, object>();

        public void Set(string key, object value)
        {
            parameters[key] = value;
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            if (parameters.TryGetValue(key, out object value))
            {
                try
                {
                    return (T)value;
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (parameters.TryGetValue(key, out object objValue))
            {
                try
                {
                    value = (T)objValue;
                    return true;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }
            value = default;
            return false;
        }

        public bool Has(string key)
        {
            return parameters.ContainsKey(key);
        }
    }
    public class ModalParametersBuilder
    {
        private ModalParameters parameters = new ModalParameters();

        public ModalParametersBuilder With(string key, object value)
        {
            parameters.Set(key, value);
            return this;
        }

        public ModalParameters Build()
        {
            return parameters;
        }
    }
}

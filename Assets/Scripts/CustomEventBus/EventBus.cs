using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CustomEventBus
{
    public class EventBus : IService
    {
        private Dictionary<string, List<object>> _signalCallbacks = new Dictionary<string, List<object>>();

        public void Subscribe<T>(Action<T> callback)
        {
            string key = typeof(T).Name;
            if (_signalCallbacks.ContainsKey(key))
            {
                _signalCallbacks[key].Add(callback);
            }
            else
            {
                _signalCallbacks.Add(key, new List<object>() { callback });
            }
        }

        public void Unsubcribe<T>(Action<T> callback)
        {
            string key = typeof(T).Name;
            if (_signalCallbacks.ContainsKey(key))
            {
                _signalCallbacks[key].Remove(callback);
            }
            else
            {
                throw new InvalidOperationException("Нет такого события");
            }
        }

        public void Invoke<T>(T signal)
        {
            string key = typeof(T).Name;
            if (_signalCallbacks.ContainsKey(key))
            {
                foreach (var obj in _signalCallbacks[key])
                {
                    var callback = obj as Action<T>;
                    callback?.Invoke(signal);
                }
            }
        }

        public void Init()
        {
            
        }
    }
}

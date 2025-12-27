using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CustomServiceManager
{
    public class ServiceManager
    {
        private ServiceManager() { }

        // Зарегистрированыне сервисы
        private readonly Dictionary<string, IService> _services = new Dictionary<string, IService>();

        public static ServiceManager Current { get; private set; }

        public static void Initialize()
        {
            Current = new ServiceManager();
        }

        public T Get<T>() where T : IService
        {
            string key = typeof(T).Name;
            if (!_services.ContainsKey(key))
            {
                throw new InvalidOperationException();
            }

            return (T)_services[key];
        }

        public void Register<T>(T service) where T : IService
        {
            string key = typeof(T).Name;
            if (_services.ContainsKey(key))
            {
                return;
            }
            _services.Add(key, service); 
        }

        public void Unregister<T>() where T : IService
        {
            string key = typeof(T).Name;
            if (!_services.ContainsKey(key))
            {
                return;
            }

            _services.Remove(key);
        }
    }
}

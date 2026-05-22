using System;
using System.Collections.Generic;

namespace Assets.Scripts.CustomServiceManager
{
    /// <summary>
    /// Менеджер сервисов (Service Locator) - реализация паттерна Service Locator.
    /// Предоставляет централизованное хранилище для всех сервисов приложения.
    /// Позволяет получать сервисы по их интерфейсу без создания прямой зависимости.
    /// Реализован как синглтон (одиночка).
    /// </summary>
    public class ServiceManager
    {
        /// <summary>
        /// Приватный конструктор для реализации паттерна синглтон.
        /// Запрещает создание экземпляров класса извне.
        /// </summary>
        private ServiceManager() { }

        /// <summary>
        /// Хранилище зарегистрированных сервисов.
        /// Ключ - имя типа сервиса (string), значение - экземпляр сервиса.
        /// </summary>
        private readonly Dictionary<string, IService> _services = new Dictionary<string, IService>();

        /// <summary>
        /// Текущий экземпляр ServiceManager.
        /// Реализует паттерн синглтон.
        /// </summary>
        public static ServiceManager Current { get; private set; }

        /// <summary>
        /// Инициализирует ServiceManager (создает единственный экземпляр).
        /// Этот метод быть вызывается перед первым использованием ServiceManager.
        /// </summary>
        public static void Initialize()
        {
            Current = new ServiceManager();
        }

        /// <summary>
        /// Получить зарегистрированный сервис по его типу.
        /// </summary>
        /// <typeparam name="T">Тип сервиса (должен реализовывать интерфейс IService)</typeparam>
        /// <returns>Экземпляр запрошенного сервиса</returns>
        /// <exception cref="InvalidOperationException">
        /// Выбрасывается, если сервис с указанным типом не зарегистрирован
        /// </exception>
        public T Get<T>() where T : IService
        {
            string key = typeof(T).Name;
            if (!_services.ContainsKey(key))
            {
                throw new InvalidOperationException();
            }

            return (T)_services[key];
        }

        /// <summary>
        /// Зарегистрировать новый сервис.
        /// </summary>
        /// <typeparam name="T">Тип регистрируемого сервиса (должен реализовывать IService)</typeparam>
        /// <param name="service">Экземпляр сервиса для регистрации</param>
        public void Register<T>(T service) where T : IService
        {
            string key = typeof(T).Name;
            if (_services.ContainsKey(key))
            {
                return;
            }
            _services.Add(key, service); 
        }

        /// <summary>
        /// Удалить зарегистрированный сервис по его типу.
        /// </summary>
        /// <typeparam name="T">Тип сервиса для удаления (должен реализовывать IService)</typeparam>
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

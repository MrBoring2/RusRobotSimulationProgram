using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.CustomEventBus
{
    /// <summary>
    /// Шина событий (Event Bus) - реализация паттерна Publisher-Subscriber.
    /// Позволяет различным компонентам обмениваться сообщениями без прямой зависимости друг от друга.
    /// Реализует интерфейс IService для интеграции с сервис-локатором.
    /// </summary>
    public class EventBus : IService
    {
        /// <summary>
        /// Хранилище подписчиков.
        /// Ключ - имя типа события (string), значение - список колбэков (обернутых в object из-за разных типов).
        /// </summary>
        private Dictionary<string, List<object>> _signalCallbacks;

        /// <summary>
        /// Подписаться на событие определенного типа.
        /// </summary>
        /// <typeparam name="T">Тип события (сигнала), на который происходит подписка</typeparam>
        /// <param name="callback">Делегат-обработчик, который будет вызван при наступлении события</param>
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
        /// <summary>
        /// Отписаться от события определенного типа.
        /// </summary>
        /// <typeparam name="T">Тип события, от которого нужно отписаться</typeparam>
        /// <param name="callback">Делегат-обработчик, который нужно удалить</param>
        /// <exception cref="InvalidOperationException">Выбрасывается, если события с таким типом не существует</exception>
        public void Unsubcribe<T>(Action<T> callback)
        {
            string key = typeof(T).Name;
            if (_signalCallbacks.ContainsKey(key))
            {
                _signalCallbacks[key].Remove(callback);
            }
            else
            {
                throw new InvalidOperationException($"События {key} не существует");
            }
        }
        /// <summary>
        /// Вызвать (запустить) событие определенного типа.
        /// Все подписчики, которые подписались на этот тип события, получат уведомление.
        /// </summary>
        /// <typeparam name="T">Тип события, которое нужно вызвать</typeparam>
        /// <param name="signal">Объект события (сигнал) с данными, которые будут переданы подписчикам</param>
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
        /// <summary>
        /// Инициализация шины событий.
        /// Реализация метода интерфейса IService.
        /// </summary>
        public void Init()
        {
            _signalCallbacks = new Dictionary<string, List<object>>();
        }
    }
}

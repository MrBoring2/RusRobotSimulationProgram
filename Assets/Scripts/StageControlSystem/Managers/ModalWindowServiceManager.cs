using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.EventSystems.StandaloneInputModule;

namespace Assets.Scripts.Managers
{
    public class ModalWindowServiceManager : MonoBehaviour, IService
    {
        private EventBus _eventBus;
        private Dictionary<string, IModalWindow> registeredWindows = new Dictionary<string, IModalWindow>();
        public GameObject windowContainer;
        public void Init()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            if (windowContainer != null)
            {
                var allWindows = windowContainer.GetComponents<BaseModalWindow>();

                foreach (var window in allWindows)
                {
                    RegisterWindow(window);
                }
            }
        }

        public void RegisterWindow(IModalWindow window)
        {
            if (!registeredWindows.ContainsKey(window.WindowId))
            {
                registeredWindows.Add(window.WindowId, window);
            }
        }

        public void UnregisterWindow(string windowId)
        {
            if (registeredWindows.ContainsKey(windowId))
            {
                registeredWindows.Remove(windowId);
            }
        }

        public void ShowWindow(string windowId, string message, ModalParameters parameters)
        {
            ShowWindow(windowId, message, parameters, null);
        }

        public void ShowWindow(string windowId, string message, ModalParameters parameters, Action<object> onClose)
        {
            if (registeredWindows.TryGetValue(windowId, out IModalWindow window))
            {
                window.Show(message, parameters, onClose);
            }
            else
            {
                Debug.LogError($"Окно '{windowId}' не найдено!");
                onClose?.Invoke(null);
            }
        }
        public void ShowWindow<T>(string windowId, string message, ModalParameters parameters, Action<T> onClose)
        {
            ShowWindow(windowId, message, parameters, (result) => onClose?.Invoke((T)result));
        }

        public void HideWindow(string windowId)
        {
            if (registeredWindows.TryGetValue(windowId, out IModalWindow window))
            {
                window.Hide();
            }
        }

        public void HideAllWindows()
        {
            foreach (var window in registeredWindows.Values)
            {
                if (window.IsVisible)
                    window.Hide();
            }
        }

        public T GetWindow<T>(string windowId) where T : class, IModalWindow
        {
            if (registeredWindows.TryGetValue(windowId, out var window))
                return window as T;
            return null;
        }
    }
}

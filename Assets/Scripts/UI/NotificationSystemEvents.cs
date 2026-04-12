using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Notifications;
using Assets.Scripts.CustomServiceManager;
using Assets.UI.CustomElements.Notification;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class NotificationSystemEvents : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private float spacing = 10f;

        private VisualElement root;
        private VisualElement notificationsContainer;
        private EventBus _eventBus;
        private Dictionary<long, NotificationPopup> visualNotifications = new Dictionary<long, NotificationPopup>();
        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            CreateNotificationContainer();

            _eventBus = ServiceManager.Current.Get<EventBus>();

            // Подписываемся на события от менеджера
            _eventBus.Subscribe<ShowNotificationSignal>(OnShowNotification);
            _eventBus.Subscribe<ClearAllNotificationsSignal>(OnClearAllNotifications);
        }

        private void OnClearAllNotifications(ClearAllNotificationsSignal signal)
        {
            foreach (var notification in visualNotifications.Values)
            {
                notification.HideImmediate();
            }
            visualNotifications.Clear();
        }

        private void OnShowNotification(ShowNotificationSignal signal)
        {
            var notification = new NotificationPopup();
            notification.DisplayDuration = signal.NotificationData.customDuration ?? 3000;
            notification.SetNotificationData(signal.NotificationData.Title, signal.NotificationData.Message, signal.NotificationData.Level);
            notification.style.marginBottom = spacing;
            notification.OnHide += (n) =>
            {
                _eventBus.Invoke(new NotificationHiddenSignal { NotificationId = signal.NotificationData.Id });
                visualNotifications.Remove(signal.NotificationData.Id);
                notification.RemoveFromHierarchy();
            };

            notificationsContainer.Add(notification);
            visualNotifications.Add(signal.NotificationData.Id, notification);
            notification.Show();
        }


        private void CreateNotificationContainer()
        {
            notificationsContainer = root.Q<VisualElement>("notification-container");
        }
        private void OnDestroy()
        {
            _eventBus?.Unsubcribe<ShowNotificationSignal>(OnShowNotification);
            _eventBus?.Unsubcribe<ClearAllNotificationsSignal>(OnClearAllNotifications);
        }

    }
}

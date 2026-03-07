using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Notifications;
using Assets.Scripts.CustomServiceManager;
using Assets.UI.CustomElements.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public class NotificationSystemManager : MonoBehaviour, IService
    {
        [Header("Настройки")]
        [SerializeField] private int maxVisibleNotifications = 5;
        [SerializeField] private int defaultDisplayDuration = 3000;
        private EventBus _eventBus;
        private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
        private Dictionary<long, NotificationData> activeNotifications = new Dictionary<long, NotificationData>();
        private long nextNotificationId = 0;
        public void Init()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _eventBus.Subscribe<NotificationHiddenSignal>(OnNotificationHidden);
        }

        public void ShowInfo(string message, string title = "Информация")
        {
            ShowNotification(message, title, NotificationLevel.Info);
        }

        public void ShowSuccess(string message, string title = "Успех")
        {
            ShowNotification(message, title, NotificationLevel.Success);
        }

        public void ShowWarning(string message, string title = "Предупреждение")
        {
            ShowNotification(message, title, NotificationLevel.Warning);
        }

        public void ShowError(string message, string title = "Ошибка")
        {
            ShowNotification(message, title, NotificationLevel.Error);
        }

        public void ShowNotification(string message, string title, NotificationLevel level, int? customDuration = null)
        {
            long notificationId = nextNotificationId++;
            var notificationData = new NotificationData
            {
                Id = nextNotificationId,
                Message = message,
                Title = title,
                Level = level,
                customDuration = customDuration
            };

            if (activeNotifications.Count < maxVisibleNotifications)
            {
                CreateNotification(notificationData, customDuration ?? defaultDisplayDuration);
            }
            else
            {
                notificationQueue.Enqueue(notificationData);
            }
        }

        private void CreateNotification(NotificationData data, int duration)
        {
           
            activeNotifications.Add(data.Id, data);

            _eventBus.Invoke(new ShowNotificationSignal(data));
        }

        private void OnNotificationHidden(NotificationHiddenSignal hiddenEvent)
        {
            if (activeNotifications.ContainsKey(hiddenEvent.NotificationId))
            {
                activeNotifications.Remove(hiddenEvent.NotificationId);
            }

            if (notificationQueue.Count > 0 && activeNotifications.Count < maxVisibleNotifications)
            {
                var nextNotification = notificationQueue.Dequeue();
                CreateNotification(nextNotification, defaultDisplayDuration);
            }
        }
        public void ClearAllNotifications()
        {
            _eventBus.Invoke(new ClearAllNotificationsSignal());
            notificationQueue.Clear();
            activeNotifications.Clear();
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubcribe<NotificationHiddenSignal>(OnNotificationHidden);
        }
        //private void OnNotificationHidden(NotificationPopup notification)
        //{
        //    notification.OnHide -= OnNotificationHidden;

        //    activeNotifications.Remove(notification);

        //    notification.RemoveFromHierarchy();

        //    if (notificationQueue.Count > 0 && activeNotifications.Count < maxVisibleNotifications)
        //    {
        //        var nextNotification = notificationQueue.Dequeue();
        //        CreateNotification(nextNotification, defaultDisplayDuration);
        //    }
        //}
        //public void ClearAllNotifications()
        //{
        //    foreach (var notification in activeNotifications.ToList())
        //    {
        //        notification.HideImmediate();
        //    }
        //    notificationQueue.Clear();
        //}

        //public void HideNotification(NotificationPopup notification)
        //{
        //    if (activeNotifications.Contains(notification))
        //    {
        //        notification.HideImmediate();
        //    }
        //}
    }
}

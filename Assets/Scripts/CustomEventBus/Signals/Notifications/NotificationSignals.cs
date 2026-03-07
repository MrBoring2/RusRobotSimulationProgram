using Assets.UI.CustomElements.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CustomEventBus.Signals.Notifications
{
    public class NotificationHiddenSignal
    {
        public long NotificationId { get; set; }
    }
    public class ShowNotificationSignal
    {
        public ShowNotificationSignal(NotificationData notificationData)
        {
            NotificationData = notificationData;
        }

        public NotificationData NotificationData { get; set; }
    }

    public class ClearAllNotificationsSignal
    {

    }
}

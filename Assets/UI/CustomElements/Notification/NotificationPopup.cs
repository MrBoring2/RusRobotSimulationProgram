using Assets.UI.CustomElements.Blur;
using Assets.UI.CustomElements.PopupPanelF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.UI.CustomElements.Notification
{
    public class NotificationPopup : VisualElement
    {
        [UnityEngine.Scripting.Preserve]
        public new class UxmlFactory : UxmlFactory<NotificationPopup, UxmlTraits> { }

        [UnityEngine.Scripting.Preserve]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription startVisible = new UxmlBoolAttributeDescription { name = "start-visible", defaultValue = false };
            private readonly UxmlIntAttributeDescription fadeTime = new UxmlIntAttributeDescription { name = "fade-time", defaultValue = 30 };
            private readonly UxmlIntAttributeDescription displayDuration = new UxmlIntAttributeDescription { name = "display-duration", defaultValue = 3000 }; // в миллисекундах

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var item = ve as NotificationPopup;
                var vis = startVisible.GetValueFromBag(bag, cc);
                item.FadeTime = fadeTime.GetValueFromBag(bag, cc);
                item.DisplayDuration = displayDuration.GetValueFromBag(bag, cc);

                item.SetStartVisibility(vis);
            }
        }
        private VisualElement progressFill;
        private Color infoColor = new Color(0.2f, 0.6f, 1f);
        private Color successColor = new Color(0.2f, 0.8f, 0.2f);
        private Color warningColor = new Color(1f, 0.6f, 0f);
        private Color errorColor = new Color(1f, 0.3f, 0.3f);
        private float currentProgress = 100;
        public int FadeTime { get; set; } = 30;
        public int DisplayDuration { get; set; } = 3000;

        protected IVisualElementScheduledItem showTask;
        protected IVisualElementScheduledItem hideTask;
        protected IVisualElementScheduledItem progressTask;

        private const string stylesResource = "Styles/PopupPanelStyleSheet";
        private const string ussClassName = "notification-popup";
        public event Action<NotificationPopup> OnHide;
        public NotificationPopup()
        {
            styleSheets.Add(Resources.Load<StyleSheet>(stylesResource));
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Position;
            style.unityTextAlign = TextAnchor.MiddleLeft;
            //notification.style.boxShadow = new Shadow(0, 4, 20, 0, new Color(0, 0, 0, 0.25f));

            // Заголовок
            var titleLabel = new Label("");
            titleLabel.name = "Title";
            titleLabel.style.fontSize = 14;
            //titleLabel.style.fontWeight = FontWeight.Bold;
            titleLabel.style.color = Color.white;
            titleLabel.style.marginBottom = 4;
            Add(titleLabel);

            // Сообщение
            var messageLabel = new Label("");
            messageLabel.name = "Message";
            messageLabel.style.fontSize = 12;
            messageLabel.style.color = Color.white;
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            Add(messageLabel);

            // Индикатор прогресса (для анимации)
            var progressBar = new VisualElement();
            progressBar.style.height = 3;
            progressBar.style.backgroundColor = new Color(1, 1, 1, 0.3f);
            progressBar.style.marginTop = 8;
            //progressBar.style.borderRadius = 2;
            progressBar.style.width = new Length(100, LengthUnit.Percent);

            progressFill = new VisualElement();
            progressFill.style.height = 3;
            progressFill.style.backgroundColor = new Color(1, 1, 1, 0.5f);
            //progressFill.style.borderRadius = 2;
            progressFill.style.width = new Length(100, LengthUnit.Percent);
            progressBar.name = "ProgressBar";

            progressBar.Add(progressFill);
            Add(progressBar);

            SetStartVisibility(false);
            // Show();
            RegisterCallback<ClickEvent>(OnClickNotification);

        }

        private void OnClickNotification(ClickEvent e)
        {
            if (e.button == 0)
            {
                HideImmediate();
            }
        }
        public void SetNotificationData(string title, string message, NotificationLevel level)
        {
            var titleLabel = this.Q<Label>("Title");
            if (titleLabel != null)
            {
                titleLabel.text = title;
            }

            var messageLabel = this.Q<Label>("Message");
            if (messageLabel != null)
            {
                messageLabel.text = message;
            }

            style.backgroundColor = GetColorForLevel(level);
        }
        public virtual void Show()
        {
            showTask?.Pause();
            showTask = null;
            hideTask?.Pause();
            hideTask = null;
            progressTask?.Pause();
            progressTask = null;

            if (FadeTime > 0.0f)
            {
                style.visibility = Visibility.Visible;
                style.opacity = 0f;
                float animationStartTime = Time.time;
                showTask = schedule
                    .Execute(() =>
                    {
                        style.opacity = Mathf.Clamp01(resolvedStyle.opacity + 0.1f);
                        if (resolvedStyle.opacity >= 1.0f)
                        {
                            float actualDelay = (Time.time - animationStartTime) * 1000f;
                            float remainingTime = Mathf.Max(0, DisplayDuration - actualDelay);
                            StartProgressAnimation(remainingTime);
                        }
                    })
                    .Every(FadeTime)
                    .Until(() => resolvedStyle.opacity >= 1.0f);

            }
            else
            {
                style.visibility = Visibility.Visible;
                style.opacity = 1f;
                StartProgressAnimation(DisplayDuration);
            }
            ScheduleHide();
        }

        private void StartProgressAnimation(float durationMs)
        {
            if (progressFill == null) return;

            float duration = durationMs / 1000f; // в секундах
            float startTime = Time.time;
            float targetWidth = 100f;

            progressTask = schedule.Execute(() =>
            {
                if (progressFill == null) return;

                float elapsed = Time.time - startTime;

                if (elapsed >= duration)
                {
                    progressFill.style.width = new Length(0, LengthUnit.Percent);
                    progressTask?.Pause();
                    progressTask = null;
                    return;
                }

                // Плавная интерполяция
                float t = elapsed / duration;
                float width = Mathf.Lerp(targetWidth, 0, t);

                progressFill.style.width = new Length(width, LengthUnit.Percent);

            }).Every(10); // Каждые 10мс для максимальной плавности
            //if (progressFill == null) return;

            //progressFill.style.width = new Length(100, LengthUnit.Percent);

            //progressFill.experimental.animation.Start(100f, 0f, (int)durationMs, (element, value) =>
            //{
            //    if (element != null)
            //    {
            //        element.style.width = new Length(value, LengthUnit.Percent);
            //    }
            //});
        }

        private void ScheduleHide()
        {
            hideTask = schedule.Execute(Hide);
            hideTask.ExecuteLater(DisplayDuration);
        }
        public virtual void Hide()
        {
            showTask?.Pause();
            showTask = null;
            hideTask?.Pause();
            hideTask = null;
            progressTask?.Pause();
            progressTask = null;

            if (FadeTime > 0.0f)
            {
                showTask = schedule
                    .Execute(() =>
                    {
                        var o = Mathf.Clamp01(resolvedStyle.opacity - 0.1f);
                        style.opacity = o;
                        if (o <= 0.0f)
                        {
                            style.visibility = Visibility.Hidden;
                            OnCompleteHide();
                        }
                    })
                    .Every(FadeTime)
                    .Until(() => resolvedStyle.opacity <= 0.0f);
            }
            else
            {
                style.visibility = Visibility.Hidden;
                style.opacity = 0f;
                OnCompleteHide();
            }
        }
        private void OnCompleteHide()
        {
            OnHide?.Invoke(this);
            // RemoveFromHierarchy();
        }

        public void HideImmediate()
        {
            showTask?.Pause();
            showTask = null;
            hideTask?.Pause();
            hideTask = null;

            style.visibility = Visibility.Hidden;
            style.opacity = 0f;

            OnCompleteHide();
        }

        protected void SetStartVisibility(bool isVisible)
        {
            if (isVisible)
            {
                style.visibility = Visibility.Visible;
                style.opacity = 1f;
            }
            else
            {
                style.visibility = Visibility.Hidden;
                style.opacity = 0f;
            }
        }
        private Color GetColorForLevel(NotificationLevel level)
        {
            switch (level)
            {
                case NotificationLevel.Info: return infoColor;
                case NotificationLevel.Success: return successColor;
                case NotificationLevel.Warning: return warningColor;
                case NotificationLevel.Error: return errorColor;
                default: return infoColor;
            }
        }
    }
    public enum NotificationLevel
    {
        Info,
        Success,
        Warning,
        Error
    }
    public struct NotificationData
    {
        public long Id;
        public string Message;
        public string Title;
        public NotificationLevel Level;
        public int? customDuration;

        public NotificationData(long id, string message, string title, NotificationLevel level, int? customDuration)
        {
            Id = id;
            Message = message;
            Title = title;
            Level = level;
            this.customDuration = customDuration;
        }
    }
}

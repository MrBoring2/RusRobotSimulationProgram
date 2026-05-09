using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Models
{
    public abstract class BaseModalWindow : MonoBehaviour, IModalWindow
    {
        [SerializeField] protected string windowId;
        public VisualTreeAsset windowUXML;
        public UIBlocker UIBlocker;
        public UIDocument UIDocument;
        protected VisualElement root;
        protected VisualElement windowRoot;
        protected VisualElement overlay;
        protected Label messageLabel;
        protected Button closeButton;

        protected Action<object> onCloseCallback;
        protected object returnValue;
        protected bool isDragging = false;
        protected Vector2 dragOffset;
        protected ModalParameters currentParameters;
        protected EventBus _eventBus;

        public string WindowId => windowId;
        public bool IsVisible { get; protected set; }
        public VisualElement RootElement => overlay;
        protected virtual void Start()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            root = UIDocument.rootVisualElement;
            windowRoot = windowUXML.CloneTree();
            InitializeElements(windowRoot);
            RegisterEvents();
            EnableDrag();
            overlay = root.Q<VisualElement>("overlay");
            overlay.Add(windowRoot);
            Hide();

        }
        protected virtual void InitializeElements(VisualElement root)
        {
            closeButton = windowRoot.Q<Button>("close-button");
        }
        protected virtual void RegisterEvents()
        {
            if (closeButton != null)
                closeButton.clicked += () => Hide(null);
        }
        public virtual void Show(string message, ModalParameters parameters)
        {
            Show(message, parameters, null);
        }
        public virtual void Show(string message, ModalParameters parameters, Action<object> onClose)
        {
            OnBeforeShow(parameters);
            UIBlocker.AddNewModalWindow(windowRoot);
            onCloseCallback = onClose;
            returnValue = null;
            currentParameters = parameters ?? new ModalParameters();

            if (!string.IsNullOrEmpty(message) && messageLabel != null)
                messageLabel.text = message;
            windowRoot.RegisterCallback<GeometryChangedEvent>(OnWindowSizeChanged);
            windowRoot.style.display = DisplayStyle.Flex;
            IsVisible = true;
        }
        protected abstract void OnBeforeShow(ModalParameters parameters);

        public virtual void Hide()
        {
            Hide(null);
        }
        public virtual void Hide(object returnValue)
        {
            UIBlocker.ResolveUI();
            UIBlocker.DisableInputMode();
            UIBlocker.RemoveModalWindow(windowRoot);
            this.returnValue = returnValue;
            windowRoot.style.display = DisplayStyle.None;
            //overlay.style.display = DisplayStyle.None;
            IsVisible = false;

            onCloseCallback?.Invoke(returnValue);
            onCloseCallback = null;
            currentParameters = null;
        }
        protected void CloseWithValue(object value)
        {
            Hide(value);
        }


        private void EnableDrag()
        {
            var rootElement = windowRoot.Q("modal-window-container");

            rootElement.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.LeftMouse)
                {
                    isDragging = true;
                    dragOffset = evt.mousePosition - rootElement.layout.position;
                }
            });

            rootElement.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (isDragging)
                {
                    rootElement.style.left = evt.mousePosition.x - dragOffset.x;
                    rootElement.style.top = evt.mousePosition.y - dragOffset.y;
                }
            });

            rootElement.RegisterCallback<MouseUpEvent>(evt => isDragging = false);
        }
        private void OnWindowSizeChanged(GeometryChangedEvent evt)
        {
            var rootElement = windowRoot.Q("modal-window-container");
            float windowWidth = rootElement.resolvedStyle.width;
            float windowHeight = rootElement.resolvedStyle.height;
            float screenWidth = root.resolvedStyle.width;
            float screenHeight = root.resolvedStyle.height;

            windowRoot.style.left = (screenWidth - windowWidth) / 2;
            windowRoot.style.top = (screenHeight - windowHeight) / 2;
            windowRoot.UnregisterCallback<GeometryChangedEvent>(OnWindowSizeChanged);
        }

    }
}

using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomEventBus.Signals.ObjectsLibrary;
using Assets.Scripts.CustomEventBus.Signals.PLC;
using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class ConditionGenerationPanelEvents : MonoBehaviour
    {
        public VisualTreeAsset windowUXML;

        public event Action<GameObject> OnObjectSelected;

        private VisualElement root;
        private VisualElement windowRoot;
        private VisualElement list;
        public UIBlocker UIBlocker;
        private bool isDragging = false;
        private Vector2 dragOffset;
        private EventBus _eventBus;
        private string expression = "";
        private string currentParentObjectId = null;
        private TextField textBox;

        void Start()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _eventBus.Subscribe<PLCShowExpressionPanelSignal>(OnShowExpression);

            root = GetComponent<UIDocument>().rootVisualElement;
            windowRoot = windowUXML.CloneTree();

            list = windowRoot.Q<ScrollView>("list");
            textBox = windowRoot.Q<TextField>("conditionField");
            textBox.RegisterCallback<ChangeEvent<string>>(p =>
            {
                expression = p.newValue;
            });
            var button = windowRoot.Q<Button>("cancelBtn");
            button.clicked += () => { windowRoot.style.display = DisplayStyle.None; UIBlocker.RemoveModalWindow(windowRoot); };
            var confirmBurron = windowRoot.Q<Button>("confirmBtn");
            confirmBurron.clicked += () => { ConfirmCondition(); };

            EnableDrag();
        }

        private void ConfirmCondition()
        {
            _eventBus.Invoke(new PLCChangeExpressionSignal(currentParentObjectId, expression));
            expression = "";
            textBox.SetValueWithoutNotify("");
            windowRoot.style.display = DisplayStyle.None;
            UIBlocker.RemoveModalWindow(windowRoot);
        }

        private void OnShowExpression(PLCShowExpressionPanelSignal signal)
        {
            currentParentObjectId = signal.ParentId;
            Show();
        }

        private void EnableDrag()
        {
            var rootElement = windowRoot.Q("condition-generator-container");

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

        public void Show()
        {
            if (windowRoot.parent == null)
                root.Q("overlay").Add(windowRoot);
            windowRoot.style.display = DisplayStyle.Flex;

            windowRoot.RegisterCallback<GeometryChangedEvent>(OnWindowSizeChanged);
            UIBlocker.AddNewModalWindow(windowRoot);
        }

        private void OnWindowSizeChanged(GeometryChangedEvent evt)
        {
            var rootElement = windowRoot.Q("condition-generator-container");
            float windowWidth = rootElement.resolvedStyle.width;
            float windowHeight = rootElement.resolvedStyle.height;
            float screenWidth = root.resolvedStyle.width;
            float screenHeight = root.resolvedStyle.height;

            // Центрируем окно
            windowRoot.style.left = (screenWidth - windowWidth) / 2;
            windowRoot.style.top = (screenHeight - windowHeight) / 2;
            windowRoot.UnregisterCallback<GeometryChangedEvent>(OnWindowSizeChanged);
        }

    }
}

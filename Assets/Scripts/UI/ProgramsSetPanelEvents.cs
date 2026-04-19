using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.PLC;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.UI.CustomElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Collections.AllocatorManager;

namespace Assets.Scripts.UI
{
    internal class ProgramsSetPanelEvents : MonoBehaviour
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
        private SceneObjectsManager _sceneObjectsManager;
        private RobotProgramObject selectedProgram;
        private string blockId = "";
        private string currentRobotId = null;
        private ProgramsPopupField programsListPopupField;
        private List<RobotProgramObject> programs = new List<RobotProgramObject>();

        void Start()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
            _eventBus.Subscribe<PLCShowSetProgramPanelSignal>(OnShowPanel);

            root = GetComponent<UIDocument>().rootVisualElement;
            windowRoot = windowUXML.CloneTree();

            list = windowRoot.Q<ScrollView>("list");
            programsListPopupField = windowRoot.Q<ProgramsPopupField>("programs-list");
            programsListPopupField.choices = programs;
            programsListPopupField.RegisterCallback<ChangeEvent<RobotProgramObject>>(p =>
            {
                selectedProgram = p.newValue;
            });
            var button = windowRoot.Q<Button>("cancelBtn");
            button.clicked += () => { windowRoot.style.display = DisplayStyle.None; UIBlocker.RemoveModalWindow(windowRoot); };
            var confirmBurron = windowRoot.Q<Button>("confirmBtn");
            confirmBurron.clicked += () => { ConfirmCondition(); };

            EnableDrag();
        }

        private void ConfirmCondition()
        {
            _eventBus.Invoke(new PLCSelectProgramPanelSignal(blockId, selectedProgram));
            selectedProgram = null;
            windowRoot.style.display = DisplayStyle.None;
            UIBlocker.RemoveModalWindow(windowRoot);
        }

        private void OnShowPanel(PLCShowSetProgramPanelSignal signal)
        {
            blockId = signal.BlockId;
            currentRobotId = signal.RobotId;
            programs = _sceneObjectsManager.Commands.GetSubPrograms(currentRobotId);
            programsListPopupField.choices = programs;
            Show();
        }

        private void EnableDrag()
        {
            var rootElement = windowRoot.Q("programs-set-container");

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
            var rootElement = windowRoot.Q("programs-set-container");
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

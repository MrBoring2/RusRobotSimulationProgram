using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.HierarhyPanel;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class TopMenuPanelEvents : MonoBehaviour
    {
        private VisualElement fileMenu;
        private VisualElement viewMenu;
        private VisualElement root;
        private UIStatusManager _uIStatusManager;
        private SceneObjectsManager _sceneObjectsManager;
        private SaveLoadManager _saveLoadManager;
        private Toggle propertiesToggle;
        private Toggle objectsListToggle;
        private Button newFile;
        private Button loadFile;
        private Button saveFile;
        private EventBus _eventBus;
        public void Start()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _eventBus.Subscribe<TogglePropertiesSignal>(OnToggleProperties);
            _eventBus.Subscribe<ToggleObjectsListSignal>(OnToggleObjectsList);
            _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
            _saveLoadManager = ServiceManager.Current.Get<SaveLoadManager>();
            _uIStatusManager = ServiceManager.Current.Get<UIStatusManager>();
            root = GetComponent<UIDocument>().rootVisualElement;
            var overlay = root.Q<VisualElement>("overlay");
            fileMenu = overlay.Q<VisualElement>("FileMenu");
            viewMenu = overlay.Q<VisualElement>("ViewMenu");
            propertiesToggle = overlay.Q<Toggle>("PropertiesToggle");
            objectsListToggle = overlay.Q<Toggle>("HierarchyToggle");
            newFile = overlay.Q<Button>("NewBtn");
            loadFile = overlay.Q<Button>("OpenBtn");
            saveFile = overlay.Q<Button>("SaveBtn");

            // Кнопки
            var fileBtn = root.Q<Button>("FileButton");
            var viewBtn = root.Q<Button>("ViewButton");

            fileBtn.clicked += () => ToggleMenu(fileMenu, fileBtn);
            viewBtn.clicked += () => ToggleMenu(viewMenu, viewBtn);

            // Закрытие при клике вне меню
            root.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.target != fileBtn && evt.target != viewBtn)
                {
                    HideAll();
                }
            });

            // Обработка кликов внутри меню, чтобы не закрывалось
            fileMenu.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            viewMenu.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            propertiesToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                propertiesToggle.value = evt.newValue;
                _uIStatusManager.SetPropertiesPanelVisibility(evt.newValue);
            });

            objectsListToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                objectsListToggle.value = evt.newValue;
                _uIStatusManager.SetObjectsListPanelVisibility(evt.newValue);
            });

            newFile.RegisterCallback<ClickEvent>(OnNewFileClicked);
            loadFile.RegisterCallback<ClickEvent>(OnFileLoadClicked);
            saveFile.RegisterCallback<ClickEvent>(OnFileSaveClicked);


            propertiesToggle.value = _uIStatusManager.IsPropertiesPanelVisible;
            objectsListToggle.value = _uIStatusManager.IsObjectsListVisible;

        }

        private void OnFileSaveClicked(ClickEvent evt)
        {
            _saveLoadManager.SaveScene();
        }

        private void OnFileLoadClicked(ClickEvent evt)
        {
            _saveLoadManager.LoadScene();
        }

        private void OnNewFileClicked(ClickEvent evt)
        {
            _saveLoadManager.ClearScene(); 
        }

        private void OnToggleObjectsList(ToggleObjectsListSignal signal)
        {
            objectsListToggle.SetValueWithoutNotify(_uIStatusManager.IsObjectsListVisible);
        }

        private void OnToggleProperties(TogglePropertiesSignal signal)
        {
            propertiesToggle.SetValueWithoutNotify(_uIStatusManager.IsPropertiesPanelVisible);
        }

        private void ToggleMenu(VisualElement menu, VisualElement button)
        {
            HideAll();

            // Позиционирование под кнопкой
            var rect = button.worldBound;
            menu.style.left = rect.x;
            menu.style.top = rect.yMax;

            menu.RemoveFromClassList("hidden");
        }
        private void HideAll()
        {
            fileMenu.AddToClassList("hidden");
            viewMenu.AddToClassList("hidden");
        }
    }
}

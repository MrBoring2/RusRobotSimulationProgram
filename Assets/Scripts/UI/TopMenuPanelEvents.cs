using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.HierarhyPanel;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class TopMenuPanelEvents : MonoBehaviour
    {
        private VisualElement fileMenu;
        private VisualElement viewMenu;
        private VisualElement testMenu;
        private VisualElement root;
        private UIStatusManager _uIStatusManager;
        private SceneObjectsManager _sceneObjectsManager;
        private SaveLoadManager _saveLoadManager;
        private Toggle propertiesToggle;
        private Toggle objectsListToggle;
        private Button newFile;
        private Button loadFile;
        private Button saveFile;

        private Button mes1;
        private Button mes2;
        private Button mes3;
        private Button mes4;
        private Button mes5;
        
        private EventBus _eventBus;

        private NotificationSystemManager _notificationSystem;
        public void Start()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _eventBus.Subscribe<TogglePropertiesSignal>(OnToggleProperties);
            _eventBus.Subscribe<ToggleObjectsListSignal>(OnToggleObjectsList);
            _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
            _saveLoadManager = ServiceManager.Current.Get<SaveLoadManager>();
            _uIStatusManager = ServiceManager.Current.Get<UIStatusManager>();
            _notificationSystem = ServiceManager.Current.Get<NotificationSystemManager>();
            root = GetComponent<UIDocument>().rootVisualElement;
            var overlay = root.Q<VisualElement>("overlay");
            fileMenu = overlay.Q<VisualElement>("FileMenu");
            viewMenu = overlay.Q<VisualElement>("ViewMenu");
            testMenu = overlay.Q<VisualElement>("TestMenu");
            propertiesToggle = overlay.Q<Toggle>("PropertiesToggle");
            objectsListToggle = overlay.Q<Toggle>("HierarchyToggle");
            newFile = overlay.Q<Button>("NewBtn");
            loadFile = overlay.Q<Button>("OpenBtn");
            saveFile = overlay.Q<Button>("SaveBtn");
            mes1 = overlay.Q<Button>("mes1");
            mes2 = overlay.Q<Button>("mes2");
            mes3 = overlay.Q<Button>("mes3");
            mes4 = overlay.Q<Button>("mes4");
            mes5 = overlay.Q<Button>("mes5");

            // Кнопки
            var fileBtn = root.Q<Button>("FileButton");
            var viewBtn = root.Q<Button>("ViewButton");
            var testBtn = root.Q<Button>("Test");

            fileBtn.clicked += () => ToggleMenu(fileMenu, fileBtn);
            viewBtn.clicked += () => ToggleMenu(viewMenu, viewBtn);
            testBtn.clicked += () => ToggleMenu(testMenu, testBtn);

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

            mes1.RegisterCallback<ClickEvent>(test1);
            mes2.RegisterCallback<ClickEvent>(test2);
            mes3.RegisterCallback<ClickEvent>(test3);
            mes4.RegisterCallback<ClickEvent>(test4);
            mes5.RegisterCallback<ClickEvent>(test5);


            propertiesToggle.value = _uIStatusManager.IsPropertiesPanelVisible;
            objectsListToggle.value = _uIStatusManager.IsObjectsListVisible;

        }

        private void test5(ClickEvent evt)
        {
            _notificationSystem.ShowInfo("А я 1", "Сообщение");
            _notificationSystem.ShowInfo("А я 2", "Сообщение");
            _notificationSystem.ShowInfo("А я 3", "Сообщение");
            _notificationSystem.ShowInfo("Иди нахуй", "Сообщение");
            _notificationSystem.ShowInfo("Ты иди нахуй", "Сообщение");
            _notificationSystem.ShowInfo("Нет ты иди", "Сообщение");
            _notificationSystem.ShowInfo("Доблаёбы блять", "Сообщение");
            _notificationSystem.ShowInfo("ААААААААА", "Сообщение");
            _notificationSystem.ShowInfo("БББББББББ", "Сообщение");
            _notificationSystem.ShowInfo("ВВВВВВВВВ", "Сообщение");
        }

        private void test4(ClickEvent evt)
        {
            _notificationSystem.ShowSuccess("Ебать всё норм", "Успех");
        }

        private void test3(ClickEvent evt)
        {
            _notificationSystem.ShowError("Пизда нахуй", "Опасность");
        }

        private void test2(ClickEvent evt)
        {
            _notificationSystem.ShowWarning("Чо-то не так", "Предупреждение");
        }

        private void test1(ClickEvent evt)
        {
            _notificationSystem.ShowInfo("Чо-то за инфа", "Инфа");
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

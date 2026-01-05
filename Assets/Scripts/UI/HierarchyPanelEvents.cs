using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Lines;
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomEventBus.Signals.ObjectsLibrary;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomEventBus.Signals.UndoRedoSystem;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.SystemManager;
using Assets.Scripts.UI;
using Assets.UI.CustomElements;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class HierarchyPanelEvents : MonoBehaviour
{
    private EventBus _eventBus;
    private SceneObjectsManager _sceneObjectManager;
    private LineManager _lineManager;
    private VisualElement root;
    [SerializeField] 
    private VisualElement hierarchyPanel;
    private VisualElement contextMenu;
    public CustomFoldout MainHierarchyItem { get; private set; }
    public UIBlocker iBlocker;
    //public PropertiesPanelEvents propertiesPanelEvents;
    //public ObjectPicker objectPicker;
    //public ObjectsLibraryEvents objectsLibraryEvents;
    private string selectedElementId;
    private VisualElement lastSelectedElement;
    private CustomScrollView customScrollView;
    private Dictionary<string, VisualElement> elementCache = new Dictionary<string, VisualElement>();
    private UndoRedoManager _undoRedoManager;

    private void Start()
    {
       
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _eventBus.Subscribe<AddSceneObjectSignal>(OnObjectAdded);
        _eventBus.Subscribe<RemoveSceneObjectSignal>(OnObjectRemoved);
        _eventBus.Subscribe<ChangeObjectNameSignal>(OnObjectNameChanged);
        _eventBus.Subscribe<LoadObjectsSignal>(OnLoadObjects);
        _eventBus.Subscribe<SelectObjectinLibrary>(OnObjectSelectedInLibrary);
        _eventBus.Subscribe<SelectObjectInScene>(OnObjectSelectedInScene);
        _eventBus.Subscribe<ChangeNamePropertySignal>(OnChangeNameProperty);
        _eventBus.Subscribe<ExecuteCommandSignal>(OnCommandExecuted);
        _eventBus.Subscribe<UndoneCommandSignal>(OnCommandUndoned);
        _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
        _lineManager = ServiceManager.Current.Get<LineManager>();
        _undoRedoManager = ServiceManager.Current.Get<UndoRedoManager>();
        root = GetComponent<UIDocument>().rootVisualElement;
        hierarchyPanel = root.Q("hierarchy-container");
        //propertiesPanelEvents.OnTargetNameChanged += PropertiesPanelEvents_OnTargetNameChanged;
        //UndoRedoManager.Instance.OnCommandExecuted += Instance_OnCommandExecuted;
        //UndoRedoManager.Instance.OnCommandUndone += Instance_OnCommandUndone;
        RegisterElements();
        if (_sceneObjectManager.GetGameObjectsList().Count > 0)
        {
            UpdateHierarchy();
        }
    }



    #region Обработчики событий
    private void OnObjectSelectedInScene(SelectObjectInScene scene) => SelectHierarchyItem(scene.Id);

    private void OnRobotCommandAdd(AddCommand command) => UpdateHierarchy();

    private void OnPropgrammAdd(AddProgram program) => UpdateHierarchy();

    private void Instance_OnCommandUndone(ICommand obj)
    {
        if (obj is IDestructiveCommand)
        {
            UpdateHierarchy();
        }
    }

    private void Instance_OnCommandExecuted(ICommand obj)
    {
        if (obj is IDestructiveCommand)
        {
            UpdateHierarchy();
        }
    }
    private void OnCommandUndoned(UndoneCommandSignal signal)
    {
        if (signal.Command is IDestructiveCommand)
        {
            UpdateHierarchy();
        }
    }

    private void OnCommandExecuted(ExecuteCommandSignal signal)
    {
        if (signal.Command is IDestructiveCommand)
        {
            UpdateHierarchy();
        }
    }

    private void OnObjectAdded(AddSceneObjectSignal evt) => AddHierarchyItem(evt.GameObject);
    private void OnObjectRemoved(RemoveSceneObjectSignal evt) => RemoveHierarchyItem(evt.GameObject.Id);
    private void OnObjectSelectedInLibrary(SelectObjectinLibrary evt) => AddObject(evt.Prefab);
    private void OnObjectNameChanged(ChangeObjectNameSignal evt) { }
    private void OnExecuteCommand(ChangeObjectNameSignal evt) { }
    private void OnObjectsLoaded(LoadObjectsSignal evt) => UpdateHierarchy();
    private void OnLoadObjects(LoadObjectsSignal signal) => UpdateHierarchy();
    private void OnChangeNameProperty(ChangeNamePropertySignal signal) => UpdateHierarchy();
    #endregion
    #region Методы для работы с иерархией
    /// <summary>
    /// Добавить элемент в иерархию
    /// </summary>
    /// <param name="item">Ссылка на объект</param>
    /// <param name="parentId">Id родителя, не обязатлен</param>
    public void AddHierarchyItem(SceneObject item, string parentId = null)
    {
        if (item == null) return;
        // Проверяем, нет ли уже такого элемента в кэше (защита от дублирования)
        if (elementCache.ContainsKey(item.Id))
        {
            Debug.LogWarning($"Element with id {item.Id} already exists in hierarchy");
            return;
        }
        VisualElement parentElement = null;

        // Находим родительский элемент
        if (!string.IsNullOrEmpty(parentId))
        {
            parentElement = FindElementByUserIdCached(parentId);
        }

        // Если родитель не найден или parentId пустой, используем MainHierarchyItem
        if (parentElement == null)
        {
            parentElement = MainHierarchyItem;
        }

        // Добавляем элемент
        if (parentElement is CustomFoldout parentFoldout)
        {
            DrawSingleItem(item, parentFoldout);
        }
    }

    /// <summary>
    /// Нарисовать один элемент и его потомков
    /// </summary>
    /// <param name="item">Ссылка на объект</param>
    /// <param name="parent">Ссылка на родительский элемент, в котором размещать объект</param>
    private void DrawSingleItem(SceneObject item, CustomFoldout parent)
    {
        VisualElement element = null;

        switch (item.Type)
        {
            case ObjectType.Robot:
                element = new CustomFoldout { Text = item.Reference.name };
                element.name = "hierarchy-item-robot";
                element.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
                break;
            case ObjectType.Program:
                element = new CustomFoldout { Text = item.Reference.name };
                element.name = "hierarchy-item-program";
                element.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
                break;
            case ObjectType.LinearMoveCommand or ObjectType.StateEndEffectorCommand:
                element = CreateHierarchyElement("hierarchy-item-command", item.Reference.name, item.Id);
                break;
            default:
                element = CreateHierarchyElement("hierarchy-item", item.Reference.name, item.Id);
                break;
        }

        if (element != null)
        {

            element.userData = item.Id;

            CacheElement(element, item.Id);

            parent.AddContent(element);

            if (selectedElementId == item.Id)
            {
                SelectHierarchyItem(element);
            }

            var children = _sceneObjectManager.GetGameObjectsList()
                                            .Where(o => o.ParentId == item.Id && o.Reference.activeSelf);

            foreach (var child in children)
            {
                if (element is CustomFoldout fold)
                {
                    DrawSingleItem(child, fold);
                }
            }
        }
    }

    /// <summary>
    /// Удалить элемент из иерархии
    /// </summary>
    /// <param name="itemId">Id элемента</param>
    public void RemoveHierarchyItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        var element = FindElementByUserIdCached(itemId);
        if (element != null)
        {
            if (element.parent != null)
            {
                element.parent.Remove(element);
            }

            elementCache.Remove(itemId);

            if (selectedElementId == itemId)
            {
                ClearAllSelections();
                selectedElementId = null;
                lastSelectedElement = null;
                _eventBus.Invoke(new UnpickObjectSignal());
                //objectPicker.UnpickObject();
                _eventBus.Invoke(new HidePropertiesSignal());
                //propertiesPanelEvents.HidePanel();
            }

            RemoveChildrenFromCache(itemId);
        }
    }
    /// <summary>
    /// Удалить всех детей элемента из кэша
    /// </summary>
    /// <param name="parentId">Id родителя</param>
    private void RemoveChildrenFromCache(string parentId)
    {
        var childrenToRemove = new List<string>();

        foreach (var kvp in elementCache)
        {
            var sceneObject = _sceneObjectManager.GetById(kvp.Key);
            if (sceneObject != null && sceneObject.ParentId == parentId)
            {
                childrenToRemove.Add(kvp.Key);
            }
        }

        foreach (var childId in childrenToRemove)
        {
            elementCache.Remove(childId);
        }
    }

    /// <summary>
    /// Переименовать элемент в иерархии
    /// </summary>
    /// <param name="itemId">Id элемента</param>
    /// <param name="newName">Новое имя элемента</param>
    public void RenameHierarchyItem(string itemId, string newName)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        var element = FindElementByUserIdCached(itemId);
        if (element != null)
        {
            if (element is CustomFoldout foldout)
            {
                foldout.Text = newName;
            }
            else if (element is Label label)
            {
                label.text = newName;
            }
        }
    }
    #endregion
    #region Кэширование элементов
    /// <summary>
    /// Добавить элемент в кэш
    /// </summary>
    /// <param name="element">Ссылка на элемент</param>
    /// <param name="id">Id элемента</param>
    private void CacheElement(VisualElement element, string id)
    {
        if (!string.IsNullOrEmpty(id) && element != null)
        {
            elementCache[id] = element;
        }
    }

    /// <summary>
    /// Найти элемент по ID через кэш
    /// </summary>
    /// <param name="id">Id элемента</param>
    /// <returns></returns>
    private VisualElement FindElementByUserIdCached(string id)
    {
        if (elementCache.TryGetValue(id, out var element))
            return element;

        return null;
    }

    /// <summary>
    /// Найти элемент рекурсивно
    /// </summary>
    /// <param name="parent">Ссылка на элемент, в котором искать</param>
    /// <param name="id">Id искомого элемента</param>
    /// <returns></returns>
    private VisualElement FindElementByUserId(VisualElement parent, string id)
    {
        if (parent == null) return null;

        if (parent.userData?.ToString() == id)
            return parent;

        foreach (var child in parent.Children())
        {
            var found = FindElementByUserId(child, id);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// Очистить кэш
    /// </summary>
    private void ClearCache()
    {
        elementCache.Clear();
    }

    #endregion
    #region Работа с UI
    /// <summary>
    /// Нажатие внутри панели
    /// </summary>
    /// <param name="evt">Данные собтия мыши</param>
    private void OnMouseDownInsidePanel(MouseDownEvent evt)
    {
        if (evt.button == 1)
        {
            if (IsInsideHierarchyPanel(evt.target as VisualElement))
            {
                ShowContextMenu(evt.mousePosition, evt.target as VisualElement);
                evt.StopPropagation();
            }
            else
            {
                HideContextMenu();
            }
        }
        else
        {
            HideContextMenu();
        }
        evt.StopPropagation();
    }
    /// <summary>
    /// Нажатие на элемент иерархии
    /// </summary>
    /// <param name="evt">Данные события мыши</param>
    private void OnMouseDownHierarchyItem(MouseDownEvent evt)
    {
        if (evt.button == 0)
        {
            if (evt.target is VisualElement element)
            {
                evt.StopPropagation();
                if (element.name == "")
                {
                    element = GetFoldoutFromElement(element);
                }
                var gameObject = _sceneObjectManager.GetById(element.userData.ToString());
                if (gameObject != null)
                {
                    var objectId = element.userData?.ToString();
                    switch (gameObject.Type)
                    {
                        case ObjectType.Program:
                            // Для программ: рисуем маршрут
                            _eventBus.Invoke(new StartLineDrawer(objectId));
                            break;

                        case ObjectType.LinearMoveCommand:
                            if (!string.IsNullOrEmpty(element.userData.ToString()) &&
                                    _lineManager.IsCommandInCurrentProgram(objectId))
                            {
                                _eventBus.Invoke(new PickObjectSignal(gameObject.Reference));
                                //objectPicker.PickObject(gameObject.Reference);
                            }
                            else
                            {
                                //objectPicker.PickObject(gameObject.Reference);
                                _eventBus.Invoke(new PickObjectSignal(gameObject.Reference));
                                _eventBus.Invoke(new StopLineDrawer());
                            }
                            break;
                        case ObjectType.StateEndEffectorCommand:
                            break;
                        default:
                            // Для других типов (Robot и т.д.): только выделение
                            //objectPicker.PickObject(gameObject.Reference);
                            _eventBus.Invoke(new PickObjectSignal(gameObject.Reference));
                            _eventBus.Invoke(new StopLineDrawer());
                            break;
                    }
                    ShowProperties(element);
                    SelectHierarchyItem(element);
                }
            }
        }
    }
    /// <summary>
    /// Проверка находится ли мы сейчас внутри панели
    /// </summary>
    /// <param name="element">Ссылка на элемент</param>
    /// <returns></returns>
    private bool IsInsideHierarchyPanel(VisualElement element)
    {
        while (element != null)
        {
            if (element == hierarchyPanel)
                return true;
            element = element.parent;
        }
        return false;
    }
    /// <summary>
    /// Выбрать элемент иерархии по Id, сначла поиск в кэше, иначе находим рекурсивно в списке элементов
    /// </summary>
    /// <param name="id">ID элемента</param>
    private void SelectHierarchyItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        var element = FindElementByUserIdCached(id);

        if (element != null)
        {
            SelectHierarchyItem(element);
        }
        else
        {
            element = FindElementByUserId(MainHierarchyItem, id);
            if (element != null)
            {
                CacheElement(element, id);
                SelectHierarchyItem(element);
            }
            else
            {
                Debug.LogWarning($"Element with userId '{id}' not found in hierarchy");
            }
        }
    }
    /// <summary>
    /// Выбрать элемент иерархии по ссылке на элемент
    /// </summary>
    /// <param name="element">Ссылка на элемент</param>
    private void SelectHierarchyItem(VisualElement element)
    {
        ClearAllSelections();

        if (element != null)
        {
            if (element is CustomFoldout foldout)
            {
                foldout.SetSelected(true);
            }
            else
            {
                element.AddToClassList("selected");
            }
            selectedElementId = element.userData?.ToString();
            lastSelectedElement = element;
        }
        else
        {
            selectedElementId = null;
            lastSelectedElement = null;
        }
    }
    /// <summary>
    /// Очистить отмеченный элемент
    /// </summary>
    private void ClearAllSelections()
    {
        var allSelected = hierarchyPanel.Query<VisualElement>(className: "selected").ToList();
        foreach (var selected in allSelected)
        {
            selected.RemoveFromClassList("selected");
        }
    }
    /// <summary>
    /// Показать контестное меню
    /// </summary>
    /// <param name="position">Позиция, в котором появлистя меню</param>
    /// <param name="clickedElement">Ссылка на кликнутый элемент</param>
    private void ShowContextMenu(Vector2 position, VisualElement clickedElement)
    {
        if (contextMenu != null)
            root.Remove(contextMenu);

        contextMenu = new VisualElement();
        contextMenu.AddToClassList("context-menu-hierarchy-container");
        contextMenu.style.position = Position.Absolute;
        contextMenu.style.left = position.x;
        contextMenu.style.top = position.y;
        //contextMenu.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        //contextMenu.style.borderTopWidth = 1;
        //contextMenu.style.borderBottomWidth = 1;
        //contextMenu.style.borderLeftWidth = 1;
        //contextMenu.style.borderRightWidth = 1;
        //contextMenu.style.borderBottomColor = Color.black;
        //contextMenu.style.borderTopColor = Color.black;
        //contextMenu.style.borderLeftColor = Color.black;
        //contextMenu.style.borderRightColor = Color.black;
        contextMenu.style.flexDirection = FlexDirection.Column;
        //contextMenu.style.paddingTop = 2;
        //contextMenu.style.paddingBottom = 2;
        //contextMenu.style.paddingLeft = 4;
        //contextMenu.style.paddingRight = 4;

        if (clickedElement != null && clickedElement.name.Contains("hierarchy-item"))
        {
            contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
        }

        if (clickedElement.name == "")
        {
            var foldout = GetFoldoutFromElement(clickedElement);
            if (foldout != null)
            {
                if (foldout.name == "hierarchy-item-robot")
                {
                    var robot = _sceneObjectManager.GetById(foldout.userData.ToString());
                    contextMenu.Add(CreateMenuButton("Добавить линейное движение", () => CreatePoint(robot)));
                    contextMenu.Add(CreateMenuButton("Добавить состояние захвата", () => CreateStateEndEffector(robot)));
                    contextMenu.Add(CreateMenuButton("Добавить подпрограмму", () => CreateProgram(robot)));
                    contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
                }
                else if (foldout.name == "hierarchy-item-program")
                {
                    ;
                    var parentId = foldout.userData.ToString();
                    contextMenu.Add(CreateMenuButton("Добавить команду", () => CreatePoint(parentId)));
                    contextMenu.Add(CreateMenuButton("Добавить состояние захвата", () => CreateStateEndEffector(parentId)));
                    contextMenu.Add(CreateMenuButton("Добавить подпрограмму", () => CreateProgram(parentId)));
                    contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
                }
                else { contextMenu = null; return; }

            }

        }
        else if (clickedElement.name == "hierarchy-item-command")
        {

        }
        else
        {
            contextMenu.Add(CreateMenuButton("Добавить объект", CreateObject));
        }

        root.Add(contextMenu);
        iBlocker.AddNewContextMenu(contextMenu);
    }

    /// <summary>
    /// Создать точку по ссылке на робота
    /// </summary>
    /// <param name="robot">Ссылка на робота</param>
    private void CreateStateEndEffector(SceneObject robot)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Program/Задать состояние захвата");
        AddObject(prefab, robot.Id);
    }
    /// <summary>
    /// Создать программу по ID программы
    /// </summary>
    /// <param name="programId">ID программы</param>
    private void CreateStateEndEffector(string programId)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Program/Задать состояние захвата");
        AddObject(prefab, programId);
    }
    /// <summary>
    /// Создать точку по ID программы
    /// </summary>
    /// <param name="programId">ID программы</param>
    private void CreatePoint(string programId)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Program/Линейная точка");
        AddObject(prefab, programId);
    }
    /// <summary>
    /// Создать точку по ссылке на робота
    /// </summary>
    /// <param name="robot">Ссылка на робота</param>
    private void CreatePoint(SceneObject robot)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Program/Линейная точка");
        AddObject(prefab, robot.Id);
    }
    /// <summary>
    /// Создать программу по ID программы
    /// </summary>
    /// <param name="programId">ID программы</param>
    private void CreateProgram(string programId)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Program/Программа");
        AddObject(prefab, programId);
    }
    /// <summary>
    /// Создать программу по ссылке на робота
    /// </summary>
    /// <param name="robot">Ссылка на робота</param>
    private void CreateProgram(SceneObject robot)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Program/Программа");
        AddObject(prefab, robot.Id);
    }
    /// <summary>
    /// Создать кнопку в контестноем меню
    /// </summary>
    /// <param name="text">Текст кнопки</param>
    /// <param name="action">Действие, выполняемое кнокой</param>
    /// <returns></returns>
    private Button CreateMenuButton(string text, System.Action action)
    {
        var btn = new Button(() =>
        {
            action?.Invoke();
            HideContextMenu();
        });
        btn.text = text;
        btn.style.unityTextAlign = TextAnchor.MiddleLeft;
        btn.style.height = 20;
        //btn.style.width = 150;
        //btn.style.marginBottom = 2;
        return btn;
    }
    /// <summary>
    /// Спрятать контестное меню
    /// </summary>
    private void HideContextMenu()
    {
        if (contextMenu != null)
        {
            iBlocker.RemoveContextMenu(contextMenu);
            root.Remove(contextMenu);
            contextMenu = null;
            //iBlocker.ResolveUI();
        }
    }
    /// <summary>
    /// Открыть библиотеку оъхектов
    /// </summary>
    private void CreateObject()
    {
        _eventBus.Invoke(new ShowObjectsLibrarySignal());
        //objectsLibraryEvents.Show();
    }
    /// <summary>
    /// Добавить объект
    /// </summary>
    /// <param name="prefab">Ссылка на префаб объекта</param>
    /// <param name="parentId">Id родителя, необязателен</param>
    private void AddObject(GameObject prefab, string parentId = null)
    {
        if (gameObject == null)
            return;

        //objectPicker.UnpickObject();
        _eventBus.Invoke(new UnpickObjectSignal());
        var type = prefab.GetComponent<SceneObjectMarker>().type;
        var command = new AddObjectCommand(prefab, type, Vector3.zero, parentId);
        _undoRedoManager.Execute(command);
    }

    /// <summary>
    /// Обновить иерархию
    /// </summary>
    private void UpdateHierarchy()
    {
        ClearCache();
        MainHierarchyItem.ClearContent();
        var rootObjects = _sceneObjectManager.GetGameObjectsList()
                                         .Where(o => string.IsNullOrEmpty(o.ParentId));
        foreach (var item in rootObjects)
        {
            if (item.Reference.activeSelf == false) continue;

            DrawSingleItem(item, MainHierarchyItem);
            //DrawItemRecursive(item, MainHierarchyItem);
        }
        if (customScrollView != null)
        {
            customScrollView.schedule.Execute(() => customScrollView.Refresh()).ExecuteLater(100);
        }
    }

    /// <summary>
    /// Удалить объект
    /// </summary>
    /// <param name="clickedElement">Ссылка на кликнутый элемент</param>
    private void DeleteObject(VisualElement clickedElement)
    {
        if (clickedElement.name == "")
        {
            var foldout = GetFoldoutFromElement(clickedElement);
            if (foldout != null)
            {
                clickedElement = foldout;
            }
        }
        string id = (string)clickedElement.userData;
        var obj = _sceneObjectManager.GetById(clickedElement.userData.ToString()); //objectManager.GetObjectByUniqueID(id);

        if (obj == null)
            return;
        //objectPicker.UnpickObject();
        _eventBus.Invoke(new UnpickObjectSignal());
        _eventBus.Invoke(new HidePropertiesSignal());
        //propertiesPanelEvents.HidePanel();
        var command = new RemoveObjectCommand(obj);
        _undoRedoManager.Execute(command);
    }
    /// <summary>
    ///  Показать панель свойств
    /// </summary>
    /// <param name="clickedElement">Ссылка на кликнутый элемент</param>
    private void ShowProperties(VisualElement clickedElement)
    {
        var obj = _sceneObjectManager.GetById(clickedElement.userData.ToString()); //objectManager.GetObjectByUniqueID((int)clickedElement.userData);
        if (obj != null)
        {
            var provider = obj.Reference.TryGetComponent<IPropertyProvider>(out IPropertyProvider d);
            if (d != null)
            {
                _eventBus.Invoke(new ShowPropertiesSignal(d));
                //propertiesPanelEvents.ShowPanel();
                //propertiesPanelEvents.ShowProperties(d);
            }
        }
    }
    #endregion
    #region Вспомогательные методы
    /// <summary>
    /// Получить CustomFoldout их элемента
    /// </summary>
    /// <param name="element">Ссылка на элемент</param>
    /// <returns></returns>
    private CustomFoldout GetFoldoutFromElement(VisualElement element)
    {
        while (element != null && !(element is CustomFoldout))
        {
            element = element.parent;
        }
        return element as CustomFoldout;
    }

    /// <summary>
    /// Создать элемент иерархии
    /// </summary>
    /// <param name="elemName">Имя элемента</param>
    /// <param name="text">Отображаемый текст</param>
    /// <param name="id">Id элемента</param>
    /// <returns></returns>
    private VisualElement CreateHierarchyElement(string elemName, string text, string id)
    {
        var element = new Label(text);
        element.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
        element.name = elemName;
        element.style.color = new StyleColor(new Color(255, 255, 255));
        element.userData = id;
        element.style.fontSize = 12;
        element.style.height = 20;
        element.style.marginTop = 2;
        element.style.marginBottom = 2;
        element.style.paddingLeft = 10;
        return element;
    }

    /// <summary>
    /// Зарегистрировать элементы
    /// </summary>
    private void RegisterElements()
    {
        customScrollView = root.Q<CustomScrollView>("custom-scroll-view");
        MainHierarchyItem = root.Q<CustomFoldout>("main-item");
        root.RegisterCallback<MouseDownEvent>(OnMouseDownInsidePanel);
    }
    #endregion
}
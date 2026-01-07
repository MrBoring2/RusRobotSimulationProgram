using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.HierarhyPanel;
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
    private UIStatusManager _uIStatusManager;

    private DragDropData currentDragData;
    private VisualElement dragPreviewElement;
    private DropTargetInfo currentDropTarget;
    private const string DRAG_PREVIEW_CLASS = "drag-preview";
    private const string DROP_TARGET_ABOVE_CLASS = "drop-target-above";
    private const string DROP_TARGET_BELOW_CLASS = "drop-target-below";
    private const string DROP_TARGET_INSIDE_CLASS = "drop-target-inside";
    private const string DRAGGING_CLASS = "dragging";
    private bool isDragging = false;

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
        _eventBus.Subscribe<ToggleObjectsListSignal>(OnToggleObjectsList);
        _eventBus.Subscribe<UpdateHierarchySignal>(OnUpdateHierarhy);
        _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
        _lineManager = ServiceManager.Current.Get<LineManager>();
        _undoRedoManager = ServiceManager.Current.Get<UndoRedoManager>();
        _uIStatusManager = ServiceManager.Current.Get<UIStatusManager>();
        root = GetComponent<UIDocument>().rootVisualElement;
        hierarchyPanel = root.Q("hierarchy-container");
        //propertiesPanelEvents.OnTargetNameChanged += PropertiesPanelEvents_OnTargetNameChanged;
        //UndoRedoManager.Instance.OnCommandExecuted += Instance_OnCommandExecuted;
        //UndoRedoManager.Instance.OnCommandUndone += Instance_OnCommandUndone;
        RegisterElements();
        RegisterButtons();
        InitializeDragAndDrop();

        if (_sceneObjectManager.GetGameObjectsList().Count > 0)
        {
            UpdateHierarchy();
        }
        ToggleObjectsList();

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

    private void OnUpdateHierarhy(UpdateHierarchySignal signal) => UpdateHierarchy();

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
    private void OnToggleObjectsList(ToggleObjectsListSignal signal) => ToggleObjectsList();


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
    /// Переключить видимость панели
    /// </summary>
    private void ToggleObjectsList()
    {
        hierarchyPanel.style.display = _uIStatusManager.IsObjectsListVisible == true ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void RegisterButtons()
    {
        var closeBtn = root.Q<Button>("close-hierarhy-button");
        closeBtn.clicked += () =>
        {
            _uIStatusManager.ToggleObjectsListPanel();
        };
    }
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
                _uIStatusManager.SetPropertiesPanelVisibility(false);
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
        if (evt.button != 0) return;

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

                // Сохраняем информацию о потенциальном drag
                // Drag начнется только при движении мыши с зажатой кнопкой
                currentDragData = new DragDropData
                {
                    SourceId = gameObject.Id,
                    SourceElement = element,
                    SceneObject = gameObject,
                    StartPosition = evt.mousePosition
                };

                // Обрабатываем клик (выделение, свойства и т.д.)
                switch (gameObject.Type)
                {
                    case ObjectType.Program:
                        _eventBus.Invoke(new StartLineDrawer(objectId));
                        break;

                    case ObjectType.LinearMoveCommand:
                        if (!string.IsNullOrEmpty(element.userData.ToString()) &&
                                _lineManager.IsCommandInCurrentProgram(objectId))
                        {
                            _eventBus.Invoke(new PickObjectSignal(gameObject.Reference));
                        }
                        else
                        {
                            _eventBus.Invoke(new PickObjectSignal(gameObject.Reference));
                            _eventBus.Invoke(new StopLineDrawer());
                        }
                        break;
                    case ObjectType.StateEndEffectorCommand:
                        break;
                    default:
                        _eventBus.Invoke(new PickObjectSignal(gameObject.Reference));
                        _eventBus.Invoke(new StopLineDrawer());
                        break;
                }
                ShowProperties(element);
                SelectHierarchyItem(element);
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
            if (element.name == "menu") return false;
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
        _eventBus.Invoke(new ChangePropertiesProviderSignal(null));
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
                _eventBus.Invoke(new ChangePropertiesProviderSignal(d));
                //propertiesPanelEvents.ShowPanel();
                //propertiesPanelEvents.ShowProperties(d);
            }
        }
    }
    #endregion
    #region Drag & Drop
    private void InitializeDragAndDrop()
    {
        hierarchyPanel.RegisterCallback<MouseMoveEvent>(OnMouseMoveForDrag);
        hierarchyPanel.RegisterCallback<MouseUpEvent>(OnMouseUpForDrag);
        hierarchyPanel.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);

        dragPreviewElement = new VisualElement();
        dragPreviewElement.AddToClassList(DRAG_PREVIEW_CLASS);
        dragPreviewElement.style.display = DisplayStyle.None;
        dragPreviewElement.pickingMode = PickingMode.Ignore;
        root.Add(dragPreviewElement);
        dragPreviewElement.style.position = Position.Absolute;
    }

    private void OnMouseMoveForDrag(MouseMoveEvent evt)
    {
        if (evt.pressedButtons == 1 && currentDragData != null && !isDragging)
        {
            float dragThreshold = 10f;
            if (Vector2.Distance(currentDragData.StartPosition, evt.mousePosition) > dragThreshold)
            {
                isDragging = true;
                currentDragData.SourceElement.AddToClassList(DRAGGING_CLASS);
                UpdateDragPreview(evt.mousePosition);
                hierarchyPanel.CaptureMouse();
                evt.StopPropagation();
            }
        }
        else if (isDragging && currentDragData != null)
        {
            UpdateDragPreview(evt.mousePosition);
            var dropTarget = FindDropTarget(evt.mousePosition);
            UpdateDropIndicators(dropTarget);
            evt.StopPropagation();
        }
    }

    private void OnMouseUpForDrag(MouseUpEvent evt)
    {
        if (evt.button != 0) return;

        if (isDragging && currentDragData != null)
        {
            if (currentDropTarget != null)
            {
                HandleDrop();
            }
            CleanupDrag();
            if (hierarchyPanel.HasMouseCapture())
            {
                hierarchyPanel.ReleaseMouse();
            }
            evt.StopPropagation();
        }
        else
        {
            currentDragData = null;
        }
    }

    private void OnMouseCaptureOut(MouseCaptureOutEvent evt)
    {
        if (isDragging)
        {
            CleanupDrag();
        }
    }

    private void UpdateDragPreview(Vector2 position)
    {
        if (currentDragData == null || currentDragData.SourceElement == null) return;

        var element = currentDragData.SourceElement;
        float elementWidth = element.resolvedStyle.width > 0 ? element.resolvedStyle.width : 150f;
        float elementHeight = element.resolvedStyle.height > 0 ? element.resolvedStyle.height : 24f;

        dragPreviewElement.style.width = elementWidth;
        dragPreviewElement.style.height = elementHeight;
        dragPreviewElement.Clear();

        string text = element is CustomFoldout foldout ? foldout.Text :
                      (element is Label label ? label.text : element.name);

        var content = new Label(text);
        content.style.color = Color.white;
        content.style.unityTextAlign = TextAnchor.MiddleLeft;
        content.style.paddingLeft = 10;
        content.style.paddingTop = 4;
        content.style.fontSize = 12;
        dragPreviewElement.Add(content);

        dragPreviewElement.style.left = position.x + 15f;
        dragPreviewElement.style.top = position.y + 15f;
        dragPreviewElement.style.display = DisplayStyle.Flex;
    }

    private DropTargetInfo FindDropTarget(Vector2 position)
    {
        var allElements = GetHierarchyElementsInOrder();
        DropTargetInfo bestTarget = null;
        float minDistance = float.MaxValue;

        var panelWorldBounds = hierarchyPanel.worldBound;
        float localPosX = position.x - panelWorldBounds.x;
        float localPosY = position.y - panelWorldBounds.y;
        var localPos = new Vector2(localPosX, localPosY);

        // Проверяем возможность дропа в начало
        if (allElements.Count > 0 && CanDropInRoot(currentDragData.SceneObject))
        {
            var firstElement = allElements[0];
            var firstBounds = firstElement.worldBound;
            var firstLocalY = firstBounds.y - panelWorldBounds.y;

            if (localPos.y < firstLocalY - 10)
            {
                var distance = Mathf.Abs(localPos.y - firstLocalY);
                bestTarget = new DropTargetInfo
                {
                    TargetElement = firstElement,
                    Position = DropPosition.Above,
                    Distance = distance,
                    IsBeforeFirst = true
                };
                minDistance = distance;
            }
        }

        foreach (var element in allElements)
        {
            if (element == currentDragData.SourceElement) continue;

            var sceneObject = GetSceneObjectFromElement(element);
            if (sceneObject == null) continue;

            if (!CanDropOnTarget(currentDragData.SceneObject, sceneObject))
                continue;

            var elementWorldBounds = element.worldBound;
            var elementLocalBounds = new Rect(
                elementWorldBounds.x - panelWorldBounds.x,
                elementWorldBounds.y - panelWorldBounds.y,
                elementWorldBounds.width,
                elementWorldBounds.height
            );

            var dropInfo = CalculateDropPosition(element, elementLocalBounds, localPos);
            if (dropInfo != null && dropInfo.Distance < minDistance)
            {
                minDistance = dropInfo.Distance;
                bestTarget = dropInfo;
            }
        }

        // Проверяем возможность дропа в конец
        if (allElements.Count > 0 && CanDropInRoot(currentDragData.SceneObject) && bestTarget == null)
        {
            var lastElement = allElements[allElements.Count - 1];
            var lastBounds = lastElement.worldBound;
            var lastLocalY = lastBounds.y - panelWorldBounds.y + lastBounds.height;

            if (localPos.y > lastLocalY + 10)
            {
                bestTarget = new DropTargetInfo
                {
                    TargetElement = lastElement,
                    Position = DropPosition.Below,
                    Distance = Mathf.Abs(localPos.y - lastLocalY)
                };
            }
        }

        return bestTarget;
    }
    private VisualElement FindDraggableElement(VisualElement element)
    {
        while (element != null && element != hierarchyPanel)
        {
            if (element.name.Contains("hierarchy-item"))
            {
                return element;
            }
            element = element.parent;
        }
        return null;
    }

    private SceneObject GetSceneObjectFromElement(VisualElement element)
    {
        if (element?.userData == null) return null;
        return _sceneObjectManager.GetById(element.userData.ToString());
    }

    private bool CanBeDragged(SceneObject sceneObject)
    {
        if (sceneObject.Type == ObjectType.Primitive)
            return true;

        if (sceneObject.Type == ObjectType.Robot)
            return true;

        if (sceneObject.Type == ObjectType.Program ||
            sceneObject.Type == ObjectType.LinearMoveCommand ||
            sceneObject.Type == ObjectType.StateEndEffectorCommand)
        {
            return GetRobotParent(sceneObject) != null;
        }

        return false;
    }

    private SceneObject GetRobotParent(SceneObject sceneObject)
    {
        var current = sceneObject;
        while (current != null && !string.IsNullOrEmpty(current.ParentId))
        {
            var parent = _sceneObjectManager.GetById(current.ParentId);
            if (parent == null) return null;

            if (parent.Type == ObjectType.Robot)
                return parent;

            current = parent;
        }
        return null;
    }
    private List<VisualElement> GetHierarchyElementsInOrder()
    {
        var result = new List<VisualElement>();

        var roots = _sceneObjectManager.GetGameObjectsList()
            .Where(o => string.IsNullOrEmpty(o.ParentId))
            .OrderBy(o => GetObjectIndex(o.Id))
            .ToList();

        foreach (var root in roots)
        {
            var rootElement = FindElementByUserIdCached(root.Id);
            if (rootElement == null) continue;

            result.Add(rootElement);

            AddChildrenRecursive(root.Id, result);
        }

        return result;
    }

    private void AddChildrenRecursive(string parentId, List<VisualElement> elements)
    {
        var children = _sceneObjectManager.GetGameObjectsList()
            .Where(o => o.ParentId == parentId)
            .OrderBy(o => GetObjectIndex(o.Id))
            .ToList();

        foreach (var child in children)
        {
            var element = FindElementByUserIdCached(child.Id);
            if (element != null)
            {
                elements.Add(element);
                // Рекурсивно добавляем детей если это foldout и он раскрыт
                AddChildrenRecursive(child.Id, elements);
            }
        }
    }

    private DropTargetInfo CalculateDropPosition(VisualElement element, Rect bounds, Vector2 localPos)
    {
        float height = bounds.height;
        float topZone = bounds.y + height * 0.25f;
        float bottomZone = bounds.y + height * 0.75f;

        if (localPos.y < topZone)
        {
            return new DropTargetInfo
            {
                TargetElement = element,
                Position = DropPosition.Above,
                Distance = Mathf.Abs(localPos.y - bounds.y)
            };
        }

        if (localPos.y > bottomZone)
        {
            return new DropTargetInfo
            {
                TargetElement = element,
                Position = DropPosition.Below,
                Distance = Mathf.Abs(localPos.y - (bounds.y + bounds.height))
            };
        }

        // ⛔ Inside — только если реально можно дропать
        var targetObject = GetSceneObjectFromElement(element);
        if (targetObject != null &&
            (targetObject.Type == ObjectType.Robot ||
             targetObject.Type == ObjectType.Program))
        {
            return new DropTargetInfo
            {
                TargetElement = element,
                Position = DropPosition.Inside,
                Distance = 0
            };
        }

        return null;
    }

    private bool CanDropOnTarget(SceneObject draggedObject, SceneObject targetObject)
    {
        if (draggedObject.Id == targetObject.Id || IsChildOf(draggedObject.Id, targetObject.Id))
            return false;

        // Проверяем, находится ли targetObject в корне
        bool isTargetInRoot = string.IsNullOrEmpty(targetObject.ParentId);

        switch (draggedObject.Type)
        {
            case ObjectType.Primitive:
                if (targetObject.Type == ObjectType.Primitive)
                    return true;

                if (isTargetInRoot)
                    return true;

                return false;

            case ObjectType.Robot:
                // Роботов можно дропать только в корень
                return isTargetInRoot;

            case ObjectType.Program:
                // Программы можно дропать в роботов или в другие программы
                return targetObject.Type == ObjectType.Robot ||
                       targetObject.Type == ObjectType.Program;

            case ObjectType.LinearMoveCommand:
            case ObjectType.StateEndEffectorCommand:
                // Команды можно дропать в роботов или программы
                return targetObject.Type == ObjectType.Robot ||
                       targetObject.Type == ObjectType.Program;

            default:
                return false;
        }
    }

    private bool CanDropInRoot(SceneObject draggedObject)
    {
        // В корень можно дропать только примитивы и роботов
        return draggedObject.Type == ObjectType.Primitive ||
               draggedObject.Type == ObjectType.Robot;
    }

    private bool IsChildOf(string potentialChildId, string potentialParentId)
    {
        var current = _sceneObjectManager.GetById(potentialChildId);
        while (current != null && !string.IsNullOrEmpty(current.ParentId))
        {
            if (current.ParentId == potentialParentId)
                return true;
            current = _sceneObjectManager.GetById(current.ParentId);
        }
        return false;
    }

    private void UpdateDropIndicators(DropTargetInfo dropTarget)
    {
        ClearDropIndicators();

        currentDropTarget = dropTarget;
        if (dropTarget == null) return;

        switch (dropTarget.Position)
        {
            case DropPosition.Above:
                dropTarget.TargetElement.AddToClassList(DROP_TARGET_ABOVE_CLASS);
                break;
            case DropPosition.Below:
                dropTarget.TargetElement.AddToClassList(DROP_TARGET_BELOW_CLASS);
                break;
            case DropPosition.Inside:
                dropTarget.TargetElement.AddToClassList(DROP_TARGET_INSIDE_CLASS);
                break;
        }
    }

    private void ClearDropIndicators()
    {
        if (currentDropTarget?.TargetElement != null)
        {
            currentDropTarget.TargetElement.RemoveFromClassList(DROP_TARGET_ABOVE_CLASS);
            currentDropTarget.TargetElement.RemoveFromClassList(DROP_TARGET_BELOW_CLASS);
            currentDropTarget.TargetElement.RemoveFromClassList(DROP_TARGET_INSIDE_CLASS);
        }
        currentDropTarget = null;
    }

    private void HandleDrop()
    {
        if (currentDragData == null || currentDropTarget == null) return;

        var draggedObject = currentDragData.SceneObject;
        var targetElement = currentDropTarget.TargetElement;
        var targetObject = GetSceneObjectFromElement(targetElement);
        if (targetObject != null && targetObject.Id == draggedObject.Id)
        {
            CleanupDrag();
            return;
        }

        string newParentId = null;
        int? insertAtIndex = null;

        if (currentDropTarget.IsBeforeFirst)
        {
            newParentId = null;
            insertAtIndex = 0;
        }
        else if (currentDropTarget.Position == DropPosition.Above)
        {
            // ⬅️ Команда над программой = корень робота
            if ((draggedObject.Type == ObjectType.LinearMoveCommand ||
                 draggedObject.Type == ObjectType.StateEndEffectorCommand) &&
                targetObject.Type == ObjectType.Program)
            {
                var robot = _sceneObjectManager.GetById(targetObject.ParentId);
                if (robot != null && robot.Type == ObjectType.Robot)
                {
                    newParentId = robot.Id;
                    insertAtIndex = GetObjectIndex(targetObject.Id);
                }
                else
                {
                    newParentId = targetObject.ParentId;
                    insertAtIndex = GetObjectIndex(targetObject.Id);
                }
            }
            else
            {
                newParentId = targetObject?.ParentId;
                insertAtIndex = GetObjectIndex(targetObject.Id);
            }
        }
        else if (currentDropTarget.Position == DropPosition.Below)
        {
            if ((draggedObject.Type == ObjectType.LinearMoveCommand ||
                 draggedObject.Type == ObjectType.StateEndEffectorCommand) &&
                targetObject.Type == ObjectType.Program)
            {
                var robot = _sceneObjectManager.GetById(targetObject.ParentId);
                if (robot != null && robot.Type == ObjectType.Robot)
                {
                    newParentId = robot.Id;
                    insertAtIndex = GetObjectIndex(targetObject.Id) + 1;
                }
                else
                {
                    newParentId = targetObject.ParentId;
                    insertAtIndex = GetObjectIndex(targetObject.Id) + 1;
                }
            }
            else
            {
                newParentId = targetObject?.ParentId;
                insertAtIndex = GetObjectIndex(targetObject.Id) + 1;
            }
        }
        else if (currentDropTarget.Position == DropPosition.Inside)
        {
            // Внутрь можно только роботов и программ
            if (targetObject.Type == ObjectType.Robot || targetObject.Type == ObjectType.Program)
            {
                newParentId = targetObject.Id;
                var children = GetDirectChildren(targetObject.Id);
                if (children.Count > 0)
                {
                    var lastChild = children.OrderByDescending(c => GetObjectIndex(c.Id)).First();
                    insertAtIndex = GetObjectIndex(lastChild.Id) + 1;
                }
                else
                {
                    insertAtIndex = GetObjectIndex(targetObject.Id) + 1;
                }
            }
            else
            {
                // Если пытаемся дропнуть внутрь примитива - просто отменяем
                CleanupDrag();
                return;
            }
        }

        // Проверяем, не то же самое ли место
        if (newParentId == draggedObject.ParentId &&
            GetObjectIndex(draggedObject.Id) == insertAtIndex)
        {
            CleanupDrag();
            return;
        }

        if ((draggedObject.Type == ObjectType.Program ||
             draggedObject.Type == ObjectType.LinearMoveCommand ||
             draggedObject.Type == ObjectType.StateEndEffectorCommand) &&
            string.IsNullOrEmpty(newParentId))
        {
            CleanupDrag();
            return;
        }

        var oldIndex = GetObjectIndex(draggedObject.Id);

        if (insertAtIndex.HasValue && insertAtIndex.Value > oldIndex)
        {
            insertAtIndex--;
        }
        // Выполняем команду
        var command = new ChangeParentCommand(draggedObject.Id, newParentId, insertAtIndex);
        _undoRedoManager.Execute(command);
    }

    private List<SceneObject> GetDirectChildren(string parentId)
    {
        return _sceneObjectManager.GetGameObjectsList()
            .Where(o => o.ParentId == parentId)
            .ToList();
    }

    private int GetObjectIndex(string objectId)
    {
        if (string.IsNullOrEmpty(objectId)) return -1;

        var items = _sceneObjectManager.Items;
        int index = 0;

        foreach (System.Collections.DictionaryEntry entry in items)
        {
            if (entry.Key?.ToString() == objectId)
            {
                return index;
            }
            index++;
        }

        return -1;
    }

    private void CleanupDrag()
    {
        ClearDropIndicators();

        if (currentDragData?.SourceElement != null)
        {
            currentDragData.SourceElement.RemoveFromClassList(DRAGGING_CLASS);
        }

        if (dragPreviewElement != null)
        {
            dragPreviewElement.style.display = DisplayStyle.None;
        }

        currentDragData = null;
        isDragging = false;
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
using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.HierarhyPanel;
using Assets.Scripts.CustomEventBus.Signals.Lines;
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomEventBus.Signals.ObjectsLibrary;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomEventBus.Signals.RobotPanel;
using Assets.Scripts.CustomEventBus.Signals.UndoRedoSystem;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.StageControlSystem.Models;
using Assets.UI.CustomElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class HierarchyPanelEvents : MonoBehaviour
{
    private EventBus _eventBus;
    private SceneObjectsManager _sceneObjectManager;
    private ModalWindowServiceManager _modalWindowServiceManager;
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
    private Dictionary<string, bool> expandedFoldouts = new Dictionary<string, bool>();
    private UndoRedoManager _undoRedoManager;
    private UIStatusManager _uIStatusManager;
    private SimulationManager _simulationManager;
    private NotificationSystemManager _notificationSystemManager;
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
        _notificationSystemManager = ServiceManager.Current.Get<NotificationSystemManager>();
        _modalWindowServiceManager = ServiceManager.Current.Get<ModalWindowServiceManager>();
        _eventBus.Subscribe<AddSceneObjectSignal>(OnObjectAdded);
        _eventBus.Subscribe<RemoveSceneObjectSignal>(OnObjectRemoved);
        _eventBus.Subscribe<ChangeObjectNameSignal>(OnObjectNameChanged);
        _eventBus.Subscribe<LoadObjectsSignal>(OnLoadObjects);
        // _eventBus.Subscribe<SelectObjectinLibrary>(OnObjectSelectedInLibrary);
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
        _simulationManager = ServiceManager.Current.Get<SimulationManager>();
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
        if (signal.Command is IDestructiveCommand || signal.Command is PropertyChangeCommand || signal.Command is CustomPropertyChangeCommand)
        {
            UpdateHierarchy();
            _eventBus.Invoke(new UpdateLineDrawer());
        }

    }

    private void OnCommandExecuted(ExecuteCommandSignal signal)
    {
        if (signal.Command is IDestructiveCommand || signal.Command is PropertyChangeCommand || signal.Command is CustomPropertyChangeCommand)
        {
            UpdateHierarchy();
            _eventBus.Invoke(new UpdateLineDrawer());
        }

    }
    private void OnToggleObjectsList(ToggleObjectsListSignal signal) => ToggleObjectsList();


    private void OnObjectAdded(AddSceneObjectSignal evt) => AddHierarchyItem(evt.GameObject);
    private void OnObjectRemoved(RemoveSceneObjectSignal evt)
    {
        if (evt.GameObject.Type == ObjectType.LinearMoveCommand)
        {
            _eventBus.Invoke(new UpdateLineDrawer());
        }
        RemoveHierarchyItem(evt.GameObject.Id);
    }
    private void OnObjectSelectedInLibrary(SelectObjectinLibrary evt) => AddObject(evt.Prefab, evt.ParentId);
    private void OnObjectNameChanged(ChangeObjectNameSignal evt)
    {

    }
    private void OnExecuteCommand(ChangeObjectNameSignal evt) { }
    private void OnObjectsLoaded(LoadObjectsSignal evt) => UpdateHierarchy();
    private void OnLoadObjects(LoadObjectsSignal signal) => UpdateHierarchy();
    private void OnChangeNameProperty(ChangeNamePropertySignal signal)
    {
        //var elem = FindElementByUserIdCached(signal.Id);
        //if (elem is CustomFoldout f)
        //{
        //    f.Text = signal.Name;
        //}
        //else if (elem is Label l)
        //{
        //    l.text = signal.Name;
        //}
        //UpdateHierarchy();
        //signal.Name
        //signal.Id
        //обновить имя
    }
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
        var closeBtn = hierarchyPanel.Q<Button>("close-hierarhy-button");
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

        if (item.Type == ObjectType.Program || item.Type != ObjectType.LinearMoveCommand ||
            item.Type != ObjectType.StateEndEffectorCommand || item.Type != ObjectType.WaitCommand)
            return;

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
            // Сохраняем текущее состояние foldout
            //bool wasExpanded = parentFoldout.IsExpanded;

            //// Раскрываем только если foldout был свернут
            //if (!wasExpanded)
            //{
            //    parentFoldout.SetExpanded(true);
            //    expandedFoldouts[parentFoldout.userData?.ToString() ?? "root"] = true;
            //}

            DrawSingleItem(item, parentFoldout, null, false);
        }
    }

    /// <summary>
    /// Нарисовать один элемент и его потомков
    /// </summary>
    /// <param name="item">Ссылка на объект</param>
    /// <param name="parent">Ссылка на родительский элемент, в котором размещать объект</param>
    private void DrawSingleItem(SceneObject item, CustomFoldout parent, Dictionary<string, bool> savedStates, bool restoreState = false)
    {
        VisualElement element = null;

        //if (!expandedFoldouts.ContainsKey(parent.userData.ToString()))
        //{
        //    expandedFoldouts.Add(parent.userData.ToString(), parent.IsExpanded);
        //}
        //else expandedFoldouts[parent.userData.ToString()] = parent.IsExpanded;
        //if (parent.userData != null)
        //{
        //    expandedFoldouts[parent.userData.ToString()] = parent.IsExpanded;
        //}
        var cachedElem = FindElementByUserIdCached(item.Id);
        if (cachedElem != null)
        {
            parent.AddChild(cachedElem);
            if (restoreState && cachedElem is CustomFoldout fold && savedStates != null && savedStates.ContainsKey(item.Id))
            {
                fold.SetExpanded(savedStates[item.Id]);
            }
            if (selectedElementId == item.Id)
            {
                SelectHierarchyItem(cachedElem);
            }

            var children = _sceneObjectManager.GetGameObjectsList()
                                            .Where(o => o.ParentId == item.Id);

            foreach (var child in children)
            {
                if (cachedElem is CustomFoldout foldout)
                {
                    DrawSingleItem(child, foldout, savedStates, restoreState);
                }
            }

            return;
        }
        Texture2D texture = null;
        switch (item.Type)
        {
            case ObjectType.Unknown:
                break;
            //case ObjectType.LinearMoveCommand:
            //    texture = Resources.Load<Texture2D>("Icons/icon_line_mode");
            //    break;
            //case ObjectType.StateEndEffectorCommand:
            //    texture = Resources.Load<Texture2D>("Icons/icon_grip");
            //    break;
            //case ObjectType.WaitCommand:
            //    texture = Resources.Load<Texture2D>("Icons/icon_wait");
            //    break;
            case ObjectType.Primitive:
                texture = Resources.Load<Texture2D>("Icons/icon_primitive");
                break;
            case ObjectType.Static:
                break;
            case ObjectType.Movement:
                texture = Resources.Load<Texture2D>("Icons/icon_movement");
                break;
            case ObjectType.PLC:
                texture = Resources.Load<Texture2D>("Icons/icon_plc");
                break;
            case ObjectType.Detectors:
                texture = Resources.Load<Texture2D>("Icons/icon_detector");
                break;
            //case ObjectType.Program:
            //    texture = Resources.Load<Texture2D>("Icons/icon_program");
            //    break;
            case ObjectType.Robot:
                texture = Resources.Load<Texture2D>("Icons/icon_robot");
                break;
            case ObjectType.Node:
                texture = Resources.Load<Texture2D>("Icons/icon_node");
                break;
            case ObjectType.Workpiece:
                texture = Resources.Load<Texture2D>("Icons/icon_workpiece");
                break;
            case ObjectType.Work:
                texture = Resources.Load<Texture2D>("Icons/icon_work");
                break;
            case ObjectType.Environment:
                texture = Resources.Load<Texture2D>("Icons/icon_environment");
                break;
            default:
                break;
        }
        switch (item.Type)
        {
            case ObjectType.PLC:
                element = CreateHierarchyElement("hierarchy-item-plc", item.Reference.name, item.Id, texture);
                element.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
                break;
            case ObjectType.Robot:
                element = CreateHierarchyElement("hierarchy-item-robot", item.Reference.name, item.Id, texture);
                element.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);

                break;
            //case ObjectType.Program:
            //    element = new CustomFoldout { Text = item.Reference.name };
            //    ((CustomFoldout)element).SetHeaderImage(texture);
            //    element.name = "hierarchy-item-program";
            //    element.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
            //    var programFoldout = (CustomFoldout)element;
            //    programFoldout.OnExpandedChanged += (isExpanded) =>
            //    {
            //        if (programFoldout.userData != null)
            //        {
            //            expandedFoldouts[programFoldout.userData.ToString()] = isExpanded;
            //        }
            //    };
            //    break;
            case ObjectType.Node:
                element = new CustomFoldout { Text = item.Reference.name };
                ((CustomFoldout)element).SetHeaderImage(texture);
                element.name = "hierarchy-item-node";
                element.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
                break;
            //case ObjectType.LinearMoveCommand or ObjectType.StateEndEffectorCommand or ObjectType.WaitCommand:
            //    element = CreateHierarchyElement("hierarchy-item-command", item.Reference.name, item.Id, texture);
            //    break;
            default:
                element = CreateHierarchyElement("hierarchy-item", item.Reference.name, item.Id, texture);
                element.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
                break;
        }

        if (element != null)
        {

            element.userData = item;
            CacheElement(element, item.Id);

            parent.AddChild(element);

            // При обновлении иерархии восстанавливаем состояние
            if (restoreState && savedStates != null && savedStates.ContainsKey(item.Id) && element is CustomFoldout newFold)
            {
                // При восстановлении - восстанавливаем сохраненное состояние
                newFold.SetExpanded(savedStates[item.Id]);
            }

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
                    DrawSingleItem(child, fold, savedStates, restoreState);
                }
            }
        }
    }
    private Dictionary<string, bool> SaveFoldoutStates()
    {
        var states = new Dictionary<string, bool>();

        foreach (var kvp in expandedFoldouts)
        {
            states[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in elementCache)
        {
            if (kvp.Value is CustomFoldout foldout && foldout.userData is SceneObject obj)
            {
                if (!states.ContainsKey(obj.Id))
                {
                    states[obj.Id] = foldout.IsExpanded;
                }
            }
        }

        return states;
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
            expandedFoldouts.Remove(itemId);
            if (selectedElementId == itemId)
            {
                ClearAllSelections();
                selectedElementId = null;
                lastSelectedElement = null;
                _eventBus.Invoke(new UnpickObjectSignal());
                _uIStatusManager.SetPropertiesPanelVisibility(false);
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
            //evt.StopPropagation();

            if (element.name == "" || element.name == "label-hierarchy" || element.name == "foldout-header")
            {
                element = GetParentElement(element);
            }

            var gameObject = element.userData as SceneObject;
            Debug.Log($"ID: {gameObject.Id} || PARENT: {gameObject.ParentId}");
            if (gameObject != null)
            {
                var obj = element.userData as SceneObject;
                var objectId = obj?.Id;

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
                            _eventBus.Invoke(new PickObjectSignal(gameObject));
                        }
                        else
                        {
                            _eventBus.Invoke(new PickObjectSignal(gameObject));
                            _eventBus.Invoke(new StopLineDrawer());
                        }
                        break;
                    case ObjectType.StateEndEffectorCommand or ObjectType.WaitCommand:
                        break;
                    default:
                        _eventBus.Invoke(new PickObjectSignal(gameObject));
                        _eventBus.Invoke(new StopLineDrawer());
                        break;
                }
                SelectHierarchyItem(element);
                ShowProperties(element);

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
        contextMenu.name = "context-menu";
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

        }

        if (clickedElement.name == "" || clickedElement.name == "foldout-header" || clickedElement.name == "label-hierarchy")
        {
            var foldout = GetParentElement(clickedElement);
            if (foldout != null)
            {
                if (foldout.name == "hierarchy-item-robot")
                {
                    var robot = foldout.userData as SceneObject;
                    //contextMenu.Add(CreateMenuButton("Добавить линейное движение", () => CreatePoint(robot)));
                    //contextMenu.Add(CreateMenuButton("Добавить состояние захвата", () => CreateStateEndEffector(robot)));
                    //contextMenu.Add(CreateMenuButton("Добавить ожидание", () => CreateWaitCommand(robot)));
                    //contextMenu.Add(CreateMenuButton("Добавить подпрограмму", () => CreateProgram(robot)));
                    contextMenu.Add(CreateMenuButton("Открыть редактор", () => OpenRobotPanel(robot.Id)));
                    contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
                }
                //else if (foldout.name == "hierarchy-item-program")
                //{
                //    var parentId = foldout.userData.ToString();
                //    contextMenu.Add(CreateMenuButton("Добавить команду", () => CreatePoint(parentId)));
                //    contextMenu.Add(CreateMenuButton("Добавить состояние захвата", () => CreateStateEndEffector(parentId)));
                //    contextMenu.Add(CreateMenuButton("Добавить ожидание", () => CreateWaitCommand(parentId)));
                //    contextMenu.Add(CreateMenuButton("Добавить подпрограмму", () => CreateProgram(parentId)));
                //    contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
                //}
                else if (foldout.name == "hierarchy-item-node")
                {
                    var obj = foldout.userData as SceneObject;
                    var parentId = obj?.Id;
                    contextMenu.Add(CreateMenuButton("Добавить объект", () => CreateObject(parentId)));
                    contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
                }
                else if (foldout.name == "hierarchy-item-plc")
                {
                    var obj = foldout.userData as SceneObject;
                    var parentId = obj?.Id;
                    contextMenu.Add(CreateMenuButton("Открыть редактор", () => OpenRobotPanel(parentId)));
                    contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
                }
                else if (foldout.name == "main-item")
                {
                    contextMenu.Add(CreateMenuButton("Добавить объект", () => CreateObject()));
                }
                else
                {
                    contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
                }

            }

        }
        //else if (clickedElement.name == "hierarchy-item-command")
        //{

        //}
        else
        {
            contextMenu.Add(CreateMenuButton("Добавить объект", () => CreateObject()));
        }

        root.Add(contextMenu);
        iBlocker.AddNewContextMenu(contextMenu);
    }

    private void OpenRobotPanel(string id)
    {
        ModalParameters parameters = new ModalParameters();
        parameters.Set("FileToOpen", id);
        _modalWindowServiceManager.ShowWindow<GameObject>("code-editor-window", "Библиотека объектов", parameters, (prefab) =>
        {
            //if (prefab != null)
            //    AddObject(prefab, parentId);
        });

        // _eventBus.Invoke(new OpenRobotPanelSignal());
    }

    //private void CreateWaitCommand(SceneObject robot)
    //{
    //    var prefab = Resources.Load<GameObject>("Prefabs/Program/Ожидание");
    //    AddObject(prefab, robot.Id);
    //}
    //private void CreateWaitCommand(string programId)
    //{
    //    var prefab = Resources.Load<GameObject>("Prefabs/Program/Ожидание");
    //    AddObject(prefab, programId);
    //}

    /// <summary>
    /// Создать точку по ссылке на робота
    /// </summary>
    /// <param name="robot">Ссылка на робота</param>
    //private void CreateStateEndEffector(SceneObject robot)
    //{
    //    var prefab = Resources.Load<GameObject>("Prefabs/Program/Задать состояние захвата");
    //    AddObject(prefab, robot.Id);
    //}
    /// <summary>
    /// Создать программу по ID программы
    /// </summary>
    /// <param name="programId">ID программы</param>
    //private void CreateStateEndEffector(string programId)
    //{
    //    var prefab = Resources.Load<GameObject>("Prefabs/Program/Задать состояние захвата");
    //    AddObject(prefab, programId);
    //}
    /// <summary>
    /// Создать точку по ID программы
    /// </summary>
    /// <param name="programId">ID программы</param>
    //private void CreatePoint(string programId)
    //{
    //    var prefab = Resources.Load<GameObject>("Prefabs/Program/Линейная точка");
    //    AddObject(prefab, programId);
    //}
    /// <summary>
    /// Создать точку по ссылке на робота
    /// </summary>
    /// <param name="robot">Ссылка на робота</param>
    //private void CreatePoint(SceneObject robot)
    //{
    //    var prefab = Resources.Load<GameObject>("Prefabs/Program/Линейная точка");
    //    AddObject(prefab, robot.Id);
    //}
    /// <summary>
    /// Создать программу по ID программы
    /// </summary>
    /// <param name="programId">ID программы</param>
    //private void CreateProgram(string programId)
    //{
    //    var prefab = Resources.Load<GameObject>("Prefabs/Program/Программа");
    //    AddObject(prefab, programId);
    //}
    /// <summary>
    /// Создать программу по ссылке на робота
    /// </summary>
    /// <param name="robot">Ссылка на робота</param>
    //private void CreateProgram(SceneObject robot)
    //{
    //    var prefab = Resources.Load<GameObject>("Prefabs/Program/Программа");
    //    AddObject(prefab, robot.Id);
    //}
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
    private void CreateObject(string parentId = null)
    {
        //AddObject(evt.Prefab, evt.ParentId);
        ModalParameters parameters = new ModalParameters();
        parameters.Set("currentParentObjectId", parentId);
        _modalWindowServiceManager.ShowWindow<GameObject>("objects-library-window", "Библиотека объектов", parameters, (prefab) =>
        {
            if (prefab != null)
                AddObject(prefab, parentId);
        });
        // _eventBus.Invoke(new ShowObjectsLibrarySignal(parentId));
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

        if (type == ObjectType.PLC && _sceneObjectManager.GetGameObjectsList().FirstOrDefault(p => p.Type == ObjectType.PLC) != null)
        {
            _notificationSystemManager.ShowWarning("ПЛК ячейки может быть только один");
            return;
        }

        var pos = Vector3.zero;
        var rot = Quaternion.identity;
        if (_simulationManager.GetModeSim().SimulationMode == MODE.JOG_MODE)
        {
            if (type == ObjectType.LinearMoveCommand)
            {
                var robot = FindParentRobot(parentId);
                var manipulator = FindChildByName(robot.Reference.transform, "JOG_Manipulator");
                var provider = manipulator.gameObject.GetComponent<JOGPropertyProvider>();
                pos = new Vector3(provider.GlobalPosition.x, provider.GlobalPosition.y, provider.GlobalPosition.z);
                var a = provider.GlobalRotationQ;
                rot = Quaternion.Euler(provider.Rotation);
                var b = rot.eulerAngles;
            }
        }
        var command = new AddObjectCommand(prefab, type, pos, rot, parentId);
        _undoRedoManager.Execute(command);
    }
    public Transform FindChildByName(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name)
                return child;

            var found = FindChildByName(child, name);
            if (found != null)
                return found;
        }

        return null;
    }
    private SceneObject FindParentRobot(string parentId)
    {
        SceneObject obj = null;
        while (obj?.Type != ObjectType.Robot)
        {
            obj = _sceneObjectManager.GetById(parentId);
            parentId = obj.ParentId;
        }
        return obj;
    }

    /// <summary>
    /// Обновить иерархию
    /// </summary>
    private void UpdateHierarchy()
    {
        var savedStates = SaveFoldoutStates();
        MainHierarchyItem.ClearContent(true);
        elementCache.Clear();
        var rootObjects = _sceneObjectManager.GetGameObjectsList()
                              .Where(o => string.IsNullOrEmpty(o.ParentId) && o.Type != ObjectType.Program &&
                              o.Type != ObjectType.LinearMoveCommand && o.Type != ObjectType.StateEndEffectorCommand &&
                              o.Type != ObjectType.WaitCommand);

        foreach (var item in rootObjects)
        {
            if (item.Reference.activeSelf == false) continue;

            DrawSingleItem(item, MainHierarchyItem, savedStates, true);
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
        if (clickedElement.name == "" || clickedElement.name == "label-hierarchy" || clickedElement.name == "foldout-header")
        {
            var foldout = GetParentElement(clickedElement);
            if (foldout != null)
            {
                clickedElement = foldout;
            }
        }
        var obj = clickedElement.userData as SceneObject;

        if (obj == null)
            return;
        //objectPicker.UnpickObject();
        _eventBus.Invoke(new UnpickObjectSignal());
        _eventBus.Invoke(new ChangePropertiesProviderSignal(null));
        _eventBus.Invoke(new RemoveSceneObjectSignal(obj));
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
        var obj = clickedElement.userData as SceneObject;
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
                //evt.StopPropagation();
            }
        }
        else if (isDragging && currentDragData != null)
        {
            UpdateDragPreview(evt.mousePosition);
            var dropTarget = FindDropTarget(evt.mousePosition);
            UpdateDropIndicators(dropTarget);
            //evt.StopPropagation();
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
            //evt.StopPropagation();
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
        string text = "";
        if (/*element.name == "hierarchy-item-command" || */element.name == "hierarchy-item")
        {
            text = element.Q<Label>("label-hierarchy").text;
        }
        else
            text = element is CustomFoldout foldout ? foldout.Text :
                          (element is Label label ? label.text : element.name);

        var content = new Label(text);
        content.style.color = Color.white;
        content.style.unityTextAlign = TextAnchor.MiddleLeft;
        content.style.paddingLeft = 10;
        content.style.paddingTop = 4;
        content.style.fontSize = 12;
        dragPreviewElement.style.opacity = 0.85f;
        dragPreviewElement.style.backgroundColor = new Color(0, 0, 0, 0.75f);
        dragPreviewElement.style.borderBottomLeftRadius = 4;
        dragPreviewElement.style.borderBottomRightRadius = 4;
        dragPreviewElement.style.borderTopLeftRadius = 4;
        dragPreviewElement.style.borderTopRightRadius = 4;
        dragPreviewElement.style.height = 18;
        dragPreviewElement.Add(content);

        dragPreviewElement.style.left = position.x + 15f;
        dragPreviewElement.style.top = position.y + 15f;
        dragPreviewElement.style.display = DisplayStyle.Flex;
    }

    private DropTargetInfo FindDropTarget(Vector2 position)
    {
        var allElements = GetHierarchyElementsInOrder();
        //var draggedRobot = GetRobotParent(currentDragData.SceneObject);

        allElements = allElements
         .Where(e => GetSceneObjectFromElement(e) != null)
         .ToList();
        DropTargetInfo bestTarget = null;
        float minDistance = float.MaxValue;

        var panelWorldBounds = hierarchyPanel.worldBound;
        float localPosX = position.x - panelWorldBounds.x;
        float localPosY = position.y - panelWorldBounds.y;
        var localPos = new Vector2(localPosX, localPosY);

        foreach (var element in allElements)
        {
            if (element == currentDragData?.SourceElement) continue;

            var sceneObject = GetSceneObjectFromElement(element);
            if (sceneObject == null) continue;

            if (!CanDropOnTarget(element)) continue;

            var elementBounds = GetElementBounds(element, panelWorldBounds);
            var dropInfo = CalculateDropPosition(element, elementBounds, localPos);
            if (dropInfo != null && dropInfo.Distance < minDistance)
            {
                minDistance = dropInfo.Distance;
                bestTarget = dropInfo;
            }
        }

        return bestTarget;
    }

    private Rect GetElementBounds(VisualElement element, Rect panelWorldBounds)
    {
        Rect bounds;
        if (element is CustomFoldout foldout && foldout.Header != null)
        {
            var headerBounds = foldout.Header.worldBound;
            bounds = new Rect(
                headerBounds.x - panelWorldBounds.x,
                headerBounds.y - panelWorldBounds.y,
                headerBounds.width,
                headerBounds.height
            );
        }
        else
        {
            var wb = element.worldBound;
            bounds = new Rect(
                wb.x - panelWorldBounds.x,
                wb.y - panelWorldBounds.y,
                wb.width,
                wb.height
            );
        }
        return bounds;
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
        return element?.userData as SceneObject;
    }

    private bool CanBeDragged(SceneObject sceneObject)
    {
        if (IsRootOnlyType(sceneObject))
            return true;

        //if (IsProgramOrCommand(sceneObject))
        //    return GetRobotParent(sceneObject) != null;

        return false;
    }

    private SceneObject GetRobotParent(SceneObject sceneObject)
    {
        var current = sceneObject;
        while (current != null)
        {
            if (current.Type == ObjectType.Robot)
                return current;

            if (string.IsNullOrEmpty(current.ParentId))
                return null;

            current = _sceneObjectManager.GetById(current.ParentId);
        }
        return null;
    }


    private bool IsRootOnlyType(SceneObject obj)
    {
        return obj.Type == ObjectType.Robot;
    }

    //private bool IsProgramOrCommand(SceneObject obj)
    //{
    //    return obj.Type == ObjectType.Program
    //        || obj.Type == ObjectType.LinearMoveCommand
    //        || obj.Type == ObjectType.StateEndEffectorCommand
    //        || obj.Type == ObjectType.WaitCommand;
    //}

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
       .OrderBy(o => GetSiblingIndex(o.Id, parentId))  // ← Используем новый метод
       .ToList();

        foreach (var child in children)
        {
            var element = FindElementByUserIdCached(child.Id);
            if (element != null)
            {
                elements.Add(element);
                AddChildrenRecursive(child.Id, elements);
            }
        }
    }

    private DropTargetInfo CalculateDropPosition(VisualElement element, Rect bounds, Vector2 localPos)
    {
        float h = bounds.height;
        float top = bounds.y + h * 0.25f;
        float bottom = bounds.y + h * 0.75f;

        var target = GetSceneObjectFromElement(element);
        var dragged = currentDragData.SceneObject;

        if (localPos.y < top)
        {
            return new DropTargetInfo
            {
                TargetElement = element,
                Position = DropPosition.Above,
                Distance = Mathf.Abs(localPos.y - bounds.y)
            };
        }
        else if (localPos.y > bottom)
        {
            return new DropTargetInfo
            {
                TargetElement = element,
                Position = DropPosition.Below,
                Distance = Mathf.Abs(localPos.y - (bounds.y + bounds.height))
            };
        }
        else
        {
            // Разрешаем Inside для всех объектов
            if (element is CustomFoldout)
            {
                return new DropTargetInfo
                {
                    TargetElement = element,
                    Position = DropPosition.Inside,
                    Distance = 0
                };
            }
        }

        return null;
    }
    private bool CanDropOnTarget(VisualElement targetElement)
    {
        if (currentDragData == null) return false;
        if (currentDragData.SceneObject == null) return false;

        var target = GetSceneObjectFromElement(targetElement);
        if (target == null) return false;
        if (currentDragData.SceneObject.Id == target.Id) return false;
        if (IsChildOf(target.Id, currentDragData.SceneObject.Id)) return false;

        return true;
    }


    private bool CanDropInRoot(SceneObject draggedObject)
    {
        return IsRootOnlyType(draggedObject);
    }

    private bool IsChildOf(string potentialChildId, string potentialParentId)
    {
        // Если проверяем относительно корня (нет родителя)
        if (string.IsNullOrEmpty(potentialParentId))
            return false;

        // Если потенциальный ребенок null - проверка невозможна
        if (string.IsNullOrEmpty(potentialChildId))
            return false;

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
        var targetObject = GetSceneObjectFromElement(currentDropTarget.TargetElement);

        if (targetObject == null || draggedObject == null)
        {
            CleanupDrag();
            return;
        }

        if (targetObject.Id == draggedObject.Id)
        {
            CleanupDrag();
            return;
        }

        string newParentId = null;
        int? insertAtIndex = null;

        switch (currentDropTarget.Position)
        {
            case DropPosition.Above:
                newParentId = targetObject.ParentId;
                insertAtIndex = GetSiblingIndex(targetObject.Id, targetObject.ParentId);
                // Если dragged выше target в том же родителе, индекс не меняется
                if (draggedObject.ParentId == targetObject.ParentId &&
                    GetSiblingIndex(draggedObject.Id, draggedObject.ParentId) < insertAtIndex)
                {
                    // dragged удалится, target сдвинется вверх, поэтому индекс правильный
                }
                break;

            case DropPosition.Below:
                newParentId = targetObject.ParentId;
                insertAtIndex = GetSiblingIndex(targetObject.Id, targetObject.ParentId) + 1;
                // Если dragged выше target в том же родителе, после удаления dragged индекс уменьшится на 1
                if (draggedObject.ParentId == targetObject.ParentId &&
                    GetSiblingIndex(draggedObject.Id, draggedObject.ParentId) < GetSiblingIndex(targetObject.Id, targetObject.ParentId))
                {
                    insertAtIndex--;
                }
                break;

            case DropPosition.Inside:
                newParentId = targetObject.Id;
                insertAtIndex = 0;
                break;
        }

        if (!string.IsNullOrEmpty(newParentId) && IsChildOf(newParentId, draggedObject.Id))
        {
            CleanupDrag();
            return;
        }

        var command = new ChangeParentCommand(draggedObject.Id, newParentId, insertAtIndex);
        _undoRedoManager.Execute(command);
        CleanupDrag();
    }

    private int GetSiblingIndexInParent(SceneObject sceneObject)
    {
        if (sceneObject == null) return 0;

        var siblings = _sceneObjectManager.GetGameObjectsList()
            .Where(o => o.ParentId == sceneObject.ParentId)
            .ToList();

        for (int i = 0; i < siblings.Count; i++)
        {
            if (siblings[i].Id == sceneObject.Id)
            {
                return i;
            }
        }

        return 0;
    }

    private int GetSiblingIndex(string objectId, string parentId)
    {
        if (string.IsNullOrEmpty(objectId)) return -1;

        var siblings = _sceneObjectManager.GetGameObjectsList()
            .Where(o => o.ParentId == parentId)
            .ToList();

        for (int i = 0; i < siblings.Count; i++)
        {
            if (siblings[i].Id == objectId)
            {
                return i;
            }
        }

        return 0;
    }
    private int GetRootObjectIndex(string objectId)
    {
        var rootObjects = GetRootObjects();
        return GetSiblingIndex(objectId, null);
    }

    private List<SceneObject> GetRootObjects()
    {
        return _sceneObjectManager.GetGameObjectsList()
            .Where(o => string.IsNullOrEmpty(o.ParentId))
            .ToList();
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
    private VisualElement GetParentElement(VisualElement element)
    {
        while (element != null)
        {
            if (element is CustomFoldout)
            {
                return element;
            }

            else if (element.name == "hierarchy-item-command" || element.name == "hierarchy-item" || element.name == "hierarchy-item-robot" || element.name == "hierarchy-item-plc") 
            {
                return element;
            }
            element = element.parent;
        }
        return element;
    }

    /// <summary>
    /// Создать элемент иерархии
    /// </summary>
    /// <param name="elemName">Имя элемента</param>
    /// <param name="text">Отображаемый текст</param>
    /// <param name="id">Id элемента</param>
    /// <returns></returns>
    private VisualElement CreateHierarchyElement(string elemName, string text, string id, Texture2D texture)
    {
        // Создаем контейнер для элемента
        var container = new VisualElement();
        container.AddToClassList("hierarchy-item-container-base");
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;
        container.name = elemName;
        container.userData = id;

        // Добавляем обработчик клика на весь контейнер
        container.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);

        // Добавляем картинку, если указан путь
        var imageElement = CreateImageElement(texture);
        if (imageElement != null)
        {
            container.Add(imageElement);
        }


        // Создаем текстовый элемент
        var label = new Label(text);
        label.name = "label-hierarchy";
        label.style.color = new StyleColor(new Color(255, 255, 255));
        label.style.fontSize = 12;

        label.style.unityTextAlign = TextAnchor.MiddleLeft;
        label.style.flexGrow = 1;

        // Добавляем текст в контейнер
        container.Add(label);
        container.style.height = 20;
        container.style.marginTop = 2;
        container.style.marginBottom = 2;
        container.style.marginLeft = 8;

        return container;
        //var element = new Label(text);
        //element.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
        //element.name = elemName;
        //element.style.color = new StyleColor(new Color(255, 255, 255));
        //element.userData = id;
        //element.style.fontSize = 12;
        //element.style.height = 20;
        //element.style.marginTop = 2;
        //element.style.marginBottom = 2;
        //element.style.paddingLeft = 10;
        //return element;
    }

    private VisualElement CreateImageElement(Texture2D texture, int width = 16, int height = 16)
    {

        if (texture == null)
        {
            return null;
        }

        // Создаем элемент для изображения
        var imageElement = new Image();
        imageElement.image = texture;
        imageElement.style.width = width;
        imageElement.style.height = height;
        imageElement.style.marginRight = 8;
        imageElement.style.paddingLeft = 4;

        return imageElement;
    }

    /// <summary>
    /// Зарегистрировать элементы
    /// </summary>
    private void RegisterElements()
    {
        customScrollView = hierarchyPanel.Q<CustomScrollView>("custom-scroll-view");
        MainHierarchyItem = hierarchyPanel.Q<CustomFoldout>("main-item");
        root.RegisterCallback<MouseDownEvent>(OnMouseDownInsidePanel);
        MainHierarchyItem.userData = Guid.NewGuid().ToString();
        MainHierarchyItem.SetExpanded(true);
        expandedFoldouts.Add(MainHierarchyItem.userData.ToString(), MainHierarchyItem.IsExpanded);
        MainHierarchyItem.OnExpandedChanged += (isExpanded) =>
        {
            if (MainHierarchyItem.userData != null)
            {
                expandedFoldouts[MainHierarchyItem.userData.ToString()] = isExpanded;
            }
        };
    }
    #endregion
}
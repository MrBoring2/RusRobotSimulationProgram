using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.HierarhyPanel;
using Assets.Scripts.CustomEventBus.Signals.Lines;
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomEventBus.Signals.ObjectsLibrary;
using Assets.Scripts.CustomEventBus.Signals.PLC;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomEventBus.Signals.RobotPanel;
using Assets.Scripts.CustomEventBus.Signals.UndoRedoSystem;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using Assets.UI.CustomElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class HierarchyCommandsPanelEvents : MonoBehaviour
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
        private IPropertyProvider current;
        public UIBlocker iBlocker;
        //public PropertiesPanelEvents propertiesPanelEvents;
        //public ObjectPicker objectPicker;
        //public ObjectsLibraryEvents objectsLibraryEvents;
        private string selectedElementId;
        private VisualElement lastSelectedElement;
        private CustomFoldout header;
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
            _modalWindowServiceManager = ServiceManager.Current.Get<ModalWindowServiceManager>();
            //_eventBus.Subscribe<AddSceneObjectSignal>(OnObjectAdded);
            _eventBus.Subscribe<RemoveSceneObjectSignal>(OnObjectRemoved);
            //_eventBus.Subscribe<ChangeObjectNameSignal>(OnObjectNameChanged);
            //_eventBus.Subscribe<LoadObjectsSignal>(OnLoadObjects);
            _eventBus.Subscribe<ChangePropertiesProviderSignal>(OnChangePropertiesProvider);
            //_eventBus.Subscribe<PLCChangeExpressionSignal>(OnChangeExpression);
            //_eventBus.Subscribe<PLCSelectProgramPanelSignal>(OnSelectProgram);
            //_eventBus.Subscribe<SelectObjectinLibrary>(OnObjectSelectedInLibrary);
            _eventBus.Subscribe<SelectObjectInScene>(OnObjectSelectedInScene);
            _eventBus.Subscribe<ChangeNamePropertySignal>(OnChangeNameProperty);
            _eventBus.Subscribe<ExecuteCommandSignal>(OnCommandExecuted);
            _eventBus.Subscribe<UndoneCommandSignal>(OnCommandUndoned);
            _eventBus.Subscribe<ToggleCommandsListSignal>(OnToggleCommandsList);
            //_eventBus.Subscribe<UpdateHierarchySignal>(OnUpdateHierarhy);
            _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
            _lineManager = ServiceManager.Current.Get<LineManager>();
            _undoRedoManager = ServiceManager.Current.Get<UndoRedoManager>();
            _uIStatusManager = ServiceManager.Current.Get<UIStatusManager>();
            _simulationManager = ServiceManager.Current.Get<SimulationManager>();
            root = GetComponent<UIDocument>().rootVisualElement;
            hierarchyPanel = root.Q("hierarchy-commands-container");
            header = hierarchyPanel.Q<CustomFoldout>("main-item");
            //propertiesPanelEvents.OnTargetNameChanged += PropertiesPanelEvents_OnTargetNameChanged;
            //UndoRedoManager.Instance.OnCommandExecuted += Instance_OnCommandExecuted;
            //UndoRedoManager.Instance.OnCommandUndone += Instance_OnCommandUndone;
            RegisterElements();
            RegisterButtons();
            InitializeDragAndDrop();

            if (_sceneObjectManager.GetGameObjectsList().Count > 0)
            {
                //UpdateHierarchy();
            }
            ToggleCommandsList();
            UpdateTitle();
        }

        private void OnSelectProgram(PLCSelectProgramPanelSignal signal)
        {
            AddProgramInPLC(signal.BlockId, signal.Program);
        }



        private void OnChangeExpression(PLCChangeExpressionSignal signal)
        {
            AddExpression(signal.ParentId, signal.Expression);
        }


        private void OnChangePropertiesProvider(ChangePropertiesProviderSignal signal)
        {
            if (signal.PropertyProvider != null)
            {
                ObjectType type = ObjectType.Unknown;
                if (signal.PropertyProvider is LinearPointPropertyProvider ||
                    signal.PropertyProvider is WaitPropertyProvider ||
                    signal.PropertyProvider is StateEndEffectorPropertyProvider ||
                    signal.PropertyProvider is RobotProgramPropertyProvider)
                {
                    type = ObjectType.LinearMoveCommand;
                }
                else if (signal.PropertyProvider is JOGPropertyProvider s)
                {
                    type = _sceneObjectManager.GetById(s.RobotPropertyProvider.Id).Type;
                }
                else type = _sceneObjectManager.GetById(signal.PropertyProvider.Id).Type;

                if (type == ObjectType.Robot)
                {
                    if (signal.PropertyProvider is JOGPropertyProvider s)
                        current = s.RobotPropertyProvider;
                    else current = signal.PropertyProvider;
                    MainHierarchyItem.userData = current.Id;
                    UpdateHierarchy();
                }
                else if (type == ObjectType.PLC)
                {
                    current = signal.PropertyProvider;
                    MainHierarchyItem.userData = current.Id;
                    UpdateHierarchy();
                }
                else if (type == ObjectType.LinearMoveCommand) { }
                else
                {
                    current = null;
                    MainHierarchyItem.ClearContent(true);
                    elementCache.Clear();
                }
            }
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            if (current == null)
            {
                header.Text = "Не выбрано";
            }
            else header.Text = current.Name;
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
            if (signal.Command is IDestructiveCommand || signal.Command is PropertyChangeCommand)
            {
                UpdateHierarchy();
                UpdateTitle();
                _eventBus.Invoke(new UpdateLineDrawer());
            }

        }

        private void OnCommandExecuted(ExecuteCommandSignal signal)
        {
            if (signal.Command is IDestructiveCommand || signal.Command is PropertyChangeCommand)
            {
                UpdateHierarchy();
                UpdateTitle();
                _eventBus.Invoke(new UpdateLineDrawer());
            }

        }
        private void OnToggleCommandsList(ToggleCommandsListSignal signal) => ToggleCommandsList();


        private void OnObjectAdded(AddSceneObjectSignal evt) => AddHierarchyItem(evt.GameObject);
        private void OnObjectRemoved(RemoveSceneObjectSignal evt)
        {
            if (evt.GameObject.Type == ObjectType.Robot)
            {
                current = null;
                UpdateTitle();
            }
            //RemoveHierarchyItem(evt.GameObject.Id);
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
            if (_sceneObjectManager.GetById(signal.Id) == null || _sceneObjectManager.GetById(signal.Id).Type == ObjectType.Robot)
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
                UpdateHierarchy();
                UpdateTitle();
                //signal.Name
                //signal.Id
                //обновить имя
            }
        }
        #endregion
        #region Методы для работы с иерархией

        /// <summary>
        /// Переключить видимость панели
        /// </summary>
        private void ToggleCommandsList()
        {
            hierarchyPanel.style.display = _uIStatusManager.IsCommandsListVisible == true ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RegisterButtons()
        {
            var closeBtn = hierarchyPanel.Q<Button>("close-hierarhy-button");
            closeBtn.clicked += () =>
            {
                _uIStatusManager.ToggleCommandsListPanel();
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

                //var children = _sceneObjectManager.GetGameObjectsList()
                //                                .Where(o => o.ParentId == item.Id);
                ObjectType childType = item.Type;
                List<SceneObject> children = new List<SceneObject>();
                if (childType == ObjectType.Program)
                {
                    var commands = _sceneObjectManager.Commands.GetCommandsFromSubProgram(current.Id, item.Id);
                    foreach (var command in commands)
                    {
                        if (cachedElem is CustomFoldout foldout)
                        {
                            DrawSingleItem(command, foldout, savedStates, restoreState);
                        }
                    }
                }
            }
            Texture2D texture = null;
            switch (item.Type)
            {
                case ObjectType.Unknown:
                    break;
                case ObjectType.LinearMoveCommand:
                    texture = Resources.Load<Texture2D>("Icons/icon_line_mode");
                    break;
                case ObjectType.StateEndEffectorCommand:
                    texture = Resources.Load<Texture2D>("Icons/icon_grip");
                    break;
                case ObjectType.WaitCommand:
                    texture = Resources.Load<Texture2D>("Icons/icon_wait");
                    break;
                case ObjectType.Program:
                    texture = Resources.Load<Texture2D>("Icons/icon_program");
                    break;
                default:
                    break;
            }
            switch (item.Type)
            {
                case ObjectType.Program:
                    element = new CustomFoldout { Text = item.Reference.name };
                    ((CustomFoldout)element).SetHeaderImage(texture);
                    element.name = "hierarchy-item-program";
                    element.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
                    var programFoldout = (CustomFoldout)element;
                    programFoldout.OnExpandedChanged += (isExpanded) =>
                    {
                        if (programFoldout.userData != null)
                        {
                            expandedFoldouts[programFoldout.userData.ToString()] = isExpanded;
                        }
                    };
                    break;
                case ObjectType.LinearMoveCommand or ObjectType.StateEndEffectorCommand or ObjectType.WaitCommand:
                    element = CreateHierarchyElement("hierarchy-item-command", item.Reference.name, item.Id, texture);
                    break;
                default:
                    element = CreateHierarchyElement("hierarchy-item", item.Reference.name, item.Id, texture);
                    break;
            }

            if (element != null)
            {

                element.userData = item.Id;

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


                ObjectType childType = item.Type;
                List<SceneObject> children = new List<SceneObject>();
                if (childType == ObjectType.Program)
                {
                    var commands = _sceneObjectManager.Commands.GetCommandsFromSubProgram(current.Id, item.Id);
                    foreach (var command in commands)
                    {
                        if (element is CustomFoldout foldout)
                        {
                            DrawSingleItem(command, foldout, savedStates, restoreState);
                        }
                    }
                }
            }
        }
        private Dictionary<string, bool> SaveFoldoutStates()
        {
            var states = new Dictionary<string, bool>();

            // Сохраняем состояние из словаря и проверяем актуальное состояние
            foreach (var kvp in expandedFoldouts)
            {
                states[kvp.Key] = kvp.Value;
            }

            // Дополнительная проверка актуального состояния элементов
            foreach (var kvp in elementCache)
            {
                if (kvp.Value is CustomFoldout foldout && foldout.userData != null)
                {
                    string id = foldout.userData.ToString();
                    if (!states.ContainsKey(id))
                    {
                        states[id] = foldout.IsExpanded;
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
                    //evt.StopPropagation();
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
            //evt.StopPropagation();
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

                var gameObject = _sceneObjectManager.Commands.FindElementById(element.userData.ToString());

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

        private void ShowProgramSetWindow(string programBlock, string robotId)
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Set("robotId", robotId);
            _modalWindowServiceManager.ShowWindow<RobotProgramObject>("program-set-window", "Выбор программы робота", parameters, (program) =>
            {
                if (program != null)
                    AddProgramInPLC(programBlock, program);
            });
        }
        private void ShowExpressionWindow(string parentId, bool isElseIf = false, string startExpression = "")
        {
            ModalParameters parameters = new ModalParameters();
            parameters.Set("expression", startExpression);
            parameters.Set("currentParentObjectId", parentId);
            _modalWindowServiceManager.ShowWindow<string>("condition-window", "Добавление условия", parameters, (expression) =>
            {
                if (expression != null)
                {
                    if (!isElseIf)
                    {
                        if (startExpression != "")
                            UpdateConditionExpression(parentId, expression);
                        else AddExpression(parentId, expression);
                    }

                    else
                    {
                        if (startExpression != "")
                            UpdateElseIfExpression(parentId, expression);
                        else AddElseIfExpression(parentId, expression);
                    }
                }
            });
        }



        /// <summary>
        /// Показать контестное меню
        /// </summary>
        /// <param name="position">Позиция, в котором появлистя меню</param>
        /// <param name="clickedElement">Ссылка на кликнутый элемент</param>
        private void ShowContextMenu(Vector2 position, VisualElement clickedElement)
        {
            if (current == null) return;
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

            }

            if (clickedElement.name == "" || clickedElement.name == "foldout-header" || clickedElement.name == "label-hierarchy")
            {
                var foldout = GetParentElement(clickedElement);
                if (foldout != null)
                {
                    //if (foldout.name == "hierarchy-item-robot")
                    //{
                    //    var robot = _sceneObjectManager.GetById(foldout.userData.ToString());
                    //    contextMenu.Add(CreateMenuButton("Добавить линейное движение", () => CreatePoint(robot)));
                    //    contextMenu.Add(CreateMenuButton("Добавить состояние захвата", () => CreateStateEndEffector(robot)));
                    //    contextMenu.Add(CreateMenuButton("Добавить ожидание", () => CreateWaitCommand(robot)));
                    //    contextMenu.Add(CreateMenuButton("Добавить подпрограмму", () => CreateProgram(robot)));
                    //    contextMenu.Add(CreateMenuButton("Открыть планшет робота", () => OpenRobotPanel()));
                    //    contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
                    //}
                    if (foldout.name == "hierarchy-item-program")
                    {
                        var parentId = foldout.userData.ToString();
                        contextMenu.Add(CreateMenuButton("Добавить линейное движение", () => CreatePoint(parentId)));
                        contextMenu.Add(CreateMenuButton("Добавить состояние захвата", () => CreateStateEndEffector(parentId)));
                        contextMenu.Add(CreateMenuButton("Добавить ожидание", () => CreateWaitCommand(parentId)));
                        contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
                    }
                    //else if (foldout.name == "hierarchy-item-node")
                    //{
                    //    var parentId = foldout.userData.ToString();
                    //    contextMenu.Add(CreateMenuButton("Добавить объект", () => CreateObject(parentId)));
                    //    contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
                    //}
                    else if (foldout.name == "main-item")
                    {
                        if (current == null)
                        {
                            contextMenu = null;
                            return;
                        }

                        if (!(current is RobotPropertyProvider))
                        {
                            contextMenu = null;
                            return;
                        }

                        var robot = _sceneObjectManager.GetById(MainHierarchyItem.userData.ToString());
                        contextMenu.Add(CreateMenuButton("Добавить задачу", () => CreateProgram(robot.Id)));
                    }
                    else if (foldout.name == "plc-init-block")
                    {
                        contextMenu.Add(CreateMenuButton("Добавить переменную", () => Debug.Log("Добавить переменную в инициализацию")));
                    }
                    else if (foldout.name == "plc-logic-block")
                    {
                        var parentId = foldout.userData.ToString();
                        contextMenu.Add(CreateMenuButton("Добавить условие", () => ShowExpressionWindow(parentId)));
                    }
                    else if (foldout.name == "plc-robot-block")
                    {
                        var parentId = foldout.userData.ToString();
                        contextMenu.Add(CreateMenuButton("Добавить условие", () => ShowExpressionWindow(parentId)));
                    }
                    else if (foldout.name == "plc-condition-block")
                    {
                        var parentId = foldout.userData.ToString();
                        contextMenu.Add(CreateMenuButton("Добавить иначе если", () => ShowExpressionWindow(parentId, true)));
                        contextMenu.Add(CreateMenuButton("Удалить условие", () => DeletePLCBlockCondition(parentId)));
                    }
                    else if (foldout.name == "plc-if-block")
                    {
                        var parentId = foldout.userData.ToString();
                        var condition = GetConditionById(parentId);
                        string robotId = GetRobotIdFromPLCBlock(foldout);
                        contextMenu.Add(CreateMenuButton("Изменить условие", () => ShowExpressionWindow(parentId, false, condition.Expression)));
                        contextMenu.Add(CreateMenuButton("Добавить вложенное условие", () => ShowExpressionWindow(parentId)));
                        contextMenu.Add(CreateMenuButton("Добавить задачу робота", () =>
                        {
                            if (!HasProgramCallInCondition(parentId))
                            {
                                ShowProgramSetWindow(parentId, robotId);
                            }
                        }));
                    }
                    else if (foldout.name == "plc-elif-block")
                    {
                        var parentId = foldout.userData.ToString();
                        var condition = GetConditionById(parentId);
                        string robotId = GetRobotIdFromPLCBlock(foldout);
                        contextMenu.Add(CreateMenuButton("Изменить условие", () => ShowExpressionWindow(parentId, true, condition.Expression)));
                        contextMenu.Add(CreateMenuButton("Добавить вложенное условие", () => ShowExpressionWindow(parentId)));
                        contextMenu.Add(CreateMenuButton("Добавить задачу робота", () =>
                        {
                            if (!HasProgramCallInCondition(parentId))
                            {
                                ShowProgramSetWindow(parentId, robotId);
                            }
                        }));
                        contextMenu.Add(CreateMenuButton("Удалить блок иначе если", () => DeleteELIFCondition(parentId)));
                    }
                    else if (foldout.name == "plc-else-block")
                    {
                        var parentId = foldout.userData.ToString();
                        string robotId = GetRobotIdFromPLCBlock(foldout);
                        contextMenu.Add(CreateMenuButton("Добавить вложенное условие", () => ShowExpressionWindow(parentId)));
                        contextMenu.Add(CreateMenuButton("Добавить задачу робота", () =>
                        {
                            if (!HasProgramCallInCondition(parentId))
                            {
                                ShowProgramSetWindow(parentId, robotId);
                            }
                        }));
                    }
                    else if (foldout.name == "plc-command")
                    {
                        var parentId = foldout.userData.ToString();
                        contextMenu.Add(CreateMenuButton("Удалить команду", () => DeletePLCCommand(parentId)));
                    }
                    //else
                    //{
                    //    contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
                    //}

                }
                lastSelectedElement = foldout;

            }
            else if (clickedElement.name == "hierarchy-item-command")
            {

            }
            else
            {
                if (current == null) return;
                var robot = _sceneObjectManager.GetById(MainHierarchyItem.userData.ToString());
                contextMenu.Add(CreateMenuButton("Добавить задачу", () => CreateProgram(robot.Id)));
            }

            root.Add(contextMenu);
            iBlocker.AddNewContextMenu(contextMenu);
        }

        private void UpdateConditionExpression(string conditionId, string newExpression)
        {
            var condition = GetConditionById(conditionId);
            if (condition != null)
            {
                condition.Expression = newExpression;
                UpdateHierarchy();
            }
        }

        private void UpdateElseIfExpression(string conditionId, string newExpression)
        {
            var condition = GetConditionById(conditionId);
            if (condition != null)
            {
                condition.Expression = newExpression;
                UpdateHierarchy();
            }
        }

        private PLCCondition GetConditionById(string id)
        {
            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                var found = FindConditionInList(rb.ConditionsList, id);
                if (found != null) return found;
            }
            return FindConditionInList(_sceneObjectManager.PLCData.LogicBlockItems, id);
        }

        private bool HasProgramCallInCondition(string conditionId)
        {
            PLCCondition targetCondition = null;

            // Поиск в роботах
            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                targetCondition = FindConditionInList(rb.ConditionsList, conditionId);
                if (targetCondition != null) break;
            }

            // Поиск в логике
            if (targetCondition == null)
                targetCondition = FindConditionInList(_sceneObjectManager.PLCData.LogicBlockItems, conditionId);

            if (targetCondition == null) return false;

            // Проверяем наличие PLCStartProgram
            foreach (var item in targetCondition.Content)
            {
                if (item is PLCStartProgram)
                    return true;
            }

            return false;
        }

        private PLCCondition FindConditionInList(List<PLCBase> items, string id)
        {
            foreach (var item in items)
            {
                if (item is PLCBlockCondition block)
                {
                    if (block.IfCondition.Id == id) return block.IfCondition;
                    foreach (var elif in block.ElifConditions)
                        if (elif.Id == id) return elif;
                    if (block.ElseConndition != null && block.ElseConndition.Id == id)
                        return block.ElseConndition;

                    var found = FindConditionInList(block.IfCondition.Content, id);
                    if (found != null) return found;
                    foreach (var elif in block.ElifConditions)
                    {
                        found = FindConditionInList(elif.Content, id);
                        if (found != null) return found;
                    }
                    if (block.ElseConndition != null)
                    {
                        found = FindConditionInList(block.ElseConndition.Content, id);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }
        private void OpenRobotPanel()
        {
            _eventBus.Invoke(new OpenRobotPanelSignal());
        }

        private void CreateWaitCommand(SceneObject robot)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Program/Ожидание");
            AddObject(prefab, robot.Id);
        }
        private void CreateWaitCommand(string programId)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Program/Ожидание");
            AddObject(prefab, programId);
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
        //private void CreateProgram(string programId)
        //{
        //    var prefab = Resources.Load<GameObject>("Prefabs/Program/Программа");
        //    AddObject(prefab, programId);
        //}

        /// <summary>
        /// Создать задачу роботу
        /// </summary>
        /// <param name="robotId">Id робота</param>
        private void CreateTask(string robotId)
        {

        }


        /// <summary>
        /// Создать программу роботу
        /// </summary>
        /// <param name="robot">Id робота</param>
        private void CreateProgram(string robotId)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Program/Программа");
            AddObject(prefab, robotId);
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
        private void CreateObject(string parentId = null)
        {
            _eventBus.Invoke(new ShowObjectsLibrarySignal(parentId));
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
            var pos = Vector3.zero;
            var rot = Quaternion.identity;
            if (_simulationManager.GetModeSim() == MODE.JOG_MODE)
            {
                if (type == ObjectType.LinearMoveCommand)
                {
                    var robot = FindParentRobot(parentId);
                    var manipulator = FindChildByName(robot.Reference.transform, "JOG_Manipulator");
                    var provider = manipulator.gameObject.GetComponent<JOGPropertyProvider>();
                    pos = new Vector3(provider.GlobalPosition.x, provider.GlobalPosition.y, provider.GlobalPosition.z);
                    var a = provider.RotationQ;
                    rot = Quaternion.Euler(provider.Rotation);
                    var b = rot.eulerAngles;
                }
            }
            var command = new AddObjCommandCommand(prefab, type, pos, rot, parentId);
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
                if (obj == null)
                    obj = _sceneObjectManager.Commands.FindElementById(parentId);
                parentId = obj.ParentId;
            }
            return obj;
        }

        /// <summary>
        /// Обновить иерархию
        /// </summary>
        private void UpdateHierarchy()
        {
            if (current == null) return;
            var savedStates = SaveFoldoutStates();
            MainHierarchyItem.ClearContent(true);
            elementCache.Clear();

            ObjectType parentType = _sceneObjectManager.GetById(current.Id).Type;

            if (parentType == ObjectType.Robot)
            {
                var programs = _sceneObjectManager.Commands.GetSubPrograms(current.Id);
                foreach (var item in programs)
                {
                    if (item.Reference.activeSelf == false) continue;
                    DrawSingleItem(item, MainHierarchyItem, savedStates, true);
                }
            }
            else if (parentType == ObjectType.PLC)
            {
                DrawPLCBlocks();
            }

            if (customScrollView != null)
            {
                customScrollView.schedule.Execute(() => customScrollView.Refresh()).ExecuteLater(100);
            }

        }
        /// <summary>
        /// Получить ID робота из любого PLC блока, поднимаясь по иерархии
        /// </summary>
        private string GetRobotIdFromPLCBlock(VisualElement element)
        {
            var current = element;
            while (current != null)
            {
                // Ищем блок робота
                if (current.name == "plc-robot-block" && current.userData != null)
                {
                    return current.userData.ToString();
                }
                current = current.parent;
            }
            return null;
        }
        // Добавить команду (программу)
        private void AddProgramInPLC(string blockId, RobotProgramObject program)
        {
            var startProgram = new PLCStartProgram { ProgramName = program.PropertyProvider.Name, ProgramId = program.Id };
            AddToPLCContent(blockId, startProgram);
        }

        // Добавить условие
        private void AddExpression(string parentId, string expression)
        {
            var newCondition = new PLCCondition(ConditionType.If, expression);
            var newBlockCondition = new PLCBlockCondition(newCondition);
            AddToPLCContent(parentId, newBlockCondition);
        }
        private void AddElseIfExpression(string parentId, string expression)
        {
            var newElseIf = new PLCCondition(ConditionType.ElseIf, expression);

            PLCBlockCondition targetBlock = null;

            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                targetBlock = FindBlockConditionById(rb.ConditionsList, parentId);
                if (targetBlock != null) break;
            }

            if (targetBlock == null)
                targetBlock = FindBlockConditionById(_sceneObjectManager.PLCData.LogicBlockItems, parentId);

            if (targetBlock != null)
            {
                targetBlock.ElifConditions.Add(newElseIf);
                UpdateHierarchy();
            }
        }

        private PLCBlockCondition FindBlockConditionById(List<PLCBase> items, string id)
        {
            foreach (var item in items)
            {
                if (item is PLCBlockCondition block)
                {
                    if (block.Id == id) return block;

                    var found = FindBlockConditionById(block.IfCondition.Content, id);
                    if (found != null) return found;

                    foreach (var elif in block.ElifConditions)
                    {
                        found = FindBlockConditionById(elif.Content, id);
                        if (found != null) return found;
                    }

                    if (block.ElseConndition != null)
                    {
                        found = FindBlockConditionById(block.ElseConndition.Content, id);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Добавить элемент (условие или команду) в Content блока
        /// </summary>
        private void AddToPLCContent(string parentId, PLCBase itemToAdd)
        {
            if (string.IsNullOrEmpty(parentId))
            {
                UpdateHierarchy();
                return;
            }

            // 1. Проверяем в блоках роботов
            var robotBlock = _sceneObjectManager.PLCData.RobotCommandsBlockItems
                .FirstOrDefault(x => x.RobotId == parentId);
            if (robotBlock != null)
            {
                robotBlock.ConditionsList.Add(itemToAdd);
                UpdateHierarchy();
                return;
            }

            // 2. Проверяем в блоке логики
            var logicParent = _sceneObjectManager.PLCData.LogicBlockItems
                .FirstOrDefault(x => x is PLCBlockCondition && ((PLCBlockCondition)x).Id == parentId);
            if (logicParent != null)
            {
                _sceneObjectManager.PLCData.LogicBlockItems.Add(itemToAdd);
                UpdateHierarchy();
                return;
            }

            // 3. Рекурсивно ищем в условиях роботов
            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                if (TryAddToContent(rb.ConditionsList, parentId, itemToAdd))
                {
                    UpdateHierarchy();
                    return;
                }
            }

            // 4. Рекурсивно ищем в блоке логики
            if (TryAddToContent(_sceneObjectManager.PLCData.LogicBlockItems, parentId, itemToAdd))
            {
                UpdateHierarchy();
                return;
            }

            UpdateHierarchy();
        }

        /// <summary>
        /// Рекурсивный поиск и добавление элемента в Content нужного блока
        /// </summary>
        private bool TryAddToContent(List<PLCBase> items, string parentId, PLCBase itemToAdd)
        {
            foreach (var item in items)
            {
                if (item is PLCBlockCondition block)
                {
                    // Проверяем IF блок
                    if (block.IfCondition.Id == parentId)
                    {
                        block.IfCondition.Content.Add(itemToAdd);
                        return true;
                    }

                    // Проверяем ELSE IF блоки
                    foreach (var elif in block.ElifConditions)
                    {
                        if (elif.Id == parentId)
                        {
                            elif.Content.Add(itemToAdd);
                            return true;
                        }
                    }

                    // Проверяем ELSE блок
                    if (block.ElseConndition != null && block.ElseConndition.Id == parentId)
                    {
                        block.ElseConndition.Content.Add(itemToAdd);
                        return true;
                    }

                    // Рекурсивно ищем в IF блоке
                    if (TryAddToContent(block.IfCondition.Content, parentId, itemToAdd))
                        return true;

                    // Рекурсивно ищем в ELSE IF блоках
                    foreach (var elif in block.ElifConditions)
                    {
                        if (TryAddToContent(elif.Content, parentId, itemToAdd))
                            return true;
                    }

                    // Рекурсивно ищем в ELSE блоке
                    if (block.ElseConndition != null && TryAddToContent(block.ElseConndition.Content, parentId, itemToAdd))
                        return true;
                }
            }
            return false;
        }

        private void DeletePLCBlockCondition(string conditionId)
        {
            // Удаляем из блоков роботов
            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                if (RemoveConditionFromList(rb.ConditionsList, conditionId))
                {
                    UpdateHierarchy();
                    return;
                }
            }

            // Удаляем из логики
            if (RemoveConditionFromList(_sceneObjectManager.PLCData.LogicBlockItems, conditionId))
            {
                UpdateHierarchy();
                return;
            }

            UpdateHierarchy();
        }

        private bool RemoveConditionFromList(List<PLCBase> items, string conditionId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is PLCBlockCondition block && block.Id == conditionId)
                {
                    items.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private void DeletePLCCommand(string commandId)
        {
            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                if (RemoveCommandFromList(rb.ConditionsList, commandId))
                {
                    UpdateHierarchy();
                    return;
                }
            }

            if (RemoveCommandFromList(_sceneObjectManager.PLCData.LogicBlockItems, commandId))
            {
                UpdateHierarchy();
                return;
            }
        }

        private bool RemoveCommandFromList(List<PLCBase> items, string commandId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is PLCCommand cmd && cmd.Id == commandId)
                {
                    items.RemoveAt(i);
                    return true;
                }
            }

            foreach (var item in items)
            {
                if (item is PLCBlockCondition block)
                {
                    if (RemoveCommandFromList(block.IfCondition.Content, commandId)) return true;
                    foreach (var elif in block.ElifConditions)
                    {
                        if (RemoveCommandFromList(elif.Content, commandId)) return true;
                    }
                    if (block.ElseConndition != null && RemoveCommandFromList(block.ElseConndition.Content, commandId)) return true;
                }
            }
            return false;
        }

        private void DeleteELIFCondition(string elifId)
        {
            // Ищем и удаляем ELIF
            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                if (RemoveELIFFromList(rb.ConditionsList, elifId))
                {
                    UpdateHierarchy();
                    return;
                }
            }

            if (RemoveELIFFromList(_sceneObjectManager.PLCData.LogicBlockItems, elifId))
            {
                UpdateHierarchy();
                return;
            }

            UpdateHierarchy();
        }

        private bool RemoveELIFFromList(List<PLCBase> items, string elifId)
        {
            foreach (var item in items)
            {
                if (item is PLCBlockCondition block)
                {
                    for (int i = 0; i < block.ElifConditions.Count; i++)
                    {
                        if (block.ElifConditions[i].Id == elifId)
                        {
                            block.ElifConditions.RemoveAt(i);
                            return true;
                        }
                    }

                    // Рекурсивно ищем
                    if (RemoveELIFFromList(block.IfCondition.Content, elifId)) return true;
                    foreach (var elif in block.ElifConditions)
                    {
                        if (RemoveELIFFromList(elif.Content, elifId)) return true;
                    }
                    if (block.ElseConndition != null && RemoveELIFFromList(block.ElseConndition.Content, elifId)) return true;
                }
            }
            return false;
        }

        private void DrawPLCBlocks()
        {
            var initBlock = new CustomFoldout { Text = "Инициализация" };
            initBlock.userData = "init_block";
            initBlock.name = "plc-init-block";
            initBlock.AddToClassList("plc-init-block");
            RegisterExpanedFoldout(initBlock);

            foreach (var variable in _sceneObjectManager.PLCData.InitBlockItems)
            {
                var varElement = new VisualElement();
                varElement.name = "plc-var";
                varElement.userData = variable.Id;
                varElement.Add(new Label($"{variable.VariableName} = {variable.Value}"));
                initBlock.AddChild(varElement);
            }
            MainHierarchyItem.AddChild(initBlock);

            var robotsBlock = new CustomFoldout { Text = "Блоки роботов" };
            robotsBlock.userData = "robot_blocks";
            robotsBlock.name = "plc-robots-block";
            robotsBlock.AddToClassList("plc-robots-block");
            RegisterExpanedFoldout(robotsBlock);

            var allRobots = _sceneObjectManager.GetGameObjectsList()
                .Where(obj => obj.Type == ObjectType.Robot)
                .ToList();

            foreach (var robot in allRobots)
            {
                var robotBlock = new CustomFoldout { Text = robot.Reference.name };
                robotBlock.name = "plc-robot-block";
                robotBlock.userData = robot.Id;
                robotBlock.AddToClassList("plc-robot-block");
                RegisterExpanedFoldout(robotBlock);

                var robotData = _sceneObjectManager.PLCData.RobotCommandsBlockItems
                    .FirstOrDefault(r => r.RobotId == robot.Id);

                if (robotData != null)
                {
                    // Рекурсивно отрисовываем содержимое
                    foreach (var item in robotData.ConditionsList)
                    {
                        DrawPLCItemRecursive(item, robotBlock);
                    }
                }

                robotsBlock.AddChild(robotBlock);
            }

            MainHierarchyItem.AddChild(robotsBlock);

            var logicBlock = new CustomFoldout { Text = "Логика" };
            logicBlock.userData = "logic_block";
            logicBlock.name = "plc-logic-block";
            logicBlock.AddToClassList("plc-logic-block");
            RegisterExpanedFoldout(logicBlock);
            foreach (var item in _sceneObjectManager.PLCData.LogicBlockItems)
            {
                DrawPLCItemRecursive(item, logicBlock);
            }
            MainHierarchyItem.AddChild(logicBlock);
        }

        /// <summary>
        /// Рекурсивная отрисовка любого PLC элемента
        /// </summary>
        private void DrawPLCItemRecursive(PLCBase item, CustomFoldout parent)
        {
            if (item is PLCBlockCondition conditionBlock)
            {
                DrawConditionBlock(conditionBlock, parent);
            }
            else if (item is PLCCommand command)
            {
                DrawCommand(command, parent);
            }
        }
        /// <summary>
        /// Отрисовка блока условия (if/elif/else)
        /// </summary>
        private void DrawConditionBlock(PLCBlockCondition block, CustomFoldout parent)
        {
            var conditionFoldout = new CustomFoldout { Text = $"Блок условия" };
            conditionFoldout.name = "plc-condition-block";
            conditionFoldout.AddToClassList("plc-condition-block");
            conditionFoldout.userData = block.Id;
            RegisterExpanedFoldout(conditionFoldout); 
            // Отрисовка IF блока
            var ifFoldout = new CustomFoldout { Text = $"Если: {block.IfCondition.Expression}" };
            ifFoldout.name = "plc-if-block";
            ifFoldout.userData = block.IfCondition.Id;
            ifFoldout.AddToClassList("plc-if-block");
            RegisterExpanedFoldout(ifFoldout);

            foreach (var content in block.IfCondition.Content)
            {
                DrawPLCItemRecursive(content, ifFoldout);
            }
            conditionFoldout.AddChild(ifFoldout);

            // Отрисовка ELSE IF блоков
            foreach (var elif in block.ElifConditions)
            {
                var elifFoldout = new CustomFoldout { Text = $"Иначе если: {elif.Expression}" };
                elifFoldout.name = "plc-elif-block";
                elifFoldout.userData = elif.Id;
                elifFoldout.AddToClassList("plc-elif-block");
                RegisterExpanedFoldout(elifFoldout);
                foreach (var content in elif.Content)
                {
                    DrawPLCItemRecursive(content, elifFoldout);
                }
                conditionFoldout.AddChild(elifFoldout);
            }

            // Отрисовка ELSE блока
            if (block.ElseConndition != null)
            {
                var elseFoldout = new CustomFoldout { Text = $"Иначе: {block.ElseConndition.Expression}" };
                elseFoldout.name = "plc-else-block";
                elseFoldout.userData = block.ElseConndition.Id;
                elseFoldout.AddToClassList("plc-else-block");
                RegisterExpanedFoldout(elseFoldout);
                foreach (var content in block.ElseConndition.Content)
                {
                    DrawPLCItemRecursive(content, elseFoldout);
                }
                conditionFoldout.AddChild(elseFoldout);
            }

            parent.AddChild(conditionFoldout);
        }


        private void RegisterExpanedFoldout(CustomFoldout foldout)
        {
            foldout.OnExpandedChanged += (isExpanded) =>
            {
                if (foldout.userData != null)
                {
                    expandedFoldouts[foldout.userData.ToString()] = isExpanded;
                }
            };
            foldout.SetExpanded(expandedFoldouts.TryGetValue(foldout.userData.ToString(), out bool d));
        }

        /// <summary>
        /// Отрисовка команды
        /// </summary>
        private void DrawCommand(PLCCommand command, CustomFoldout parent)
        {
            var container = new VisualElement();
            container.AddToClassList("hierarchy-item-container-base");
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.AddToClassList("plc-command");
            container.name = "plc-command";
            container.userData = command.Id;
            //commandElement.AddToClassList("plc-command-item");

            string commandText = "";

            switch (command)
            {
                case PLCStartProgram start:
                    commandText = $"Запустить программу: {start.ProgramName}";
                    break;
                case PLCIncrement inc:
                    commandText = $"Инкремент: {inc.VariableName} += {inc.Step}";
                    break;
                case PLCDecrement dec:
                    commandText = $"Декремент: {dec.VariableName} -= {dec.Step}";
                    break;
                case PLCSetVariable set:
                    commandText = $"Присвоить: {set.VariableName} = {set.Value}";
                    break;
                default:
                    commandText = "Неизвестная команда";
                    break;
            }
            var label = new Label(commandText);
            label.name = "label-hierarchy";
            label.style.color = new StyleColor(new Color(255, 255, 255));
            label.style.fontSize = 12;

            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.flexGrow = 1;
            container.Add(label);
            container.style.height = 20;
            container.style.marginTop = 2;
            container.style.marginBottom = 2;
            container.style.marginLeft = 8;
            container.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);

            parent.AddChild(container);
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
            string id = (string)clickedElement.userData;
            var obj = _sceneObjectManager.Commands.FindElementById(clickedElement.userData.ToString()); //objectManager.GetObjectByUniqueID(id);

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
            if (obj == null)
            {
                obj = _sceneObjectManager.Commands.FindElementById(clickedElement.userData.ToString());
            }
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

        #region PLC Отрисовка

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
            if (element.name == "hierarchy-item-command" || element.name == "hierarchy-item")
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
            var draggedRobot = GetRobotParent(currentDragData.SceneObject);

            allElements = allElements
             .Where(e => GetSceneObjectFromElement(e) != null)
             .ToList();
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

                Rect elementLocalBounds;

                if (element is CustomFoldout foldout && foldout.Header != null)
                {
                    var headerBounds = foldout.Header.worldBound;

                    elementLocalBounds = new Rect(
                        headerBounds.x - panelWorldBounds.x,
                        headerBounds.y - panelWorldBounds.y,
                        headerBounds.width,
                        headerBounds.height
                    );
                }
                else
                {
                    // обычные элементы (примитивы и т.п.)
                    var wb = element.worldBound;

                    elementLocalBounds = new Rect(
                        wb.x - panelWorldBounds.x,
                        wb.y - panelWorldBounds.y,
                        wb.width,
                        wb.height
                    );
                }

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
            // Добавьте проверку на null для userData
            if (element?.userData == null)
                return null;

            string id = element.userData.ToString();

            // Дополнительная проверка на пустую строку
            if (string.IsNullOrEmpty(id))
                return null;

            return _sceneObjectManager.GetById(id);
        }

        private bool CanBeDragged(SceneObject sceneObject)
        {
            if (IsRootOnlyType(sceneObject))
                return true;

            if (IsProgramOrCommand(sceneObject))
                return GetRobotParent(sceneObject) != null;

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

        private bool IsProgramOrCommand(SceneObject obj)
        {
            return obj.Type == ObjectType.Program
                || obj.Type == ObjectType.LinearMoveCommand
                || obj.Type == ObjectType.StateEndEffectorCommand
                || obj.Type == ObjectType.WaitCommand;
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

            // ABOVE
            if (localPos.y < top)
            {
                return new DropTargetInfo
                {
                    TargetElement = element,
                    Position = DropPosition.Above,
                    Distance = Mathf.Abs(localPos.y - bounds.y)
                };
            }
            // BELOW
            else if (localPos.y > bottom)
            {
                return new DropTargetInfo
                {
                    TargetElement = element,
                    Position = DropPosition.Below,
                    Distance = Mathf.Abs(localPos.y - (bounds.y + bounds.height))
                };
            }
            // INSIDE
            else
            {
                if (target.Type == ObjectType.Node &&
                    dragged.Type == ObjectType.Primitive)
                {
                    return new DropTargetInfo
                    {
                        TargetElement = element,
                        Position = DropPosition.Inside,
                        Distance = 0
                    };
                }

                if (!(element is CustomFoldout)) return null;

                if (target.Type == ObjectType.Node &&
                    dragged.Type == ObjectType.Node)
                {
                    return new DropTargetInfo
                    {
                        TargetElement = element,
                        Position = DropPosition.Inside,
                        Distance = 0
                    };
                }

                // Команды/программы могут быть внутри робота
                else if (target.Type == ObjectType.Robot &&
                    (IsProgramOrCommand(dragged)))
                {
                    return new DropTargetInfo
                    {
                        TargetElement = element,
                        Position = DropPosition.Inside,
                        Distance = 0
                    };
                }

                else if (target.Type == ObjectType.Program &&
                    (dragged.Type == ObjectType.LinearMoveCommand
                    || dragged.Type == ObjectType.StateEndEffectorCommand
                    || dragged.Type == ObjectType.WaitCommand))
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

        private bool CanDropOnTarget(SceneObject dragged, SceneObject target)
        {
            if (dragged == null || target == null)
                return false;

            if (dragged.Id == target.Id)
                return false;

            // нельзя дропать в своего потомка
            if (IsChildOf(target.Id, dragged.Id))
                return false;

            // ===== ROBOT =====
            if (dragged.Type == ObjectType.Robot)
            {
                // роботы перемещаются только в корень
                return string.IsNullOrEmpty(target.ParentId);
            }

            // ===== WORKPIECE =====
            if (dragged.Type == ObjectType.Workpiece)
            {
                // роботы перемещаются только в корень
                return string.IsNullOrEmpty(target.ParentId);
            }

            // ===== PRIMITIVE =====
            if (dragged.Type == ObjectType.Primitive)
            {
                // Находим родительскую ноду для dragged
                SceneObject draggedParent = null;
                if (!string.IsNullOrEmpty(dragged.ParentId))
                {
                    draggedParent = _sceneObjectManager.GetById(dragged.ParentId);
                }

                // Находим родительскую ноду для target
                SceneObject targetParent = null;
                if (!string.IsNullOrEmpty(target.ParentId))
                {
                    targetParent = _sceneObjectManager.GetById(target.ParentId);
                }

                // Если оба примитива в одной ноде, можно перемещать над/под
                if (draggedParent != null && targetParent != null &&
                    draggedParent.Type == ObjectType.Node &&
                    targetParent.Type == ObjectType.Node)
                {
                    return draggedParent.Id == targetParent.Id;
                }

                // Если оба примитива в корне (нет ParentId), можно перемещать над/под
                if (string.IsNullOrEmpty(dragged.ParentId) && string.IsNullOrEmpty(target.ParentId))
                {
                    return true;
                }

                if (string.IsNullOrEmpty(dragged.ParentId) && targetParent != null && targetParent.Type == ObjectType.Node)
                {
                    return true;
                }

                if (draggedParent != null && draggedParent.Type == ObjectType.Node &&
                        string.IsNullOrEmpty(target.ParentId))
                {
                    return true;
                }

                // Если один примитив в ноде, а другой в корне - нельзя дропать
                return false;
            }

            // ===== NODE =====
            if (dragged.Type == ObjectType.Node)
            {
                return string.IsNullOrEmpty(target.ParentId) || target.Type == ObjectType.Node;
            }

            // ===== PROGRAM / COMMAND =====
            if (IsProgramOrCommand(dragged))
            {
                // только внутри своего робота
                var draggedRobot = GetRobotParent(dragged);
                var targetRobot = GetRobotParent(target);
                if (draggedRobot == null || targetRobot == null)
                    return false;

                return draggedRobot.Id == targetRobot.Id;
            }

            return false;
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
            var targetElement = currentDropTarget.TargetElement;
            var targetObject = GetSceneObjectFromElement(targetElement);

            // Проверка на попытку перетащить объект на самого себя
            if (targetObject != null && targetObject.Id == draggedObject.Id)
            {
                CleanupDrag();
                return;
            }

            string newParentId = null;
            int? insertAtIndex = null;

            // Обработка для роботов - всегда перемещаем в корень
            switch (currentDropTarget.Position)
            {
                case DropPosition.Above:
                    newParentId = targetObject?.ParentId;
                    insertAtIndex = GetSiblingIndexInParent(targetObject);
                    break;

                case DropPosition.Below:
                    newParentId = targetObject?.ParentId;
                    insertAtIndex = GetSiblingIndexInParent(targetObject) + 1;
                    break;

                case DropPosition.Inside:
                    newParentId = targetObject?.Id;
                    insertAtIndex = 0; // В начало списка детей
                    break;
            }
            if (!string.IsNullOrEmpty(newParentId) && IsChildOf(newParentId, draggedObject.Id))
            {
                CleanupDrag();
                return;
            }

            // Специальные проверки
            if (IsProgramOrCommand(draggedObject) && string.IsNullOrEmpty(newParentId))
            {
                CleanupDrag();
                return;
            }

            // Проверяем, не пытаемся ли переместить объект в его собственного потомка
            if (IsChildOf(newParentId, draggedObject.Id))
            {
                CleanupDrag();
                return;
            }

            // Для команд/программ проверяем, что остаемся в том же роботе
            if (IsProgramOrCommand(draggedObject))
            {
                var newParent = _sceneObjectManager.GetById(newParentId);
                var newRobot = GetRobotParent(newParent ?? targetObject);
                var oldRobot = GetRobotParent(draggedObject);

                if (newRobot == null || newRobot.Id != oldRobot.Id)
                {
                    CleanupDrag();
                    return;
                }
            }

            // Выполняем команду перемещения
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

                else if (element.name == "hierarchy-item-command" || element.name == "plc-command" || element.name == "hierarchy-item")
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
            MainHierarchyItem.SetExpanded(true);
            //expandedFoldouts.Add(MainHierarchyItem.userData.ToString(), MainHierarchyItem.IsExpanded);
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
}
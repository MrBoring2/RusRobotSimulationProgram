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
using Assets.Scripts.StageControlSystem.Models;
using Assets.UI.CustomElements;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Burst.Intrinsics.X86.Avx;
using static Unity.Collections.AllocatorManager;

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
            _eventBus.Subscribe<ClearSceneSignal>(OnClearSceneSignal);
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
                if (signal.PropertyProvider is PointPropertyProvider ||
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
        private void OnClearSceneSignal(ClearSceneSignal signal) => UpdateHierarchy();
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
            if (signal.Command is IDestructiveCommand || signal.Command is PropertyChangeCommand || signal.Command is AddPLCCommandCommand || signal.Command is RemovePLCCommandCommand)
            {
                UpdateHierarchy();
                UpdateTitle();
                _eventBus.Invoke(new UpdateLineDrawer());
            }

        }

        private void OnCommandExecuted(ExecuteCommandSignal signal)
        {
            if (signal.Command is IDestructiveCommand || signal.Command is PropertyChangeCommand || signal.Command is AddPLCCommandCommand || signal.Command is RemovePLCCommandCommand)
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

                if (element.name == "" || element.name == "label-hierarchy" || element.name == "foldout-header")
                {
                    element = GetParentElement(element);
                }
                if (element.name == "plc-command")
                {
                    var commandId = element.userData.ToString();
                    var command = GetCommandById(commandId);
                    if (command != null)
                    {
                        currentDragData = new DragDropData
                        {
                            SourceId = commandId,
                            SourceElement = element,
                            SceneObject = null,
                            StartPosition = evt.mousePosition,
                            UserData = command
                        };
                        SelectHierarchyItem(element);
                        ShowProperties(element);

                    }
                    return;
                }
                else if (element.name == "hierarchy-item-command")
                {
                    var commandId = element.userData.ToString();
                    var command = _sceneObjectManager.Commands.FindElementById(commandId) as CommandObject;
                    if (command != null)
                    {
                        currentDragData = new DragDropData
                        {
                            SourceId = commandId,
                            SourceElement = element,
                            SceneObject = command,
                            StartPosition = evt.mousePosition,
                            UserData = command
                        };
                        switch (command.Type)
                        {
                            case ObjectType.LinearMoveCommand:
                                if (!string.IsNullOrEmpty(element.userData.ToString()) &&
                                        _lineManager.IsCommandInCurrentProgram(commandId))
                                {
                                    _eventBus.Invoke(new PickObjectSignal(command));
                                }
                                else
                                {
                                    _eventBus.Invoke(new PickObjectSignal(command));
                                    _eventBus.Invoke(new StopLineDrawer());
                                }
                                break;
                            case ObjectType.StateEndEffectorCommand or ObjectType.WaitCommand:
                                break;
                            default:
                                _eventBus.Invoke(new PickObjectSignal(command));
                                _eventBus.Invoke(new StopLineDrawer());
                                break;
                        }
                        SelectHierarchyItem(element);
                        ShowProperties(element);

                    }
                    return;
                }
                SelectHierarchyItem(element);
                ShowProperties(element);

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
                    else if (foldout.name == "hierarchy-item-command")
                    {
                        contextMenu.Add(CreateMenuButton("Удалить команду", () => DeleteObject(clickedElement)));
                    }
                    else if (foldout.name == "plc-init-block")
                    {
                        var parentId = foldout.userData.ToString();
                        contextMenu.Add(CreateMenuButton("Добавить переменную", () => ShowAddVariableWindow(parentId)));
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
                        bool isInLogic = IsInsideLogicBlock(foldout);

                        contextMenu.Add(CreateMenuButton("Изменить условие", () => ShowExpressionWindow(parentId, false, condition.Expression)));
                        contextMenu.Add(CreateMenuButton("Добавить вложенное условие", () => ShowExpressionWindow(parentId)));
                        contextMenu.Add(CreateMenuButton("Добавить изменение переменной", () =>
                        {
                            ShowSetVariableWindow(parentId);
                        }));
                        if (!isInLogic)
                        {

                            contextMenu.Add(CreateMenuButton("Добавить задачу робота", () =>
                            {
                                if (!HasProgramCallInCondition(parentId))
                                {
                                    ShowProgramSetWindow(parentId, robotId);
                                }
                            }));
                        }
                    }
                    else if (foldout.name == "plc-elif-block")
                    {
                        var parentId = foldout.userData.ToString();
                        var condition = GetConditionById(parentId);
                        string robotId = GetRobotIdFromPLCBlock(foldout);
                        bool isInLogic = IsInsideLogicBlock(foldout);

                        contextMenu.Add(CreateMenuButton("Изменить условие", () => ShowExpressionWindow(parentId, true, condition.Expression)));
                        contextMenu.Add(CreateMenuButton("Добавить вложенное условие", () => ShowExpressionWindow(parentId)));
                        contextMenu.Add(CreateMenuButton("Добавить изменение переменной", () =>
                        {
                            ShowSetVariableWindow(parentId);
                        }));

                        if (!isInLogic)
                        {
                            contextMenu.Add(CreateMenuButton("Добавить задачу робота", () =>
                            {
                                if (!HasProgramCallInCondition(parentId))
                                {
                                    ShowProgramSetWindow(parentId, robotId);
                                }
                            }));
                        }
                        contextMenu.Add(CreateMenuButton("Удалить блок иначе если", () => DeleteELIFCondition(parentId)));
                    }
                    else if (foldout.name == "plc-else-block")
                    {
                        var parentId = foldout.userData.ToString();
                        string robotId = GetRobotIdFromPLCBlock(foldout);
                        bool isInLogic = IsInsideLogicBlock(foldout);

                        contextMenu.Add(CreateMenuButton("Добавить вложенное условие", () => ShowExpressionWindow(parentId)));
                        contextMenu.Add(CreateMenuButton("Добавить изменение переменной", () =>
                        {
                            ShowSetVariableWindow(parentId);
                        }));

                        if (!isInLogic)
                        {
                            contextMenu.Add(CreateMenuButton("Добавить задачу робота", () =>
                            {
                                if (!HasProgramCallInCondition(parentId))
                                {
                                    ShowProgramSetWindow(parentId, robotId);
                                }
                            }));
                        }
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
        private bool IsInsideLogicBlock(VisualElement element)
        {
            var current = element.parent;
            while (current != null)
            {
                if (current.name == "plc-logic-block")
                    return true;
                if (current.name == "plc-robot-block")
                    return false;
                current = current.parent;
            }
            return false;
        }
        private void ShowSetVariableWindow(string parentId)
        {

            ModalParameters parameters = new ModalParameters();
            _modalWindowServiceManager.ShowWindow<PLCSetVariable>("variable-set-window", "Изменение переменной", parameters, (result) =>
            {
                if (result != null)
                {
                    AddToPLCContent(parentId, result);
                }
            });
        }

        private void ShowAddVariableWindow(string blockId)
        {
            ModalParameters parameters = new ModalParameters();
            _modalWindowServiceManager.ShowWindow<PLCInitVariable>("variable-init-window", "Инициализация переменной", parameters, (result) =>
            {
                if (result != null)
                {
                    AddVariableInitToPLC(blockId, result);
                }
            });
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
                    if (block.ElseCondition != null && block.ElseCondition.Id == id)
                        return block.ElseCondition;

                    var found = FindConditionInList(block.IfCondition.Content, id);
                    if (found != null) return found;
                    foreach (var elif in block.ElifConditions)
                    {
                        found = FindConditionInList(elif.Content, id);
                        if (found != null) return found;
                    }
                    if (block.ElseCondition != null)
                    {
                        found = FindConditionInList(block.ElseCondition.Content, id);
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
            //if (_simulationManager.GetModeSim().SimulationMode == MODE.JOG_MODE)
            //{
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
            //}
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

            while (true)
            {
                obj = _sceneObjectManager.GetById(parentId)
                      ?? _sceneObjectManager.Commands.FindElementById(parentId);

                if (obj == null)
                    return null;

                if (obj.Type == ObjectType.Robot)
                    return obj;

                parentId = obj.ParentId;
            }
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

        private PLCCommand GetCommandById(string commandId)
        {
            foreach (var item in _sceneObjectManager.PLCData.InitBlockItems)
            {
                if (item.Id == commandId) return item;
            }

            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                var found = FindCommandInList(rb.ConditionsList, commandId);
                if (found != null) return found;
            }
            return FindCommandInList(_sceneObjectManager.PLCData.LogicBlockItems, commandId);
        }

        private PLCCommand FindCommandInList(List<PLCBase> items, string commandId)
        {
            foreach (var item in items)
            {
                if (item is PLCCommand cmd && cmd.Id == commandId)
                    return cmd;

                if (item is PLCBlockCondition block)
                {
                    var found = FindCommandInList(block.IfCondition.Content, commandId);
                    if (found != null) return found;
                    foreach (var elif in block.ElifConditions)
                    {
                        found = FindCommandInList(elif.Content, commandId);
                        if (found != null) return found;
                    }
                    if (block.ElseCondition != null)
                    {
                        found = FindCommandInList(block.ElseCondition.Content, commandId);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }

        private void AddProgramInPLC(string blockId, RobotProgramObject program)
        {
            var startProgram = new PLCStartProgram { ProgramName = program.PropertyProvider.Name, ProgramId = program.Id };
            AddToPLCContent(blockId, startProgram);
        }

        private void AddVariableInitToPLC(string blockId, PLCInitVariable pLCSet)
        {
            AddToPLCContent(blockId, pLCSet);
            _sceneObjectManager.PLCData.Variables.Add(new Variable(pLCSet.Id, pLCSet.VarType, pLCSet.VariableName));
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

                    if (block.ElseCondition != null)
                    {
                        found = FindBlockConditionById(block.ElseCondition.Content, id);
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


            if (itemToAdd is PLCInitVariable s)
            {
                var list = _sceneObjectManager.PLCData.InitBlockItems;
                var command = new AddPLCCommandCommand(itemToAdd, list);
                _undoRedoManager.Execute(command);
            }

            else if (itemToAdd is PLCStartProgram || itemToAdd is PLCSetVariable || itemToAdd is PLCBlockCondition || itemToAdd is PLCCondition)
            {
                // 1. Проверяем в блоках роботов
                var robotBlock = _sceneObjectManager.PLCData.RobotCommandsBlockItems
                    .FirstOrDefault(x => x.RobotId == parentId);
                if (robotBlock != null)
                {
                    var command = new AddPLCCommandCommand(itemToAdd, robotBlock.ConditionsList);
                    _undoRedoManager.Execute(command);
                    UpdateHierarchy();
                    return;
                }
                if (parentId == "logic_block")
                {
                    var command = new AddPLCCommandCommand(itemToAdd, _sceneObjectManager.PLCData.LogicBlockItems);
                    _undoRedoManager.Execute(command);
                    UpdateHierarchy();
                    return;
                }
                //// 2. Проверяем в блоке логики
                //var logicParent = _sceneObjectManager.PLCData.LogicBlockItems
                //    .FirstOrDefault(x => x is PLCBlockCondition && ((PLCBlockCondition)x).Id == parentId);
                //if (logicParent is PLCBlockCondition block)
                //{
                //    block.IfCondition.Content.Add(itemToAdd);
                //    UpdateHierarchy();
                //    return;
                //}

                // 3. Рекурсивно ищем в условиях роботов
                foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
                {
                    if (TryAddToContent(rb.ConditionsList, parentId, itemToAdd, out var list))
                    {
                        var command = new AddPLCCommandCommand(itemToAdd, list);
                        _undoRedoManager.Execute(command);
                        UpdateHierarchy();
                        return;
                    }
                }

                // Рекурсивный поиск в логике
                if (TryAddToContent(_sceneObjectManager.PLCData.LogicBlockItems, parentId, itemToAdd, out var list2))
                {
                    var command = new AddPLCCommandCommand(itemToAdd, list2);
                    _undoRedoManager.Execute(command);
                    UpdateHierarchy();
                    return;
                }
            }

            UpdateHierarchy();
        }

        /// <summary>
        /// Рекурсивный поиск и добавление элемента в Content нужного блока
        /// </summary>
        private bool TryAddToContent(List<PLCBase> items, string parentId, PLCBase itemToAdd, out IList targetList)
        {
            targetList = null;

            foreach (var item in items)
            {
                if (item is PLCBlockCondition block)
                {
                    if (block.IfCondition.Id == parentId)
                    {
                        targetList = block.IfCondition.Content;
                        return true;
                    }

                    foreach (var elif in block.ElifConditions)
                    {
                        if (elif.Id == parentId)
                        {
                            targetList = elif.Content;
                            return true;
                        }
                    }

                    if (block.ElseCondition != null && block.ElseCondition.Id == parentId)
                    {
                        targetList = block.ElseCondition.Content;
                        return true;
                    }

                    if (TryAddToContent(block.IfCondition.Content, parentId, itemToAdd, out targetList))
                        return true;

                    foreach (var elif in block.ElifConditions)
                    {
                        if (TryAddToContent(elif.Content, parentId, itemToAdd, out targetList))
                            return true;
                    }

                    if (block.ElseCondition != null && TryAddToContent(block.ElseCondition.Content, parentId, itemToAdd, out targetList))
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
                if (items[i] is PLCBlockCondition block)
                {
                    if (block.Id == conditionId)
                    {
                        items.RemoveAt(i);
                        return true;
                    }

                    if (RemoveConditionFromList(block.IfCondition.Content, conditionId))
                        return true;

                    foreach (var elif in block.ElifConditions)
                    {
                        if (RemoveConditionFromList(elif.Content, conditionId))
                            return true;
                    }

                    if (block.ElseCondition != null &&
                        RemoveConditionFromList(block.ElseCondition.Content, conditionId))
                        return true;
                }
            }
            return false;
        }

        private void DeletePLCCommand(string commandId)
        {
            // Init блок
            var initList = _sceneObjectManager.PLCData.InitBlockItems;
            for (int i = 0; i < initList.Count; i++)
            {
                if (initList[i].Id == commandId)
                {
                    var cmd = initList[i];
                    var command = new RemovePLCCommandCommand(cmd, initList);
                    _undoRedoManager.Execute(command);
                    UpdateHierarchy();
                    return;
                }
            }

            // RobotCommands
            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                if (RemoveCommandFromList(rb.ConditionsList, commandId, out var cmd, out var list))
                {
                    var command = new RemovePLCCommandCommand(cmd, list);
                    _undoRedoManager.Execute(command);
                    UpdateHierarchy();
                    return;
                }
            }

            // Logic
            if (RemoveCommandFromList(_sceneObjectManager.PLCData.LogicBlockItems, commandId, out var cmd2, out var list2))
            {
                var command = new RemovePLCCommandCommand(cmd2, list2);
                _undoRedoManager.Execute(command);
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
                    if (block.ElseCondition != null && RemoveCommandFromList(block.ElseCondition.Content, commandId)) return true;
                }
            }
            return false;
        }

        // Новый метод с out-параметрами для DeletePLCCommand
        private bool RemoveCommandFromList(List<PLCBase> items, string commandId, out PLCCommand removedCmd, out List<PLCBase> sourceList)
        {
            removedCmd = null;
            sourceList = null;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is PLCCommand cmd && cmd.Id == commandId)
                {
                    removedCmd = cmd;
                    sourceList = items;
                    items.RemoveAt(i);
                    return true;
                }
            }

            foreach (var item in items)
            {
                if (item is PLCBlockCondition block)
                {
                    if (RemoveCommandFromList(block.IfCondition.Content, commandId, out removedCmd, out sourceList)) return true;
                    foreach (var elif in block.ElifConditions)
                    {
                        if (RemoveCommandFromList(elif.Content, commandId, out removedCmd, out sourceList)) return true;
                    }
                    if (block.ElseCondition != null && RemoveCommandFromList(block.ElseCondition.Content, commandId, out removedCmd, out sourceList)) return true;
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
                    if (block.ElseCondition != null && RemoveELIFFromList(block.ElseCondition.Content, elifId)) return true;
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
                DrawCommand(variable, initBlock);
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
            if (block.ElseCondition != null)
            {
                var elseFoldout = new CustomFoldout { Text = $"Иначе: {block.ElseCondition.Expression}" };
                elseFoldout.name = "plc-else-block";
                elseFoldout.userData = block.ElseCondition.Id;
                elseFoldout.AddToClassList("plc-else-block");
                RegisterExpanedFoldout(elseFoldout);
                foreach (var content in block.ElseCondition.Content)
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

            if (expandedFoldouts.TryGetValue(foldout.userData?.ToString() ?? "", out bool savedState))
            {
                foldout.SetExpanded(savedState);
            }
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
                case PLCSetVariable setVariable:
                    switch (setVariable.Operation)
                    {
                        case OperationType.Increment:
                            commandText = $"Инкремент: {setVariable.VariableName} += {setVariable.Value}";
                            break;
                        case OperationType.Decrement:
                            commandText = $"Декремент: {setVariable.VariableName} -= {setVariable.Value}";
                            break;
                        case OperationType.Assign:
                            commandText = $"Присвоить: {setVariable.VariableName} = {setVariable.Value}";
                            break;
                        default:
                            break;
                    }

                    break;
                case PLCInitVariable set:
                    commandText = $"Создать {set.VarType}: {set.VariableName} = {set.StartValue}";
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
        private List<VisualElement> GetAllDropTargets()
        {
            var result = new List<VisualElement>();
            CollectDropTargets(MainHierarchyItem, result);
            return result;
        }

        private void CollectDropTargets(VisualElement parent, List<VisualElement> result)
        {
            foreach (var child in parent.Children())
            {
                // PLC условия - цели для PLC команд
                if (child.name == "plc-if-block" || child.name == "plc-elif-block" || child.name == "plc-else-block")
                    result.Add(child);

                // Программы - цели для команд (внутрь программы)
                if (child.name == "hierarchy-item-program")
                    result.Add(child);

                // Команды - цели для вставки выше/ниже
                if (child.name == "hierarchy-item-command")
                    result.Add(child);
                // ===============================================

                // SceneObject цели
                if (child is CustomFoldout && GetSceneObjectFromElement(child) != null)
                    result.Add(child);

                CollectDropTargets(child, result);
            }
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
                }
            }
            else if (isDragging && currentDragData != null)
            {
                UpdateDragPreview(evt.mousePosition);
                var dropTarget = FindDropTarget(evt.mousePosition);
                UpdateDropIndicators(dropTarget);
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
            else if (element.name == "plc-command")
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

        private List<VisualElement> GetPLCBlocksInOrder()
        {
            var result = new List<VisualElement>();
            CollectPLCBlocks(MainHierarchyItem, result);
            return result;
        }

        private void CollectPLCBlocks(VisualElement parent, List<VisualElement> result)
        {
            foreach (var child in parent.Children())
            {
                if (child.name == "plc-if-block" || child.name == "plc-elif-block" || child.name == "plc-else-block")
                {
                    result.Add(child);
                }
                CollectPLCBlocks(child, result);
            }
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

        private DropTargetInfo FindDropTarget(Vector2 position)
        {
            var allElementsList = new List<VisualElement>();
            allElementsList.AddRange(hierarchyPanel.Query<VisualElement>("hierarchy-item-program").ToList());
            allElementsList.AddRange(hierarchyPanel.Query<VisualElement>("hierarchy-item-command").ToList());
            allElementsList.AddRange(hierarchyPanel.Query<VisualElement>("plc-if-block").ToList());
            allElementsList.AddRange(hierarchyPanel.Query<VisualElement>("plc-elif-block").ToList());
            allElementsList.AddRange(hierarchyPanel.Query<VisualElement>("plc-else-block").ToList());
            allElementsList.AddRange(hierarchyPanel.Query<VisualElement>("plc-command").ToList());
            allElementsList.AddRange(hierarchyPanel.Query<VisualElement>("plc-robot-block").ToList());
            allElementsList.AddRange(hierarchyPanel.Query<VisualElement>("plc-logic-block").ToList());
            allElementsList.AddRange(hierarchyPanel.Query<VisualElement>("plc-condition-block").ToList());
            DropTargetInfo bestTarget = null;
            float minDistance = float.MaxValue;

            var panelWorldBounds = hierarchyPanel.worldBound;
            float localPosX = position.x - panelWorldBounds.x;
            float localPosY = position.y - panelWorldBounds.y;
            var localPos = new Vector2(localPosX, localPosY);

            foreach (var element in allElementsList)
            {
                if (element == currentDragData?.SourceElement) continue;

                if (currentDragData?.UserData is CommandObject)
                {
                    // Для программы - дроп внутрь (в конец)
                    if (element.name == "hierarchy-item-program")
                    {
                        Debug.Log($"Found program: {element.userData}");
                        if (!CanDropOnTarget(element)) continue;

                        var bounds = GetElementBounds(element, panelWorldBounds);
                        if (!bounds.Contains(localPos)) continue;

                        float dist = Vector2.Distance(localPos, bounds.center);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestTarget = new DropTargetInfo
                            {
                                TargetElement = element,
                                Position = DropPosition.Inside,
                                Distance = dist
                            };
                        }
                    }

                    if (element.name == "hierarchy-item-command")
                    {
                        if (!CanDropOnTarget(element)) continue;

                        var bounds = GetElementBounds(element, panelWorldBounds);
                        if (!bounds.Contains(localPos)) continue;

                        var dropInfoRobot = CalculateCommandDropPosition(element, bounds, localPos);
                        if (dropInfoRobot != null && dropInfoRobot.Distance < minDistance)
                        {
                            minDistance = dropInfoRobot.Distance;
                            bestTarget = dropInfoRobot;
                        }
                    }
                    continue;
                }

                if (currentDragData?.UserData is PLCCommand)
                {
                    bool isInitCommand = element.name == "plc-command" &&
                              element.parent?.parent?.name == "plc-init-block";
                    if (isInitCommand)
                    {
                        if (!CanDropOnTarget(element)) continue;

                        var bounds = GetElementBounds(element, panelWorldBounds);
                        if (!bounds.Contains(localPos)) continue;

                        var dropInfoInit = CalculateCommandDropPosition(element, bounds, localPos);
                        if (dropInfoInit != null && dropInfoInit.Distance < minDistance)
                        {
                            minDistance = dropInfoInit.Distance;
                            bestTarget = dropInfoInit;
                        }
                        continue;
                    }
                    if (element.name == "plc-condition-block")
                    {
                        if (!CanDropOnTarget(element)) continue;

                        var bounds = GetElementBounds(element, panelWorldBounds);
                        if (!bounds.Contains(localPos)) continue;

                        var dropInfoCond = CalculateCommandDropPosition(element, bounds, localPos);
                        if (dropInfoCond != null && dropInfoCond.Distance < minDistance)
                        {
                            minDistance = dropInfoCond.Distance;
                            bestTarget = dropInfoCond;
                        }
                        continue;
                    }
                    if (element.name == "plc-robot-block" || element.name == "plc-logic-block")
                    {
                        if (!CanDropOnTarget(element)) continue;

                        var bounds = GetElementBounds(element, panelWorldBounds);
                        if (!bounds.Contains(localPos)) continue;

                        float dist = Vector2.Distance(localPos, bounds.center);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestTarget = new DropTargetInfo
                            {
                                TargetElement = element,
                                Position = DropPosition.Inside,
                                Distance = dist
                            };
                        }
                        continue;
                    }
                    if (element.name == "plc-command")
                    {
                        if (!CanDropOnTarget(element)) continue;

                        var bounds = GetElementBounds(element, panelWorldBounds);
                        if (!bounds.Contains(localPos)) continue;

                        var dropInfoCmd = CalculateCommandDropPosition(element, bounds, localPos);
                        if (dropInfoCmd != null && dropInfoCmd.Distance < minDistance)
                        {
                            minDistance = dropInfoCmd.Distance;
                            bestTarget = dropInfoCmd;
                        }
                        continue;
                    }

                    var condition = GetConditionFromElement(element);
                    if (condition != null)
                    {
                        if (!CanDropOnTarget(element)) continue;

                        var plcBounds = GetElementBounds(element, panelWorldBounds);

                        if (!plcBounds.Contains(localPos)) continue;

                        float dist = Vector2.Distance(localPos, plcBounds.center);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestTarget = new DropTargetInfo
                            {
                                TargetElement = element,
                                Position = DropPosition.Inside,
                                Distance = dist
                            };
                        }
                    }
                    continue;
                }

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

            //if (allElementsList.Count > 0 && CanDropInRoot(currentDragData.SceneObject) && bestTarget == null)
            //{
            //    var lastElement = allElementsList[allElementsList.Count - 1];
            //    var lastBounds = lastElement.worldBound;
            //    float lastLocalY = lastBounds.y - panelWorldBounds.y + lastBounds.height;
            //    if (localPos.y > lastLocalY + 10)
            //    {
            //        bestTarget = new DropTargetInfo
            //        {
            //            TargetElement = lastElement,
            //            Position = DropPosition.Below,
            //            Distance = Mathf.Abs(localPos.y - lastLocalY)
            //        };
            //    }
            //}

            return bestTarget;
        }
        private DropTargetInfo CalculateCommandDropPosition(VisualElement element, Rect bounds, Vector2 localPos)
        {
            float h = bounds.height;
            float top = bounds.y + h * 0.25f;
            float bottom = bounds.y + h * 0.75f;

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

            return null;
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
                if (target.Type == ObjectType.Node && dragged.Type == ObjectType.Primitive)
                {
                    return new DropTargetInfo
                    {
                        TargetElement = element,
                        Position = DropPosition.Inside,
                        Distance = 0
                    };
                }

                if (!(element is CustomFoldout)) return null;

                if (target.Type == ObjectType.Node && dragged.Type == ObjectType.Node)
                {
                    return new DropTargetInfo
                    {
                        TargetElement = element,
                        Position = DropPosition.Inside,
                        Distance = 0
                    };
                }
                else if (target.Type == ObjectType.Robot && IsProgramOrCommand(dragged))
                {
                    return new DropTargetInfo
                    {
                        TargetElement = element,
                        Position = DropPosition.Inside,
                        Distance = 0
                    };
                }
                else if (target.Type == ObjectType.Program && (dragged.Type == ObjectType.LinearMoveCommand
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

        private bool CanDropOnTarget(VisualElement targetElement)
        {
            if (currentDragData == null) return false;

            if (currentDragData.UserData is CommandObject draggedRobotCommand)
            {
                if (targetElement.name == "hierarchy-item-program")
                {
                    var targetProgram = _sceneObjectManager.Commands.FindElementById(targetElement.userData?.ToString()) as RobotProgramObject;
                    if (targetProgram == null) return false;

                    // Нельзя дропнуть в ту же программу, если команда уже в ней?
                    // Тут можно добавить логику

                    return true;
                }

                if (targetElement.name == "hierarchy-item-command")
                {
                    var targetCommand = ResolveCommand(targetElement);
                    if (targetCommand == null) return false;

                    var draggedProgram = GetParentProgram(draggedRobotCommand);
                    var targetProgram = GetParentProgram(targetCommand);

                    if (draggedProgram == null || targetProgram == null)
                        return false;

                    var draggedRobot = GetRobotParent(draggedProgram);
                    var targetRobot = GetRobotParent(targetProgram);

                    if (draggedRobot == null || targetRobot == null)
                        return false;

                    if (draggedRobot.Id != targetRobot.Id)
                        return false;

                    return true;
                }

                return false;
            }

            // Для PLC команд
            if (currentDragData.UserData is PLCCommand draggedPLCCommand)
            {
                bool isFromInit = draggedPLCCommand is PLCInitVariable;
                bool targetIsInit = IsInsideInitBlock(targetElement) || targetElement.name == "plc-init-block";
                if (isFromInit)
                {
                    if (!targetIsInit) return false;
                    if (targetElement.name == "plc-command")
                    {
                        if (targetElement.userData?.ToString() == draggedPLCCommand.Id) return false;
                        return true;
                    }
                    return false;
                }
                if (targetIsInit) return false;
                if (targetElement.name == "plc-condition-block")
                {
                    // Проверяем что в том же роботе
                    string sourceRobId = GetRobotIdFromCommand(draggedPLCCommand.Id);
                    string targetRobId = GetRobotIdFromConditionBlock(targetElement);
                    return sourceRobId == targetRobId;
                }
                if (targetElement.name == "plc-robot-block" || targetElement.name == "plc-logic-block")
                {
                    if (draggedPLCCommand is PLCStartProgram) return false;
                    return true;
                }
                if (targetElement.name == "plc-command")
                {
                    if (targetElement.userData?.ToString() == draggedPLCCommand.Id) return false;

                    string sourceRobId = GetRobotIdFromCommand(draggedPLCCommand.Id);
                    string targetRobId = GetRobotIdFromCommandById(targetElement.userData?.ToString());

                    return sourceRobId == targetRobId;
                }

                var targetCondition = GetConditionFromElement(targetElement);
                if (targetCondition == null) return false;

                // Нельзя дропнуть в то же условие
                if (IsCommandInCondition(draggedPLCCommand.Id, targetCondition)) return false;

                // Получаем роботов
                string sourceRobotId = GetRobotIdFromCommand(draggedPLCCommand.Id);
                string targetRobotId = GetRobotIdFromCondition(targetCondition);

                // Если роботы разные - запрещаем
                if (sourceRobotId != targetRobotId) return false;

                // Если это StartProgram - можно только если нет другой StartProgram
                if (draggedPLCCommand is PLCStartProgram && HasStartProgramInCondition(targetCondition)) return false;

                return true;
            }

            // Для SceneObject
            if (currentDragData.SceneObject == null) return false;

            var target = GetSceneObjectFromElement(targetElement);
            if (target == null) return false;
            if (currentDragData.SceneObject.Id == target.Id) return false;
            if (IsChildOf(target.Id, currentDragData.SceneObject.Id)) return false;

            return true;
        }

        private string GetRobotIdFromConditionBlock(VisualElement element)
        {
            // Ищем родительский plc-robot-block или plc-logic-block
            var current = element.parent;
            while (current != null)
            {
                if (current.name == "plc-robot-block" && current.userData != null)
                    return current.userData.ToString();
                if (current.name == "plc-logic-block")
                    return "logic";
                current = current.parent;
            }
            return null;
        }
        private string GetRobotIdFromCommandById(string commandId)
        {
            if (string.IsNullOrEmpty(commandId)) return null;

            // Ищем в init блоке
            foreach (var item in _sceneObjectManager.PLCData.InitBlockItems)
            {
                if (item.Id == commandId)
                    return "init";
            }

            // Ищем в блоках роботов
            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                // Проверяем корень ConditionsList
                foreach (var item in rb.ConditionsList)
                {
                    if (item is PLCCommand cmd && cmd.Id == commandId)
                        return rb.RobotId;
                }
                // Рекурсивно
                if (FindCommandInListRecursive(rb.ConditionsList, commandId))
                    return rb.RobotId;
            }

            // Ищем в логике
            foreach (var item in _sceneObjectManager.PLCData.LogicBlockItems)
            {
                if (item is PLCCommand cmd && cmd.Id == commandId)
                    return "logic";
            }
            if (FindCommandInListRecursive(_sceneObjectManager.PLCData.LogicBlockItems, commandId))
                return "logic";

            return null;
        }
        private bool IsInsideInitBlock(VisualElement element)
        {
            var current = element.parent;
            while (current != null)
            {
                if (current.name == "plc-init-block")
                    return true;
                current = current.parent;
            }
            return false;
        }

        private RobotProgramObject GetParentProgram(CommandObject command)
        {
            // Ищем во ВСЕХ роботах
            foreach (var robot in _sceneObjectManager.GetGameObjectsList().Where(r => r.Type == ObjectType.Robot))
            {
                var programs = _sceneObjectManager.Commands.GetSubPrograms(robot.Id);
                foreach (var program in programs)
                {
                    if (program.Items.Contains(command))
                        return program;
                }
            }
            return null;
        }
        private void UpdateDropIndicators(DropTargetInfo dropTarget)
        {
            ClearDropIndicators();
            currentDropTarget = dropTarget;
            if (dropTarget == null) return;

            // Для команд робота
            if (currentDragData?.UserData is CommandObject)
            {
                if (dropTarget.TargetElement.name == "hierarchy-item-program")
                {
                    dropTarget.TargetElement.AddToClassList(DROP_TARGET_INSIDE_CLASS);
                }
                else if (dropTarget.TargetElement.name == "hierarchy-item-command" || dropTarget.TargetElement.name == "plc-command")
                {
                    if (dropTarget.Position == DropPosition.Above)
                    {
                        dropTarget.TargetElement.AddToClassList(DROP_TARGET_ABOVE_CLASS);
                        Debug.Log("Adding ABOVE class to command");
                    }
                    else if (dropTarget.Position == DropPosition.Below)
                    {
                        dropTarget.TargetElement.AddToClassList(DROP_TARGET_BELOW_CLASS);
                        Debug.Log("Adding BELOW class to command");
                    }
                }
                return;
            }

            // Для PLC команд
            if (currentDragData?.UserData is PLCCommand draggedCommand)
            {
                if (dropTarget.TargetElement.name == "plc-command" || dropTarget.TargetElement.name == "plc-condition-block")
                {
                    if (dropTarget.Position == DropPosition.Above)
                    {
                        dropTarget.TargetElement.AddToClassList(DROP_TARGET_ABOVE_CLASS);
                    }
                    else if (dropTarget.Position == DropPosition.Below)
                    {
                        dropTarget.TargetElement.AddToClassList(DROP_TARGET_BELOW_CLASS);
                    }
                    return;
                }

                var targetCondition = GetConditionFromElement(dropTarget.TargetElement);
                if (targetCondition != null)
                {
                    string sourceRobotId = GetRobotIdFromCommand(draggedCommand.Id);
                    string targetRobotId = GetRobotIdFromCondition(targetCondition);
                    if (sourceRobotId != targetRobotId) return;
                    if (draggedCommand is PLCStartProgram && HasStartProgramInCondition(targetCondition)) return;
                }
                dropTarget.TargetElement.AddToClassList(DROP_TARGET_INSIDE_CLASS);
                return;
            }

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

        private void RemoveCommandFromAllLists(string commandId)
        {
            var initList = _sceneObjectManager.PLCData.InitBlockItems;
            for (int i = initList.Count - 1; i >= 0; i--)
            {
                if (initList[i].Id == commandId)
                {
                    initList.RemoveAt(i);
                    break;
                }
            }
            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
                RemoveCommandFromList(rb.ConditionsList, commandId);
            RemoveCommandFromList(_sceneObjectManager.PLCData.LogicBlockItems, commandId);
        }

        private void HandleDrop()
        {
            if (currentDragData == null || currentDropTarget == null) return;

            if (currentDragData.UserData is CommandObject draggedRobotCommand)
            {
                var targetElement = currentDropTarget.TargetElement;

                // Дроп в программу (в конец)
                if (targetElement.name == "hierarchy-item-program")
                {
                    var targetProgram = _sceneObjectManager.Commands.FindElementById(targetElement.userData?.ToString()) as RobotProgramObject;
                    if (targetProgram != null)
                    {
                        // Удаляем из старого места
                        RemoveCommandFromProgram(draggedRobotCommand);
                        // Добавляем в новую программу
                        targetProgram.Items.Add(draggedRobotCommand);
                        UpdateHierarchy();
                        CleanupDrag();
                        return;
                    }
                }

                // Дроп выше/ниже команды
                if (targetElement.name == "hierarchy-item-command")
                {
                    var targetCommand = ResolveCommand(targetElement);
                    if (targetCommand != null)
                    {
                        var targetProgram = GetParentProgram(targetCommand);
                        if (targetProgram != null)
                        {
                            int oldIndex = targetProgram.Items.IndexOf(draggedRobotCommand);
                            int targetIndex = targetProgram.Items.IndexOf(targetCommand);

                            if (currentDropTarget.Position == DropPosition.Below)
                                targetIndex++;
                            if (targetIndex > oldIndex && oldIndex != -1)
                                targetIndex--;

                            // Удаляем со старого места
                            if (oldIndex != -1)
                                targetProgram.Items.RemoveAt(oldIndex);
                            else
                                RemoveCommandFromProgram(draggedRobotCommand);

                            // Вставляем на новое место
                            targetProgram.Items.Insert(targetIndex, draggedRobotCommand);
                            UpdateHierarchy();
                            CleanupDrag();
                            return;
                        }
                    }
                }
                CleanupDrag();
                return;
            }

            if (currentDragData.UserData is PLCCommand draggedCommand)
            {
                var targetElement = currentDropTarget.TargetElement;
                if (targetElement.name == "plc-robot-block")
                {
                    var robotId = targetElement.userData?.ToString();
                    if (!string.IsNullOrEmpty(robotId))
                    {
                        var robotData = _sceneObjectManager.PLCData.RobotCommandsBlockItems
                            .FirstOrDefault(r => r.RobotId == robotId);
                        if (robotData != null)
                        {
                            RemoveCommandFromAllLists(draggedCommand.Id);
                            robotData.ConditionsList.Add(draggedCommand);
                            UpdateHierarchy();
                            CleanupDrag();
                            return;
                        }
                    }
                    CleanupDrag();
                    return;
                }

                // Дроп в корень логики
                if (targetElement.name == "plc-logic-block")
                {
                    RemoveCommandFromAllLists(draggedCommand.Id);
                    _sceneObjectManager.PLCData.LogicBlockItems.Add(draggedCommand);
                    UpdateHierarchy();
                    CleanupDrag();
                    return;
                }
                if (targetElement.name == "plc-condition-block")
                {
                    var targetConditionBlockId = targetElement.userData?.ToString();
                    if (!string.IsNullOrEmpty(targetConditionBlockId))
                    {
                        foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
                        {
                            int idx = FindConditionBlockIndex(rb.ConditionsList, targetConditionBlockId);
                            if (idx >= 0)
                            {
                                RemoveCommandFromAllLists(draggedCommand.Id);
                                if (currentDropTarget.Position == DropPosition.Below)
                                    idx++;
                                idx = Mathf.Clamp(idx, 0, rb.ConditionsList.Count);
                                rb.ConditionsList.Insert(idx, draggedCommand);
                                UpdateHierarchy();
                                CleanupDrag();
                                return;
                            }
                        }
                        int idx2 = FindConditionBlockIndex(_sceneObjectManager.PLCData.LogicBlockItems, targetConditionBlockId);
                        if (idx2 >= 0)
                        {
                            RemoveCommandFromAllLists(draggedCommand.Id);
                            if (currentDropTarget.Position == DropPosition.Below)
                                idx2++;
                            idx2 = Mathf.Clamp(idx2, 0, _sceneObjectManager.PLCData.LogicBlockItems.Count);
                            _sceneObjectManager.PLCData.LogicBlockItems.Insert(idx2, draggedCommand);
                            UpdateHierarchy();
                            CleanupDrag();
                            return;
                        }
                    }
                    CleanupDrag();
                    return;
                }
                // Дроп Above/Below на команду
                if (targetElement.name == "plc-command")
                {
                    var targetCommand = GetCommandById(targetElement.userData?.ToString());
                    if (targetCommand == null)
                    {
                        CleanupDrag();
                        return;
                    }

                    // Init блок
                    if (IsInsideInitBlock(targetElement))
                    {
                        var initList = _sceneObjectManager.PLCData.InitBlockItems;

                        RemoveCommandFromAllLists(draggedCommand.Id);

                        int targetIndex = initList.FindIndex(x => x.Id == targetCommand.Id);

                        if (targetIndex >= 0)
                        {
                            if (currentDropTarget.Position == DropPosition.Below)
                                targetIndex++;

                            initList.Insert(targetIndex, (PLCInitVariable)draggedCommand);
                            UpdateHierarchy();
                            CleanupDrag();
                            return;
                        }

                        CleanupDrag();
                        return;
                    }

                    foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
                    {
                        var foundList = FindParentListForCommandInConditions(rb.ConditionsList, targetCommand.Id);
                        if (foundList != null)
                        {
                            RemoveCommandFromAllLists(draggedCommand.Id);

                            int targetIndex = foundList.IndexOf(targetCommand);
                            if (currentDropTarget.Position == DropPosition.Below)
                                targetIndex++;

                            foundList.Insert(targetIndex, draggedCommand);
                            UpdateHierarchy();
                            CleanupDrag();
                            return;
                        }

                        // Проверяем в корне ConditionsList
                        int idx = rb.ConditionsList.IndexOf(targetCommand);
                        if (idx >= 0)
                        {
                            RemoveCommandFromAllLists(draggedCommand.Id);
                            if (currentDropTarget.Position == DropPosition.Below) idx++;
                            idx = Mathf.Clamp(idx, 0, rb.ConditionsList.Count);
                            rb.ConditionsList.Insert(idx, draggedCommand);
                            UpdateHierarchy();
                            CleanupDrag();
                            return;
                        }
                    }

                    // 2. Ищем в логике
                    var logicList = FindParentListForCommandInConditions(_sceneObjectManager.PLCData.LogicBlockItems, targetCommand.Id);
                    if (logicList != null)
                    {
                        RemoveCommandFromAllLists(draggedCommand.Id);

                        int targetIndex = logicList.IndexOf(targetCommand);
                        if (currentDropTarget.Position == DropPosition.Below)
                            targetIndex++;

                        logicList.Insert(targetIndex, draggedCommand);
                        UpdateHierarchy();
                        CleanupDrag();
                        return;
                    }

                    // Проверяем в корне LogicBlockItems
                    int idx2 = _sceneObjectManager.PLCData.LogicBlockItems.IndexOf(targetCommand);
                    if (idx2 >= 0)
                    {
                        RemoveCommandFromAllLists(draggedCommand.Id);
                        if (currentDropTarget.Position == DropPosition.Below) idx2++;
                        idx2 = Mathf.Clamp(idx2, 0, _sceneObjectManager.PLCData.LogicBlockItems.Count);
                        _sceneObjectManager.PLCData.LogicBlockItems.Insert(idx2, draggedCommand);
                        UpdateHierarchy();
                        CleanupDrag();
                        return;
                    }

                    CleanupDrag();
                    return;
                }


                // Дроп Inside условия
                var targetCondition2 = GetConditionFromElement(currentDropTarget.TargetElement);
                if (targetCondition2 != null)
                {
                    if (draggedCommand is PLCStartProgram && HasStartProgramInCondition(targetCondition2))
                    {
                        CleanupDrag();
                        return;
                    }
                    RemoveCommandFromAllLists(draggedCommand.Id);
                    targetCondition2.Content.Add(draggedCommand);
                    UpdateHierarchy();
                    CleanupDrag();
                    return;
                }

                CleanupDrag();
                return;
            }

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
                    insertAtIndex = GetSiblingIndexInParent(targetObject);
                    break;
                case DropPosition.Below:
                    newParentId = targetObject.ParentId;
                    insertAtIndex = GetSiblingIndexInParent(targetObject) + 1;
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

            if (IsProgramOrCommand(draggedObject) && string.IsNullOrEmpty(newParentId))
            {
                CleanupDrag();
                return;
            }

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

            var command = new ChangeParentCommand(draggedObject.Id, newParentId, insertAtIndex);
            _undoRedoManager.Execute(command);
            CleanupDrag();
        }
        private void RemoveCommandFromProgram(CommandObject command)
        {
            foreach (var robot in _sceneObjectManager.GetGameObjectsList().Where(r => r.Type == ObjectType.Robot))
            {
                var programs = _sceneObjectManager.Commands.GetSubPrograms(robot.Id);
                foreach (var program in programs)
                {
                    if (program.Items.Contains(command))
                    {
                        program.Items.Remove(command);
                        return;
                    }
                }
            }
        }
        private int FindConditionBlockIndex(List<PLCBase> items, string blockId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is PLCBlockCondition block && block.Id == blockId)
                    return i;
            }
            return -1;
        }
        private void CleanupDrag()
        {
            ClearDropIndicators();
            if (currentDragData?.SourceElement != null)
                currentDragData.SourceElement.RemoveFromClassList(DRAGGING_CLASS);
            if (dragPreviewElement != null)
                dragPreviewElement.style.display = DisplayStyle.None;
            currentDragData = null;
            isDragging = false;
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
                .OrderBy(o => GetSiblingIndex(o.Id, parentId))
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

        private int GetSiblingIndex(string objectId, string parentId)
        {
            var parent = !string.IsNullOrEmpty(parentId) ?
                ((SceneObject)_sceneObjectManager.Items[parentId])?.Reference : null;

            if (parent != null)
            {
                for (int i = 0; i < parent.transform.childCount; i++)
                {
                    var child = parent.transform.GetChild(i);
                    var childProvider = child.GetComponent<IPropertyProvider>();
                    if (childProvider != null && childProvider.Id == objectId)
                    {
                        return i;
                    }
                }
            }

            var rootObjects = _sceneObjectManager.GetGameObjectsList()
                .Where(o => string.IsNullOrEmpty(o.ParentId))
                .ToList();

            for (int i = 0; i < rootObjects.Count; i++)
            {
                if (rootObjects[i].Id == objectId)
                {
                    return i;
                }
            }

            return 0;
        }
        private List<PLCBase> FindParentListForCommandInConditions(List<PLCBase> items, string commandId)
        {
            foreach (var item in items)
            {
                if (item is PLCBlockCondition block)
                {
                    // Проверяем IF Content
                    foreach (var c in block.IfCondition.Content)
                    {
                        if (c is PLCCommand cmd && cmd.Id == commandId)
                            return block.IfCondition.Content;
                    }
                    var found = FindParentListForCommandInConditions(block.IfCondition.Content, commandId);
                    if (found != null) return found;

                    // Проверяем ELIF Content
                    foreach (var elif in block.ElifConditions)
                    {
                        foreach (var c in elif.Content)
                        {
                            if (c is PLCCommand cmd && cmd.Id == commandId)
                                return elif.Content;
                        }
                        found = FindParentListForCommandInConditions(elif.Content, commandId);
                        if (found != null) return found;
                    }

                    // Проверяем ELSE Content
                    if (block.ElseCondition != null)
                    {
                        foreach (var c in block.ElseCondition.Content)
                        {
                            if (c is PLCCommand cmd && cmd.Id == commandId)
                                return block.ElseCondition.Content;
                        }
                        found = FindParentListForCommandInConditions(block.ElseCondition.Content, commandId);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }
        private PLCCondition GetConditionFromElement(VisualElement element)
        {
            if (element.name == "plc-command")
            {
                var current = element.parent;
                while (current != null)
                {
                    if (current.name == "plc-if-block" || current.name == "plc-elif-block" || current.name == "plc-else-block")
                    {
                        var conditionId = current.userData?.ToString();
                        if (conditionId != null)
                            return GetConditionById(conditionId);
                    }
                    current = current.parent;
                }
                return null;
            }

            // Для foldout — старая логика
            var foldout = GetParentElement(element);
            if (foldout != null && (foldout.name == "plc-if-block" || foldout.name == "plc-elif-block" || foldout.name == "plc-else-block"))
            {
                var conditionId = foldout.userData?.ToString();
                if (conditionId != null)
                    return GetConditionById(conditionId);
            }
            return null;
        }

        private bool IsProgramOrCommand(SceneObject obj)
        {
            return obj.Type == ObjectType.Program
                || obj.Type == ObjectType.LinearMoveCommand
                || obj.Type == ObjectType.StateEndEffectorCommand
                || obj.Type == ObjectType.WaitCommand;
        }

        private bool HasStartProgramInCondition(PLCCondition condition)
        {
            foreach (var item in condition.Content)
            {
                if (item is PLCStartProgram)
                    return true;
            }
            return false;
        }

        private bool IsCommandInCondition(string commandId, PLCCondition condition)
        {
            foreach (var item in condition.Content)
            {
                if (item is PLCCommand cmd && cmd.Id == commandId)
                    return true;
            }
            return false;
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

        private bool IsChildOf(string potentialChildId, string potentialParentId)
        {
            if (string.IsNullOrEmpty(potentialParentId))
                return false;

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

        private bool CanDropInRoot(SceneObject draggedObject)
        {
            return IsRootOnlyType(draggedObject);
        }

        private bool IsRootOnlyType(SceneObject obj)
        {
            return obj.Type == ObjectType.Robot;
        }

        private string GetRobotIdFromCommand(string commandId)
        {
            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                if (FindCommandInListRecursive(rb.ConditionsList, commandId))
                    return rb.RobotId;
            }

            if (FindCommandInListRecursive(_sceneObjectManager.PLCData.LogicBlockItems, commandId))
                return "logic";

            return null;
        }
        private bool FindCommandInListRecursive(List<PLCBase> items, string commandId)
        {
            foreach (var item in items)
            {
                if (item is PLCCommand cmd && cmd.Id == commandId)
                    return true;

                if (item is PLCBlockCondition block)
                {
                    // Рекурсивно ищем в IF блоке
                    if (FindCommandInListRecursive(block.IfCondition.Content, commandId))
                        return true;
                    // Рекурсивно ищем в ELSE IF блоках
                    foreach (var elif in block.ElifConditions)
                    {
                        if (FindCommandInListRecursive(elif.Content, commandId))
                            return true;
                    }
                    // Рекурсивно ищем в ELSE блоке
                    if (block.ElseCondition != null && FindCommandInListRecursive(block.ElseCondition.Content, commandId))
                        return true;
                }
            }
            return false;
        }
        private string GetRobotIdFromCondition(PLCCondition condition)
        {
            // Ищем, какому блоку условий принадлежит это condition
            foreach (var rb in _sceneObjectManager.PLCData.RobotCommandsBlockItems)
            {
                if (FindConditionInBlockRecursive(rb.ConditionsList, condition.Id))
                    return rb.RobotId;
            }

            if (FindConditionInBlockRecursive(_sceneObjectManager.PLCData.LogicBlockItems, condition.Id))
                return "logic";

            return null;
        }

        private bool FindConditionInBlockRecursive(List<PLCBase> items, string conditionId)
        {
            foreach (var item in items)
            {
                if (item is PLCBlockCondition block)
                {
                    // Проверяем сам блок и его внутренние условия
                    if (block.IfCondition.Id == conditionId)
                        return true;
                    foreach (var elif in block.ElifConditions)
                    {
                        if (elif.Id == conditionId)
                            return true;
                    }
                    if (block.ElseCondition != null && block.ElseCondition.Id == conditionId)
                        return true;

                    // Рекурсивно ищем во вложенных Content
                    if (FindConditionInBlockRecursive(block.IfCondition.Content, conditionId))
                        return true;
                    foreach (var elif in block.ElifConditions)
                    {
                        if (FindConditionInBlockRecursive(elif.Content, conditionId))
                            return true;
                    }
                    if (block.ElseCondition != null && FindConditionInBlockRecursive(block.ElseCondition.Content, conditionId))
                        return true;
                }
            }
            return false;
        }
        private SceneObject GetSceneObjectFromElement(VisualElement element)
        {
            if (element?.userData == null)
                return null;

            string id = element.userData.ToString();

            if (string.IsNullOrEmpty(id))
                return null;

            return _sceneObjectManager.GetById(id);
        }

        private CommandObject ResolveCommand(VisualElement element)
        {
            if (element == null) return null;

            // 1. напрямую
            if (element.userData is string id)
            {
                foreach (var robot in _sceneObjectManager.GetGameObjectsList().Where(r => r.Type == ObjectType.Robot))
                {
                    var programs = _sceneObjectManager.Commands.GetSubPrograms(robot.Id);

                    foreach (var program in programs)
                    {
                        var cmd = program.Items.FirstOrDefault(c => c.Id == id);
                        if (cmd != null) return cmd;
                    }
                }
            }

            // 2. fallback по иерархии (если userData не заполнен)
            var sceneObj = GetSceneObjectFromElement(element);
            if (sceneObj is CommandObject cmdObj)
                return cmdObj;

            return null;
        }

        private RobotProgramObject ResolveProgram(VisualElement element)
        {
            if (element?.userData is string id)
            {
                foreach (var robot in _sceneObjectManager.GetGameObjectsList().Where(r => r.Type == ObjectType.Robot))
                {
                    var program = _sceneObjectManager.Commands.GetSubPrograms(robot.Id)
                        .FirstOrDefault(p => p.Id == id);

                    if (program != null)
                        return program;
                }
            }

            return null;
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
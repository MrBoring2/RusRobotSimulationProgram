using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.HierarhyPanel;
using Assets.Scripts.CustomEventBus.Signals.Lines;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using Assets.Scripts.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Главный менеджер для управления объектами на сцене.
    /// Отвечает за создание, удаление, перемещение и иерархию всех объектов и команд.
    /// </summary>
    public class SceneObjectsManager : MonoBehaviour, IService
    {
        private EventBus _eventBus;
        private NotificationSystemManager _notificationSystemManager;
        /// <summary>
        /// OrderedDictionary хранит все объекты сцены с доступом по ID.
        /// Сохраняет порядок объектов, важный для иерархии и сериализации.
        /// Ключ: string (ID объекта), Значение: SceneObject
        /// </summary>
        public OrderedDictionary Items { get; private set; } = new OrderedDictionary();
        /// <summary>
        /// Контейнер для хранения команд роботов (программы, подпрограммы, команды).
        /// </summary>
        public CommandsContainer Commands { get; private set; } = new CommandsContainer();
        /// <summary>
        /// Данные ПЛК (программируемого логического контроллера).
        /// Содержит информацию о блоках команд роботов.
        /// </summary>
        public PLCData PLCData { get; private set; } = new PLCData();
        /// <summary>
        /// Инициализация менеджера объектов сцены.
        /// Получает необходимые сервисы и инициализирует существующие объекты.
        /// </summary>
        public void Init()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _notificationSystemManager = ServiceManager.Current.Get<NotificationSystemManager>();
            InitExistedObjects();
        }
        /// <summary>
        /// Устанавливает данные ПЛК.
        /// </summary>
        /// <param name="data">Новые данные ПЛК</param>
        public void SetPLCData(PLCData data)
        {
            PLCData = data;
        }

        /// <summary>
        /// Создает новый объект на сцене.
        /// </summary>
        /// <param name="prefab">Префаб создаваемого объекта</param>
        /// <param name="position">Позиция в мировом пространстве</param>
        /// <param name="rotation">Вращение</param>
        /// <param name="type">Тип создаваемого объекта</param>
        /// <param name="id">Уникальный ID (если null, генерируется новый)</param>
        /// <param name="parentId">ID родительского объекта (может быть null)</param>
        /// <returns>Созданный SceneObject или null в случае ошибки</returns>
        public SceneObject Create(GameObject prefab, Vector3 position, Quaternion rotation, ObjectType type, string id = null, string parentId = null)
        {
            SceneObject sceneObj = null;
            if (prefab != null)
            {
                GameObject parent = null;
                if (!string.IsNullOrEmpty(parentId))
                {
                    parent = ((SceneObject)Items[parentId])?.Reference;
                    if (parent == null) return null;
                }
                var obj = Instantiate(prefab, position, rotation, parent?.transform);
                obj.name = prefab.name;
                var objectMaker = obj.GetComponent<SceneObjectMarker>();
                if (objectMaker != null)
                {
                    if (id == null)
                        id = Guid.NewGuid().ToString();

                    switch (type)
                    {
                        case ObjectType.Unknown:
                            sceneObj = new SceneObject(id, objectMaker.type, obj, parentId);
                            break;
                        case ObjectType.Node:
                        case ObjectType.Primitive:
                        case ObjectType.Static:
                            sceneObj = new StaticObject(id, objectMaker.type, obj, parentId);
                            break;
                        case ObjectType.Dynamic:
                        case ObjectType.Workpiece:
                            sceneObj = new DynamicObject(id, objectMaker.type, obj, parentId);
                            break;
                        case ObjectType.Robot:
                            sceneObj = new RobotObject(id, objectMaker.type, obj, parentId);
                            if (!Items.Contains(id))
                            {
                                PLCData.RobotCommandsBlockItems.Add(new PLCRobotBlock(id));
                            }
                            break;
                        case ObjectType.PLC:
                            sceneObj = new PLCObject(id, objectMaker.type, obj, parentId);
                            var allRobots = GetGameObjectsList()
                                .Where(obj => obj.Type == ObjectType.Robot)
                                .ToList();
                            foreach (var item in allRobots)
                            {
                                PLCData.RobotCommandsBlockItems.Add(new PLCRobotBlock(item.Id));
                            }
                            break;
                        default:
                            sceneObj = new SceneObject(id, objectMaker.type, obj, parentId);
                            break;
                    }


                    if (!Items.Contains(id))
                    {
                        Items[id] = sceneObj;
                        sceneObj.Reference.GetComponent<IPropertyProvider>().Id = id;
                        _eventBus.Invoke<AddSceneObjectSignal>(new AddSceneObjectSignal(sceneObj));
                        _eventBus.Invoke(new UpdateLineDrawer());
                    }
                }
            }
            return sceneObj;
        }

        /// <summary>
        /// Создает объект команду.
        /// Отдельный метод, так как команды имеют иную логику иерархии.
        /// </summary>
        /// <param name="prefab">Префаб команды</param>
        /// <param name="position">Позиция</param>
        /// <param name="rotation">Вращение</param>
        /// <param name="type">Тип команды</param>
        /// <param name="id">ID команды</param>
        /// <param name="parentId">ID родителя (программы или подпрограммы)</param>
        /// <returns>Созданный SceneObject или null</returns>
        public SceneObject CreateCommand(GameObject prefab, Vector3 position, Quaternion rotation, ObjectType type, string id = null, string parentId = null)
        {
            SceneObject sceneObj = null;
            if (prefab != null)
            {
                GameObject parent = null;
                if (!string.IsNullOrEmpty(parentId))
                {
                    if (type == ObjectType.Program)
                    {
                        parent = ((SceneObject)Items[parentId])?.Reference;
                    }
                    else parent = Commands.GetSubProgram(parentId)?.Reference;
                    if (parent == null) return null;
                }
                var obj = Instantiate(prefab, position, rotation, parent?.transform);
                obj.name = prefab.name;
                var objectMaker = obj.GetComponent<SceneObjectMarker>();
                if (objectMaker != null)
                {
                    if (id == null)
                        id = Guid.NewGuid().ToString();
                    switch (type)
                    {
                        case ObjectType.LinearMoveCommand:
                        case ObjectType.StateEndEffectorCommand:
                        case ObjectType.WaitCommand:
                            sceneObj = new CommandObject(id, objectMaker.type, obj, parentId);
                            break;
                        case ObjectType.Program:
                            sceneObj = new RobotProgramObject(id, objectMaker.type, obj, parentId);
                            break;
                        default:
                            sceneObj = new CommandObject(id, objectMaker.type, obj, parentId);
                            break;
                    }
                    sceneObj.Reference.GetComponent<IPropertyProvider>().Id = id;


                    if (sceneObj.Type == ObjectType.Program)
                    {
                        Commands.AddSubProgram(parentId, sceneObj as RobotProgramObject);
                    }
                    else if (sceneObj.Type == ObjectType.WaitCommand ||
                        sceneObj.Type == ObjectType.LinearMoveCommand ||
                        sceneObj.Type == ObjectType.StateEndEffectorCommand)
                    {
                        var robot = GetById(Commands.GetSubProgram(parentId).ParentId);
                        Commands.AddCommand(robot.Id, parentId, sceneObj as CommandObject);
                    }
                    _eventBus.Invoke<AddSceneObjectSignal>(new AddSceneObjectSignal(sceneObj));
                    _eventBus.Invoke(new UpdateLineDrawer());
                }
            }
            return sceneObj;
        }

        /// <summary>
        /// Удаляет объект со сцены по его ID.
        /// </summary>
        /// <param name="id">ID удаляемого объекта</param>
        public void Remove(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var sceneObject = ((SceneObject)Items[id]);
                if (Items.Contains(id))
                {
                    Items.Remove(id);
                    _eventBus.Invoke<RemoveSceneObjectSignal>(new RemoveSceneObjectSignal(sceneObject));
                    Destroy(sceneObject.Reference);
                    _eventBus.Invoke(new UpdateLineDrawer());
                }
            }
        }

        /// <summary>
        /// Получает объект по ID.
        /// </summary>
        /// <param name="id">ID объекта</param>
        /// <returns>SceneObject или null</returns>
        public SceneObject GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return Items.Contains(id) ? (SceneObject)Items[id] : null;
        }

        /// <summary>
        /// Получает все игровые объекты на сцене.
        /// </summary>
        /// <returns>Массив GameObject</returns>
        public GameObject[] GetGameObjectsList2()
        {
            var objects = GameObject.FindGameObjectsWithTag("SceneObject");
            System.Array.Sort(objects, (a, b) =>
                a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            return objects;
        }

        /// <summary>
        /// Получает список всех SceneObject.
        /// </summary>
        /// <param name="getOnlyActive">Если true, возвращает только активные объекты</param>
        /// <returns>Список SceneObject</returns>
        public List<SceneObject> GetGameObjectsList(bool getOnlyActive = true)
        {
            if (getOnlyActive)
            {
                var b = Items.Values.Cast<SceneObject>().ToList();
                var a = Items.Values.Cast<SceneObject>().Where(p => p.Reference.activeInHierarchy == getOnlyActive).ToList();
                return Items.Values.Cast<SceneObject>().Where(p => p.Reference.activeInHierarchy == getOnlyActive).ToList();
            }
            else
            {
                return Items.Values.Cast<SceneObject>().ToList();
            }
        }

        /// <summary>
        /// Полностью очищает сцену от всех объектов.
        /// </summary>
        /// <param name="spawnFloor">Создать ли пол после очистки</param>

        public void ClearScene(bool spawnFloor = true)
        {
            foreach (var gameObject in GetGameObjectsList(false))
            {
                Remove(gameObject.Id);
            }

            Commands = new CommandsContainer();
            PLCData = new PLCData();

            if (spawnFloor)
            {
                var prefab = Resources.Load<GameObject>("Prefabs/Primitive/Куб");
                var type = prefab.GetComponent<SceneObjectMarker>().type;
                var pos = Vector3.zero;
                var rot = Quaternion.identity;
                var obj = Create(prefab, pos, rot, type);
                obj.Reference.name = "Поверхность";
                obj.Reference.transform.localScale = new Vector3(25, 0.2f, 25);
            }
            _eventBus.Invoke(new ClearSceneSignal());
            _eventBus.Invoke(new LoadObjectsSignal(Items.Values.Cast<SceneObject>().ToList()));
        }

        /// <summary>
        /// Восстанавливает сохраненные объекты на сцене.
        /// Выполняется в 3 прохода для корректного восстановления иерархии.
        /// </summary>
        /// <param name="data">Список информации об объектах</param>
        /// <param name="commandsData">Данные о командах роботов</param>
        public void SpawnRestoredObjects(List<ObjectInfo> data, CommandsContainerData commandsData)
        {
            var createdObjects = new Dictionary<string, SceneObject>();

            foreach (var item in data)
            {
                var prefab = Resources.Load<GameObject>(item.SourcePath);
                if (prefab == null)
                {
                    Debug.LogError($"Prefab not found: {item.SourcePath}");
                    continue;
                }

                var instance = Create(prefab, item.Position.ToVector3(), item.Rotation.ToQuaternion(),
                                      item.ObjectType, item.Id, null);

                if (instance == null) continue;

                var provider = instance.Reference.GetComponent<IPropertyProvider>();
                provider?.RestoreCustomState(item.ProviderData);
                instance.Reference.name = item.Name;
                instance.Reference.tag = "SceneObject";
                instance.Reference.transform.localScale = item.Scale.ToVector3();

                var m = instance.Reference.GetComponent<SceneObjectMarker>();
                if (m == null)
                    m = instance.Reference.AddComponent<SceneObjectMarker>();
                m.type = item.ObjectType;
                m.sourcePath = item.SourcePath;

                createdObjects[item.Id] = instance;
            }

            foreach (var item in data)
            {
                if (!string.IsNullOrEmpty(item.ParentId) && createdObjects.TryGetValue(item.ParentId, out var parent))
                {
                    if (createdObjects.TryGetValue(item.Id, out var child))
                    {
                        child.Reference.transform.SetParent(parent.Reference.transform, false);
                        child.SetParent(item.ParentId);
                    }
                }
            }

            if (commandsData != null)
            {
                foreach (var robotData in commandsData.RobotsCommands)
                {
                    var robot = GetById(robotData.RobotId);
                    if (robot == null) continue;

                    foreach (var programData in robotData.Programs)
                    {
                        var programPrefab = Resources.Load<GameObject>("Prefabs/Program/Программа");
                        if (programPrefab == null) continue;

                        var program = CreateCommand(programPrefab, Vector3.zero, Quaternion.identity,
                                                    ObjectType.Program, programData.ProgramId, robot.Id) as RobotProgramObject;

                        if (program == null) continue;

                        foreach (var cmdSaveData in programData.Commands)
                        {
                            var cmdPrefab = Resources.Load<GameObject>(cmdSaveData.SourcePath);
                            if (cmdPrefab == null) continue;

                            var cmd = CreateCommand(cmdPrefab, cmdSaveData.Position.ToVector3(), cmdSaveData.Rotation.ToQuaternion(),
                                    cmdSaveData.CommandType, cmdSaveData.Id, program.Id) as CommandObject;

                            if (cmd != null)
                            {
                                var cmdProvider = cmd.Reference.GetComponent<IPropertyProvider>();
                                cmdProvider?.RestoreCustomState(cmdSaveData.ProviderData);
                                cmdProvider.Name = cmdSaveData.Name;
                            }
                        }
                    }
                }
            }
            _eventBus.Invoke(new LoadObjectsSignal(GetGameObjectsList()));
        }

        /// <summary>
        /// Инициализирует уже существующие на сцене объекты при старте.
        /// </summary>
        private void InitExistedObjects()
        {

            foreach (var obj in GetGameObjectsList2())
            {
                var objectMaker = obj.GetComponent<SceneObjectMarker>();
                if (objectMaker != null)
                {
                    var id = Guid.NewGuid().ToString();
                    var sceneObj = new SceneObject(id, objectMaker.type, obj);
                    if (!Items.Contains(id))
                    {
                        sceneObj.Reference.GetComponent<IPropertyProvider>().Id = id;
                        Items.Add(id, sceneObj);
                    }
                }
            }
            var prefab = Resources.Load<GameObject>("Prefabs/Primitive/Куб");
            var type = prefab.GetComponent<SceneObjectMarker>().type;
            var pos = Vector3.zero;
            var rot = Quaternion.identity;
            var objd = Create(prefab, pos, rot, type);
            objd.Reference.name = "Поверхность";
            objd.Reference.transform.localScale = new Vector3(25, 0.2f, 25);

            _eventBus.Invoke(new LoadObjectsSignal(Items.Values.Cast<SceneObject>().ToList()));
        }

        /// <summary>
        /// Изменяет порядок объекта в иерархии (родителя и индекс).
        /// </summary>
        /// <param name="objectId">ID перемещаемого объекта</param>
        /// <param name="newParentId">ID нового родителя</param>
        /// <param name="insertIndex">Индекс вставки в списке дочерних объектов</param>
        /// <returns>Успех операции</returns>
        public bool ChangeObjectOrder(string objectId, string newParentId, int? insertIndex)
        {
            if (!Items.Contains(objectId))
                return false;

            var sceneObject = (SceneObject)Items[objectId];
            var oldParentId = sceneObject.ParentId;

            if (oldParentId == newParentId &&
                GetSiblingIndex(objectId, oldParentId) == insertIndex)
                return true;

            int oldSiblingIndex = GetSiblingIndex(objectId, oldParentId);

            sceneObject.SetParent(newParentId);

            UpdateTransformParent(sceneObject, newParentId);

            bool success = MoveInOrderedDictionary(objectId, newParentId, insertIndex, oldParentId, oldSiblingIndex);

            if (success)
            {
                UpdateTransformSiblingIndex(sceneObject, newParentId, insertIndex);
            }

            _eventBus.Invoke(new UpdateHierarchySignal());
            _eventBus.Invoke(new UpdateLineDrawer());

            return success;
        }

        /// <summary>
        /// Обновляет Transform родителя для объекта.
        /// </summary>
        private void UpdateTransformParent(SceneObject sceneObject, string newParentId)
        {
            GameObject newParent = null;
            if (!string.IsNullOrEmpty(newParentId))
            {
                var newParentObj = (SceneObject)Items[newParentId];
                newParent = newParentObj?.Reference;
            }

            sceneObject.Reference.transform.SetParent(newParent?.transform, false);
        }

        /// <summary>
        /// Перемещает объект в OrderedDictionary (сохраняя порядок иерархии).
        /// </summary>
        private bool MoveInOrderedDictionary(string objectId, string newParentId, int? insertIndex, string oldParentId, int oldSiblingIndex)
        {
            var sceneObject = (SceneObject)Items[objectId];

            var oldIndex = OrderedDictionaryExtensions.IndexOf(Items, objectId);
            if (oldIndex == -1) return false;

            Items.RemoveAt(oldIndex);

            int newIndex = CalculateNewDictionaryIndex(objectId, newParentId, insertIndex, oldParentId, oldSiblingIndex);

            if (newIndex >= 0 && newIndex <= Items.Count)
            {
                Items.Insert(newIndex, objectId, sceneObject);
            }
            else
            {
                Items.Add(objectId, sceneObject);
            }

            return true;
        }

        /// <summary>
        /// Вычисляет новый индекс в словаре на основе родителя и позиции.
        /// </summary>
        private int CalculateNewDictionaryIndex(string objectId, string newParentId, int? insertIndex, string oldParentId, int oldSiblingIndex)
        {
            if (string.IsNullOrEmpty(newParentId))
            {
                if (insertIndex.HasValue)
                {
                    var rootObjects = GetDirectChildren(null);
                    if (insertIndex.Value >= 0 && insertIndex.Value < rootObjects.Count)
                    {
                        var targetRoot = rootObjects[insertIndex.Value];
                        return OrderedDictionaryExtensions.IndexOf(Items, targetRoot.Id);
                    }
                }
                return Items.Count;
            }

            int parentIndex = OrderedDictionaryExtensions.IndexOf(Items, newParentId);
            if (parentIndex == -1) return Items.Count;

            var children = GetDirectChildren(newParentId);

            if (oldParentId == newParentId && insertIndex.HasValue && insertIndex.Value > oldSiblingIndex)
            {
                insertIndex--;
            }

            if (insertIndex.HasValue && insertIndex.Value >= 0)
            {
                if (insertIndex.Value < children.Count)
                {
                    var targetChild = children[insertIndex.Value];
                    return OrderedDictionaryExtensions.IndexOf(Items, targetChild.Id);
                }
                else
                {
                    int lastChildIndex = parentIndex;
                    foreach (var child in children)
                    {
                        int childIndex = OrderedDictionaryExtensions.IndexOf(Items, child.Id);
                        if (childIndex > lastChildIndex)
                            lastChildIndex = childIndex;
                    }
                    return lastChildIndex + 1;
                }
            }
            else
            {
                int lastChildIndex = parentIndex;
                foreach (var child in children)
                {
                    int childIndex = OrderedDictionaryExtensions.IndexOf(Items, child.Id);
                    if (childIndex > lastChildIndex)
                        lastChildIndex = childIndex;
                }
                return lastChildIndex + 1;
            }
        }

        /// <summary>
        /// Обновляет индекс в Transform родителя (с задержкой в 1 кадр).
        /// </summary>
        private void UpdateTransformSiblingIndex(SceneObject sceneObject, string parentId, int? insertIndex)
        {
            if (string.IsNullOrEmpty(parentId)) return;

            var parentObj = ((SceneObject)Items[parentId])?.Reference;
            if (parentObj == null) return;

            StartCoroutine(SetSiblingIndexDelayed(sceneObject.Reference.transform, parentObj.transform, insertIndex ?? 0));
        }

        /// <summary>
        /// Корoutine для установки sibling index с задержкой.
        /// Необходима для корректной работы после изменений иерархии.
        /// </summary>
        private IEnumerator SetSiblingIndexDelayed(Transform child, Transform parent, int index)
        {
            yield return null;

            if (index >= parent.childCount)
            {
                index = parent.childCount - 1;
            }

            if (index >= 0 && index < parent.childCount)
            {
                child.SetSiblingIndex(index);
            }
        }

        /// <summary>
        /// Получает прямых дочерних объектов для указанного родителя.
        /// </summary>
        /// <param name="parentId">ID родителя (null для корневых объектов)</param>
        /// <returns>Список дочерних объектов, отсортированных по индексу</returns>
        public List<SceneObject> GetDirectChildren(string parentId)
        {
            var children = Items.Values
                .Cast<SceneObject>()
                .Where(o => o.ParentId == parentId)
                .ToList();

            children.Sort((a, b) =>
            {
                int indexA = GetSiblingIndex(a.Id, parentId);
                int indexB = GetSiblingIndex(b.Id, parentId);
                return indexA.CompareTo(indexB);
            });

            return children;
        }

        /// <summary>
        /// Получает индекс объекта в иерархии родителя.
        /// </summary>
        /// <param name="objectId">ID объекта</param>
        /// <param name="parentId">ID родителя</param>
        /// <returns>Индекс объекта</returns>
        public int GetSiblingIndex(string objectId, string parentId)
        {
            var parent = !string.IsNullOrEmpty(parentId) ?
                ((SceneObject)Items[parentId])?.Reference : null;

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
            var rootObjects = GetGameObjectsList()
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
    }
}
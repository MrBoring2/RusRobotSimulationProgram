using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.HierarhyPanel;
using Assets.Scripts.CustomEventBus.Signals.Lines;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using Assets.Scripts.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public class SceneObjectsManager : MonoBehaviour, IService
    {
        private EventBus _eventBus;
        public void Init()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            InitExistedObjects();
        }

        public OrderedDictionary Items { get; private set; } = new OrderedDictionary();

        public SceneObject Create(GameObject prefab, Vector3 position, ObjectType type, string id = null, string parentId = null)
        {
            SceneObject sceneObj = null;
            if (prefab != null)
            {
                var obj = Instantiate(prefab, position, Quaternion.identity);
                obj.name = prefab.name;
                var objectMaker = obj.GetComponent<SceneObjectMarker>();
                if (objectMaker != null)
                {
                    if (id == null)
                        id = Guid.NewGuid().ToString();
                    sceneObj = new SceneObject(id, objectMaker.type, obj, parentId);
                    if (!Items.Contains(id))
                    {

                        if (!string.IsNullOrEmpty(parentId))
                        {
                            var parentObj = ((SceneObject)Items[parentId])?.Reference;
                            if (parentObj == null) return null;
                            obj.transform.SetParent(parentObj.transform, false);
                        }
                        Items[id] = sceneObj;
                        sceneObj.Reference.GetComponent<IPropertyProvider>().Id = id;
                        _eventBus.Invoke<AddSceneObjectSignal>(new AddSceneObjectSignal(sceneObj));
                        _eventBus.Invoke(new UpdateLineDrawer());
                    }
                }
            }
            return sceneObj;
        }

        //public SceneObject CreateProgram(GameObject prefab, Vector3 position, string robotId, string parentProgramId = null)
        //{
        //    // Создаём объект
        //    var obj = Instantiate(prefab, position, Quaternion.identity);
        //    obj.name = prefab.name;

        //    var marker = obj.GetComponent<SceneObjectMarker>();
        //    if (marker == null) return null;

        //    var id = Guid.NewGuid().ToString();
        //    string parentId = null;
        //    if (parentProgramId != null)
        //        parentId = parentProgramId;
        //    else
        //        parentId = robotId;
        //    var sceneObj = new SceneObject(id, marker.type, obj, id);
        //    Items[id] = sceneObj;

        //    // Если есть родитель (подпрограмма), сохраняем связь в сцене


        //    _eventBus.Invoke(new AddSceneObjectSignal(sceneObj));
        //    return sceneObj;
        //}

        //public SceneObject CreateCommand(GameObject prefab, Vector3 position, string robotId, string parentProgramId = null)
        //{
        //    var obj = Instantiate(prefab, position, Quaternion.identity);
        //    obj.name = prefab.name;

        //    var marker = obj.GetComponent<SceneObjectMarker>();
        //    if (marker == null) return null;

        //    var id = Guid.NewGuid().ToString();
        //    var sceneObj = new SceneObject(id, marker.type, obj);
        //    Items[id] = sceneObj;

        //    sceneObj.ParentId = parentProgramId ?? robotId;

        //    _eventBus.Invoke(new AddSceneObjectSignal(sceneObj));
        //    return sceneObj;
        //}

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

        //public IReadOnlyDictionary<string, SceneObject> GetGameObjectsDictionary()
        //{
        //    return Items;
        //}

        public SceneObject GetById(string id)
        {
            // Добавьте проверку на null и пустую строку
            if (string.IsNullOrEmpty(id))
                return null;

            return Items.Contains(id) ? (SceneObject)Items[id] : null;
        }

        public GameObject[] GetGameObjectsList2()
        {
            var objects = GameObject.FindGameObjectsWithTag("SceneObject");
            System.Array.Sort(objects, (a, b) =>
                a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            return objects;
        }

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

        

        public void ClearScene()
        {
            foreach (var gameObject in GetGameObjectsList(false))
            {
                Remove(gameObject.Id);
            }
            _eventBus.Invoke(new ClearSceneSignal());
        }

        public void SpawnRestoredObjects(List<ObjectInfo> data)
        {
            foreach (var item in data)
            {
                var prefab = Resources.Load<GameObject>(item.SourcePath);
                var instance = Create(prefab, Vector3.zero, item.ObjectType, item.Id, item.ParentId);
                var provider = instance.Reference.GetComponent<IPropertyProvider>();// GetProvider(instance.Reference, item.ProviderData.ProviderType);
                if (provider is WaitPropertyProvider a)
                {

                }
                provider?.RestoreCustomState(item.ProviderData);
                if (provider is WaitPropertyProvider b)
                {

                }
                instance.Reference.name = item.Name;
                instance.Reference.tag = "SceneObject";
                instance.Reference.transform.position = item.Position.ToVector3();
                instance.Reference.transform.rotation = item.Rotation.ToQuaternion();
                instance.Reference.transform.localScale = item.Scale.ToVector3();
                var m = instance.Reference.AddComponent<SceneObjectMarker>();
                m.type = item.ObjectType;
                m.sourcePath = item.SourcePath;

                //var sceneObject = new SceneObject(item.Id, item.ObjectType, instance, item.ParentId);
            }

        }
        //private IPropertyProvider GetProvider(GameObject obj, string type)
        //{
        //    return type switch
        //    {
        //        nameof(PrimitivePropertyProvider) => obj.AddComponent<PrimitivePropertyProvider>(),
        //        nameof(RobotPropertyProvider) => obj.AddComponent<RobotPropertyProvider>(),
        //        nameof(LinearPointPropertyProvider) => obj.AddComponent<LinearPointPropertyProvider>(),
        //        nameof(StateEndEffectorPropertyProvider) => obj.AddComponent<StateEndEffectorPropertyProvider>(),
        //        nameof(RobotProgramPropertyProvider) => obj.AddComponent<RobotProgramPropertyProvider>(),
        //        nameof(WaitPropertyProvider) => obj.AddComponent<WaitPropertyProvider>(),
        //        _ => null
        //    };
        //}
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
                        //Items[id] = sceneObj; 
                    }
                }
            }
            _eventBus.Invoke(new LoadObjectsSignal(Items.Values.Cast<SceneObject>().ToList()));
        }
        public bool ChangeObjectOrder(string objectId, string newParentId, int? insertIndex)
        {
            if (!Items.Contains(objectId))
                return false;

            var sceneObject = (SceneObject)Items[objectId];
            var oldParentId = sceneObject.ParentId;

            // Если родитель не изменился и индекс тот же - ничего не делаем
            if (oldParentId == newParentId &&
                GetSiblingIndex(objectId, oldParentId) == insertIndex)
                return true;

            // Сохраняем старый индекс для коррекции
            int oldSiblingIndex = GetSiblingIndex(objectId, oldParentId);

            // 1. Обновляем ParentId объекта
            sceneObject.SetParent(newParentId);

            // 2. Обновляем Transform иерархию
            UpdateTransformParent(sceneObject, newParentId);

            // 3. Перемещаем в OrderedDictionary
            bool success = MoveInOrderedDictionary(objectId, newParentId, insertIndex, oldParentId, oldSiblingIndex);

            // 4. Обновляем порядок в Transform (SetSiblingIndex)
            if (success)
            {
                UpdateTransformSiblingIndex(sceneObject, newParentId, insertIndex);
            }

            // Отправляем сигнал об обновлении
            _eventBus.Invoke(new UpdateHierarchySignal());
            _eventBus.Invoke(new UpdateLineDrawer());

            return success;
        }
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

        private bool MoveInOrderedDictionary(string objectId, string newParentId, int? insertIndex, string oldParentId, int oldSiblingIndex)
        {
            var sceneObject = (SceneObject)Items[objectId];

            // Удаляем объект из текущей позиции
            var oldIndex = OrderedDictionaryExtensions.IndexOf(Items, objectId);
            if (oldIndex == -1) return false;

            Items.RemoveAt(oldIndex);

            // Определяем новую позицию для вставки
            int newIndex = CalculateNewDictionaryIndex(objectId, newParentId, insertIndex, oldParentId, oldSiblingIndex);

            // Вставляем на новую позицию
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

        private int CalculateNewDictionaryIndex(string objectId, string newParentId, int? insertIndex, string oldParentId, int oldSiblingIndex)
        {
            // Вариант 1: Перемещение в корень
            if (string.IsNullOrEmpty(newParentId))
            {
                if (insertIndex.HasValue)
                {
                    // Находим индекс среди корневых объектов
                    var rootObjects = GetDirectChildren(null);
                    if (insertIndex.Value >= 0 && insertIndex.Value < rootObjects.Count)
                    {
                        var targetRoot = rootObjects[insertIndex.Value];
                        return OrderedDictionaryExtensions.IndexOf(Items, targetRoot.Id);
                    }
                }
                // Если индекс не указан или вне диапазона - в конец
                return Items.Count;
            }

            // Вариант 2: Перемещение внутрь другого объекта
            // Находим родителя в словаре
            int parentIndex = OrderedDictionaryExtensions.IndexOf(Items, newParentId);
            if (parentIndex == -1) return Items.Count;

            // Получаем детей нового родителя (уже без перемещаемого объекта, если он был там же)
            var children = GetDirectChildren(newParentId);

            // Если перемещаем вниз по списку внутри того же родителя,
            // нужно учесть что мы временно удалили объект из списка детей
            if (oldParentId == newParentId && insertIndex.HasValue && insertIndex.Value > oldSiblingIndex)
            {
                insertIndex--;
            }

            if (insertIndex.HasValue && insertIndex.Value >= 0)
            {
                // Вставляем на конкретную позицию среди детей
                if (insertIndex.Value < children.Count)
                {
                    var targetChild = children[insertIndex.Value];
                    return OrderedDictionaryExtensions.IndexOf(Items, targetChild.Id);
                }
                else
                {
                    // Вставляем после всех детей этого родителя
                    // Находим последнего ребенка в OrderedDictionary
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
                // Вставляем в конец детей
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

        private void UpdateTransformSiblingIndex(SceneObject sceneObject, string parentId, int? insertIndex)
        {
            if (string.IsNullOrEmpty(parentId)) return;

            var parentObj = ((SceneObject)Items[parentId])?.Reference;
            if (parentObj == null) return;

            // Ждем один кадр для обновления Transform иерархии
            StartCoroutine(SetSiblingIndexDelayed(sceneObject.Reference.transform, parentObj.transform, insertIndex ?? 0));
        }

        private System.Collections.IEnumerator SetSiblingIndexDelayed(Transform child, Transform parent, int index)
        {
            yield return null; // Ждем обновления Transform

            // Корректируем индекс, если он больше количества детей
            if (index >= parent.childCount)
            {
                index = parent.childCount - 1;
            }

            if (index >= 0 && index < parent.childCount)
            {
                child.SetSiblingIndex(index);
            }
        }
        private void UpdateTransformOrder(SceneObject sceneObject, string parentId, int? insertIndex)
        {
            GameObject parentObj = null;
            if (!string.IsNullOrEmpty(parentId))
            {
                parentObj = ((SceneObject)Items[parentId])?.Reference;
            }

            if (parentObj != null)
            {
                sceneObject.Reference.transform.SetParent(parentObj.transform, false);
            }
            else
            {
                sceneObject.Reference.transform.SetParent(null, false);
            }

            // Устанавливаем порядок среди детей
            if (parentObj != null && insertIndex.HasValue)
            {
                int childCount = parentObj.transform.childCount;
                int targetIndex = Mathf.Clamp(insertIndex.Value, 0, childCount - 1);
                sceneObject.Reference.transform.SetSiblingIndex(targetIndex);
            }
        }

        public List<SceneObject> GetDirectChildren(string parentId)
        {
            var children = Items.Values
                .Cast<SceneObject>()
                .Where(o => o.ParentId == parentId)
                .ToList();

            // Сортируем по фактическому порядку в Transform иерархии
            children.Sort((a, b) =>
            {
                int indexA = GetSiblingIndex(a.Id, parentId);
                int indexB = GetSiblingIndex(b.Id, parentId);
                return indexA.CompareTo(indexB);
            });

            return children;
        }

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

            // Если родителя нет или объект не найден среди детей,
            // ищем среди корневых объектов
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

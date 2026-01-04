using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Lines;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
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
                                                                                                                                    
        public OrderedDictionary Items { get; private set; }  = new OrderedDictionary();

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
                            var parentObj = ((SceneObject)Items[parentId]).Reference;
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
            return ((SceneObject)Items[id]);
        }

        public GameObject[] GetGameObjectsList2()
        {
            var objects = GameObject.FindGameObjectsWithTag("SceneObject");
            System.Array.Sort(objects, (a, b) =>
                a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            return objects;
        }

        public List<SceneObject> GetGameObjectsList()
        {
            return Items.Values.Cast<SceneObject>().ToList();
        }
        public void ClearScene()
        {
            foreach (var gameObject in GetGameObjectsList())
            {
                Remove(gameObject.Id);
            }
        }

        public void SpawnRestoredObjects(List<ObjectInfo> data)
        {
            foreach (var item in data)
            {
                var prefab = Resources.Load<GameObject>(item.SourcePath);
                var instance = Create(prefab, Vector3.zero, item.ObjectType, item.Id, item.ParentId);
                var provider = GetProvider(instance.Reference, item.ProviderData.ProviderType);
                provider?.RestoreCustomState(item.ProviderData);
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
        private IPropertyProvider GetProvider(GameObject obj, string type)
        {
            return type switch
            {
                nameof(PrimitivePropertyProvider) => obj.AddComponent<PrimitivePropertyProvider>(),
                nameof(RobotPropertyProvider) => obj.AddComponent<RobotPropertyProvider>(),
                _ => null
            };
        }
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
    }
}

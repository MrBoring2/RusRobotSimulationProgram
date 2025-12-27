using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Lines;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using System;
using System.Collections.Generic;
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
                                                                                                                                    
        public Dictionary<string, SceneObject> Items { get; private set; }  = new Dictionary<string, SceneObject>();

        public SceneObject Create(GameObject prefab, Vector3 position, ObjectType type, string parentId = null)
        {
            SceneObject sceneObj = null;
            if (prefab != null)
            {
                var obj = Instantiate(prefab, position, Quaternion.identity);
                obj.name = prefab.name;
                var objectMaker = obj.GetComponent<SceneObjectMarker>();
                if (objectMaker != null)
                {
                    var id = Guid.NewGuid().ToString();
                    sceneObj = new SceneObject(id, objectMaker.type, obj, parentId);
                    if (!Items.ContainsKey(id))
                    {

                        if (!string.IsNullOrEmpty(parentId))
                        {
                            var parentObj = Items[parentId].Reference;
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
                var sceneObject = Items[id];
                if (Items.ContainsKey(id))
                {
                    Items.Remove(id);
                    _eventBus.Invoke<RemoveSceneObjectSignal>(new RemoveSceneObjectSignal(sceneObject));
                    Destroy(sceneObject.Reference);
                    _eventBus.Invoke(new UpdateLineDrawer());
                }
            }
        }

        public IReadOnlyDictionary<string, SceneObject> GetGameObjectsDictionary()
        {
            return Items;
        }

        public SceneObject GetById(string id)
        {
            return Items[id];
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
            return Items.Values.ToList();
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
                    if (!Items.ContainsKey(id))
                    {
                        sceneObj.Reference.GetComponent<IPropertyProvider>().Id = id;
                        Items[id] = sceneObj; 
                    }
                }
            }
            _eventBus.Invoke(new LoadObjectsSignal(Items.Values.ToList()));
        }
    }
}

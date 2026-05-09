using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using UnityEngine;

public class GameObjectManager2 : MonoBehaviour, IService
{
    private EventBus _eventBus;
    public event Action<GameObject> OnObjectAdded;
    public event Action<GameObject> OnObjectRemoved;

    public void Init()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>(); 
    }

    public void DeleteObject(GameObject obj)
    {
        if (obj == null) return;

        OnObjectRemoved?.Invoke(obj);
        Destroy(obj);

    }
    public GameObject[] GetGameObjectsList()
    {                                         
        var objects = GameObject.FindGameObjectsWithTag("SceneObject");
        return objects;
    }
    public GameObject GetObjectByUniqueID(int uniqueID)
    {
        GameObject[] allObjects = GetGameObjectsList();
        foreach (var obj in allObjects)
        {
            if (obj.GetInstanceID() == uniqueID)
            {
                return obj;
            }
        }
        return null;
    }
}

using System;
using System.Runtime.Serialization;
using System.Xml;
using UnityEngine;

public class GameObjectManager : MonoBehaviour
{
    public event Action<GameObject> OnObjectAdded;
    public event Action<GameObject> OnObjectRemoved;
    public GameObject CreateObject(GameObject prefab, Vector3 position)
    {
        var obj = Instantiate(prefab, position, Quaternion.identity);
        obj.name = prefab.name;
        OnObjectAdded?.Invoke(obj);
        return obj;
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

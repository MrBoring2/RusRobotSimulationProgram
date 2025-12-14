using System;
using System.Runtime.Serialization;
using System.Xml;
using UnityEngine;

public class GameObjectManager : MonoBehaviour
{
    public GameObject AddCube()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "New Cube";
        cube.tag = "SceneObject";
        cube.AddComponent<UniqueId>();
        cube.transform.position = new Vector3(0, 0, 0);
        cube.AddComponent<PrimitivePropertyProvider>();
        cube.transform.localScale = new Vector3(1, 1, 1);
        return cube;
    }
    public void DeleteObject(int objectId)
    {
        GameObject objToDelete = GetObjectByUniqueID(objectId);
        if (objToDelete != null)
        {
            Debug.Log("Объект удален: " + objToDelete.name);
            Destroy(objToDelete);

        }
        else
        {
            Debug.LogWarning("Объект с таким уникальным ID не найден.");
        }

    }
    public GameObject[] GetGameObjectsList()
    {
        var objects = GameObject.FindGameObjectsWithTag("SceneObject");
        Debug.Log(objects.Length);
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

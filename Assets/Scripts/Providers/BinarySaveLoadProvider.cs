using Assets.Scripts.Models;
using NUnit.Framework;
using System.Dynamic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class BinarySaveLoadProvider : ISaveLoadProvider
{
    
    public void Save(string path, System.Collections.Generic.List<GameObject> objects)
    {
        SceneData sceneData = new SceneData();
        Debug.Log("Колво " + objects.Count);
        // Проходим по всем объектам на сцене и собираем информацию
        foreach (var obj in objects)
        {
            
            var marker = obj.GetComponent<SceneObjectMarker>();
            ObjectInfo objectInfo = new ObjectInfo(obj.name, marker.sourcePath, marker.type, obj.transform.position, obj.transform.rotation, obj.transform.localScale);
            sceneData.objectsData.Add(objectInfo);
        }

        BinaryFormatter formatter = new BinaryFormatter();

        using (FileStream fs = new FileStream(path, FileMode.Create))
        {
            formatter.Serialize(fs, sceneData);
        }
    }
    public SceneData Load(string path)
    {
        if (!File.Exists(path)) return null;
        BinaryFormatter formatter = new BinaryFormatter();
        SceneData sceneData;
        using (var fs = new FileStream(path, FileMode.Open))
        {
            sceneData = (SceneData)formatter.Deserialize(fs);
        }
        return sceneData;
    }
}

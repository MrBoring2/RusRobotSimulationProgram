using Assets.Scripts.Models;
using NUnit.Framework;
using System.Dynamic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class BinarySaveLoadProvider : ISaveLoadProvider
{
    
    public void Save(string path, System.Collections.Generic.List<SceneObject> objects)
    {
        SceneData sceneData = new SceneData();
        foreach (var obj in objects)
        {
            var marker = obj.Reference.GetComponent<SceneObjectMarker>();
            var provider = obj.Reference.GetComponent<IPropertyProvider>();
            ObjectInfo objectInfo = new ObjectInfo(obj.Id, provider.Name, 
                                                    marker.sourcePath,
                                                    marker.type, 
                                                    provider.Position,
                                                    Quaternion.Euler(provider.Rotation), 
                                                    provider.Scale, 
                                                    obj.ParentId,
                                                    provider?.CaptureCustomState());
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

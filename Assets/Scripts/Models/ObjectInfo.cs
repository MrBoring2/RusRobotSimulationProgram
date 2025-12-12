using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum ObjectType
{
    Unknown,
    Primitive,
    Static,
    Dynamic,
    Robot
}
[System.Serializable]
public class ObjectInfo
{
    public string Name { get; set; }
    public string SourcePath { get;set; }
    public ObjectType ObjectType { get; set; }
    public SerializableTransform Position { get; set; }
    public SerializableQuaternion Rotation { get; set; }
    public SerializableTransform Scale { get; set; }

    public ObjectInfo(string name, string sourcePath, ObjectType objectType, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Name = name;
        SourcePath = sourcePath;
        ObjectType = objectType;
        Position = new SerializableTransform(position);
        Rotation = new SerializableQuaternion(rotation);
        Scale = new SerializableTransform(scale);
    }
}

[System.Serializable]
public class SceneData
{
    public List<ObjectInfo> objectsData = new List<ObjectInfo>();
}

[System.Serializable]
public class SerializableTransform
{
    public float x;
    public float y;
    public float z;

    public SerializableTransform(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[System.Serializable]
public class SerializableQuaternion
{
    public float x, y, z, w;

    public SerializableQuaternion(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}
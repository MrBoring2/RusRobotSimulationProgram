using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum ObjectType
{
    Unknown,
    LinearMoveCommand,
    StateEndEffectorCommand,
    Primitive,
    Static,
    Dynamic,
    Program,
    Robot,
    WaitCommand,
    Node,
    Workpiece,
    PLC
}

[System.Serializable]
public class ColorObj
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }

    public ColorObj(float r, float g, float b, float a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}

[System.Serializable]
public class ObjectInfo
{                                                 
    public string Id { get; set; }
    public string Name { get; set; }
    public string SourcePath { get; set; }
    public ObjectType ObjectType { get; set; }
    public SerializableTransform Position { get; set; }
    public SerializableQuaternion Rotation { get; set; }
    public SerializableTransform Scale { get; set; }                                                                                                  
    public string ParentId { get; set; }

    public ProviderSaveData ProviderData;

    public ObjectInfo(string id, string name, string sourcePath, ObjectType objectType, Vector3 position, Quaternion rotation, Vector3 scale, string parentId, ProviderSaveData data)
    {
        Id = id;
        Name = name;
        SourcePath = sourcePath;
        ObjectType = objectType;
        Position = new SerializableTransform(position);
        Rotation = new SerializableQuaternion(rotation);
        Scale = new SerializableTransform(scale);
        ParentId = parentId;
        ProviderData = data;
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
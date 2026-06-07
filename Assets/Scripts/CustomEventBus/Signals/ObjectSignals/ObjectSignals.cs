using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.CustomEventBus.Signals.ObjectSignals
{
    public class AddSceneObjectSignal
    {
        public readonly SceneObject GameObject;
        public AddSceneObjectSignal(SceneObject gameObject)
        {
            GameObject = gameObject;
        }
    }
    
    public class RemoveSceneObjectSignal
    {
        public readonly SceneObject GameObject;
        public RemoveSceneObjectSignal(SceneObject gameObject)
        {
            GameObject = gameObject;
        }
    }
    public class ChangeObjectNameSignal
    {
    }
    public class ChangeObjectTransfromSignal
    {
    }

    public class ClearSceneSignal { }
    public class SelectObjectInScene
    {
        public readonly string Id;
        public SelectObjectInScene(string id)
        {
            Id = id;
        }
    }
    public class SelectObjectinLibrary
    {
        public readonly string ParentId;
        public readonly GameObject Prefab;
        public SelectObjectinLibrary(GameObject prefab, string parentId)
        {
            Prefab = prefab;
            ParentId = parentId;
        }
    }
    public class LoadObjectsSignal
    {
        public readonly List<SceneObject> SceneObjects;
        public LoadObjectsSignal(List<SceneObject> sceneObjects)
        {
            SceneObjects = sceneObjects;
        }
    }
    public class SelectObjectInHierarchy
    {
        public readonly bool IsSelected;
        public SelectObjectInHierarchy(bool isSelected)
        {
            IsSelected = isSelected;
        }
    }
    public class SelectObjectInHierarchyCommands
    {
        public readonly bool IsSelected;
        public SelectObjectInHierarchyCommands(bool isSelected)
        {
            IsSelected = isSelected;
        }
    }
}

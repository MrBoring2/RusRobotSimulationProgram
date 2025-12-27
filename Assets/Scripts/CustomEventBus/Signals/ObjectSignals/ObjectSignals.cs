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
        public readonly GameObject Prefab;
        public SelectObjectinLibrary(GameObject prefab)
        {
            Prefab = prefab;
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
}

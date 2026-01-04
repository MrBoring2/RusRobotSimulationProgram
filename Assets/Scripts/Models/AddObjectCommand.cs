using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public class AddObjectCommand : ICommand, IDestructiveCommand
    {
        private SceneObjectsManager _sceneObjectManager;
        private GameObject prefab;
        private SceneObject instance;
        private Vector3 position;
        private string parentId;
        private ObjectType type;
        public SceneObject Instance => instance;

        public AddObjectCommand(GameObject prefab, ObjectType type, Vector3 position, string parentId = null)
        {
            _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
            this.type = type;   
            this.prefab = prefab;
            this.position = position;
            this.parentId = parentId;
        }

        public void Execute()
        {
            if (instance == null)
                instance = _sceneObjectManager.Create(prefab, position, type, parentId: parentId);
            else
                instance.Reference.SetActive(true);
        }

        public void Undo()
        {
            if (instance != null)
                instance.Reference.SetActive(false);
        }

        public void FinalizeDestroy()
        {
            if (instance != null)
                _sceneObjectManager.Remove(instance.Id);
        }
    }
}

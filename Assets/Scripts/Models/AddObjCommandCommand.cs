using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Models
{
	public class AddObjCommandCommand : ICommand, IDestructiveCommand
    {

        private SceneObjectsManager _sceneObjectManager;
        private GameObject prefab;
        private SceneObject instance;
        private Vector3 position;
        private Quaternion rotation;
        private string parentId;
        private ObjectType type;
        public SceneObject Instance => instance;

        public AddObjCommandCommand(GameObject prefab, ObjectType type, Vector3 position, Quaternion rotation, string parentId = null)
        {
            _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
            this.type = type;
            this.prefab = prefab;
            this.position = position;
            this.rotation = rotation;
            this.parentId = parentId;
        }

        public void Execute()
        {
            if (instance == null)
                instance = _sceneObjectManager.CreateCommand(prefab, position, rotation, type, parentId: parentId);
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
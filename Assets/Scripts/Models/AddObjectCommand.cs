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
        private GameObjectManager _gameObjectManager;
        private GameObject prefab;
        private GameObject instance;
        private Vector3 position;
        public GameObject Instance => instance;

        public AddObjectCommand(GameObjectManager gameObjectManager, GameObject prefab, Vector3 position)
        {
            _gameObjectManager = gameObjectManager;
            this.prefab = prefab;
            this.position = position;
        }

        public void Execute()
        {
            if (instance == null)
                instance = _gameObjectManager.CreateObject(prefab, position);
            else
                instance.SetActive(true);
        }

        public void Undo()
        {
            if (instance != null)
                instance.SetActive(false);
        }

        public void FinalizeDestroy()
        {
            if (instance != null)
                _gameObjectManager.DeleteObject(instance);
        }
    }
}

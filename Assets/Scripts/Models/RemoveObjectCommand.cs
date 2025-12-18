using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public class RemoveObjectCommand : ICommand, IDestructiveCommand
    {
        private GameObjectManager _gameObjectManager;
        private GameObject instance;
        public GameObject Instance => instance;

        public RemoveObjectCommand(GameObjectManager gameObjectManager, GameObject instance)
        {
            _gameObjectManager = gameObjectManager;
            this.instance = instance;
        }

        public void Execute()
        {
            if (instance == null)
                throw new Exception("Обхекта не существует");
            else
                instance.SetActive(false);
        }

        public void Undo()
        {
            if (instance != null)
                instance.SetActive(true);
        }

        public void FinalizeDestroy()
        {
            if (instance != null)
                _gameObjectManager.DeleteObject(instance);
        }
    }
}

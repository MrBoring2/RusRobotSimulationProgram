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
    public class RemoveObjectCommand : ICommand, IDestructiveCommand
    {
        private SceneObjectsManager _sceneObjectManager;
        private SceneObject instance;
        public SceneObject Instance => instance;

        public RemoveObjectCommand(SceneObject instance)
        {
            _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
            this.instance = instance;
        }

        public void Execute()
        {
            if (instance == null)
                throw new Exception("Обхекта не существует");
            else
                instance.Reference.SetActive(false);
        }

        public void Undo()
        {
            if (instance != null)
                instance.Reference.SetActive(true);
        }

        public void FinalizeDestroy()
        {
            if (instance != null)
                _sceneObjectManager.Remove(instance.Id);
        }
    }
}

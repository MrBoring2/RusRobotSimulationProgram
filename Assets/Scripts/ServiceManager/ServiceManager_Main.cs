using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using Assets.Scripts.CustomEventBus;
using Assets.Scripts.Managers;

namespace Assets.Scripts.CustomServiceManager
{
    public class ServiceManager_Main : MonoBehaviour
    {
        [SerializeField] private SceneObjectsManager _sceneObjectManager;
        [SerializeField] private LineManager _lineManager;
        private CustomEventBus.EventBus _eventBus;

        private void Awake()
        {
            _eventBus = new CustomEventBus.EventBus();

            RegisterServices();
            Init();
        }

        private void RegisterServices()
        {
            ServiceManager.Initialize();
            ServiceManager.Current.Register(_eventBus);
            ServiceManager.Current.Register(_sceneObjectManager);
            ServiceManager.Current.Register(_lineManager);
        }

        private void Init()
        {
            _sceneObjectManager.Init();
        }

    }
}

using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public class SaveLoadManager: MonoBehaviour, IService
    {
        private string savePath = "";
        public ISaveLoadProvider saveLoadProvider;
        private EventBus _eventBus;
        private UndoRedoManager _undoRedoManager;
        private SceneObjectsManager _sceneObjectManager;
        public void Init()
        {
            saveLoadProvider = new BinarySaveLoadProvider();
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
            _undoRedoManager = ServiceManager.Current.Get<UndoRedoManager>();
        }
        public void SetSavePath(string path)
        {
            savePath = path;
        }
        public void LoadScene()
        {
            var extentionsList = new[]
            {
                new ExtensionFilter("Файл RusRobot", "rusbot")
            };
            StandaloneFileBrowser.OpenFilePanelAsync("Выберите файл", "", extentionsList, false, OnSceneFileSelected);
        }
        public void SaveScene()
        {
            if (!string.IsNullOrEmpty(savePath))
            {
                if (File.Exists(savePath))
                    saveLoadProvider.Save(savePath,
                              _sceneObjectManager.GetGameObjectsList(),
                              _sceneObjectManager.Commands,
                              _sceneObjectManager.PLCData);
                return;
            }
            var extentionsList = new[]
            {
            new ExtensionFilter("Файл RusRobot", "rusbot")
        };
            StandaloneFileBrowser.SaveFilePanelAsync("Выберите место для сохранения", "", "", extentionsList, (string path) =>
            {
                if (string.IsNullOrEmpty(path))
                    return;
                savePath = path;
                saveLoadProvider.Save(savePath,
                              _sceneObjectManager.GetGameObjectsList(),
                              _sceneObjectManager.Commands,
                              _sceneObjectManager.PLCData);
            });
        }

        private void OnSceneFileSelected(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return;

            _undoRedoManager.BeginExternalOperation();

            try
            {
                ClearScene(false);

                var loaded = saveLoadProvider.Load(paths[0]);
                if (loaded == null)
                {
                    Debug.LogError("Ошибка загрузки сцены");
                    return;
                }
                savePath = paths[0];
                if (loaded.PLCData != null)
                {
                    _sceneObjectManager.SetPLCData(loaded.PLCData);
                }
                // hierarchyPanelEvents.LoadHierarchy();
                _sceneObjectManager.SpawnRestoredObjects(loaded.objectsData, loaded.CommandsData);
                //foreach (var data in loaded.objectsData)
                //{
                //    SpawnRestoredObject(data);
                //}
                
                _eventBus.Invoke(new LoadObjectsSignal(_sceneObjectManager.GetGameObjectsList()));
            }
            finally
            {
                _undoRedoManager.EndExternalOperation();
            }
        }

        public void ClearScene(bool spawnFloor = true)
        {
            //var itemsToDelete = new List<GameObject>(hierarchyPanelEvents.Items.Select(item => item.Reference));
            _sceneObjectManager.ClearScene(spawnFloor);
            //foreach (var gameObject in _sceneObjectManager.GetGameObjectsList())
            //{
            //    _sceneObjectManager.Remove(gameObject.Id);
            //}
        }
    }
}

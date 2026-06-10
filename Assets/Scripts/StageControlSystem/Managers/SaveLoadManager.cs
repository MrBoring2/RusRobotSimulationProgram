using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomServiceManager;
using SFB;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Менеджер сохранения и загрузки сцены.
    /// Отвечает за сохранение состояния сцены в файл и восстановление из файла.
    /// Использует провайдер сохранения (например, BinarySaveLoadProvider) для фактической сериализации.
    /// Реализует интерфейс IService для интеграции с ServiceManager.
    /// </summary>
    public class SaveLoadManager: MonoBehaviour, IService
    {
        private string savePath = "";
        public ISaveLoadProvider saveLoadProvider;
        private EventBus _eventBus;
        private UndoRedoManager _undoRedoManager;
        private SceneObjectsManager _sceneObjectManager;
        /// <summary>
        /// Инициализация менеджера сохранения/загрузки.
        /// Создает провайдер сохранения и получает необходимые сервисы.
        /// </summary>
        public void Init()
        {
            saveLoadProvider = new BinarySaveLoadProvider();
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
            _undoRedoManager = ServiceManager.Current.Get<UndoRedoManager>();
        }
        /// <summary>
        /// Устанавливает путь для сохранения (если путь уже известен).
        /// </summary>
        /// <param name="path">Путь к файлу сохранения</param>
        public void SetSavePath(string path)
        {
            savePath = path;
        }

        /// <summary>
        /// Загружает сцену из файла.
        /// Открывает диалог выбора файла и загружает выбранную сцену.
        /// </summary>
        public void LoadScene()
        {
            var extentionsList = new[]
            {
                new ExtensionFilter("Файл RusRobot", "rusbot")
            };
            StandaloneFileBrowser.OpenFilePanelAsync("Выберите файл", "", extentionsList, false, OnSceneFileSelected);
        }

        /// <summary>
        /// Сохраняет текущую сцену в файл.
        /// Если путь сохранения уже установлен - сохраняет без диалога.
        /// Иначе открывает диалог выбора места сохранения.
        /// </summary>
        public void SaveScene()
        {
            if (!string.IsNullOrEmpty(savePath))
            {
                if (File.Exists(savePath))
                    saveLoadProvider.Save(savePath,
                              _sceneObjectManager.GetGameObjectsList(),
                              _sceneObjectManager.Commands,
                              _sceneObjectManager.PLCData);
                _undoRedoManager.ClearHistory();
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
                _undoRedoManager.ClearHistory();
            });
        }
        /// <summary>
        /// Обработчик выбора файла в диалоге загрузки.
        /// Выполняет загрузку и восстановление сцены из выбранного файла.
        /// </summary>
        /// <param name="paths">Массив выбранных путей (обычно один файл)</param>
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
                _sceneObjectManager.SpawnRestoredObjects(loaded.objectsData, loaded.CommandsData);
                if (loaded.PLCData != null)
                {
                    _sceneObjectManager.SetPLCData(loaded.PLCData);
                }
               
                
                _eventBus.Invoke(new LoadObjectsSignal(_sceneObjectManager.GetGameObjectsList()));
            }
            finally
            {
                _undoRedoManager.EndExternalOperation();
            }
        }
        /// <summary>
        /// Очищает сцену.
        /// </summary>
        /// <param name="spawnFloor">Создать ли пол после очистки</param>
        public void ClearScene(bool spawnFloor = true)
        {
            _sceneObjectManager.ClearScene(spawnFloor);
        }
    }
}
